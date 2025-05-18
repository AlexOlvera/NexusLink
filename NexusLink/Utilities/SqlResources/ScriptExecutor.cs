using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using NexusLink.Logging;

namespace NexusLink.Utilities.SqlResources
{
    /// <summary>
    /// Ejecutor de scripts SQL
    /// </summary>
    public class ScriptExecutor
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        // Patrón para dividir scripts SQL por GO
        private static readonly Regex _batchSeparator = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        /// <summary>
        /// Constructor con cadena de conexión y logger
        /// </summary>
        public ScriptExecutor(string connectionString, ILogger logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ejecuta un script SQL
        /// </summary>
        public void ExecuteScript(string script)
        {
            Guard.NotNull(script, nameof(script));

            _logger.Debug("Ejecutando script SQL...");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string[] batches = _batchSeparator.Split(script);

                foreach (string batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    using (var command = new SqlCommand(batch, connection))
                    {
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error al ejecutar script SQL: {ex.Message}");
                            throw;
                        }
                    }
                }

                _logger.Debug("Script SQL ejecutado correctamente.");
            }
        }

        /// <summary>
        /// Ejecuta un script SQL y devuelve un DataSet
        /// </summary>
        public DataSet ExecuteScriptWithResults(string script)
        {
            Guard.NotNull(script, nameof(script));

            _logger.Debug("Ejecutando script SQL con resultados...");

            var dataSet = new DataSet();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string[] batches = _batchSeparator.Split(script);

                foreach (string batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    using (var command = new SqlCommand(batch, connection))
                    {
                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dataSet);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error al ejecutar script SQL: {ex.Message}");
                            throw;
                        }
                    }
                }

                _logger.Debug($"Script SQL ejecutado correctamente. Se obtuvieron {dataSet.Tables.Count} tablas.");
            }

            return dataSet;
        }

        /// <summary>
        /// Ejecuta scripts SQL en una transacción
        /// </summary>
        public void ExecuteScriptsInTransaction(IEnumerable<string> scripts)
        {
            Guard.NotNull(scripts, nameof(scripts));

            _logger.Debug("Ejecutando scripts SQL en transacción...");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (string script in scripts)
                        {
                            if (string.IsNullOrWhiteSpace(script))
                                continue;

                            string[] batches = _batchSeparator.Split(script);

                            foreach (string batch in batches)
                            {
                                if (string.IsNullOrWhiteSpace(batch))
                                    continue;

                                using (var command = new SqlCommand(batch, connection, transaction))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        _logger.Debug("Transacción completada correctamente.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.Error($"Error al ejecutar scripts SQL en transacción: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado
        /// </summary>
        public object ExecuteStoredProcedure(string procedureName, IDictionary<string, object> parameters = null)
        {
            Guard.NotNullOrEmpty(procedureName, nameof(procedureName));

            _logger.Debug($"Ejecutando procedimiento almacenado '{procedureName}'...");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    try
                    {
                        var result = command.ExecuteScalar();
                        _logger.Debug($"Procedimiento almacenado '{procedureName}' ejecutado correctamente.");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar procedimiento almacenado '{procedureName}': {ex.Message}");
                        throw;
                    }
                }
            }
        }
    }
}