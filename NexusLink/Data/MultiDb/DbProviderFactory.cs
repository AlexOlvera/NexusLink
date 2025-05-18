using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using NexusLink.Core.Configuration;

namespace NexusLink.Data.MultiDb
{
    /// <summary>
    /// Fábrica para crear proveedores específicos de base de datos
    /// </summary>
    public class DbProviderFactory
    {
        private readonly ConnectionSettings _settings;
        private readonly System.Data.Common.DbProviderFactory _factory;

        public DbProviderFactory(ConnectionSettings settings)
        {
            _settings = settings;

            // Seleccionar proveedor basado en tipo
            switch (settings.ProviderType.ToLowerInvariant())
            {
                case "sqlserver":
                    _factory = SqlClientFactory.Instance;
                    break;
                // Añadir más proveedores según sea necesario
                default:
                    throw new ArgumentException($"Unsupported provider type: {settings.ProviderType}");
            }
        }

        /// <summary>
        /// Crea una nueva conexión
        /// </summary>
        public DbConnection CreateConnection()
        {
            var connection = _factory.CreateConnection();
            connection.ConnectionString = _settings.ConnectionString;
            return connection;
        }

        /// <summary>
        /// Crea un nuevo comando
        /// </summary>
        public DbCommand CreateCommand()
        {
            return _factory.CreateCommand();
        }

        /// <summary>
        /// Crea un nuevo parámetro
        /// </summary>
        public DbParameter CreateParameter(string name, object value)
        {
            var parameter = _factory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        /// <summary>
        /// Crea un adaptador de datos
        /// </summary>
        public DbDataAdapter CreateDataAdapter()
        {
            return _factory.CreateDataAdapter();
        }
    }
}