using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Core.Configuration;

namespace NexusLink.Data.Commands
{
    /// <summary>
    /// Constructor fluido para comandos SQL
    /// </summary>
    public class CommandBuilder
    {
        private readonly ConnectionFactory _connectionFactory;
        private string _connectionName;
        private string _commandText;
        private CommandType _commandType = CommandType.Text;
        private readonly List<SqlParameter> _parameters = new List<SqlParameter>();
        private int? _timeout;
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _ownsConnection;

        /// <summary>
        /// Inicializa una nueva instancia de CommandBuilder
        /// </summary>
        /// <param name="connectionFactory">Fábrica de conexiones</param>
        public CommandBuilder(ConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new ConnectionFactory();
        }

        /// <summary>
        /// Establece el nombre de la conexión
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithConnection(string connectionName)
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
        public CommandBuilder WithConnection(SqlConnection connection, bool ownsConnection = false)
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
        public CommandBuilder WithTransaction(SqlTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            return this;
        }

        /// <summary>
        /// Establece el texto del comando
        /// </summary>
        /// <param name="commandText">Texto del comando</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithCommandText(string commandText)
        {
            _commandText = commandText;
            return this;
        }

        /// <summary>
        /// Establece el tipo de comando
        /// </summary>
        /// <param name="commandType">Tipo de comando</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithCommandType(CommandType commandType)
        {
            _commandType = commandType;
            return this;
        }

        /// <summary>
        /// Establece el timeout del comando
        /// </summary>
        /// <param name="timeoutSeconds">Timeout en segundos</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithTimeout(int timeoutSeconds)
        {
            _timeout = timeoutSeconds;
            return this;
        }

        /// <summary>
        /// Agrega un parámetro
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithParameter(string name, object value)
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
        /// Agrega un parámetro con tipo específico
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <param name="type">Tipo de parámetro</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithParameter(string name, object value, SqlDbType type)
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
        /// Agrega un parámetro
        /// </summary>
        /// <param name="parameter">Parámetro SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithParameter(SqlParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // Agregar al listado
            _parameters.Add(parameter);

            return this;
        }

        /// <summary>
        /// Agrega múltiples parámetros
        /// </summary>
        /// <param name="parameters">Parámetros SQL</param>
        /// <returns>El constructor para encadenamiento</returns>
        public CommandBuilder WithParameters(IEnumerable<SqlParameter> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            // Agregar al listado
            _parameters.AddRange(parameters);

            return this;
        }

        /// <summary>
        /// Crea el comando SQL
        /// </summary>
        /// <returns>Comando SQL</returns>
        public SqlCommand Build()
        {
            // Verificar si ya tenemos una conexión
            SqlConnection connection = _connection;
            bool ownsConnection = _ownsConnection;

            // Crear conexión si no existe
            if (connection == null)
            {
                connection = _connectionFactory.CreateSqlConnection(_connectionName);
                ownsConnection = true;
            }

            // Asegurar que la conexión está abierta
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Crear comando
            var command = connection.CreateCommand();
            command.CommandText = _commandText;
            command.CommandType = _commandType;

            // Establecer transacción
            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            // Establecer timeout
            if (_timeout.HasValue)
            {
                command.CommandTimeout = _timeout.Value;
            }

            // Agregar parámetros
            if (_parameters.Count > 0)
            {
                command.Parameters.AddRange(_parameters.ToArray());
            }

            // Si somos propietarios de la conexión, asegurar que se cierre al disponer el comando
            if (ownsConnection)
            {
                var originalConnection = connection;
                command.Disposed += (sender, args) =>
                {
                    if (originalConnection.State == ConnectionState.Open)
                        originalConnection.Close();

                    originalConnection.Dispose();
                };
            }

            return command;
        }

        /// <summary>
        /// Ejecuta el comando como no-query
        /// </summary>
        /// <returns>Número de filas afectadas</returns>
        public int ExecuteNonQuery()
        {
            using (var command = Build())
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta el comando como no-query de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de filas afectadas</returns>
        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
        {
            using (var command = Build())
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ejecuta el comando como escalar
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
        /// Ejecuta el comando como escalar de forma asíncrona
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
        /// Ejecuta el comando como escalar con tipo específico
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
        /// Ejecuta el comando como escalar con tipo específico de forma asíncrona
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
        /// Ejecuta el comando y devuelve un reader
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
        /// Ejecuta el comando y devuelve un reader de forma asíncrona
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
        /// Ejecuta el comando y devuelve un DataSet
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
        /// Ejecuta el comando y devuelve un DataTable
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
        /// Ejecuta el comando y devuelve una lista de objetos
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
        /// Ejecuta el comando y devuelve una lista de objetos de forma asíncrona
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
        /// Ejecuta el comando y devuelve un único objeto
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
        /// Ejecuta el comando y devuelve un único objeto de forma asíncrona
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
    }
}