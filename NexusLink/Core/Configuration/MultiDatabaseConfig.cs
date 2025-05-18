using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusLink.Core.Configuration
{
    /// <summary>
    /// Gestiona la configuración para múltiples bases de datos
    /// </summary>
    public class MultiDatabaseConfig
    {
        private readonly Dictionary<string, ConnectionSettings> _connections;
        private string _defaultConnectionName;

        /// <summary>
        /// Inicializa una nueva instancia de MultiDatabaseConfig
        /// </summary>
        public MultiDatabaseConfig()
        {
            _connections = new Dictionary<string, ConnectionSettings>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Nombre de la conexión predeterminada
        /// </summary>
        public string DefaultConnectionName
        {
            get => _defaultConnectionName ?? _connections.Keys.FirstOrDefault();
            set => _defaultConnectionName = value;
        }

        /// <summary>
        /// Agrega o actualiza una configuración de conexión
        /// </summary>
        /// <param name="settings">Configuración de conexión</param>
        public void AddOrUpdateConnection(ConnectionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(settings.Name))
                throw new ArgumentException("El nombre de la conexión no puede estar vacío", nameof(settings));

            _connections[settings.Name] = settings;

            // Si es la primera conexión, establecerla como predeterminada
            if (_connections.Count == 1 && string.IsNullOrEmpty(_defaultConnectionName))
                _defaultConnectionName = settings.Name;
        }

        /// <summary>
        /// Elimina una configuración de conexión
        /// </summary>
        /// <param name="name">Nombre de la conexión</param>
        /// <returns>True si se eliminó, false si no existía</returns>
        public bool RemoveConnection(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Si eliminamos la conexión predeterminada, actualizar
            if (string.Equals(name, _defaultConnectionName, StringComparison.OrdinalIgnoreCase))
                _defaultConnectionName = _connections.Keys.FirstOrDefault(k => !string.Equals(k, name, StringComparison.OrdinalIgnoreCase));

            return _connections.Remove(name);
        }

        /// <summary>
        /// Obtiene una configuración de conexión por nombre
        /// </summary>
        /// <param name="name">Nombre de la conexión</param>
        /// <returns>La configuración de conexión o null si no existe</returns>
        public ConnectionSettings GetConnection(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = DefaultConnectionName;

            if (string.IsNullOrEmpty(name) || !_connections.ContainsKey(name))
                return null;

            return _connections[name];
        }

        /// <summary>
        /// Obtiene todas las configuraciones de conexión
        /// </summary>
        /// <returns>Lista de configuraciones</returns>
        public IEnumerable<ConnectionSettings> GetAllConnections()
        {
            return _connections.Values;
        }

        /// <summary>
        /// Obtiene la configuración de conexión predeterminada
        /// </summary>
        /// <returns>La configuración predeterminada o null si no hay ninguna</returns>
        public ConnectionSettings GetDefaultConnection()
        {
            return GetConnection(DefaultConnectionName);
        }

        /// <summary>
        /// Carga la configuración desde la configuración de la aplicación
        /// </summary>
        public void LoadFromConfig()
        {
            // Buscar todas las cadenas de conexión en la configuración
            var connectionStrings = new Dictionary<string, string>();

            // Intentar cargar primero desde el archivo de configuración
            int index = 0;
            while (true)
            {
                string connName = ConfigManager.GetAppSetting($"NexusLink:ConnectionNames:{index}");
                if (string.IsNullOrEmpty(connName))
                    break;

                string connString = ConfigManager.GetConnectionString(connName);
                if (!string.IsNullOrEmpty(connString))
                    connectionStrings[connName] = connString;

                index++;
            }

            // Si no se encontraron mediante el método anterior, intentar cargar todas las cadenas de conexión
            if (connectionStrings.Count == 0)
            {
                foreach (var key in GetAllConfigConnectionNames())
                {
                    string connString = ConfigManager.GetConnectionString(key);
                    if (!string.IsNullOrEmpty(connString))
                        connectionStrings[key] = connString;
                }
            }

            // Crear configuraciones para cada cadena de conexión
            foreach (var pair in connectionStrings)
            {
                string providerName = ConfigManager.GetAppSetting($"NexusLink:ConnectionProvider:{pair.Key}") ?? "SqlServer";
                DbProviderType providerType;
                Enum.TryParse(providerName, true, out providerType);

                var settings = new ConnectionSettings
                {
                    Name = pair.Key,
                    ConnectionString = pair.Value,
                    ProviderType = providerType,
                    DefaultCommandTimeout = ConfigManager.GetSetting($"NexusLink:ConnectionTimeout:{pair.Key}", 30),
                    RetryCount = ConfigManager.GetSetting($"NexusLink:ConnectionRetryCount:{pair.Key}", 3),
                    RetryInterval = ConfigManager.GetSetting($"NexusLink:ConnectionRetryInterval:{pair.Key}", 500)
                };

                AddOrUpdateConnection(settings);
            }

            // Establecer conexión predeterminada
            string defaultConn = ConfigManager.GetAppSetting("NexusLink:DefaultConnection");
            if (!string.IsNullOrEmpty(defaultConn) && _connections.ContainsKey(defaultConn))
                DefaultConnectionName = defaultConn;
        }

        private IEnumerable<string> GetAllConfigConnectionNames()
        {
            // Este método varía según la implementación específica
            // Por ahora, retornamos una lista vacía
            return new List<string>();
        }
    }
}