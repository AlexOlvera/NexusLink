using NexusLink.Core.Connection;
using NexusLink.Data.Queries;
using NexusLink.Dynamic.Expando;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink
{
    /// <summary>
    /// Clase para ejecutar consultas dinámicas
    /// </summary>
    public class DynamicQuery
    {
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// Inicializa una nueva instancia de DynamicQuery
        /// </summary>
        /// <param name="connectionFactory">Fábrica de conexiones</param>
        public DynamicQuery(ConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new ConnectionFactory();
        }

        /// <summary>
        /// Ejecuta una consulta y devuelve una lista de objetos dinámicos
        /// </summary>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>Lista de objetos dinámicos</returns>
        public List<dynamic> ExecuteList(string sql, object parameters = null, string connectionName = null)
        {
            using (var connection = _connectionFactory.CreateSqlConnection(connectionName))
            {
                connection.Open();

                using (var command = CreateCommand(connection, sql, parameters))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.ToDynamicList();
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta y devuelve una lista de objetos dinámicos de forma asíncrona
        /// </summary>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de objetos dinámicos</returns>
        public async Task<List<dynamic>> ExecuteListAsync(string sql, object parameters = null, string connectionName = null, CancellationToken cancellationToken = default)
        {
            using (var connection = _connectionFactory.CreateSqlConnection(connectionName))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var command = CreateCommand(connection, sql, parameters))
                {
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return await reader.ToDynamicListAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta y devuelve un único objeto dinámico
        /// </summary>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>Objeto dinámico o null si no hay resultados</returns>
        public dynamic ExecuteSingle(string sql, object parameters = null, string connectionName = null)
        {
            using (var connection = _connectionFactory.CreateSqlConnection(connectionName))
            {
                connection.Open();

                using (var command = CreateCommand(connection, sql, parameters))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToExpando();
                        }

                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta y devuelve un único objeto dinámico de forma asíncrona
        /// </summary>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Objeto dinámico o null si no hay resultados</returns>
        public async Task<dynamic> ExecuteSingleAsync(string sql, object parameters = null, string connectionName = null, CancellationToken cancellationToken = default)
        {
            using (var connection = _connectionFactory.CreateSqlConnection(connectionName))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var command = CreateCommand(connection, sql, parameters))
                {
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            return reader.ToExpando();
                        }

                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta y devuelve un DataTable
        /// </summary>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>DataTable con los resultados</returns>
        public DataTable ExecuteDataTable(string sql, object parameters = null, string connectionName = null)
        {
            using (var connection = _connectionFactory.CreateSqlConnection(connectionName))
            {
                connection.Open();

                using (var command = CreateCommand(connection, sql, parameters))
                {
                    var dataTable = new DataTable();
                    var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataTable);
                    return dataTable;
                }
            }
        }

        /// <summary>
        /// Crea un comando SQL a partir de una consulta y parámetros
        /// </summary>
        /// <param name="connection">Conexión SQL</param>
        /// <param name="sql">Consulta SQL</param>
        /// <param name="parameters">Parámetros de la consulta</param>
        /// <returns>Comando SQL</returns>
        private SqlCommand CreateCommand(SqlConnection connection, string sql, object parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            if (parameters != null)
            {
                AddParameters(command, parameters);
            }

            return command;
        }

        /// <summary>
        /// Agrega parámetros a un comando SQL
        /// </summary>
        /// <param name="command">Comando SQL</param>
        /// <param name="parameters">Parámetros</param>
        private void AddParameters(SqlCommand command, object parameters)
        {
            if (parameters is SqlParameter[] sqlParameters)
            {
                command.Parameters.AddRange(sqlParameters);
            }
            else if (parameters is IEnumerable<SqlParameter> parameterEnumerable)
            {
                foreach (var parameter in parameterEnumerable)
                {
                    command.Parameters.Add(parameter);
                }
            }
            else if (parameters is IDictionary<string, object> parameterDictionary)
            {
                foreach (var parameter in parameterDictionary)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
            }
            else
            {
                // Para objetos anónimos o cualquier otro objeto, usar reflexión
                var properties = parameters.GetType().GetProperties();

                foreach (var prop in properties)
                {
                    string name = prop.Name;

                    // Asegurar que el nombre comienza con @
                    if (!name.StartsWith("@"))
                        name = "@" + name;

                    object value = prop.GetValue(parameters) ?? DBNull.Value;
                    command.Parameters.AddWithValue(name, value);
                }
            }
        }
    }
}
