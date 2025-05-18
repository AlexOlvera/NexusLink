using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using NexusLink.Core.Configuration;
using NexusLink.Logging;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Proporciona conexiones específicas para cada tenant en un entorno multi-tenant
    /// </summary>
    public class MultiTenantConnection
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly AsyncLocal<string> _currentTenant = new AsyncLocal<string>();
        private readonly Dictionary<string, ConnectionSettings> _tenantConnections;
        private readonly ConnectionSettings _defaultConnection;

        public MultiTenantConnection(
            ILogger logger,
            ConnectionFactory connectionFactory,
            ConnectionSettings defaultConnection)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _defaultConnection = defaultConnection;
            _tenantConnections = new Dictionary<string, ConnectionSettings>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene o establece el tenant actual
        /// </summary>
        public string CurrentTenant
        {
            get => _currentTenant.Value ?? "default";
            set => _currentTenant.Value = value;
        }

        /// <summary>
        /// Registra una conexión para un tenant específico
        /// </summary>
        public void RegisterTenantConnection(string tenantId, ConnectionSettings connectionSettings)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("El ID del tenant no puede estar vacío");
            }

            _tenantConnections[tenantId] = connectionSettings ??
                throw new ArgumentNullException(nameof(connectionSettings));

            _logger.Info($"Conexión registrada para tenant: {tenantId}");
        }

        /// <summary>
        /// Registra una conexión para un tenant específico a partir de una cadena de conexión
        /// </summary>
        public void RegisterTenantConnection(string tenantId, string connectionString, string providerName = "System.Data.SqlClient")
        {
            RegisterTenantConnection(tenantId, new ConnectionSettings
            {
                Name = $"Tenant_{tenantId}",
                ConnectionString = connectionString,
                ProviderName = providerName
            });
        }

        /// <summary>
        /// Determina si existe una conexión registrada para un tenant específico
        /// </summary>
        public bool HasTenantConnection(string tenantId)
        {
            return _tenantConnections.ContainsKey(tenantId);
        }

        /// <summary>
        /// Obtiene una conexión para el tenant actual
        /// </summary>
        public DbConnection GetConnection()
        {
            return GetConnection(CurrentTenant);
        }

        /// <summary>
        /// Obtiene una conexión para un tenant específico
        /// </summary>
        public DbConnection GetConnection(string tenantId)
        {
            ConnectionSettings settings;

            if (_tenantConnections.TryGetValue(tenantId, out var tenantSettings))
            {
                settings = tenantSettings;
                _logger.Debug($"Usando conexión específica para tenant: {tenantId}");
            }
            else
            {
                settings = _defaultConnection;
                _logger.Debug($"Tenant {tenantId} no tiene conexión específica, usando la predeterminada");
            }

            return _connectionFactory.CreateConnection(settings.ConnectionString, settings.ProviderName);
        }

        /// <summary>
        /// Obtiene una conexión SQL para el tenant actual
        /// </summary>
        public SqlConnection GetSqlConnection()
        {
            return GetSqlConnection(CurrentTenant);
        }

        /// <summary>
        /// Obtiene una conexión SQL para un tenant específico
        /// </summary>
        public SqlConnection GetSqlConnection(string tenantId)
        {
            ConnectionSettings settings;

            if (_tenantConnections.TryGetValue(tenantId, out var tenantSettings))
            {
                settings = tenantSettings;
            }
            else
            {
                settings = _defaultConnection;
            }

            if (!settings.ProviderName.Contains("SqlClient"))
            {
                throw new InvalidOperationException($"La conexión para el tenant {tenantId} no es una conexión SQL Server");
            }

            return new SqlConnection(settings.ConnectionString);
        }

        /// <summary>
        /// Ejecuta una acción con un tenant específico
        /// </summary>
        public T ExecuteWithTenant<T>(string tenantId, Func<T> action)
        {
            string previousTenant = CurrentTenant;
            try
            {
                CurrentTenant = tenantId;
                return action();
            }
            finally
            {
                CurrentTenant = previousTenant;
            }
        }

        /// <summary>
        /// Ejecuta una acción con un tenant específico (versión void)
        /// </summary>
        public void ExecuteWithTenant(string tenantId, Action action)
        {
            string previousTenant = CurrentTenant;
            try
            {
                CurrentTenant = tenantId;
                action();
            }
            finally
            {
                CurrentTenant = previousTenant;
            }
        }
    }
}