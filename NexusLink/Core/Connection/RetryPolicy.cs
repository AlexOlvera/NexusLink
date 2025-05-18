using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Define una política de reintentos para operaciones de base de datos
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetryCount;
        private readonly TimeSpan _delay;
        private readonly TimeSpan _maxDelay;
        private readonly bool _exponentialBackoff;
        private readonly Func<Exception, bool> _retryPredicate;

        /// <summary>
        /// Inicializa una nueva instancia de RetryPolicy
        /// </summary>
        /// <param name="maxRetryCount">Número máximo de reintentos</param>
        /// <param name="delayMilliseconds">Retardo entre reintentos en milisegundos</param>
        /// <param name="maxDelayMilliseconds">Retardo máximo en milisegundos</param>
        /// <param name="exponentialBackoff">Indica si se debe usar retardo exponencial</param>
        /// <param name="retryPredicate">Predicado para determinar si se debe reintentar en caso de error</param>
        public RetryPolicy(
            int maxRetryCount = 3,
            int delayMilliseconds = 500,
            int maxDelayMilliseconds = 5000,
            bool exponentialBackoff = true,
            Func<Exception, bool> retryPredicate = null)
        {
            if (maxRetryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "El número máximo de reintentos no puede ser negativo");

            if (delayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "El retardo no puede ser negativo");

            if (maxDelayMilliseconds < delayMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(maxDelayMilliseconds), "El retardo máximo debe ser mayor o igual al retardo inicial");

            _maxRetryCount = maxRetryCount;
            _delay = TimeSpan.FromMilliseconds(delayMilliseconds);
            _maxDelay = TimeSpan.FromMilliseconds(maxDelayMilliseconds);
            _exponentialBackoff = exponentialBackoff;
            _retryPredicate = retryPredicate ?? IsTransientError;
        }

        /// <summary>
        /// Ejecuta una operación con reintentos
        /// </summary>
        /// <typeparam name="T">Tipo de resultado</typeparam>
        /// <param name="operation">Operación a ejecutar</param>
        /// <returns>Resultado de la operación</returns>
        public T Execute<T>(Func<T> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var exceptions = new List<Exception>();

            for (int retryCount = 0; retryCount <= _maxRetryCount; retryCount++)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    // Verificar si debemos reintentar
                    if (retryCount == _maxRetryCount || !_retryPredicate(ex))
                    {
                        if (exceptions.Count > 0)
                        {
                            throw new AggregateException($"Se produjo un error después de {retryCount} reintentos", exceptions);
                        }

                        throw;
                    }

                    exceptions.Add(ex);

                    // Calcular retardo
                    TimeSpan delay = CalculateDelay(retryCount);

                    // Esperar
                    Thread.Sleep(delay);
                }
            }

            // Nunca debería llegar aquí, pero por si acaso
            throw new InvalidOperationException("Se alcanzó el número máximo de reintentos");
        }

        /// <summary>
        /// Ejecuta una operación con reintentos de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de resultado</typeparam>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var exceptions = new List<Exception>();

            for (int retryCount = 0; retryCount <= _maxRetryCount; retryCount++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Verificar si debemos reintentar
                    if (retryCount == _maxRetryCount || !_retryPredicate(ex))
                    {
                        if (exceptions.Count > 0)
                        {
                            throw new AggregateException($"Se produjo un error después de {retryCount} reintentos", exceptions);
                        }

                        throw;
                    }

                    exceptions.Add(ex);

                    // Calcular retardo
                    TimeSpan delay = CalculateDelay(retryCount);

                    // Esperar
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // Nunca debería llegar aquí, pero por si acaso
            throw new InvalidOperationException("Se alcanzó el número máximo de reintentos");
        }

        /// <summary>
        /// Ejecuta una operación sin retorno con reintentos
        /// </summary>
        /// <param name="operation">Operación a ejecutar</param>
        public void Execute(Action operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            Execute<object>(() =>
            {
                operation();
                return null;
            });
        }

        /// <summary>
        /// Ejecuta una operación sin retorno con reintentos de forma asíncrona
        /// </summary>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            return ExecuteAsync<object>(async () =>
            {
                await operation().ConfigureAwait(false);
                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Calcula el retardo para un reintento
        /// </summary>
        /// <param name="retryCount">Número de reintento</param>
        /// <returns>Retardo a aplicar</returns>
        private TimeSpan CalculateDelay(int retryCount)
        {
            TimeSpan delay = _delay;

            if (_exponentialBackoff && retryCount > 0)
            {
                // Fórmula: delay * (2^retryCount)
                long delayMilliseconds = (long)(_delay.TotalMilliseconds * Math.Pow(2, retryCount));
                delay = TimeSpan.FromMilliseconds(Math.Min(delayMilliseconds, _maxDelay.TotalMilliseconds));
            }

            return delay;
        }

        /// <summary>
        /// Determina si un error es transitorio
        /// </summary>
        /// <param name="exception">Excepción a verificar</param>
        /// <returns>True si es un error transitorio, false en caso contrario</returns>
        public static bool IsTransientError(Exception exception)
        {
            if (exception == null)
                return false;

            // Comprobar si es una SqlException y verificar el número de error
            if (exception is System.Data.SqlClient.SqlException sqlEx)
            {
                // Códigos de error transitorios comunes de SQL Server
                int[] transientErrorNumbers = new int[]
                {
                    // Timeout de conexión
                    -2, 
                    // Timeout de la consulta
                    -1,
                    // Error de conexión
                    4060,
                    // Transporte cerrado
                    11001,
                    // Conexión rota
                    10054,
                    // Timeout de conexión
                    10060,
                    // Timeout de transacción
                    1205,
                    // Deadlock víctima
                    1222,
                    // Bloqueo
                    1204
                };

                foreach (System.Data.SqlClient.SqlError error in sqlEx.Errors)
                {
                    foreach (int transientError in transientErrorNumbers)
                    {
                        if (error.Number == transientError)
                            return true;
                    }
                }
            }

            // TimeoutException es siempre transitorio
            if (exception is TimeoutException)
                return true;

            // TransientException específico de SQL
            if (exception.GetType().Name == "SqlTransientException")
                return true;

            return false;
        }
    }
}