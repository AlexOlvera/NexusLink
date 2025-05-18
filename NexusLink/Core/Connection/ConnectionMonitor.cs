using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using NexusLink.Logging;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Monitorea conexiones a bases de datos para diagnóstico y optimización
    /// </summary>
    public class ConnectionMonitor
    {
        private readonly ILogger _logger;
        private static readonly Dictionary<string, ConnectionStatistics> _statistics = new Dictionary<string, ConnectionStatistics>();
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public ConnectionMonitor(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registra el inicio de una operación de conexión
        /// </summary>
        public void BeginConnection(DbConnection connection, string operationName)
        {
            string connectionId = GetConnectionId(connection);

            _lock.EnterWriteLock();
            try
            {
                if (!_statistics.ContainsKey(connectionId))
                {
                    _statistics[connectionId] = new ConnectionStatistics
                    {
                        ConnectionString = MaskConnectionString(connection.ConnectionString),
                        ServerName = connection.DataSource,
                        DatabaseName = connection.Database
                    };
                }

                _statistics[connectionId].ActiveOperations++;
                _statistics[connectionId].TotalOperations++;
                _statistics[connectionId].LastOperation = operationName;
                _statistics[connectionId].LastActivityTime = DateTime.UtcNow;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _logger.Debug($"Begin connection operation: {operationName} on {connection.Database}");
        }

        /// <summary>
        /// Registra el fin de una operación de conexión
        /// </summary>
        public void EndConnection(DbConnection connection, string operationName, TimeSpan duration)
        {
            string connectionId = GetConnectionId(connection);

            _lock.EnterWriteLock();
            try
            {
                if (_statistics.ContainsKey(connectionId))
                {
                    _statistics[connectionId].ActiveOperations--;
                    _statistics[connectionId].TotalDuration += duration;

                    if (duration > _statistics[connectionId].LongestDuration)
                    {
                        _statistics[connectionId].LongestDuration = duration;
                        _statistics[connectionId].LongestOperation = operationName;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _logger.Debug($"End connection operation: {operationName} on {connection.Database}, duration: {duration.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Registra un error en una operación de conexión
        /// </summary>
        public void RecordError(DbConnection connection, string operationName, Exception exception)
        {
            string connectionId = GetConnectionId(connection);

            _lock.EnterWriteLock();
            try
            {
                if (_statistics.ContainsKey(connectionId))
                {
                    _statistics[connectionId].ErrorCount++;
                    _statistics[connectionId].LastError = exception.Message;
                    _statistics[connectionId].LastErrorTime = DateTime.UtcNow;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            _logger.Error($"Connection error: {operationName} on {connection.Database}: {exception.Message}");
        }

        /// <summary>
        /// Obtiene estadísticas de todas las conexiones monitoreadas
        /// </summary>
        public Dictionary<string, ConnectionStatistics> GetStatistics()
        {
            Dictionary<string, ConnectionStatistics> result = new Dictionary<string, ConnectionStatistics>();

            _lock.EnterReadLock();
            try
            {
                foreach (var kvp in _statistics)
                {
                    result[kvp.Key] = kvp.Value.Clone();
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// Limpia todas las estadísticas (solo para pruebas)
        /// </summary>
        public void ClearStatistics()
        {
            _lock.EnterWriteLock();
            try
            {
                _statistics.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private string GetConnectionId(DbConnection connection)
        {
            return $"{connection.DataSource}-{connection.Database}-{connection.GetHashCode()}";
        }

        private string MaskConnectionString(string connectionString)
        {
            // Implementación simplificada: oculta contraseñas
            return connectionString.Replace(
                System.Text.RegularExpressions.Regex.Match(
                    connectionString,
                    @"Password\s*=\s*[^;]*",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .Value,
                "Password=******");
        }
    }

    /// <summary>
    /// Estadísticas de una conexión de base de datos
    /// </summary>
    public class ConnectionStatistics
    {
        public string ConnectionString { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public int ActiveOperations { get; set; }
        public long TotalOperations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan LongestDuration { get; set; }
        public string LongestOperation { get; set; }
        public string LastOperation { get; set; }
        public DateTime LastActivityTime { get; set; }
        public int ErrorCount { get; set; }
        public string LastError { get; set; }
        public DateTime LastErrorTime { get; set; }

        public ConnectionStatistics Clone()
        {
            return new ConnectionStatistics
            {
                ConnectionString = this.ConnectionString,
                ServerName = this.ServerName,
                DatabaseName = this.DatabaseName,
                ActiveOperations = this.ActiveOperations,
                TotalOperations = this.TotalOperations,
                TotalDuration = this.TotalDuration,
                LongestDuration = this.LongestDuration,
                LongestOperation = this.LongestOperation,
                LastOperation = this.LastOperation,
                LastActivityTime = this.LastActivityTime,
                ErrorCount = this.ErrorCount,
                LastError = this.LastError,
                LastErrorTime = this.LastErrorTime
            };
        }
    }
}