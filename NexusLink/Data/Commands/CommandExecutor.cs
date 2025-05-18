using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Logging;

namespace NexusLink.Data.Commands
{
    /// <summary>
    /// Ejecuta comandos SQL y procedimientos almacenados
    /// </summary>
    public class CommandExecutor
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;

        public CommandExecutor(ILogger logger, ConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Ejecuta un comando SQL que no devuelve resultados
        /// </summary>
        public int ExecuteNonQuery(string commandText, params DbParameter[] parameters)
        {
            return ExecuteNonQuery(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que no devuelve resultados
        /// </summary>
        public int ExecuteNonQuery(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command, commandText, commandType, parameters);

                    try
                    {
                        _logger.Debug($"Ejecutando comando: {commandText}");
                        return command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar comando: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un valor escalar
        /// </summary>
        public object ExecuteScalar(string commandText, params DbParameter[] parameters)
        {
            return ExecuteScalar(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un valor escalar
        /// </summary>
        public object ExecuteScalar(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command, commandText, commandType, parameters);

                    try
                    {
                        _logger.Debug($"Ejecutando comando escalar: {commandText}");
                        return command.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar comando escalar: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un valor escalar convertido a un tipo específico
        /// </summary>
        public T ExecuteScalar<T>(string commandText, params DbParameter[] parameters)
        {
            return ExecuteScalar<T>(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un valor escalar convertido a un tipo específico
        /// </summary>
        public T ExecuteScalar<T>(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            object result = ExecuteScalar(commandText, commandType, parameters);

            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataReader
        /// </summary>
        public DbDataReader ExecuteReader(string commandText, params DbParameter[] parameters)
        {
            return ExecuteReader(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataReader
        /// </summary>
        public DbDataReader ExecuteReader(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            var connection = _connectionFactory.CreateConnection();
            connection.Open();

            try
            {
                var command = connection.CreateCommand();
                PrepareCommand(command, commandText, commandType, parameters);

                _logger.Debug($"Ejecutando comando reader: {commandText}");

                // Nota: CommandBehavior.CloseConnection cerrará la conexión cuando se cierre el reader
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                connection.Close();
                connection.Dispose();
                _logger.Error($"Error al ejecutar comando reader: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataSet
        /// </summary>
        public DataSet ExecuteDataSet(string commandText, params DbParameter[] parameters)
        {
            return ExecuteDataSet(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataSet
        /// </summary>
        public DataSet ExecuteDataSet(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command, commandText, commandType, parameters);

                    try
                    {
                        _logger.Debug($"Ejecutando comando dataset: {commandText}");

                        var dataSet = new DataSet();
                        var dbFactory = DbProviderFactories.GetFactory(connection);
                        var adapter = dbFactory.CreateDataAdapter();
                        adapter.SelectCommand = command;
                        adapter.Fill(dataSet);

                        return dataSet;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar comando dataset: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataTable
        /// </summary>
        public DataTable ExecuteDataTable(string commandText, params DbParameter[] parameters)
        {
            return ExecuteDataTable(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL que devuelve un DataTable
        /// </summary>
        public DataTable ExecuteDataTable(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            var dataSet = ExecuteDataSet(commandText, commandType, parameters);

            if (dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }

            return new DataTable();
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que no devuelve resultados
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string commandText, params DbParameter[] parameters)
        {
            return await ExecuteNonQueryAsync(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que no devuelve resultados
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command, commandText, commandType, parameters);

                    try
                    {
                        _logger.Debug($"Ejecutando comando async: {commandText}");
                        return await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar comando async: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un valor escalar
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string commandText, params DbParameter[] parameters)
        {
            return await ExecuteScalarAsync(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un valor escalar
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command, commandText, commandType, parameters);

                    try
                    {
                        _logger.Debug($"Ejecutando comando escalar async: {commandText}");
                        return await command.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error al ejecutar comando escalar async: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un valor escalar convertido a un tipo específico
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string commandText, params DbParameter[] parameters)
        {
            return await ExecuteScalarAsync<T>(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un valor escalar convertido a un tipo específico
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            object result = await ExecuteScalarAsync(commandText, commandType, parameters);

            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un DataReader
        /// </summary>
        public async Task<DbDataReader> ExecuteReaderAsync(string commandText, params DbParameter[] parameters)
        {
            return await ExecuteReaderAsync(commandText, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta un comando SQL de forma asíncrona que devuelve un DataReader
        /// </summary>
        public async Task<DbDataReader> ExecuteReaderAsync(string commandText, CommandType commandType, params DbParameter[] parameters)
        {
            var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            try
            {
                var command = connection.CreateCommand();
                PrepareCommand(command, commandText, commandType, parameters);

                _logger.Debug($"Ejecutando comando reader async: {commandText}");

                // Nota: CommandBehavior.CloseConnection cerrará la conexión cuando se cierre el reader
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                connection.Close();
                connection.Dispose();
                _logger.Error($"Error al ejecutar comando reader async: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Prepara un comando con texto, tipo y parámetros
        /// </summary>
        private void PrepareCommand(DbCommand command, string commandText, CommandType commandType, DbParameter[] parameters)
        {
            command.CommandText = commandText;
            command.CommandType = commandType;

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
        }
    }
}