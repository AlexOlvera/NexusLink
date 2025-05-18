using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using NexusLink.Core.Connection;

namespace NexusLink.Data.Parameters
{
    /// <summary>
    /// Fábrica para crear parámetros SQL con configuración óptima
    /// </summary>
    public class SqlParameterFactory
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly TypedParameter _typedParameter;

        public SqlParameterFactory(ConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _typedParameter = new TypedParameter(connectionFactory);
        }

        /// <summary>
        /// Crea un parámetro con nombre y valor
        /// </summary>
        public DbParameter Create(string name, object value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? DBNull.Value;

            // Inferir el tipo adecuado basado en el valor
            if (value != null)
            {
                SetParameterType(parameter, value);
            }

            return parameter;
        }

        /// <summary>
        /// Crea un parámetro con nombre, valor y tipo SQL específico
        /// </summary>
        public DbParameter Create(string name, object value, DbType dbType)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro con nombre, valor, tipo SQL y dirección
        /// </summary>
        public DbParameter Create(string name, object value, DbType dbType, ParameterDirection direction)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = direction;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro con nombre, valor, tipo SQL, dirección y tamaño
        /// </summary>
        public DbParameter Create(string name, object value, DbType dbType, ParameterDirection direction, int size)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = direction;
            parameter.Size = size;
            return parameter;
        }

        /// <summary>
        /// Crea múltiples parámetros a partir de un diccionario de valores
        /// </summary>
        public IEnumerable<DbParameter> CreateFromDictionary(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var result = new List<DbParameter>();

            foreach (var kvp in parameters)
            {
                result.Add(Create(kvp.Key, kvp.Value));
            }

            return result;
        }

        /// <summary>
        /// Crea un parámetro de salida
        /// </summary>
        public DbParameter CreateOutputParameter(string name, DbType dbType)
        {
            return _typedParameter.Output(name, dbType);
        }

        /// <summary>
        /// Crea un parámetro de entrada/salida
        /// </summary>
        public DbParameter CreateInputOutputParameter(string name, object value, DbType dbType)
        {
            return _typedParameter.InputOutput(name, value, dbType);
        }

        /// <summary>
        /// Crea un parámetro de valor de retorno
        /// </summary>
        public DbParameter CreateReturnValueParameter(string name)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.DbType = DbType.Int32;
            parameter.Direction = ParameterDirection.ReturnValue;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro para un tipo definido por el usuario (UDT) (solo SQL Server)
        /// </summary>
        public SqlParameter CreateStructuredParameter(string name, DataTable value, string typeName)
        {
            var parameter = new SqlParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? throw new ArgumentNullException(nameof(value));
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.TypeName = typeName;
            return parameter;
        }

        /// <summary>
        /// Crea un conjunto de parámetros a partir de una entidad usando reflexión
        /// </summary>
        public IEnumerable<DbParameter> CreateFromEntity<T>(T entity, params string[] excludeProperties) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var result = new List<DbParameter>();
            var excludeList = new HashSet<string>(excludeProperties, StringComparer.OrdinalIgnoreCase);

            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                // Saltar propiedades excluidas
                if (excludeList.Contains(property.Name))
                {
                    continue;
                }

                // Saltar propiedades que no se pueden leer
                if (!property.CanRead)
                {
                    continue;
                }

                object value = property.GetValue(entity);
                result.Add(Create(property.Name, value));
            }

            return result;
        }

        /// <summary>
        /// Acorta un parámetro asegurando que su valor string no exceda un tamaño máximo
        /// </summary>
        public DbParameter TruncateStringParameter(DbParameter parameter, int maxLength)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (parameter.Value is string stringValue && stringValue.Length > maxLength)
            {
                parameter.Value = stringValue.Substring(0, maxLength);
            }

            return parameter;
        }

        /// <summary>
        /// Obtiene un parámetro tipado para crear parámetros con tipos específicos
        /// </summary>
        public TypedParameter GetTypedParameter()
        {
            return _typedParameter;
        }

        /// <summary>
        /// Configura el tipo adecuado para un parámetro basado en su valor
        /// </summary>
        private void SetParameterType(DbParameter parameter, object value)
        {
            if (value is string)
            {
                parameter.DbType = DbType.String;
            }
            else if (value is int)
            {
                parameter.DbType = DbType.Int32;
            }
            else if (value is long)
            {
                parameter.DbType = DbType.Int64;
            }
            else if (value is short)
            {
                parameter.DbType = DbType.Int16;
            }
            else if (value is decimal)
            {
                parameter.DbType = DbType.Decimal;
            }
            else if (value is double)
            {
                parameter.DbType = DbType.Double;
            }
            else if (value is float)
            {
                parameter.DbType = DbType.Single;
            }
            else if (value is bool)
            {
                parameter.DbType = DbType.Boolean;
            }
            else if (value is DateTime)
            {
                parameter.DbType = DbType.DateTime2;
            }
            else if (value is DateTimeOffset)
            {
                parameter.DbType = DbType.DateTimeOffset;
            }
            else if (value is Guid)
            {
                parameter.DbType = DbType.Guid;
            }
            else if (value is byte[])
            {
                parameter.DbType = DbType.Binary;
            }
            else if (value is TimeSpan)
            {
                parameter.DbType = DbType.Time;
            }
            else
            {
                // Para otros tipos, usar Object
                parameter.DbType = DbType.Object;
            }
        }

        /// <summary>
        /// Asegura que el nombre del parámetro comienza con @
        /// </summary>
        private string EnsureParamName(string name)
        {
            if (name.StartsWith("@"))
            {
                return name;
            }

            return "@" + name;
        }
    }
}