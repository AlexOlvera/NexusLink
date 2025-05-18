using NexusLink.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Core.Connection
{
    // Error: Symbol 'DatabaseSelector' not found
    // Solución: Asegúrate de que DatabaseSelector esté implementada

    public class DatabaseSelector
    {
        private readonly MultiDatabaseConfig _config;
        private readonly AsyncLocal<string> _currentDatabaseName = new AsyncLocal<string>();

        public DatabaseSelector(MultiDatabaseConfig config)
        {
            _config = config;
        }

        // Obtener conexión actual
        public ConnectionSettings CurrentConnection =>
            _config.GetConnection(CurrentDatabaseName);

        // Nombre de la base de datos actual
        public string CurrentDatabaseName
        {
            get => _currentDatabaseName.Value ?? "Default";
            set => _currentDatabaseName.Value = value;
        }

        // Ejecutar código con una base de datos específica
        public T ExecuteWith<T>(string databaseName, Func<T> action)
        {
            string previous = CurrentDatabaseName;
            try
            {
                CurrentDatabaseName = databaseName;
                return action();
            }
            finally
            {
                CurrentDatabaseName = previous;
            }
        }
    }
}
