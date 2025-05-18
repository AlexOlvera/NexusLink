using System;
using System.Collections.Generic;
using NexusLink.Context;
using NexusLink.Core.Configuration;

namespace NexusLink.Data.MultiDb
{
    /// <summary>
    /// Administra los contextos de diferentes bases de datos
    /// </summary>
    public class DbContextManager
    {
        private readonly Dictionary<string, ConnectionSettings> _connections;
        private readonly Dictionary<string, DbProviderFactory> _factories;

        public DbContextManager(MultiDatabaseConfig config)
        {
            _connections = config.Connections;
            _factories = new Dictionary<string, DbProviderFactory>();

            // Inicializar fábricas para cada conexión
            foreach (var connection in _connections)
            {
                _factories[connection.Key] = new DbProviderFactory(connection.Value);
            }
        }

        /// <summary>
        /// Obtiene la fábrica del proveedor para la base de datos actual
        /// </summary>
        public DbProviderFactory GetCurrentFactory()
        {
            string dbName = DatabaseContext.Current.CurrentDatabaseName;
            if (!_factories.TryGetValue(dbName, out var factory))
            {
                throw new InvalidOperationException($"No database provider configured for '{dbName}'");
            }

            return factory;
        }

        /// <summary>
        /// Obtiene la cadena de conexión para la base de datos actual
        /// </summary>
        public string GetCurrentConnectionString()
        {
            string dbName = DatabaseContext.Current.CurrentDatabaseName;
            if (!_connections.TryGetValue(dbName, out var settings))
            {
                throw new InvalidOperationException($"No connection settings configured for '{dbName}'");
            }

            return settings.ConnectionString;
        }

        /// <summary>
        /// Ejecuta una acción con una base de datos específica
        /// </summary>
        public T ExecuteWith<T>(string databaseName, Func<T> action)
        {
            using (var scope = DatabaseScope.Create(databaseName))
            {
                return action();
            }
        }
    }
}