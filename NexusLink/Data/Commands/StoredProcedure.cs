using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Core.Connection;

namespace NexusLink.Data.Commands
{
    /// <summary>
    /// Clase para ejecutar procedimientos almacenados
    /// </summary>
    public class StoredProcedure
    {
        private readonly CommandBuilder _builder;

        /// <summary>
        /// Inicializa una nueva instancia de StoredProcedure
        /// </summary>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="connectionName">Nombre de la conexión (opcional)</param>
        public StoredProcedure(string procedureName, string connectionName = null)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("El nombre del procedimiento no puede estar vacío", nameof(procedureName));

            _builder = new CommandBuilder()
                .WithCommandType(CommandType.StoredProcedure)
                .WithCommandText(procedureName);

            if (!string.IsNullOrEmpty(connectionName))
            {
                _builder.WithConnection(connectionName);
            }
        }

        /// <summary>
        /// Inicializa una nueva instancia de StoredProcedure con una conexión
        /// </summary>
        /// <param name="procedureName">Nombre del procedimiento</param>
        /// <param name="connection">Conexión SQL</param>
        /// <param name="transaction">Transacción SQL (opcional)</param>
        public StoredProcedure(string procedureName, SqlConnection connection, SqlTransaction transaction = null)
        {
            if (string.IsNullOrEmpty(procedureName))
                throw new ArgumentException("El nombre del procedimiento no puede estar vacío", nameof(procedureName));

            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _builder = new CommandBuilder()
                .WithCommandType(CommandType.StoredProcedure)
                .WithCommandText(procedureName)
                .WithConnection(connection);

            if (transaction != null)
            {
                _builder.WithTransaction(transaction);
            }
        }

