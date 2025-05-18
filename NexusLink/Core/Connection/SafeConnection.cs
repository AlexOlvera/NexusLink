using System;
using System.Data;
using System.Data.SqlClient;
using NexusLink.Core.Configuration;

namespace NexusLink.Core.Connection
{
    /// <summary>
    /// Envoltorio seguro para conexiones que garantiza su cierre adecuado
    /// </summary>
    public class SafeConnection : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly bool _ownsConnection;
        private SqlTransaction _currentTransaction;
        private bool _disposed;

        /// <summary>
        /// Inicializa una nueva instancia con una cadena de conexión
        /// </summary>
        /// <param name="connectionString">Cadena de conexión</param>
        public SafeConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("La cadena de conexión no puede estar vacía", nameof(connectionString));

            _connection = new SqlConnection(connectionString);
            _ownsConnection = true;
        }

        /// <summary>
        /// Inicializa una nueva instancia con una conexión existente
        /// </summary>
        /// <param name="connection">Conexión SQL</param>
        /// <param name="ownsConnection">Indica si debe cerrar la conexión al eliminar</param>
        public SafeConnection(SqlConnection connection, bool ownsConnection = false)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;
        }

        /// <summary>
        /// Inicializa una nueva instancia con un nombre de conexión
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        public SafeConnection(string connectionName, ConnectionFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _connection = factory.CreateSqlConnection(connectionName);
            _ownsConnection = true;
        }

        /// <summary>
        /// Conexión subyacente
        /// </summary>
        public SqlConnection Connection => _connection;

        /// <summary>
        /// Transacción actual
        /// </summary>
        public SqlTransaction CurrentTransaction => _currentTransaction;

        /// <summary>
        /// Abre la conexión si no está ya abierta
        /// </summary>
        public void EnsureOpen()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafeConnection));

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// Inicia una nueva transacción
        /// </summary>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        /// <returns>Transacción</returns>
        public SqlTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafeConnection));

            EnsureOpen();

            if (_currentTransaction != null)
                throw new InvalidOperationException("Ya hay una transacción activa");

            _currentTransaction = _connection.BeginTransaction(isolationLevel);
            return _currentTransaction;
        }

        /// <summary>
        /// Confirma la transacción actual
        /// </summary>
        public void CommitTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafeConnection));

            if (_currentTransaction == null)
                throw new InvalidOperationException("No hay una transacción activa");

            _currentTransaction.Commit();
            _currentTransaction = null;
        }

        /// <summary>
        /// Revierte la transacción actual
        /// </summary>
        public void RollbackTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafeConnection));

            if (_currentTransaction == null)
                throw new InvalidOperationException("No hay una transacción activa");

            _currentTransaction.Rollback();
            _currentTransaction = null;
        }

        /// <summary>
        /// Crea un comando SQL
        /// </summary>
        /// <param name="commandText">Texto del comando</param>
        /// <param name="commandType">Tipo de comando</param>
        /// <param name="parameters">Parámetros</param>
        /// <returns>Comando SQL</returns>
        public SqlCommand CreateCommand(
            string commandText = null,
            CommandType commandType = CommandType.Text,
            SqlParameter[] parameters = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafeConnection));

            EnsureOpen();

            var command = _connection.CreateCommand();

            if (!string.IsNullOrEmpty(commandText))
                command.CommandText = commandText;

            command.CommandType = commandType;

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            if (_currentTransaction != null)
                command.Transaction = _currentTransaction;

            return command;
        }

        /// <summary>
        /// Libera los recursos utilizados
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Libera los recursos utilizados
        /// </summary>
        /// <param name="disposing">Indica si es llamado desde Dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_currentTransaction != null)
                {
                    try
                    {
                        _currentTransaction.Rollback();
                    }
                    catch
                    {
                        // Ignorar errores al deshacer
                    }
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }

                if (_ownsConnection && _connection != null)
                {
                    if (_connection.State == ConnectionState.Open)
                        _connection.Close();

                    _connection.Dispose();
                }
            }

            _disposed = true;
        }
    }
}