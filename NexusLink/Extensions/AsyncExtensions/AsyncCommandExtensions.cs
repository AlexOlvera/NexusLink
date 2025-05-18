using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Extensions.AsyncExtensions
{
    public static class AsyncCommandExtensions
    {
        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona con reintentos
        /// </summary>
        public static async Task<int> ExecuteNonQueryWithRetryAsync(this SqlCommand command,
            int retryCount, int retryDelayMs, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count must be greater than or equal to zero");

            if (retryDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(retryDelayMs), "Retry delay must be greater than or equal to zero");

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    return await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex) when (IsTransientError(ex.Number))
                {
                    lastException = ex;

                    if (retry < retryCount)
                    {
                        await Task.Delay(retryDelayMs, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona y devuelve un valor escalar con reintentos
        /// </summary>
        public static async Task<T> ExecuteScalarWithRetryAsync<T>(this SqlCommand command,
            int retryCount, int retryDelayMs, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count must be greater than or equal to zero");

            if (retryDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(retryDelayMs), "Retry delay must be greater than or equal to zero");

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    var result = await command.ExecuteScalarAsync(cancellationToken);

                    if (result == null || result == DBNull.Value)
                        return default(T);

                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch (SqlException ex) when (IsTransientError(ex.Number))
                {
                    lastException = ex;

                    if (retry < retryCount)
                    {
                        await Task.Delay(retryDelayMs, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Ejecuta un lote de comandos SQL de forma asíncrona dentro de una transacción
        /// </summary>
        public static async Task<int> ExecuteBatchAsync(this SqlConnection connection,
            IEnumerable<string> commandTexts, CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (commandTexts == null)
                throw new ArgumentNullException(nameof(commandTexts));

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int totalRowsAffected = 0;

                    foreach (var commandText in commandTexts)
                    {
                        using (var command = new SqlCommand(commandText, connection, transaction))
                        {
                            totalRowsAffected += await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }

                    transaction.Commit();
                    return totalRowsAffected;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Verifica si un código de error de SQL Server es un error transitorio
        /// </summary>
        private static bool IsTransientError(int errorNumber)
        {
            // Estos son algunos de los códigos de error transitorio más comunes en SQL Server
            int[] transientErrors =
            {
                -2, // Timeout
                4060, // Cannot open database
                40197, // Error processing request
                40501, // Service is busy
                40613, // Database unavailable
                49918, // Cannot process request
                49919, // Cannot process create or update request
                49920, // Service is busy
                4221, // Login to read-secondary failed
                18456, // Login failed
                64, // A connection was successfully established with the server, but then an error occurred during the login process
                233, // No process is on the other end of the pipe
                10053, // A transport-level error has occurred when receiving results from the server
                10054, // An existing connection was forcibly closed by the remote host
                10060, // A connection attempt failed because the connected party did not properly respond after a period of time
                40143, // Connection could not be initialized
                40613, // Database is currently unavailable
                17
            };

            return Array.IndexOf(transientErrors, errorNumber) >= 0;
        }
    }
}