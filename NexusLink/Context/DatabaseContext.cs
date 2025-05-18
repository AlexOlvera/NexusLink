using System;
using System.Collections.Generic;
using System.Threading;

namespace NexusLink.Context
{
    /// <summary>
    /// Mantiene la información del contexto de base de datos actual
    /// </summary>
    public class DatabaseContext
    {
        private static readonly AsyncLocal<DatabaseContext> _current = new AsyncLocal<DatabaseContext>();
        private readonly Dictionary<string, object> _contextItems = new Dictionary<string, object>();

        public static DatabaseContext Current
        {
            get => _current.Value ?? (_current.Value = new DatabaseContext());
            set => _current.Value = value;
        }

        /// <summary>
        /// Nombre de la base de datos actual
        /// </summary>
        public string CurrentDatabaseName { get; set; } = "Default";

        /// <summary>
        /// Verifica si una conexión está en uso
        /// </summary>
        public bool IsConnectionActive { get; set; }

        /// <summary>
        /// Almacena elementos de contexto específicos para la sesión actual
        /// </summary>
        public T GetOrCreate<T>(string key, Func<T> factory) where T : class
        {
            if (!_contextItems.TryGetValue(key, out object value))
            {
                value = factory();
                _contextItems[key] = value;
            }

            return (T)value;
        }

        /// <summary>
        /// Limpia el contexto actual
        /// </summary>
        public void Clear()
        {
            _contextItems.Clear();
            IsConnectionActive = false;
        }
    }
}