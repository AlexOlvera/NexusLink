using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Transactions;
using NexusLink.Core.Connection;
using NexusLink.Logging;
using IsolationLevel = System.Data.IsolationLevel;

namespace NexusLink.Core.Transactions
{
    /// <summary>
    /// Gestiona transacciones distribuidas y locales
    /// </summary>
    public class TransactionManager
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly AsyncLocal<IsolationLevel> _currentIsolationLevel = new AsyncLocal<IsolationLevel>();

        public TransactionManager(
            ILogger logger,
            ConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _currentIsolationLevel.Value = IsolationLevel.ReadCommitted; // Nivel predeterminado
        }

        /// <summary>
        /// Obtiene o establece el nivel de aislamiento actual
        /// </summary>
        public IsolationLevel CurrentIsolationLevel
        {
            get => _currentIsolationLevel.Value;
            set => _currentIsolationLevel.Value = value;
        }

        /// <summary>
        /// Crea un ámbito de transacción
        /// </summary>
        public TransactionScope CreateTransactionScope()
        {
            return CreateTransactionScope(CurrentIsolationLevel);
        }

        /// <summary>
        /// Crea un ámbito de transacción con un nivel de aislamiento específico
        /// </summary>
        public TransactionScope CreateTransactionScope(IsolationLevel isolationLevel)
        {
            var options = new TransactionOptions
            {
                IsolationLevel = ConvertToSystemTransactionsIsolationLevel(isolationLevel),
                Timeout = TimeSpan.FromMinutes(5) // Timeout predeterminado
            };

            _logger.Debug($"Creando TransactionScope con nivel de aislamiento: {isolationLevel}");
            return new TransactionScope(TransactionScopeOption.Required, options);
        }

        /// <summary>
        /// Ejecuta una acción dentro de una transacción
        /// </summary>
        public void ExecuteInTransaction(Action action)
        {
            ExecuteInTransaction(action, CurrentIsolationLevel);
        }

        /// <summary>
        /// Ejecuta una acción dentro de una transacción con un nivel de aislamiento específico
        /// </summary>
        public void ExecuteInTransaction(Action action, IsolationLevel isolationLevel)
        {
            using (var scope = CreateTransactionScope(isolationLevel))
            {
                try
                {
                    action();
                    scope.Complete();
                    _logger.Debug("Transacción completada exitosamente");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error en transacción: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Ejecuta una función dentro de una transacción
        /// </summary>
        public T ExecuteInTransaction<T>(Func<T> func)
        {
            return ExecuteInTransaction(func, CurrentIsolationLevel);
        }

        /// <summary>
        /// Ejecuta una función dentro de una transacción con un nivel de aislamiento específico
        /// </summary>
        public T ExecuteInTransaction<T>(Func<T> func, IsolationLevel isolationLevel)
        {
            using (var scope = CreateTransactionScope(isolationLevel))
            {
                try
                {
                    T result = func();
                    scope.Complete();
                    _logger.Debug("Transacción completada exitosamente");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error en transacción: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Enlaza una conexión a la transacción actual
        /// </summary>
        public void EnlistConnection(DbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (Transaction.Current != null)
            {
                var promotable = connection as IPromotableSinglePhaseNotification;
                if (promotable != null)
                {
                    _logger.Debug("Enlistando conexión con soporte para promoción");
                }
                else
                {
                    _logger.Debug("Enlistando conexión sin soporte para promoción");
                }

                connection.EnlistTransaction(Transaction.Current);
            }
            else
            {
                _logger.Warning("No hay una transacción actual a la que enlistar la conexión");
            }
        }

        /// <summary>
        /// Convierte un nivel de aislamiento de ADO.NET a System.Transactions
        /// </summary>
        private System.Transactions.IsolationLevel ConvertToSystemTransactionsIsolationLevel(IsolationLevel isolationLevel)
        {
            switch (isolationLevel)
            {
                case IsolationLevel.Chaos:
                    return System.Transactions.IsolationLevel.Chaos;
                case IsolationLevel.ReadCommitted:
                    return System.Transactions.IsolationLevel.ReadCommitted;
                case IsolationLevel.ReadUncommitted:
                    return System.Transactions.IsolationLevel.ReadUncommitted;
                case IsolationLevel.RepeatableRead:
                    return System.Transactions.IsolationLevel.RepeatableRead;
                case IsolationLevel.Serializable:
                    return System.Transactions.IsolationLevel.Serializable;
                case IsolationLevel.Snapshot:
                    return System.Transactions.IsolationLevel.Snapshot;
                case IsolationLevel.Unspecified:
                default:
                    return System.Transactions.IsolationLevel.Unspecified;
            }
        }
    }
}