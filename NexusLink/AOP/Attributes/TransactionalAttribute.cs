using NexusLink.AOP.Interception;
using NexusLink.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Attribute that applies transaction semantics to methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionalAttribute : InterceptAttribute
    {
        /// <summary>
        /// The isolation level for the transaction
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// The connection name from configuration
        /// </summary>
        public string ConnectionName { get; set; } = "Default";

        /// <summary>
        /// Whether to retry on transient errors
        /// </summary>
        public bool RetryOnTransientErrors { get; set; } = false;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        public override IMethodInterceptor CreateInterceptor()
        {
            return new TransactionalInterceptor(this);
        }

        private class TransactionalInterceptor : MethodInterceptor
        {
            private readonly TransactionalAttribute _attribute;
            private static readonly HashSet<int> _transientErrorNumbers = new HashSet<int> {
        -2, 40613, 40197, 40501, 49918, 40549, 40550, 1205
    };

            public TransactionalInterceptor(TransactionalAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Get connection
                string connectionString = ConfigManager.GetConnectionString(_attribute.ConnectionName);

                // Determine if we need to retry
                if (_attribute.RetryOnTransientErrors)
                {
                    return ExecuteWithRetry(invocation, connectionString);
                }
                else
                {
                    return ExecuteOnce(invocation, connectionString);
                }
            }

            private object ExecuteWithRetry(IMethodInvocation invocation, string connectionString)
            {
                int attempts = 0;
                while (true)
                {
                    try
                    {
                        attempts++;
                        return ExecuteOnce(invocation, connectionString);
                    }
                    catch (SqlException ex) when (IsTransientError(ex) && attempts < _attribute.MaxRetryAttempts)
                    {
                        // Log retry attempt
                        NexusTraceAdapter.LogWarning(
                            $"Transient SQL error {ex.Number} detected, retrying ({attempts}/{_attribute.MaxRetryAttempts})");

                        // Wait with exponential backoff
                        int delayMs = (int)Math.Pow(2, attempts) * 100;
                        Thread.Sleep(delayMs);
                    }
                }
            }

            private object ExecuteOnce(IMethodInvocation invocation, string connectionString)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction = connection.BeginTransaction(_attribute.IsolationLevel))
                    {
                        try
                        {
                            // Store connection and transaction in the invocation context
                            invocation.Data["Connection"] = connection;
                            invocation.Data["Transaction"] = transaction;

                            // Proceed with the method execution
                            object result = invocation.Proceed();

                            // Commit transaction if successful
                            transaction.Commit();

                            return result;
                        }
                        catch
                        {
                            // Rollback on any exception
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }

            private bool IsTransientError(SqlException ex)
            {
                return _transientErrorNumbers.Contains(ex.Number);
            }
        }
    }
}