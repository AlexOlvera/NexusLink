using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Utilities
{
    /// <summary>
    /// Clase de utilidad para ejecutar operaciones con reintentos
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Ejecuta una acción con reintentos
        /// </summary>
        public static void Execute(Action action, int retryCount, TimeSpan delay,
            Func<Exception, bool> retryPredicate = null)
        {
            Guard.NotNull(action, nameof(action));
            Guard.GreaterThanOrEqual(retryCount, nameof(retryCount), 0);

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (retry >= retryCount)
                        break;

                    if (retryPredicate != null && !retryPredicate(ex))
                        throw;

                    Thread.Sleep(delay);
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Ejecuta una función con reintentos
        /// </summary>
        public static T Execute<T>(Func<T> func, int retryCount, TimeSpan delay,
             Func<Exception, bool> retryPredicate = null)
        {
            Guard.NotNull(func, nameof(func));
            Guard.GreaterThanOrEqual(retryCount, nameof(retryCount), 0);

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (retry >= retryCount)
                        break;

                    if (retryPredicate != null && !retryPredicate(ex))
                        throw;

                    Thread.Sleep(delay);
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Ejecuta una acción asíncrona con reintentos
        /// </summary>
        public static async Task ExecuteAsync(Func<Task> action, int retryCount, TimeSpan delay,
            Func<Exception, bool> retryPredicate = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(action, nameof(action));
            Guard.GreaterThanOrEqual(retryCount, nameof(retryCount), 0);

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (retry >= retryCount)
                        break;

                    if (retryPredicate != null && !retryPredicate(ex))
                        throw;

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Ejecuta una función asíncrona con reintentos
        /// </summary>
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> func, int retryCount, TimeSpan delay,
            Func<Exception, bool> retryPredicate = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(func, nameof(func));
            Guard.GreaterThanOrEqual(retryCount, nameof(retryCount), 0);

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (retry >= retryCount)
                        break;

                    if (retryPredicate != null && !retryPredicate(ex))
                        throw;

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Determina si una excepción es transitoria
        /// </summary>
        public static bool IsTransientException(Exception ex)
        {
            if (ex is TimeoutException)
                return true;

            if (ex is System.Data.SqlClient.SqlException sqlEx)
            {
                // Códigos de error transitorios en SQL Server
                int[] transientErrorNumbers = new[]
                {
                    -2, 4060, 40197, 40501, 40613, 49918, 49919, 49920, 4221,
                    18456, 64, 233, 10053, 10054, 10060, 40143, 40613, 17
                };

                return Array.IndexOf(transientErrorNumbers, sqlEx.Number) >= 0;
            }

            if (ex is System.Net.WebException webEx)
            {
                return webEx.Status == System.Net.WebExceptionStatus.Timeout ||
                       webEx.Status == System.Net.WebExceptionStatus.ConnectionClosed ||
                       webEx.Status == System.Net.WebExceptionStatus.ConnectFailure;
            }

            if (ex is System.Net.Sockets.SocketException socketEx)
            {
                return socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut ||
                       socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset ||
                       socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted;
            }

            if (ex is System.IO.IOException ioEx)
            {
                return ioEx.Message.Contains("network") ||
                       ioEx.Message.Contains("connection") ||
                       ioEx.Message.Contains("timeout");
            }

            return false;
        }
    }
}