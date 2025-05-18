using NexusLink.Core.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink
{
    public class ConnectionPoolManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<SqlConnection>> _pooledConnections
            = new ConcurrentDictionary<string, ConcurrentQueue<SqlConnection>>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLimits
            = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly MultiDatabaseConfig _config;

        public ConnectionPoolManager(MultiDatabaseConfig config)
        {
            _config = config;
            // Inicializar semáforos para cada conexión
            foreach (var connection in _config.Connections)
            {
                _pooledConnections[connection.Name] = new ConcurrentQueue<SqlConnection>();
                _connectionLimits[connection.Name] = new SemaphoreSlim(
                    connection.MaxPoolSize, connection.MaxPoolSize);
            }
        }

        public async Task<SqlConnection> GetConnectionAsync(string connectionName)
        {
            var semaphore = _connectionLimits[connectionName];
            await semaphore.WaitAsync();

            try
            {
                if (_pooledConnections[connectionName].TryDequeue(out SqlConnection connection))
                {
                    // Verificar que la conexión siga abierta
                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }
                    return connection;
                }

                // Crear nueva conexión
                var config = _config.GetConnection(connectionName);
                var newConnection = new SqlConnection(config.ConnectionString);
                await newConnection.OpenAsync();
                return newConnection;
            }
            catch
            {
                semaphore.Release();
                throw;
            }
        }

        public void ReturnConnection(string connectionName, SqlConnection connection)
        {
            if (connection.State == ConnectionState.Open)
            {
                _pooledConnections[connectionName].Enqueue(connection);
            }
            else
            {
                connection.Dispose();
            }

            _connectionLimits[connectionName].Release();
        }
    }
}
