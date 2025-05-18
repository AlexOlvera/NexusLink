using System;
using System.Collections.Generic;

namespace NexusLink.Logging
{
    /// <summary>
    /// Fábrica para crear loggers
    /// </summary>
    public class LoggerFactory
    {
        private static readonly Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();
        private static Func<string, ILogger> _loggerFactory = name => new ConsoleLogger(name);

        /// <summary>
        /// Establece la función para crear loggers
        /// </summary>
        /// <param name="factory">Función de fábrica</param>
        public static void SetLoggerFactory(Func<string, ILogger> factory)
        {
            _loggerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            _loggers.Clear();
        }

        /// <summary>
        /// Obtiene un logger para un tipo
        /// </summary>
        /// <typeparam name="T">Tipo para el cual obtener el logger</typeparam>
        /// <returns>Logger</returns>
        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T).FullName);
        }

        /// <summary>
        /// Obtiene un logger para un nombre
        /// </summary>
        /// <param name="name">Nombre del logger</param>
        /// <returns>Logger</returns>
        public static ILogger GetLogger(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("El nombre del logger no puede estar vacío", nameof(name));

            // Verificar si ya existe
            if (_loggers.TryGetValue(name, out ILogger logger))
                return logger;

            // Crear un nuevo logger
            logger = _loggerFactory(name);
            _loggers[name] = logger;

            return logger;
        }

        /// <summary>
        /// Limpia todos los loggers
        /// </summary>
        public static void ClearLoggers()
        {
            _loggers.Clear();
        }
    }
}