using System;

namespace NexusLink.Core.Configuration
{
    /// <summary>
    /// Proveedor de configuración extensible
    /// </summary>
    public class SettingsProvider
    {
        private static MultiDatabaseConfig _databaseConfig;

        /// <summary>
        /// Inicializa el proveedor con una configuración específica
        /// </summary>
        /// <param name="databaseConfig">Configuración de bases de datos</param>
        public static void Initialize(MultiDatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        }

        /// <summary>
        /// Obtiene la configuración actual de bases de datos
        /// </summary>
        public static MultiDatabaseConfig DatabaseConfig
        {
            get
            {
                if (_databaseConfig == null)
                {
                    _databaseConfig = new MultiDatabaseConfig();
                    _databaseConfig.LoadFromConfig();
                }
                return _databaseConfig;
            }
        }

        /// <summary>
        /// Obtiene una configuración de conexión por nombre
        /// </summary>
        /// <param name="name">Nombre de la conexión o null para la predeterminada</param>
        /// <returns>La configuración de conexión</returns>
        public static ConnectionSettings GetConnectionSettings(string name = null)
        {
            var config = DatabaseConfig.GetConnection(name);

            if (config == null)
                throw new InvalidOperationException($"No se encontró la configuración de conexión '{name ?? "predeterminada"}'");

            return config;
        }

        /// <summary>
        /// Obtiene el tiempo de comando predeterminado para una conexión
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>Tiempo de comando en segundos</returns>
        public static int GetCommandTimeout(string connectionName = null)
        {
            return GetConnectionSettings(connectionName).DefaultCommandTimeout;
        }
    }
}