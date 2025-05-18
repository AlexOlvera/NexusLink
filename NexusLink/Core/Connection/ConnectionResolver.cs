using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using NexusLink.Core.Configuration;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Resuelve conexiones a bases de datos basado en nombre o contexto
    /// </summary>
    public class ConnectionResolver
    {
        private readonly MultiDatabaseConfig _config;
        private readonly ConnectionFactory _factory;
        private readonly Dictionary<string, string> _connectionAliases;

        public ConnectionResolver(
            MultiDatabaseConfig config,
            ConnectionFactory factory)
        {
            _config = config;
            _factory = factory;
            _connectionAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Inicializa los alias predeterminados
            InitializeAliases();
        }

        /// <summary>
        /// Resuelve una conexión por nombre
        /// </summary>
        public DbConnection ResolveConnection(string nameOrAlias)
        {
            string connectionName = ResolveConnectionName(nameOrAlias);
            var settings = _config.GetConnection(connectionName);

            if (settings == null)
            {
                throw new ArgumentException($"No se encontró la configuración de conexión: {nameOrAlias}");
            }

            return _factory.CreateConnection(settings.ConnectionString, settings.ProviderName);
        }

        /// <summary>
        /// Resuelve una conexión SQL Server por nombre
        /// </summary>
        public SqlConnection ResolveSqlConnection(string nameOrAlias)
        {
            string connectionName = ResolveConnectionName(nameOrAlias);
            var settings = _config.GetConnection(connectionName);

            if (settings == null)
            {
                throw new ArgumentException($"No se encontró la configuración de conexión: {nameOrAlias}");
            }

            if (!settings.ProviderName.Contains("SqlClient"))
            {
                throw new InvalidOperationException($"La conexión {nameOrAlias} no es una conexión SQL Server");
            }

            return new SqlConnection(settings.ConnectionString);
        }

        /// <summary>
        /// Registra un alias para una conexión
        /// </summary>
        public void RegisterAlias(string alias, string connectionName)
        {
            if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(connectionName))
            {
                throw new ArgumentException("El alias y el nombre de conexión no pueden estar vacíos");
            }

            _connectionAliases[alias] = connectionName;
        }

        /// <summary>
        /// Resuelve el nombre real de una conexión a partir de un alias
        /// </summary>
        private string ResolveConnectionName(string nameOrAlias)
        {
            // Si el alias existe, devolver el nombre de conexión mapeado
            if (_connectionAliases.TryGetValue(nameOrAlias, out string connectionName))
            {
                return connectionName;
            }

            // Si no es un alias, devolver el nombre original
            return nameOrAlias;
        }

        /// <summary>
        /// Inicializa los alias predeterminados
        /// </summary>
        private void InitializeAliases()
        {
            // Los alias podría cargarse desde configuración o definirse de forma estática
            _connectionAliases["main"] = "Default";
            _connectionAliases["default"] = "Default";
            _connectionAliases["primary"] = "Default";

            // Detectar y agregar alias para bases de datos comunes
            foreach (var connectionSettings in _config.GetAllConnections())
            {
                string name = connectionSettings.Name.ToLowerInvariant();

                if (name.Contains("reporting"))
                {
                    _connectionAliases["reports"] = connectionSettings.Name;
                    _connectionAliases["reporting"] = connectionSettings.Name;
                }
                else if (name.Contains("archive"))
                {
                    _connectionAliases["archives"] = connectionSettings.Name;
                    _connectionAliases["history"] = connectionSettings.Name;
                }
                else if (name.Contains("log"))
                {
                    _connectionAliases["logs"] = connectionSettings.Name;
                    _connectionAliases["logging"] = connectionSettings.Name;
                }
            }
        }
    }
}