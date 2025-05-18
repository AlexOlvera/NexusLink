using System;
using System.IO;
using System.Text;
using System.Threading;

namespace NexusLink.Logging
{
    /// <summary>
    /// Implementación de ILogger que escribe los mensajes de registro en un archivo.
    /// Provee opciones para rotación de archivos, bloqueo y buffering.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly string _category;
        private readonly object _lockObject = new object();
        private readonly FileLoggerOptions _options;
        private StreamWriter _writer;
        private DateTime _currentLogDate;
        private int _currentFileSize;
        private int _rotationCount;
        private Timer _flushTimer;
        private readonly StringBuilder _buffer;

        /// <summary>
        /// Crea una nueva instancia de FileLogger
        /// </summary>
        /// <param name="logFilePath">Ruta del archivo de registro</param>
        /// <param name="category">Categoría para el logger</param>
        /// <param name="options">Opciones de configuración</param>
        public FileLogger(string logFilePath, string category, FileLoggerOptions options = null)
        {
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
            _category = category ?? "NexusLink";
            _options = options ?? new FileLoggerOptions();
            _currentLogDate = DateTime.Today;
            _currentFileSize = 0;
            _rotationCount = 0;

            if (_options.EnableBuffering)
            {
                _buffer = new StringBuilder(_options.BufferSize);
                _flushTimer = new Timer(FlushBuffer, null, _options.FlushInterval, _options.FlushInterval);
            }

            InitializeLogFile();
        }

        /// <summary>
        /// Inicializa el archivo de registro
        /// </summary>
        private void InitializeLogFile()
        {
            string directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool fileExists = File.Exists(_logFilePath);

            if (fileExists && _options.RotationStrategy != LogRotationStrategy.None)
            {
                // Comprobar si se necesita rotar el archivo
                CheckRotation();
            }

            // Abrir o crear el archivo de registro
            _writer = new StreamWriter(
                new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read),
                Encoding.UTF8)
            {
                AutoFlush = !_options.EnableBuffering
            };

