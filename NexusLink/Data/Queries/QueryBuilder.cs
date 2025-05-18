using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Data.Commands;

namespace NexusLink.Data.Queries
{
    /// <summary>
    /// Constructor fluido para consultas SQL
    /// </summary>
    public class QueryBuilder
    {
        private readonly ConnectionFactory _connectionFactory;
        private string _connectionName;
        private readonly StringBuilder _queryBuilder;
        private readonly List<SqlParameter> _parameters;
        private int? _timeout;
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _ownsConnection;

        /// <summary>
        /// Inicializa una nueva instancia de QueryBuilder
        /// </summary>
        /// <param name="connectionFactory">Fábrica de conexiones</param>
        public QueryBuilder(ConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new ConnectionFactory();
            _queryBuilder = new StringBuilder();
            _parameters = new List<SqlParameter>();
        }

        /// <summary>
        /// Establece el nombre de la conexión
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithConnection(string connectionName)
        {
            _connectionName = connectionName;
            return this;
        }

        /// <summary>
        /// Establece la conexión existente
        /// </summary>
        /// <param name="connection">Conexión SQL</param>
        /// <param name="ownsConnection">Indica si debe cerrar la conexión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithConnection(SqlConnection connection, bool ownsConnection = false)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;
            return this;
        }

