using System;
using System.Collections.Generic;

namespace NexusLink.Logging
{
    /// <summary>
    /// Implementación de ILogger que escribe en la consola
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly Dictionary<string, object> _scopes;

        /// <summary>
        /// Inicializa una nueva instancia de ConsoleLogger
        /// </summary>
        /// <param name="name">Nombre del logger</param>
        public ConsoleLogger(string name)
        {
            _name = name;
            _scopes = new Dictionary<string, object>();
        }

        /// <summary>
        /// Registra un mensaje de depuración
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Debug(string message, params object[] args)
        {
            Log(ConsoleColor.Gray, "DEBUG", message, args);
        }

        /// <summary>
        /// Registra un mensaje de información
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Info(string message, params object[] args)
        {
            Log(ConsoleColor.White, "INFO", message, args);
        }

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Warning(string message, params object[] args)
        {
            Log(ConsoleColor.Yellow, "WARN", message, args);
        }

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Error(string message, params object[] args)
        {
            Log(ConsoleColor.Red, "ERROR", message, args);
        }

        /// <summary>
        /// Registra un mensaje de error con excepción
        /// </summary>
        /// <param name="exception">Excepción</param>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Error(Exception exception, string message, params object[] args)
        {
            Log(ConsoleColor.Red, "ERROR", message, args);
            LogException(exception);
        }

        /// <summary>
        /// Registra un mensaje crítico
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Critical(string message, params object[] args)
        {
            Log(ConsoleColor.DarkRed, "CRIT", message, args);
        }

        /// <summary>
        /// Registra un mensaje crítico con excepción
        /// </summary>
        /// <param name="exception">Excepción</param>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        public void Critical(Exception exception, string message, params object[] args)
        {
            Log(ConsoleColor.DarkRed, "CRIT", message, args);
            LogException(exception);
        }

        /// <summary>
        /// Inicia un ámbito de logging
        /// </summary>
        /// <typeparam name="TState">Tipo del estado</typeparam>
        /// <param name="state">Estado</param>
        /// <returns>Ámbito que se puede disponer</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            string scopeId = Guid.NewGuid().ToString();
            _scopes[scopeId] = state;

            return new LoggerScope(() => _scopes.Remove(scopeId));
        }

        /// <summary>
        /// Registra un mensaje en la consola
        /// </summary>
        /// <param name="color">Color del mensaje</param>
        /// <param name="level">Nivel de log</param>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        private void Log(ConsoleColor color, string level, string message, params object[] args)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;

                string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                Console.WriteLine($"{timestamp} [{level}] [{_name}] {formattedMessage}");

                if (_scopes.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"Scopes: {string.Join(" => ", _scopes.Values)}");
                }
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        /// <summary>
        /// Registra una excepción en la consola
        /// </summary>
        /// <param name="exception">Excepción</param>
        private void LogException(Exception exception)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Exception: {exception.GetType().Name}");
                Console.WriteLine($"Message: {exception.Message}");

                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    Console.WriteLine("StackTrace:");
                    Console.WriteLine(exception.StackTrace);
                }

                if (exception.InnerException != null)
                {
                    Console.WriteLine("Inner Exception:");
                    LogException(exception.InnerException);
                }
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        /// <summary>
        /// Clase para manejar los ámbitos de logging
        /// </summary>
        private class LoggerScope : IDisposable
        {
            private readonly Action _disposeAction;

            public LoggerScope(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction?.Invoke();
            }
        }
    }
}