            if (fileExists)
            {
                _currentFileSize = (int)new FileInfo(_logFilePath).Length;
            }
            else
            {
                _currentFileSize = 0;

                // Escribir encabezado si es un archivo nuevo
                if (_options.IncludeHeader)
                {
                    string header = $"=== NexusLink Log ({_category}) - {DateTime.Now} ===\r\n";
                    WriteToFile(header);
                    _currentFileSize += header.Length;
                }
            }
        }

        /// <summary>
        /// Comprueba si se debe rotar el archivo de registro
        /// </summary>
        private void CheckRotation()
        {
            bool needRotation = false;

            switch (_options.RotationStrategy)
            {
                case LogRotationStrategy.Daily:
                    needRotation = DateTime.Today > _currentLogDate;
                    break;

                case LogRotationStrategy.Size:
                    needRotation = _currentFileSize >= _options.MaxFileSizeInBytes;
                    break;

                case LogRotationStrategy.Both:
                    needRotation = DateTime.Today > _currentLogDate || _currentFileSize >= _options.MaxFileSizeInBytes;
                    break;
            }

            if (needRotation)
            {
                RotateLogFile();
            }
        }

        /// <summary>
        /// Rota el archivo de registro actual
        /// </summary>
        private void RotateLogFile()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }

            string directory = Path.GetDirectoryName(_logFilePath);
            string fileName = Path.GetFileNameWithoutExtension(_logFilePath);
            string extension = Path.GetExtension(_logFilePath);

            string timestamp = _options.RotationStrategy == LogRotationStrategy.Daily ||
                               _options.RotationStrategy == LogRotationStrategy.Both
                ? DateTime.Now.ToString("yyyyMMdd") : "";

            string counter = (_options.RotationStrategy == LogRotationStrategy.Size ||
                             _options.RotationStrategy == LogRotationStrategy.Both) &&
                             DateTime.Today == _currentLogDate
                ? (++_rotationCount).ToString() : "";

            string newFileName = $"{fileName}_{timestamp}{counter}{extension}";
            string newFilePath = Path.Combine(directory, newFileName);

            // Si el archivo de destino ya existe, incrementar contador
            while (File.Exists(newFilePath))
            {
                counter = (++_rotationCount).ToString();
                newFileName = $"{fileName}_{timestamp}{counter}{extension}";
                newFilePath = Path.Combine(directory, newFileName);
            }

            // Mover archivo actual
            File.Move(_logFilePath, newFilePath);

            // Actualizar estado
            _currentLogDate = DateTime.Today;
            _currentFileSize = 0;

            // Si se utiliza rotación por días, reiniciar contador
            if (_options.RotationStrategy == LogRotationStrategy.Daily ||
                (_options.RotationStrategy == LogRotationStrategy.Both && DateTime.Today > _currentLogDate))
            {
                _rotationCount = 0;
            }
        }

        /// <summary>
        /// Escribe un mensaje en el archivo
        /// </summary>
        private void WriteToFile(string message)
        {
            if (_options.EnableBuffering)
            {
                lock (_lockObject)
                {
                    _buffer.AppendLine(message);

                    // Forzar flush si el buffer está casi lleno
                    if (_buffer.Length >= _options.BufferSize * 0.9)
                    {
                        FlushBuffer(null);
                    }
                }
            }
            else
            {
                lock (_lockObject)
                {
                    if (_writer == null)
                    {
                        InitializeLogFile();
                    }

                    _writer.WriteLine(message);
                    _currentFileSize += message.Length + Environment.NewLine.Length;

                    CheckRotation();
                }
            }
        }

        /// <summary>
        /// Vuelca el buffer al archivo
        /// </summary>
        private void FlushBuffer(object state)
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_buffer.Length > 0)
                {
                    if (_writer == null)
                    {
                        InitializeLogFile();
                    }

                    string content = _buffer.ToString();
                    _writer.Write(content);
                    _currentFileSize += content.Length;
                    _buffer.Clear();
                    _writer.Flush();

                    CheckRotation();
                }
            }
        }

        /// <summary>
        /// Formatea un mensaje para registro
        /// </summary>
        private string FormatMessage(string level, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{_category}] [{level}] {message}";
        }

        /// <summary>
        /// Registra un mensaje de depuración
        /// </summary>
        public void Debug(string message)
        {
            if (_options.MinimumLevel <= LogLevel.Debug)
            {
                WriteToFile(FormatMessage("DEBUG", message));
            }
        }

        /// <summary>
        /// Registra un mensaje informativo
        /// </summary>
        public void Info(string message)
        {
            if (_options.MinimumLevel <= LogLevel.Info)
            {
                WriteToFile(FormatMessage("INFO", message));
            }
        }

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        public void Warning(string message)
        {
            if (_options.MinimumLevel <= LogLevel.Warning)
            {
                WriteToFile(FormatMessage("WARNING", message));
            }
        }

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        public void Error(string message)
        {
            if (_options.MinimumLevel <= LogLevel.Error)
            {
                WriteToFile(FormatMessage("ERROR", message));
            }
        }

        /// <summary>
        /// Registra un mensaje de error con excepción
        /// </summary>
        public void Error(string message, Exception exception)
        {
            if (_options.MinimumLevel <= LogLevel.Error)
            {
                StringBuilder exceptionMessage = new StringBuilder();
                exceptionMessage.AppendLine(FormatMessage("ERROR", message));
                exceptionMessage.AppendLine($"Exception: {exception.GetType().Name}: {exception.Message}");

                if (_options.IncludeStackTrace && exception.StackTrace != null)
                {
                    exceptionMessage.AppendLine($"StackTrace: {exception.StackTrace}");
                }

                if (exception.InnerException != null)
                {
                    exceptionMessage.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}");

                    if (_options.IncludeStackTrace && exception.InnerException.StackTrace != null)
                    {
                        exceptionMessage.AppendLine($"Inner StackTrace: {exception.InnerException.StackTrace}");
                    }
                }

                WriteToFile(exceptionMessage.ToString());
            }
        }

        /// <summary>
        /// Registra un mensaje crítico
        /// </summary>
        public void Critical(string message)
        {
            if (_options.MinimumLevel <= LogLevel.Critical)
            {
                WriteToFile(FormatMessage("CRITICAL", message));
            }
        }

        /// <summary>
        /// Registra un mensaje crítico con excepción
        /// </summary>
        public void Critical(string message, Exception exception)
        {
            if (_options.MinimumLevel <= LogLevel.Critical)
            {
                StringBuilder exceptionMessage = new StringBuilder();
                exceptionMessage.AppendLine(FormatMessage("CRITICAL", message));
                exceptionMessage.AppendLine($"Exception: {exception.GetType().Name}: {exception.Message}");

                if (_options.IncludeStackTrace && exception.StackTrace != null)
                {
                    exceptionMessage.AppendLine($"StackTrace: {exception.StackTrace}");
                }

                if (exception.InnerException != null)
                {
                    exceptionMessage.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}");

                    if (_options.IncludeStackTrace && exception.InnerException.StackTrace != null)
                    {
                        exceptionMessage.AppendLine($"Inner StackTrace: {exception.InnerException.StackTrace}");
                    }
                }

                WriteToFile(exceptionMessage.ToString());
            }
        }

        /// <summary>
        /// Libera recursos utilizados por el logger
        /// </summary>
        public void Dispose()
        {
            if (_flushTimer != null)
            {
                _flushTimer.Dispose();
                _flushTimer = null;
            }

            if (_options.EnableBuffering)
            {
                FlushBuffer(null);
            }

            if (_writer != null)
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
        }
    }

    /// <summary>
    /// Opciones de configuración para FileLogger
    /// </summary>
    public class FileLoggerOptions
    {
        /// <summary>
        /// Estrategia de rotación de archivos
        /// </summary>
        public LogRotationStrategy RotationStrategy { get; set; } = LogRotationStrategy.Daily;

        /// <summary>
        /// Tamaño máximo del archivo en bytes para rotación por tamaño
        /// </summary>
        public int MaxFileSizeInBytes { get; set; } = 10 * 1024 * 1024; // 10 MB por defecto

        /// <summary>
        /// Nivel mínimo de log a registrar
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Si es true, incluye traza de la pila en los mensajes de error
        /// </summary>
        public bool IncludeStackTrace { get; set; } = true;

        /// <summary>
        /// Si es true, incluye un encabezado al inicio del archivo
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Si es true, habilita el buffering para mejorar rendimiento
        /// </summary>
        public bool EnableBuffering { get; set; } = false;

        /// <summary>
        /// Tamaño del buffer en caracteres cuando buffering está habilitado
        /// </summary>
        public int BufferSize { get; set; } = 8192; // 8 KB por defecto

        /// <summary>
        /// Intervalo de flush del buffer en milisegundos
        /// </summary>
        public int FlushInterval { get; set; } = 1000; // 1 segundo por defecto
    }

    /// <summary>
    /// Estrategias de rotación de archivos de log
    /// </summary>
    public enum LogRotationStrategy
    {
        /// <summary>
        /// Sin rotación, el archivo crece indefinidamente
        /// </summary>
        None,

        /// <summary>
        /// Rotación diaria
        /// </summary>
        Daily,

        /// <summary>
        /// Rotación por tamaño
        /// </summary>
        Size,

        /// <summary>
        /// Rotación diaria y por tamaño
        /// </summary>
        Both
    }

    /// <summary>
    /// Niveles de log
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}