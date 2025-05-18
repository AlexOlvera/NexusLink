using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Data.Parameters;
using NexusLink.Logging;

namespace NexusLink.Data.Commands
{
    /// <summary>
    /// Representa una función escalar de SQL
    /// </summary>
    public class ScalarFunction
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly string _functionName;
        private readonly List<DbParameter> _parameters;
        private CommandType _commandType;
        private int _timeout;
        private string _schema;

        public ScalarFunction(ILogger logger, ConnectionFactory connectionFactory, string functionName)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
            _parameters = new List<DbParameter>();
            _commandType = CommandType.Text;
            _timeout = 30; // Timeout predeterminado en segundos
            _schema = "dbo"; // Esquema predeterminado
        }

        /// <summary>
        /// Establece el esquema de la función
        /// </summary>
        public ScalarFunction WithSchema(string schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            return this;
        }

        /// <summary>
        /// Establece el tipo de comando
        /// </summary>
        public ScalarFunction WithCommandType(CommandType commandType)
        {
            _commandType = commandType;
            return this;
        }

        /// <summary>
        /// Establece el timeout en segundos
        /// </summary>
        public ScalarFunction WithTimeout(int seconds)
        {
            if (seconds <= 0)
            {
                throw new ArgumentException("El timeout debe ser mayor que cero", nameof(seconds));
            }

            _timeout = seconds;
            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la función
        /// </summary>
        public ScalarFunction WithParameter(string name, object value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            _parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la función con tipo específico
        /// </summary>
        public ScalarFunction WithParameter(string name, object value, DbType dbType)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            _parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la función
        /// </summary>
        public ScalarFunction WithParameter(DbParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            _parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Agrega múltiples parámetros a la función
        /// </summary>
        public ScalarFunction WithParameters(IEnumerable<DbParameter> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            _parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Agrega un parámetro a la función con dirección de parámetro específica
        /// </summary>
        public ScalarFunction WithParameter(string name, object value, DbType dbType, ParameterDirection direction)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = direction;
            _parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Ejecuta la función escalar y devuelve el resultado
        /// </summary>
        public object Execute()
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command);

                    _logger.Debug($"Ejecutando función escalar {_functionName}");
                    var result = command.ExecuteScalar();

                    // Recuperar parámetros de salida
                    UpdateOutputParameters();

                    return result;
                }
            }
        }

        /// <summary>
        /// Ejecuta la función escalar y devuelve el resultado convertido al tipo especificado
        /// </summary>
        public T Execute<T>()
        {
            object result = Execute();

            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta la función escalar de forma asíncrona y devuelve el resultado
        /// </summary>
        public async Task<object> ExecuteAsync()
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(command);

                    _logger.Debug($"Ejecutando función escalar (async) {_functionName}");
                    var result = await command.ExecuteScalarAsync();

                    // Recuperar parámetros de salida
                    UpdateOutputParameters();

                    return result;
                }
            }
        }

        /// <summary>
        /// Ejecuta la función escalar de forma asíncrona y devuelve el resultado convertido al tipo especificado
        /// </summary>
        public async Task<T> ExecuteAsync<T>()
        {
            object result = await ExecuteAsync();

            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Prepara el comando con todos los parámetros y opciones configuradas
        /// </summary>
        private void PrepareCommand(DbCommand command)
        {
            if (_commandType == CommandType.Text)
            {
                // Format as function call
                command.CommandText = $"SELECT {GetFullFunctionName()}({GetParameterList()})";
            }
            else
            {
                command.CommandText = GetFullFunctionName();
            }

            command.CommandType = _commandType;
            command.CommandTimeout = _timeout;

            foreach (var parameter in _parameters)
            {
                command.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Obtiene el nombre completo de la función incluyendo el esquema
        /// </summary>
        private string GetFullFunctionName()
        {
            return $"[{_schema}].[{_functionName}]";
        }

        /// <summary>
        /// Genera la lista de parámetros para la llamada a la función SQL
        /// </summary>
        private string GetParameterList()
        {
            if (_parameters.Count == 0)
            {
                return string.Empty;
            }

            var parameterNames = new List<string>();

            foreach (var parameter in _parameters)
            {
                parameterNames.Add(parameter.ParameterName);
            }

            return string.Join(", ", parameterNames);
        }

        /// <summary>
        /// Actualiza los valores de los parámetros de salida después de la ejecución
        /// </summary>
        private void UpdateOutputParameters()
        {
            // No se necesita hacer nada aquí, ya que los parámetros se actualizan automáticamente
            // por ADO.NET después de la ejecución
        }
    }
}