        /// <summary>
        /// Establece la transacción existente
        /// </summary>
        /// <param name="transaction">Transacción SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithTransaction(SqlTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            return this;
        }

        /// <summary>
        /// Establece el timeout de la consulta
        /// </summary>
        /// <param name="timeoutSeconds">Timeout en segundos</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithTimeout(int timeoutSeconds)
        {
            _timeout = timeoutSeconds;
            return this;
        }

        /// <summary>
        /// Agrega texto a la consulta
        /// </summary>
        /// <param name="sql">Texto SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder Append(string sql)
        {
            if (!string.IsNullOrEmpty(sql))
            {
                _queryBuilder.Append(sql);
            }

            return this;
        }

        /// <summary>
        /// Agrega texto a la consulta con un salto de línea
        /// </summary>
        /// <param name="sql">Texto SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder AppendLine(string sql = "")
        {
            if (string.IsNullOrEmpty(sql))
            {
                _queryBuilder.AppendLine();
            }
            else
            {
                _queryBuilder.AppendLine(sql);
            }

            return this;
        }

        /// <summary>
        /// Agrega una cláusula SELECT a la consulta
        /// </summary>
        /// <param name="columns">Columnas a seleccionar</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder Select(params string[] columns)
        {
            _queryBuilder.Append("SELECT ");

            if (columns == null || columns.Length == 0)
            {
                _queryBuilder.Append("*");
            }
            else
            {
                _queryBuilder.Append(string.Join(", ", columns));
            }

            return this;
        }

        /// <summary>
        /// Agrega una cláusula SELECT TOP a la consulta
        /// </summary>
        /// <param name="count">Número de filas</param>
        /// <param name="columns">Columnas a seleccionar</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder SelectTop(int count, params string[] columns)
        {
            _queryBuilder.Append($"SELECT TOP {count} ");

            if (columns == null || columns.Length == 0)
            {
                _queryBuilder.Append("*");
            }
            else
            {
                _queryBuilder.Append(string.Join(", ", columns));
            }

            return this;
        }

        /// <summary>
        /// Agrega una cláusula FROM a la consulta
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder From(string tableName)
        {
            _queryBuilder.AppendLine().Append("FROM ").Append(tableName);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula WHERE a la consulta
        /// </summary>
        /// <param name="condition">Condición</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder Where(string condition)
        {
            _queryBuilder.AppendLine().Append("WHERE ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula AND a la consulta
        /// </summary>
        /// <param name="condition">Condición</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder And(string condition)
        {
            _queryBuilder.AppendLine().Append("AND ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula OR a la consulta
        /// </summary>
        /// <param name="condition">Condición</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder Or(string condition)
        {
            _queryBuilder.AppendLine().Append("OR ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula INNER JOIN a la consulta
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="condition">Condición de unión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder InnerJoin(string tableName, string condition)
        {
            _queryBuilder.AppendLine().Append("INNER JOIN ").Append(tableName).Append(" ON ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula LEFT JOIN a la consulta
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="condition">Condición de unión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder LeftJoin(string tableName, string condition)
        {
            _queryBuilder.AppendLine().Append("LEFT JOIN ").Append(tableName).Append(" ON ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula RIGHT JOIN a la consulta
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="condition">Condición de unión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder RightJoin(string tableName, string condition)
        {
            _queryBuilder.AppendLine().Append("RIGHT JOIN ").Append(tableName).Append(" ON ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula GROUP BY a la consulta
        /// </summary>
        /// <param name="columns">Columnas para agrupar</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder GroupBy(params string[] columns)
        {
            if (columns != null && columns.Length > 0)
            {
                _queryBuilder.AppendLine().Append("GROUP BY ").Append(string.Join(", ", columns));
            }

            return this;
        }

        /// <summary>
        /// Agrega una cláusula HAVING a la consulta
        /// </summary>
        /// <param name="condition">Condición</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder Having(string condition)
        {
            _queryBuilder.AppendLine().Append("HAVING ").Append(condition);
            return this;
        }

        /// <summary>
        /// Agrega una cláusula ORDER BY a la consulta
        /// </summary>
        /// <param name="columns">Columnas para ordenar</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder OrderBy(params string[] columns)
        {
            if (columns != null && columns.Length > 0)
            {
                _queryBuilder.AppendLine().Append("ORDER BY ").Append(string.Join(", ", columns));
            }

            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la consulta
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithParameter(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("El nombre del parámetro no puede estar vacío", nameof(name));

            // Asegurar que el nombre comienza con @
            if (!name.StartsWith("@"))
                name = "@" + name;

            // Crear parámetro
            var parameter = new SqlParameter(name, value ?? DBNull.Value);

            // Agregar al listado
            _parameters.Add(parameter);

            return this;
        }

        /// <summary>
        /// Agrega un parámetro con tipo específico a la consulta
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <param name="type">Tipo de parámetro</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithParameter(string name, object value, SqlDbType type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("El nombre del parámetro no puede estar vacío", nameof(name));

            // Asegurar que el nombre comienza con @
            if (!name.StartsWith("@"))
                name = "@" + name;

            // Crear parámetro
            var parameter = new SqlParameter(name, type)
            {
                Value = value ?? DBNull.Value
            };

            // Agregar al listado
            _parameters.Add(parameter);

            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la consulta
        /// </summary>
        /// <param name="parameter">Parámetro SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public QueryBuilder WithParameter(SqlParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // Agregar al listado
            _parameters.Add(parameter);

            return this;
        }

        /// <summary>
        /// Obtiene la consulta SQL construida
        /// </summary>
        /// <returns>Consulta SQL</returns>
        public string GetSql()
        {
            return _queryBuilder.ToString();
        }
        /// <summary>
        /// Crea un comando para ejecutar la consulta
        /// </summary>
        /// <returns>Comando SQL</returns>
        public SqlCommand Build()
        {
            // Crear constructor de comandos
            var commandBuilder = new CommandBuilder(_connectionFactory)
                .WithCommandText(GetSql())
                .WithCommandType(CommandType.Text);

            // Establecer conexión
            if (_connection != null)
            {
                commandBuilder.WithConnection(_connection, _ownsConnection);
            }
            else if (!string.IsNullOrEmpty(_connectionName))
            {
                commandBuilder.WithConnection(_connectionName);
            }

            // Establecer transacción
            if (_transaction != null)
            {
                commandBuilder.WithTransaction(_transaction);
            }

            // Establecer timeout
            if (_timeout.HasValue)
            {
                commandBuilder.WithTimeout(_timeout.Value);
            }

            // Agregar parámetros
            if (_parameters.Count > 0)
            {
                commandBuilder.WithParameters(_parameters);
            }

            return commandBuilder.Build();
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un DataSet
        /// </summary>
        /// <returns>DataSet con los resultados</returns>
        public DataSet ExecuteDataSet()
        {
            using (var command = Build())
            {
                var adapter = new SqlDataAdapter(command);
                var dataSet = new DataSet();
                adapter.Fill(dataSet);
                return dataSet;
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un DataTable
        /// </summary>
        /// <returns>DataTable con los resultados</returns>
        public DataTable ExecuteDataTable()
        {
            using (var command = Build())
            {
                var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un escalar
        /// </summary>
        /// <returns>Resultado escalar</returns>
        public object ExecuteScalar()
        {
            using (var command = Build())
            {
                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un escalar de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado escalar</returns>
        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default)
        {
            using (var command = Build())
            {
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un escalar con tipo específico
        /// </summary>
        /// <typeparam name="T">Tipo de resultado</typeparam>
        /// <returns>Resultado escalar</returns>
        public T ExecuteScalar<T>()
        {
            using (var command = Build())
            {
                object result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return default(T);

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un escalar con tipo específico de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de resultado</typeparam>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado escalar</returns>
        public async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default)
        {
            using (var command = Build())
            {
                object result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                if (result == null || result == DBNull.Value)
                    return default(T);

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un reader
        /// </summary>
        /// <returns>Data reader</returns>
        public SqlDataReader ExecuteReader()
        {
            var command = Build();

            try
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                command.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un reader de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Data reader</returns>
        public async Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
        {
            var command = Build();

            try
            {
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                command.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve una lista de objetos
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <returns>Lista de objetos</returns>
        public List<T> ExecuteList<T>(Func<SqlDataReader, T> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            var result = new List<T>();

            using (var reader = ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(mapper(reader));
                }
            }

            return result;
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve una lista de objetos de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de objetos</returns>
        public async Task<List<T>> ExecuteListAsync<T>(Func<SqlDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            var result = new List<T>();

            using (var reader = await ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(mapper(reader));
                }
            }

            return result;
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un único objeto
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <returns>Objeto o default si no hay resultados</returns>
        public T ExecuteSingle<T>(Func<SqlDataReader, T> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            using (var reader = ExecuteReader())
            {
                if (reader.Read())
                {
                    return mapper(reader);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve un único objeto de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Objeto o default si no hay resultados</returns>
        public async Task<T> ExecuteSingleAsync<T>(Func<SqlDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            using (var reader = await ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return mapper(reader);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve la primera fila o default si no hay resultados
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <returns>Objeto o default si no hay resultados</returns>
        public T ExecuteFirstOrDefault<T>(Func<SqlDataReader, T> mapper)
        {
            return ExecuteSingle(mapper);
        }

        /// <summary>
        /// Ejecuta la consulta y devuelve la primera fila o default si no hay resultados de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Objeto o default si no hay resultados</returns>
        public Task<T> ExecuteFirstOrDefaultAsync<T>(Func<SqlDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            return ExecuteSingleAsync(mapper, cancellationToken);
        }
    }
}