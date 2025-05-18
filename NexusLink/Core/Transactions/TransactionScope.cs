using System;
using System.Data;
using System.Data.SqlClient;
using NexusLink.Core.Connection;

namespace NexusLink.Core.Transactions
{
    /// <summary>
    /// Proporciona un ámbito transaccional para operaciones de base de datos
    /// </summary>
    public class TransactionScope : IDisposable
    {
        private readonly SafeConnection _connection;
        private readonly IsolationLevel _isolationLevel;
        private readonly bool _ownsConnection;
        private SqlTransaction _transaction;
        private bool _completed;
        private bool _disposed;

        /// <summary>
        /// Inicializa una nueva instancia con una conexión existente
        /// </summary>
        /// <param name="connection">Conexión a utilizar</param>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        /// <param name="ownsConnection">Indica si debe cerrar la conexión al eliminar</param>
        public TransactionScope(
            SafeConnection connection,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool ownsConnection = false)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _isolationLevel = isolationLevel;
            _ownsConnection = ownsConnection;

            // Iniciar transacción
            _transaction = _connection.BeginTransaction(_isolationLevel);
        }

        /// <summary>
        /// Inicializa una nueva instancia con una conexión existente
        /// </summary>
        /// <param name="connection">Conexión a utilizar</param>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        public TransactionScope(
            SqlConnection connection,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _connection = new SafeConnection(connection);
            _isolationLevel = isolationLevel;
            _ownsConnection = false;

            // Iniciar transacción
            _transaction = _connection.BeginTransaction(_isolationLevel);
        }

        /// <summary>
        /// Inicializa una nueva instancia con un nombre de conexión
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        public TransactionScope(
            string connectionName = null,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateSqlConnection(connectionName);

            _connection = new SafeConnection(connection, true);
            _isolationLevel = isolationLevel;
            _ownsConnection = true;

            // Iniciar transacción
            _transaction = _connection.BeginTransaction(_isolationLevel);
        }

        /// <summary>
        /// Transacción subyacente
        /// </summary>
        public SqlTransaction Transaction => _transaction;

        /// <summary>
        /// Conexión subyacente
        /// </summary>
        public SqlConnection Connection => _connection.Connection;

        /// <summary>
        /// Completa la transacción realizando commit
        /// </summary>
        public void Complete()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TransactionScope));

            if (_transaction == null)
                throw new InvalidOperationException("La transacción ya ha sido completada o revertida");

            _transaction.Commit();
            _transaction = null;
            _completed = true;
        }

        /// <summary>
        /// Dispone los recursos
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispone los recursos
        /// </summary>
        /// <param name="disposing">Indica si es llamado desde Dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_transaction != null)
                {
                    try
                    {
                        if (!_completed)
                        {
                            _transaction.Rollback();
                        }
                    }
                    catch
                    {
                        // Ignorar errores al deshacer
                    }
                    finally
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }
                }

                if (_ownsConnection)
                {
                    _connection.Dispose();
                }
            }

            _disposed = true;
        }
    }
}