        /// <summary>
        /// Establece el timeout del procedimiento
        /// </summary>
        /// <param name="timeoutSeconds">Timeout en segundos</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure WithTimeout(int timeoutSeconds)
        {
            _builder.WithTimeout(timeoutSeconds);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro de entrada
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure AddParameter(string name, object value)
        {
            _builder.WithParameter(name, value);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro de entrada con tipo específico
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        /// <param name="type">Tipo de parámetro</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure AddParameter(string name, object value, SqlDbType type)
        {
            _builder.WithParameter(name, value, type);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro de salida
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="type">Tipo de parámetro</param>
        /// <param name="size">Tamaño del parámetro (opcional)</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure AddOutputParameter(string name, SqlDbType type, int size = 0)
        {
            var parameter = new SqlParameter(name, type)
            {
                Direction = ParameterDirection.Output
            };

            if (size > 0)
            {
                parameter.Size = size;
            }

            _builder.WithParameter(parameter);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro de entrada/salida
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <param name="value">Valor inicial del parámetro</param>
        /// <param name="type">Tipo de parámetro</param>
        /// <param name="size">Tamaño del parámetro (opcional)</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure AddInputOutputParameter(string name, object value, SqlDbType type, int size = 0)
        {
            var parameter = new SqlParameter(name, type)
            {
                Direction = ParameterDirection.InputOutput,
                Value = value ?? DBNull.Value
            };

            if (size > 0)
            {
                parameter.Size = size;
            }

            _builder.WithParameter(parameter);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro de retorno
        /// </summary>
        /// <param name="name">Nombre del parámetro</param>
        /// <returns>El objeto StoredProcedure para encadenamiento</returns>
        public StoredProcedure AddReturnParameter(string name = "@RETURN_VALUE")
        {
            var parameter = new SqlParameter(name, SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };

            _builder.WithParameter(parameter);
            return this;
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna el número de filas afectadas
        /// </summary>
        /// <returns>Número de filas afectadas</returns>
        public int Execute()
        {
            return _builder.ExecuteNonQuery();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna el número de filas afectadas de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de filas afectadas</returns>
        public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _builder.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un valor escalar
        /// </summary>
        /// <returns>Valor escalar</returns>
        public object ExecuteScalar()
        {
            return _builder.ExecuteScalar();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un valor escalar de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Valor escalar</returns>
        public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default)
        {
            return _builder.ExecuteScalarAsync(cancellationToken);
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un valor escalar tipado
        /// </summary>
        /// <typeparam name="T">Tipo de retorno</typeparam>
        /// <returns>Valor escalar</returns>
        public T ExecuteScalar<T>()
        {
            return _builder.ExecuteScalar<T>();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un valor escalar tipado de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de retorno</typeparam>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Valor escalar</returns>
        public Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default)
        {
            return _builder.ExecuteScalarAsync<T>(cancellationToken);
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un DataSet
        /// </summary>
        /// <returns>DataSet con los resultados</returns>
        public DataSet ExecuteDataSet()
        {
            return _builder.ExecuteDataSet();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un DataTable
        /// </summary>
        /// <returns>DataTable con los resultados</returns>
        public DataTable ExecuteDataTable()
        {
            return _builder.ExecuteDataTable();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un reader
        /// </summary>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteReader()
        {
            return _builder.ExecuteReader();
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna un reader de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>SqlDataReader</returns>
        public Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
        {
            return _builder.ExecuteReaderAsync(cancellationToken);
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna una lista de objetos
        /// </summary>
        /// <typeparam name="T">Tipo de objetos</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <returns>Lista de objetos</returns>
        public List<T> ExecuteList<T>(Func<SqlDataReader, T> mapper)
        {
            return _builder.ExecuteList(mapper);
        }

        /// <summary>
        /// Ejecuta el procedimiento y retorna una lista de objetos de forma asíncrona
        /// </summary>
        /// <typeparam name="T">Tipo de objetos</typeparam>
        /// <param name="mapper">Función de mapeo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de objetos</returns>
        public Task<List<T>> ExecuteListAsync<T>(Func<SqlDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            return _builder.ExecuteListAsync(mapper, cancellationToken);
        }

        /// <summary>
        /// Obtiene el valor de un parámetro de salida después de ejecutar el procedimiento
        /// </summary>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <returns>Valor del parámetro</returns>
        public object GetParameterValue(string parameterName)
        {
            using (var command = _builder.Build())
            {
                command.ExecuteNonQuery();

                // Asegurar que el nombre comienza con @
                if (!parameterName.StartsWith("@"))
                    parameterName = "@" + parameterName;

                return command.Parameters[parameterName].Value;
            }
        }

        /// <summary>
        /// Obtiene el valor de un parámetro de salida después de ejecutar el procedimiento de forma asíncrona
        /// </summary>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Valor del parámetro</returns>
        public async Task<object> GetParameterValueAsync(string parameterName, CancellationToken cancellationToken = default)
        {
            using (var command = _builder.Build())
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                // Asegurar que el nombre comienza con @
                if (!parameterName.StartsWith("@"))
                    parameterName = "@" + parameterName;

                return command.Parameters[parameterName].Value;
            }
        }

        /// <summary>
        /// Obtiene el valor de un parámetro de salida tipado después de ejecutar el procedimiento
        /// </summary>
        /// <typeparam name="T">Tipo de retorno</typeparam>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <returns>Valor del parámetro</returns>
        public T GetParameterValue<T>(string parameterName)
        {
            object value = GetParameterValue(parameterName);

            if (value == null || value == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Obtiene el valor de retorno después de ejecutar el procedimiento
        /// </summary>
        /// <returns>Valor de retorno</returns>
        public int GetReturnValue()
        {
            return GetParameterValue<int>("@RETURN_VALUE");
        }

        /// <summary>
        /// Obtiene el valor de retorno después de ejecutar el procedimiento de forma asíncrona
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Valor de retorno</returns>
        public async Task<int> GetReturnValueAsync(CancellationToken cancellationToken = default)
        {
            object value = await GetParameterValueAsync("@RETURN_VALUE", cancellationToken).ConfigureAwait(false);

            if (value == null || value == DBNull.Value)
                return 0;

            return Convert.ToInt32(value);
        }
    }
}