using System;
using System.Diagnostics;

namespace NexusLink.Logging
{
    /// <summary>
    /// Adaptador para integrar NexusTrace (sistema de diagnóstico propietario) con el sistema
    /// de logging de NexusLink, permitiendo que la biblioteca funcione con infraestructuras
    /// de diagnóstico existentes.
    /// </summary>
    public class NexusTraceAdapter : ILogger
    {
        private readonly string _category;
        private readonly bool _includeTimestamp;
        private readonly bool _includeStackTrace;

        // La clase NexusTrace es un ejemplo de un sistema de diagnóstico externo
        // Se simula aquí con una referencia a un Trace estándar de .NET
        private static readonly TraceSource _nexusTrace = new TraceSource("NexusTrace");

        /// <summary>
        /// Crea una nueva instancia de NexusTraceAdapter
        /// </summary>
        /// <param name="category">Categoría para el logger</param>
        /// <param name="includeTimestamp">Si es true, incluye marca de tiempo en los mensajes</param>
        /// <param name="includeStackTrace">Si es true, incluye la traza de la pila en los mensajes de error</param>
        public NexusTraceAdapter(string category, bool includeTimestamp = true, bool includeStackTrace = false)
        {
            _category = category ?? "NexusLink";
            _includeTimestamp = includeTimestamp;
            _includeStackTrace = includeStackTrace;
        }

        /// <summary>
        /// Registra un mensaje de depuración
        /// </summary>
        public void Debug(string message)
        {
            _nexusTrace.TraceEvent(TraceEventType.Verbose, 0, FormatMessage("DEBUG", message));
        }

        /// <summary>
        /// Registra un mensaje informativo
        /// </summary>
        public void Info(string message)
        {
            _nexusTrace.TraceEvent(TraceEventType.Information, 0, FormatMessage("INFO", message));
        }

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        public void Warning(string message)
        {
            _nexusTrace.TraceEvent(TraceEventType.Warning, 0, FormatMessage("WARNING", message));
        }

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        public void Error(string message)
        {
            _nexusTrace.TraceEvent(TraceEventType.Error, 0, FormatMessage("ERROR", message));
        }

        /// <summary>
        /// Registra un mensaje de error con excepción
        /// </summary>
        public void Error(string message, Exception exception)
        {
            string formattedMessage = FormatMessage("ERROR", message);
            formattedMessage += Environment.NewLine + "Exception: " + exception.Message;

            if (_includeStackTrace && exception.StackTrace != null)
            {
                formattedMessage += Environment.NewLine + "StackTrace: " + exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                formattedMessage += Environment.NewLine + "Inner Exception: " + exception.InnerException.Message;
            }

            _nexusTrace.TraceEvent(TraceEventType.Error, 0, formattedMessage);
        }

        /// <summary>
        /// Registra un mensaje crítico
        /// </summary>
        public void Critical(string message)
        {
            _nexusTrace.TraceEvent(TraceEventType.Critical, 0, FormatMessage("CRITICAL", message));
        }

        /// <summary>
        /// Registra un mensaje crítico con excepción
        /// </summary>
        public void Critical(string message, Exception exception)
        {
            string formattedMessage = FormatMessage("CRITICAL", message);
            formattedMessage += Environment.NewLine + "Exception: " + exception.Message;

            if (_includeStackTrace && exception.StackTrace != null)
            {
                formattedMessage += Environment.NewLine + "StackTrace: " + exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                formattedMessage += Environment.NewLine + "Inner Exception: " + exception.InnerException.Message;
            }

            _nexusTrace.TraceEvent(TraceEventType.Critical, 0, formattedMessage);
        }

        /// <summary>
        /// Formatea un mensaje para registro
        /// </summary>
        private string FormatMessage(string level, string message)
        {
            string formattedMessage = $"[{_category}] [{level}] {message}";

            if (_includeTimestamp)
            {
                formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " + formattedMessage;
            }

            return formattedMessage;
        }

        /// <summary>
        /// Configura el destino de salida para el adaptador de NexusTrace
        /// </summary>
        /// <param name="outputType">Tipo de salida deseada</param>
        /// <param name="filePath">Ruta de archivo (sólo para FileTraceOutput)</param>
        public void ConfigureOutput(NexusTraceOutputType outputType, string filePath = null)
        {
            // Eliminar listeners actuales
            _nexusTrace.Listeners.Clear();

            switch (outputType)
            {
                case NexusTraceOutputType.Console:
                    _nexusTrace.Listeners.Add(new ConsoleTraceListener());
                    break;

                case NexusTraceOutputType.Debug:
                    _nexusTrace.Listeners.Add(new DefaultTraceListener());
                    break;

                case NexusTraceOutputType.File:
                    if (string.IsNullOrEmpty(filePath))
                    {
                        throw new ArgumentException("File path must be specified for File output type", nameof(filePath));
                    }
                    _nexusTrace.Listeners.Add(new TextWriterTraceListener(filePath));
                    break;

                case NexusTraceOutputType.EventLog:
                    _nexusTrace.Listeners.Add(new EventLogTraceListener("NexusLink"));
                    break;

                default:
                    _nexusTrace.Listeners.Add(new DefaultTraceListener());
                    break;
            }
        }

        /// <summary>
        /// Configura el nivel de detalle para el adaptador de NexusTrace
        /// </summary>
        /// <param name="level">Nivel de detalle deseado</param>
        public void SetTraceLevel(NexusTraceLevel level)
        {
            switch (level)
            {
                case NexusTraceLevel.Critical:
                    _nexusTrace.Switch.Level = SourceLevels.Critical;
                    break;

                case NexusTraceLevel.Error:
                    _nexusTrace.Switch.Level = SourceLevels.Error;
                    break;

                case NexusTraceLevel.Warning:
                    _nexusTrace.Switch.Level = SourceLevels.Warning;
                    break;

                case NexusTraceLevel.Info:
                    _nexusTrace.Switch.Level = SourceLevels.Information;
                    break;

                case NexusTraceLevel.Debug:
                    _nexusTrace.Switch.Level = SourceLevels.Verbose;
                    break;

                case NexusTraceLevel.Off:
                    _nexusTrace.Switch.Level = SourceLevels.Off;
                    break;

                default:
                    _nexusTrace.Switch.Level = SourceLevels.Information;
                    break;
            }
        }
    }

    /// <summary>
    /// Tipos de destino para la salida de NexusTrace
    /// </summary>
    public enum NexusTraceOutputType
    {
        Console,
        Debug,
        File,
        EventLog
    }

    /// <summary>
    /// Niveles de detalle para NexusTrace
    /// </summary>
    public enum NexusTraceLevel
    {
        Critical,
        Error,
        Warning,
        Info,
        Debug,
        Off
    }
}