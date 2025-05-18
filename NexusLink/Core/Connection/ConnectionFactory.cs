using System;
using System.Data;
using System.Data.SqlClient;
using NexusLink.Core.Configuration;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Fábrica para crear y gestionar conexiones a bases de datos
    /// </summary>
    public class ConnectionFactory
    {
        private readonly MultiDatabaseConfig _config;

        /// <summary>
        /// Inicializa una nueva instancia con la configuración predeterminada
        /// </summary>
        public ConnectionFactory()
            : this(SettingsProvider.DatabaseConfig)
        {
        }

        /// <summary>
        /// Inicializa una nueva instancia con una configuración específica
        /// </summary>
        /// <param name="config">Configuración de bases de datos</param>
        public ConnectionFactory(MultiDatabaseConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Crea una nueva conexión SQL
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión o null para la predeterminada</param>
        /// <returns>Conexión SQL</returns>
        public SqlConnection CreateSqlConnection(string connectionName = null)
        {
            var settings = _config.GetConnection(connectionName) ?? _config.GetDefaultConnection();

            if (settings == null)
                throw new InvalidOperationException($"No se encontró la configuración de conexión '{connectionName ?? "predeterminada"}'");

            return settings.CreateConnection();
        }

        /// <summary>
        /// Crea una conexión SQL y la abre
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>Conexión SQL abierta</returns>
        public SqlConnection CreateOpenConnection(string connectionName = null)
        {
            var connection = CreateSqlConnection(connectionName);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Crea un nuevo comando SQL
        /// </summary>
        /// <param name="connection">Conexión existente o null para crear una nueva</param>
        /// <param name="commandText">Texto del comando</param>
        /// <param name="commandType">Tipo de comando</param>
        /// <param name="parameters">Parámetros del comando</param>
        /// <param name="connectionName">Nombre de la conexión (si connection es null)</param>
        /// <returns>Comando SQL</returns>
        public SqlCommand CreateCommand(
            SqlConnection connection = null,
            string commandText = null,
            CommandType commandType = CommandType.Text,
            SqlParameter[] parameters = null,
            string connectionName = null)
        {
            bool newConnection = false;

            if (connection == null)
            {
                connection = CreateSqlConnection(connectionName);
                newConnection = true;
            }

            var command = connection.CreateCommand();
            command.CommandType = commandType;

            if (!string.IsNullOrEmpty(commandText))
                command.CommandText = commandText;

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var settings = _config.GetConnection(connectionName);
            if (settings != null)
            {
                settings.ApplyCommandSettings(command);
            }

            // Adjuntar evento para cerrar la conexión si la creamos nosotros
            if (newConnection)
            {
                var originalConnection = connection;
                command.Disposed += (sender, args) =>
                {
                    if (originalConnection.State == ConnectionState.Open)
                        originalConnection.Close();
                    originalConnection.Dispose();
                };
            }

            return command;
        }
    }
}