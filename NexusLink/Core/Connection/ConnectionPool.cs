using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using NexusLink.Core.Configuration;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Pool de conexiones optimizado que reutiliza conexiones
    /// </summary>
    public class ConnectionPool : IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<SqlConnection>> _connectionPool;
        private readonly MultiDatabaseConfig _config;
        private readonly int _poolSize;
        private readonly TimeSpan _connectionTimeout;
        private readonly TimeSpan _connectionLifetime;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        /// <summary>
        /// Inicializa una nueva instancia del pool de conexiones
        /// </summary>
        /// <param name="config">Configuración de bases de datos</param>
        /// <param name="poolSize">Tamaño máximo del pool por conexión</param>
        /// <param name="connectionTimeoutSeconds">Timeout de conexión en segundos</param>
        /// <param name="connectionLifetimeMinutes">Tiempo de vida de conexión en minutos</param>
        /// <param name="cleanupIntervalMinutes">Intervalo de limpieza en minutos</param>
        public ConnectionPool(
            MultiDatabaseConfig config = null,
            int poolSize = 10,
            int connectionTimeoutSeconds = 30,
            int connectionLifetimeMinutes = 10,
            int cleanupIntervalMinutes = 5)
        {
            _config = config ?? SettingsProvider.DatabaseConfig;
            _poolSize = poolSize;
            _connectionTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds);
            _connectionLifetime = TimeSpan.FromMinutes(connectionLifetimeMinutes);
            _connectionPool = new ConcurrentDictionary<string, ConcurrentQueue<SqlConnection>>();

            // Iniciar timer de limpieza
            _cleanupTimer = new Timer(CleanupPoolCallback, null,
                TimeSpan.FromMinutes(cleanupIntervalMinutes),
                TimeSpan.FromMinutes(cleanupIntervalMinutes));
        }

        /// <summary>
        /// Obtiene una conexión del pool
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>Conexión SQL</returns>
        public SqlConnection GetConnection(string connectionName = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionPool));

            var settings = _config.GetConnection(connectionName) ?? _config.GetDefaultConnection();

            if (settings == null)
                throw new InvalidOperationException($"No se encontró la configuración de conexión '{connectionName ?? "predeterminada"}'");

            string key = GetPoolKey(settings);

            // Intentar obtener del pool
            var queue = _connectionPool.GetOrAdd(key, _ => new ConcurrentQueue<SqlConnection>());

            SqlConnection connection;
            while (queue.TryDequeue(out connection))
            {
                // Verificar si la conexión sigue siendo utilizable
                if (IsConnectionValid(connection))
                    return connection;

                // Descartar conexión inválida
                SafeCloseAndDispose(connection);
            }

            // Crear nueva conexión
            connection = new SqlConnection(settings.ConnectionString);

            try
            {
                // Configurar timeout
                var previousTimeout = connection.ConnectionTimeout;
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(settings.ConnectionString)
                {
                    ConnectTimeout = (int)_connectionTimeout.TotalSeconds
                };
                connection.ConnectionString = builder.ConnectionString;

                // Abrir conexión
                connection.Open();

                // Restaurar timeout original
                builder.ConnectTimeout = previousTimeout;
                connection.ConnectionString = builder.ConnectionString;

                return connection;
            }
            catch
            {
                SafeCloseAndDispose(connection);
                throw;
            }
        }

        /// <summary>
        /// Devuelve una conexión al pool para su reutilización
        /// </summary>
        /// <param name="connection">Conexión a devolver</param>
        /// <param name="connectionName">Nombre de la conexión (opcional)</param>
        public void ReturnConnection(SqlConnection connection, string connectionName = null)
        {
            if (_disposed)
                return;

            if (connection == null)
                return;

            try
            {
                // Verificar si la conexión es válida
                if (!IsConnectionValid(connection))
                {
                    SafeCloseAndDispose(connection);
                    return;
                }

                // Obtener configuración
                var settings = _config.GetConnection(connectionName) ?? _config.GetDefaultConnection();
                if (settings == null)
                {
                    SafeCloseAndDispose(connection);
                    return;
                }

                string key = GetPoolKey(settings);
                var queue = _connectionPool.GetOrAdd(key, _ => new ConcurrentQueue<SqlConnection>());

                // Verificar tamaño del pool
                if (queue.Count >= _poolSize)
                {
                    SafeCloseAndDispose(connection);
                    return;
                }

                // Limpiar la conexión
                if (connection.State != ConnectionState.Open)
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch
                        {
                            SafeCloseAndDispose(connection);
                            return;
                        }
                    }

                // Eliminar transacciones activas
                try
                {
                    var transaction = connection.BeginTransaction();
                    transaction.Rollback();
                }
                catch
                {
                    // Ignorar errores, podría no soportar transacciones
                }

                // Limpiar parámetros de comando
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1";
                        cmd.ExecuteScalar();
                    }
                }
                catch
                {
                    SafeCloseAndDispose(connection);
                    return;
                }

                // Agregar al pool
                queue.Enqueue(connection);
            }
            catch
            {
                SafeCloseAndDispose(connection);
            }
        }

        /// <summary>
        /// Verifica si una conexión es válida
        /// </summary>
        private bool IsConnectionValid(SqlConnection connection)
        {
            if (connection == null || connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
                return false;

            // Verificar tiempo de vida
            if (_connectionLifetime > TimeSpan.Zero)
            {
                // Verificamos usando una consulta simple para comprobar que la conexión funciona
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1";
                        cmd.CommandTimeout = 5; // Timeout corto para prueba
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Limpia conexiones expiradas del pool
        /// </summary>
        private void CleanupPoolCallback(object state)
        {
            if (_disposed)
                return;

            foreach (var queue in _connectionPool.Values)
            {
                // Crear una lista temporal para mantener conexiones válidas
                var validConnections = new ConcurrentQueue<SqlConnection>();

                SqlConnection connection;
                while (queue.TryDequeue(out connection))
                {
                    if (IsConnectionValid(connection))
                    {
                        validConnections.Enqueue(connection);
                    }
                    else
                    {
                        SafeCloseAndDispose(connection);
                    }
                }

                // Devolver conexiones válidas al pool
                while (validConnections.TryDequeue(out connection))
                {
                    queue.Enqueue(connection);
                }
            }
        }

        /// <summary>
        /// Cierra y dispone una conexión de forma segura
        /// </summary>
        private void SafeCloseAndDispose(SqlConnection connection)
        {
            if (connection == null)
                return;

            try
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();

                connection.Dispose();
            }
            catch
            {
                // Ignorar errores al cerrar/disponer
            }
        }

        /// <summary>
        /// Obtiene una clave única para el pool basada en la configuración
        /// </summary>
        private string GetPoolKey(ConnectionSettings settings)
        {
            return settings.ConnectionString;
        }

        /// <summary>
        /// Dispone todos los recursos
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispone los recursos
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Detener timer
                _cleanupTimer?.Dispose();

                // Cerrar y disponer todas las conexiones
                foreach (var queue in _connectionPool.Values)
                {
                    SqlConnection connection;
                    while (queue.TryDequeue(out connection))
                    {
                        SafeCloseAndDispose(connection);
                    }
                }

                _connectionPool.Clear();
            }

            _disposed = true;
        }
    }
}