using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NexusLink.Core.Configuration
{
    /// <summary>
    /// Configura los ajustes de una conexión a base de datos
    /// </summary>
    public class ConnectionSettings
    {
        /// <summary>
        /// Nombre de la conexión
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cadena de conexión
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Tipo de proveedor de base de datos
        /// </summary>
        public DbProviderType ProviderType { get; set; }

        /// <summary>
        /// Timeout de comando predeterminado en segundos
        /// </summary>
        public int DefaultCommandTimeout { get; set; } = 30;

        /// <summary>
        /// Reintentos en caso de fallos transitorios
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Tiempo de espera entre reintentos en milisegundos
        /// </summary>
        public int RetryInterval { get; set; } = 500;

        /// <summary>
        /// Crea una nueva instancia de SqlConnection basada en esta configuración
        /// </summary>
        /// <returns>Una nueva conexión SQL</returns>
        public SqlConnection CreateConnection()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("La cadena de conexión no puede estar vacía");

            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Establece opciones específicas del proveedor a un comando
        /// </summary>
        /// <param name="command">El comando a configurar</param>
        public void ApplyCommandSettings(IDbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.CommandTimeout = DefaultCommandTimeout;
        }
    }

    /// <summary>
    /// Tipos de proveedores de bases de datos soportados
    /// </summary>
    public enum DbProviderType
    {
        SqlServer,
        MySql,
        PostgreSql,
        Oracle,
        SQLite
    }
}