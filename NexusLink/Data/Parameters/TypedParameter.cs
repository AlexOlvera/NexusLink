using System;
using System.Data;
using System.Data.Common;
using NexusLink.Core.Connection;

namespace NexusLink.Data.Parameters
{
    /// <summary>
    /// Parámetro con inferencia de tipo para SQL
    /// </summary>
    public class TypedParameter
    {
        private readonly ConnectionFactory _connectionFactory;

        public TypedParameter(ConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Crea un parámetro de tipo string
        /// </summary>
        public DbParameter String(string name, string value, int size = 0)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value != null ? (object)value : DBNull.Value;
            parameter.DbType = DbType.String;

            if (size > 0)
            {
                parameter.Size = size;
            }

            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo int
        /// </summary>
        public DbParameter Int(string name, int? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Int32;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo long
        /// </summary>
        public DbParameter Long(string name, long? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Int64;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo decimal
        /// </summary>
        public DbParameter Decimal(string name, decimal? value, byte precision = 18, byte scale = 2)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Decimal;

            // Establecer precisión y escala si el proveedor lo soporta
            if (parameter is System.Data.SqlClient.SqlParameter sqlParameter)
            {
                sqlParameter.Precision = precision;
                sqlParameter.Scale = scale;
            }

            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo double
        /// </summary>
        public DbParameter Double(string name, double? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Double;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo datetime
        /// </summary>
        public DbParameter DateTime(string name, DateTime? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.DateTime2;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo date
        /// </summary>
        public DbParameter Date(string name, DateTime? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);

            if (value.HasValue)
            {
                // Solo usar la fecha, sin la hora
                parameter.Value = value.Value.Date;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }

            parameter.DbType = DbType.Date;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo time
        /// </summary>
        public DbParameter Time(string name, TimeSpan? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Time;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo boolean
        /// </summary>
        public DbParameter Boolean(string name, bool? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Boolean;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo guid
        /// </summary>
        public DbParameter Guid(string name, Guid? value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
            parameter.DbType = DbType.Guid;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo byte[]
        /// </summary>
        public DbParameter Binary(string name, byte[] value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value != null ? (object)value : DBNull.Value;
            parameter.DbType = DbType.Binary;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo xml
        /// </summary>
        public DbParameter Xml(string name, string value)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value != null ? (object)value : DBNull.Value;
            parameter.DbType = DbType.Xml;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de salida
        /// </summary>
        public DbParameter Output(string name, DbType dbType)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.Output;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de entrada/salida
        /// </summary>
        public DbParameter InputOutput(string name, object value, DbType dbType)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.InputOutput;
            return parameter;
        }

        /// <summary>
        /// Crea un parámetro de tipo tabla
        /// </summary>
        public DbParameter Table(string name, DataTable value, string typeName)
        {
            var parameter = _connectionFactory.CreateParameter();
            parameter.ParameterName = EnsureParamName(name);
            parameter.Value = value ?? throw new ArgumentNullException(nameof(value));

            // SQL Server específico
            var sqlParameter = parameter as System.Data.SqlClient.SqlParameter;
            if (sqlParameter != null)
            {
                sqlParameter.SqlDbType = System.Data.SqlDbType.Structured;
                sqlParameter.TypeName = typeName;
            }
            else
            {
                throw new NotSupportedException("Los parámetros de tipo tabla solo son soportados en SQL Server");
            }

            return parameter;
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