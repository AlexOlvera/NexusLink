using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Logging;

namespace NexusLink.Core.Transactions
{
    /// <summary>
    /// Implementa el patrón Unit of Work para transacciones
    /// </summary>
    public class UnitOfWork : IDisposable
    {
        private readonly ILogger _logger;
        private readonly DatabaseSelector _databaseSelector;
        private readonly ConnectionFactory _connectionFactory;
        private readonly Dictionary<string, DbConnection> _connections;
        private readonly Dictionary<DbConnection, DbTransaction> _transactions;
        private bool _committed;
        private bool _disposed;

        public UnitOfWork(
            ILogger logger,
            DatabaseSelector databaseSelector,
            ConnectionFactory connectionFactory)
        {
            _logger = logger;
            _databaseSelector = databaseSelector;
            _connectionFactory = connectionFactory;
            _connections = new Dictionary<string, DbConnection>();
            _transactions = new Dictionary<DbConnection, DbTransaction>();
            _committed = false;
            _disposed = false;
        }

        /// <summary>
        /// Obtiene la conexión para la base de datos actual
        /// </summary>
        public DbConnection GetConnection()
        {
            return GetConnection(_databaseSelector.CurrentDatabaseName);
        }

        /// <summary>
        /// Obtiene la conexión para una base de datos específica
        /// </summary>
        public DbConnection GetConnection(string databaseName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork));
            }

            if (_connections.TryGetValue(databaseName, out DbConnection existingConnection))
            {
                return existingConnection;
            }

            // Cambiar temporalmente al contexto de la base de datos solicitada
            return _databaseSelector.ExecuteWith(databaseName, () =>
            {
                // Obtener una conexión del factory
                var connection = _connectionFactory.CreateConnection();

                // Asegurarse de que la conexión está abierta
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                // Iniciar una transacción para esta conexión
                var transaction = connection.BeginTransaction();

                // Registrar la conexión y su transacción
                _connections[databaseName] = connection;
                _transactions[connection] = transaction;

                _logger.Debug($"Conexión creada para base de datos {databaseName} en UnitOfWork");

                return connection;
            });
        }

        /// <summary>
        /// Obtiene la transacción para una conexión específica
        /// </summary>
        public DbTransaction GetTransaction(DbConnection connection)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork));
            }

            if (_transactions.TryGetValue(connection, out DbTransaction transaction))
            {
                return transaction;
            }

            throw new InvalidOperationException("La conexión no forma parte de esta unidad de trabajo");
        }

        /// <summary>
        /// Confirma todas las transacciones en esta unidad de trabajo
        /// </summary>
        public void Commit()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork));
            }

            if (_committed)
            {
                throw new InvalidOperationException("Esta unidad de trabajo ya ha sido confirmada");
            }

            try
            {
                // Confirmar cada transacción
                foreach (var transaction in _transactions.Values)
                {
                    transaction.Commit();
                }

                _committed = true;
                _logger.Info($"UnitOfWork confirmado: {_transactions.Count} transacciones");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error al confirmar UnitOfWork: {ex.Message}");

                // Intentar revertir las transacciones no confirmadas
                try
                {
                    foreach (var transaction in _transactions.Values.Where(t => t.Connection?.State == ConnectionState.Open))
                    {
                        transaction.Rollback();
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.Error($"Error al revertir transacciones después de falla: {rollbackEx.Message}");
                }

                throw; // Re-lanzar la excepción original
            }
        }

        /// <summary>
        /// Revierte todas las transacciones en esta unidad de trabajo
        /// </summary>
        public void Rollback()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork));
            }

            if (_committed)
            {
                throw new InvalidOperationException("Esta unidad de trabajo ya ha sido confirmada");
            }

            try
            {
                // Revertir cada transacción
                foreach (var transaction in _transactions.Values)
                {
                    if (transaction.Connection?.State == ConnectionState.Open)
                    {
                        transaction.Rollback();
                    }
                }

                _logger.Info($"UnitOfWork revertido: {_transactions.Count} transacciones");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error al revertir UnitOfWork: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Libera los recursos utilizados por esta unidad de trabajo
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Si no se ha confirmado, revertir automáticamente
                if (!_committed)
                {
                    try
                    {
                        Rollback();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error en rollback automático durante Dispose: {ex.Message}");
                    }
                }

                // Dispose transacciones
                foreach (var transaction in _transactions.Values)
                {
                    try
                    {
                        transaction.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al liberar transacción: {ex.Message}");
                    }
                }

                // Cerrar y dispose conexiones
                foreach (var connection in _connections.Values)
                {
                    try
                    {
                        if (connection.State != ConnectionState.Closed)
                        {
                            connection.Close();
                        }
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al liberar conexión: {ex.Message}");
                    }
                }

                _transactions.Clear();
                _connections.Clear();
            }

            _disposed = true;
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}