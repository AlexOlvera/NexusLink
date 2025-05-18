using System;
using System.Diagnostics;

namespace NexusLink.Logging
{
    /// <summary>
    /// Implementación de ILogger que utiliza System.Diagnostics.Trace para registrar eventos.
    /// Esta clase está optimizada para integrarse con el sistema de diagnóstico de .NET.
    /// </summary>
    public class TraceLogger : ILogger
    {
        private readonly string _category;
        private readonly TraceSource _traceSource;
        private readonly bool _includeStackTrace;

        /// <summary>
        /// Crea una nueva instancia de TraceLogger
        /// </summary>
        /// <param name="category">Categoría para el logger</param>
        /// <param name="includeStackTrace">Si es true, incluye la traza de la pila en los mensajes de error</param>
        public TraceLogger(string category, bool includeStackTrace = false)
        {
            _category = category ?? "NexusLink";
            _traceSource = new TraceSource(_category);
            _includeStackTrace = includeStackTrace;
        }

        /// <summary>
        /// Registra un mensaje de depuración
        /// </summary>
        public void Debug(string message)
        {
            _traceSource.TraceEvent(TraceEventType.Verbose, 0, FormatMessage(message));
        }

        /// <summary>
        /// Registra un mensaje informativo
        /// </summary>
        public void Info(string message)
        {
            _traceSource.TraceEvent(TraceEventType.Information, 0, FormatMessage(message));
        }

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        public void Warning(string message)
        {
            _traceSource.TraceEvent(TraceEventType.Warning, 0, FormatMessage(message));
        }

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        public void Error(string message)
        {
            _traceSource.TraceEvent(TraceEventType.Error, 0, FormatMessage(message));
        }

        /// <summary>
        /// Registra un mensaje de error con excepción
        /// </summary>
        public void Error(string message, Exception exception)
        {
            string fullMessage = FormatMessage(message) + Environment.NewLine +
                                "Exception: " + exception.Message;

            if (_includeStackTrace && exception.StackTrace != null)
            {
                fullMessage += Environment.NewLine + "StackTrace: " + exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                fullMessage += Environment.NewLine + "Inner Exception: " + exception.InnerException.Message;
            }

            _traceSource.TraceEvent(TraceEventType.Error, 0, fullMessage);
        }

        /// <summary>
        /// Registra un mensaje crítico
        /// </summary>
        public void Critical(string message)
        {
            _traceSource.TraceEvent(TraceEventType.Critical, 0, FormatMessage(message));
        }

        /// <summary>
        /// Registra un mensaje crítico con excepción
        /// </summary>
        public void Critical(string message, Exception exception)
        {
            string fullMessage = FormatMessage(message) + Environment.NewLine +
                                "Exception: " + exception.Message;

            if (_includeStackTrace && exception.StackTrace != null)
            {
                fullMessage += Environment.NewLine + "StackTrace: " + exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                fullMessage += Environment.NewLine + "Inner Exception: " + exception.InnerException.Message;
            }

            _traceSource.TraceEvent(TraceEventType.Critical, 0, fullMessage);
        }

        /// <summary>
        /// Formatea un mensaje para registro
        /// </summary>
        private string FormatMessage(string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{_category}] {message}";
        }

        /// <summary>
        /// Habilita la captura de diagnósticos de SQL Server utilizando System.Diagnostics.TraceSource
        /// </summary>
        /// <param name="level">Nivel de detalle para diagnósticos SQL</param>
        public void EnableSqlDiagnostics(TraceLevel level = TraceLevel.Verbose)
        {
            // Configurar diagnósticos para System.Data
            TraceSource sqlTraceSource = new TraceSource("System.Data.SqlClient");

            // Configurar nivel de detalle
            switch (level)
            {
                case TraceLevel.Off:
                    sqlTraceSource.Switch.Level = SourceLevels.Off;
                    break;
                case TraceLevel.Error:
                    sqlTraceSource.Switch.Level = SourceLevels.Error;
                    break;
                case TraceLevel.Warning:
                    sqlTraceSource.Switch.Level = SourceLevels.Warning;
                    break;
                case TraceLevel.Info:
                    sqlTraceSource.Switch.Level = SourceLevels.Information;
                    break;
                case TraceLevel.Verbose:
                    sqlTraceSource.Switch.Level = SourceLevels.Verbose;
                    break;
            }

            // Añadir listener para escribir a la misma fuente que nuestro logger
            foreach (TraceListener listener in _traceSource.Listeners)
            {
                sqlTraceSource.Listeners.Add(listener);
            }
        }
    }

    public enum TraceLevel
    {
        Off,
        Error,
        Warning,
        Info,
        Verbose
    }
}
//using System;
//using System.Diagnostics;

//namespace NexusLink.Logging
//{
//    /// <summary>
//    /// Implementación de ILogger que utiliza System.Diagnostics.Trace
//    /// </summary>
//    public class TraceLogger : ILogger
//    {
//        private readonly string _category;
//        private readonly LogLevel _minLogLevel;

//        /// <summary>
//        /// Constructor con categoría y nivel mínimo de log
//        /// </summary>
//        public TraceLogger(string category, LogLevel minLogLevel = LogLevel.Debug)
//        {
//            _category = category ?? throw new ArgumentNullException(nameof(category));
//            _minLogLevel = minLogLevel;
//        }

//        /// <summary>
//        /// Registra un mensaje de debug
//        /// </summary>
//        public void LogDebug(string message)
//        {
//            if (_minLogLevel <= LogLevel.Debug)
//            {
//                Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [DEBUG] [{_category}] {message}");
//            }
//        }

//        /// <summary>
//        /// Registra un mensaje informativo
//        /// </summary>
//        public void LogInformation(string message)
//        {
//            if (_minLogLevel <= LogLevel.Information)
//            {
//                Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [{_category}] {message}");
//            }
//        }

//        /// <summary>
//        /// Registra un mensaje de advertencia
//        /// </summary>
//        public void LogWarning(string message)
//        {
//            if (_minLogLevel <= LogLevel.Warning)
//            {
//                Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [WARN] [{_category}] {message}");
//            }
//        }

//        /// <summary>
//        /// Registra un mensaje de error
//        /// </summary>
//        public void LogError(string message)
//        {
//            if (_minLogLevel <= LogLevel.Error)
//            {
//                Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [{_category}] {message}");
//            }
//        }

//        /// <summary>
//        /// Registra un mensaje de error crítico
//        /// </summary>
//        public void LogCritical(string message)
//        {
//            if (_minLogLevel <= LogLevel.Critical)
//            {
//                Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [CRITICAL] [{_category}] {message}");
//            }
//        }

//        /// <summary>
//        /// Registra una excepción
//        /// </summary>
//        public void LogException(Exception exception, string message = null)
//        {
//            if (_minLogLevel <= LogLevel.Error)
//            {
//                string logMessage = string.IsNullOrEmpty(message)
//                    ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [{_category}] Exception: {exception}"
//                    : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [{_category}] {message} - Exception: {exception}";

//                Trace.WriteLine(logMessage);
//            }
//        }
//    }
//}
