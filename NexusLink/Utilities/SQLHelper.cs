using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace NexusLink.Utilities
{
    /// <summary>
    /// Proporciona utilidades para operaciones SQL
    /// </summary>
    public static class SqlHelper
    {
        /// <summary>
        /// Convierte un valor .NET a un tipo SQL
        /// </summary>
        /// <param name="value">Valor .NET</param>
        /// <returns>Valor SQL o DBNull.Value si es null</returns>
        public static object ToSqlValue(object value)
        {
            if (value == null)
                return DBNull.Value;

            return value;
        }

        /// <summary>
        /// Convierte un valor SQL a un tipo .NET
        /// </summary>
        /// <typeparam name="T">Tipo .NET</typeparam>
        /// <param name="value">Valor SQL</param>
        /// <returns>Valor .NET o default si es DBNull</returns>
        public static T FromSqlValue<T>(object value)
        {
            if (value == null || value == DBNull.Value)
                return default(T);

            Type t = typeof(T);

            // Manejar tipos nullables
            Type underlyingType = Nullable.GetUnderlyingType(t);
            if (underlyingType != null)
            {
                t = underlyingType;
            }

            // Convertir al tipo adecuado
            if (t.IsEnum)
            {
                return (T)Enum.ToObject(t, value);
            }
            else
            {
                return (T)Convert.ChangeType(value, t);
            }
        }

        /// <summary>
        /// Obtiene el tipo SqlDbType equivalente a un tipo .NET
        /// </summary>
        /// <param name="type">Tipo .NET</param>
        /// <returns>Tipo SqlDbType</returns>
        public static SqlDbType GetSqlDbType(Type type)
        {
            // Manejar tipos nullables
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string) || type == typeof(char[]))
                return SqlDbType.NVarChar;
            else if (type == typeof(int) || type == typeof(uint))
                return SqlDbType.Int;
            else if (type == typeof(long) || type == typeof(ulong))
                return SqlDbType.BigInt;
            else if (type == typeof(short) || type == typeof(ushort))
                return SqlDbType.SmallInt;
            else if (type == typeof(byte) || type == typeof(sbyte))
                return SqlDbType.TinyInt;
            else if (type == typeof(bool))
                return SqlDbType.Bit;
            else if (type == typeof(decimal))
                return SqlDbType.Decimal;
            else if (type == typeof(float))
                return SqlDbType.Real;
            else if (type == typeof(double))
                return SqlDbType.Float;
            else if (type == typeof(DateTime))
                return SqlDbType.DateTime2;
            else if (type == typeof(DateTimeOffset))
                return SqlDbType.DateTimeOffset;
            else if (type == typeof(TimeSpan))
                return SqlDbType.Time;
            else if (type == typeof(Guid))
                return SqlDbType.UniqueIdentifier;
            else if (type == typeof(byte[]))
                return SqlDbType.VarBinary;
            else if (type == typeof(char))
                return SqlDbType.NChar;
            else
                return SqlDbType.Variant;
        }

        /// <summary>
        /// Escapar una cadena para evitar inyección SQL
        /// </summary>
        /// <param name="value">Cadena a escapar</param>
        /// <returns>Cadena escapada</returns>
        public static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Reemplazar comillas simples por dobles comillas simples
            return value.Replace("'", "''");
        }

        /// <summary>
        /// Construye una lista de parámetros SQL desde un objeto anónimo
        /// </summary>
        /// <param name="parameters">Objeto anónimo con parámetros</param>
        /// <returns>Lista de parámetros SQL</returns>
        public static List<SqlParameter> GetParametersFromAnonymousObject(object parameters)
        {
            if (parameters == null)
                return new List<SqlParameter>();

            var result = new List<SqlParameter>();
            var properties = parameters.GetType().GetProperties();

            foreach (var prop in properties)
            {
                string name = prop.Name;

                // Asegurar que el nombre comienza con @
                if (!name.StartsWith("@"))
                    name = "@" + name;

                object value = prop.GetValue(parameters) ?? DBNull.Value;
                result.Add(new SqlParameter(name, value));
            }

            return result;
        }

        /// <summary>
        /// Obtiene el valor de un campo específico del reader
        /// </summary>
        /// <typeparam name="T">Tipo del valor</typeparam>
        /// <param name="reader">DataReader</param>
        /// <param name="fieldName">Nombre del campo</param>
        /// <returns>Valor del campo o default si es DBNull o no existe</returns>
        public static T GetFieldValue<T>(SqlDataReader reader, string fieldName)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentException("El nombre del campo no puede estar vacío", nameof(fieldName));

            int ordinal;
            try
            {
                ordinal = reader.GetOrdinal(fieldName);
            }
            catch (IndexOutOfRangeException)
            {
                // El campo no existe
                return default(T);
            }

            if (reader.IsDBNull(ordinal))
                return default(T);

            return reader.GetFieldValue<T>(ordinal);
        }

        /// <summary>
        /// Obtiene el valor de un campo específico del reader
        /// </summary>
        /// <typeparam name="T">Tipo del valor</typeparam>
        /// <param name="reader">DataReader</param>
        /// <param name="ordinal">Índice del campo</param>
        /// <returns>Valor del campo o default si es DBNull</returns>
        public static T GetFieldValue<T>(SqlDataReader reader, int ordinal)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (ordinal < 0 || ordinal >= reader.FieldCount)
                throw new ArgumentOutOfRangeException(nameof(ordinal));

            if (reader.IsDBNull(ordinal))
                return default(T);

            return reader.GetFieldValue<T>(ordinal);
        }

        /// <summary>
        /// Obtiene el valor de un campo específico del reader o un valor predeterminado si es DBNull
        /// </summary>
        /// <typeparam name="T">Tipo del valor</typeparam>
        /// <param name="reader">DataReader</param>
        /// <param name="fieldName">Nombre del campo</param>
        /// <param name="defaultValue">Valor predeterminado</param>
        /// <returns>Valor del campo o el valor predeterminado si es DBNull o no existe</returns>
        public static T GetFieldValue<T>(SqlDataReader reader, string fieldName, T defaultValue)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentException("El nombre del campo no puede estar vacío", nameof(fieldName));

            int ordinal;
            try
            {
                ordinal = reader.GetOrdinal(fieldName);
            }
            catch (IndexOutOfRangeException)
            {
                // El campo no existe
                return defaultValue;
            }

            if (reader.IsDBNull(ordinal))
                return defaultValue;

            return reader.GetFieldValue<T>(ordinal);
        }

        /// <summary>
        /// Genera una consulta SELECT segura con paginación
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="columns">Columnas a seleccionar</param>
        /// <param name="whereClause">Cláusula WHERE (sin la palabra WHERE)</param>
        /// <param name="orderBy">Cláusula ORDER BY (sin las palabras ORDER BY)</param>
        /// <param name="pageNumber">Número de página (basado en 1)</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <returns>Consulta SQL con paginación</returns>
        public static string GeneratePagedQuery(string tableName, IEnumerable<string> columns = null, string whereClause = null, string orderBy = null, int? pageNumber = null, int? pageSize = null)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío", nameof(tableName));

            var sql = new StringBuilder();

            sql.Append("SELECT ");

            if (columns != null && columns.Any())
            {
                sql.Append(string.Join(", ", columns));
            }
            else
            {
                sql.Append("*");
            }

            sql.Append(" FROM ").Append(tableName);

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.Append(" WHERE ").Append(whereClause);
            }

            if (pageNumber.HasValue && pageSize.HasValue && pageSize.Value > 0)
            {
                // Para paginación, necesitamos una cláusula ORDER BY
                if (string.IsNullOrEmpty(orderBy))
                {
                    orderBy = "(SELECT NULL)"; // Orden arbitrario si no se especifica
                }

                // SQL Server 2012 o superior
                sql.Append(" ORDER BY ").Append(orderBy);
                sql.Append(" OFFSET ").Append((pageNumber.Value - 1) * pageSize.Value).Append(" ROWS");
                sql.Append(" FETCH NEXT ").Append(pageSize.Value).Append(" ROWS ONLY");
            }
            else if (!string.IsNullOrEmpty(orderBy))
            {
                sql.Append(" ORDER BY ").Append(orderBy);
            }

            return sql.ToString();
        }

        /// <summary>
        /// Genera una consulta de conteo
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="whereClause">Cláusula WHERE (sin la palabra WHERE)</param>
        /// <returns>Consulta SQL de conteo</returns>
        public static string GenerateCountQuery(string tableName, string whereClause = null)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío", nameof(tableName));

            var sql = new StringBuilder();

            sql.Append("SELECT COUNT(*) FROM ").Append(tableName);

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.Append(" WHERE ").Append(whereClause);
            }

            return sql.ToString();
        }

        /// <summary>
        /// Genera una consulta INSERT
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="columns">Columnas a insertar</param>
        /// <param name="returnIdentity">Indica si debe devolver el valor de identidad generado</param>
        /// <returns>Consulta SQL INSERT</returns>
        public static string GenerateInsertQuery(string tableName, IEnumerable<string> columns, bool returnIdentity = false)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío", nameof(tableName));

            if (columns == null || !columns.Any())
                throw new ArgumentException("Debe especificar al menos una columna", nameof(columns));

            var sql = new StringBuilder();

            sql.Append("INSERT INTO ").Append(tableName).Append(" (");
            sql.Append(string.Join(", ", columns));
            sql.Append(") VALUES (");

            var paramNames = columns.Select(c => "@" + c);
            sql.Append(string.Join(", ", paramNames));

            sql.Append(")");

            if (returnIdentity)
            {
                sql.Append("; SELECT SCOPE_IDENTITY()");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Genera una consulta UPDATE
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="columns">Columnas a actualizar</param>
        /// <param name="whereClause">Cláusula WHERE (sin la palabra WHERE)</param>
        /// <returns>Consulta SQL UPDATE</returns>
        public static string GenerateUpdateQuery(string tableName, IEnumerable<string> columns, string whereClause)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío", nameof(tableName));

            if (columns == null || !columns.Any())
                throw new ArgumentException("Debe especificar al menos una columna", nameof(columns));

            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("La cláusula WHERE no puede estar vacía", nameof(whereClause));

            var sql = new StringBuilder();

            sql.Append("UPDATE ").Append(tableName).Append(" SET ");

            var setClauses = columns.Select(c => c + " = @" + c);
            sql.Append(string.Join(", ", setClauses));

            sql.Append(" WHERE ").Append(whereClause);

            return sql.ToString();
        }

        /// <summary>
        /// Genera una consulta DELETE
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="whereClause">Cláusula WHERE (sin la palabra WHERE)</param>
        /// <returns>Consulta SQL DELETE</returns>
        public static string GenerateDeleteQuery(string tableName, string whereClause)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío", nameof(tableName));

            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentException("La cláusula WHERE no puede estar vacía", nameof(whereClause));

            var sql = new StringBuilder();

            sql.Append("DELETE FROM ").Append(tableName);
            sql.Append(" WHERE ").Append(whereClause);

            return sql.ToString();
        }

        /// <summary>
        /// Valida si una cadena es un nombre de objeto SQL válido (evita inyección SQL)
        /// </summary>
        /// <param name="objectName">Nombre del objeto</param>
        /// <returns>True si es válido, false en caso contrario</returns>
        public static bool IsValidSqlName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            // Patrón para nombres de objetos SQL válidos
            var pattern = @"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$";

            return Regex.IsMatch(objectName, pattern);
        }

        /// <summary>
        /// Crea una lista de nombres de columnas escapadas para SQL
        /// </summary>
        /// <param name="columns">Nombres de columnas</param>
        /// <returns>Lista de nombres escapados</returns>
        public static IEnumerable<string> EscapeColumnNames(IEnumerable<string> columns)
        {
            if (columns == null)
                yield break;

            foreach (var column in columns)
            {
                if (string.IsNullOrEmpty(column))
                    continue;

                // Si ya está entre corchetes, no hacer nada
                if (column.StartsWith("[") && column.EndsWith("]"))
                    yield return column;
                else
                    yield return "[" + column.Replace("]", "]]") + "]";
            }
        }

        /// <summary>
        /// Escapa un nombre de tabla para SQL
        /// </summary>
        /// <param name="tableName">Nombre de tabla</param>
        /// <returns>Nombre escapado</returns>
        public static string EscapeTableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return tableName;

            // Si ya está entre corchetes, no hacer nada
            if (tableName.StartsWith("[") && tableName.EndsWith("]"))
                return tableName;

            // Si contiene un punto, escapar cada parte
            if (tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]) &&
                        !(parts[i].StartsWith("[") && parts[i].EndsWith("]")))
                    {
                        parts[i] = "[" + parts[i].Replace("]", "]]") + "]";
                    }
                }

                return string.Join(".", parts);
            }

            // Caso simple
            return "[" + tableName.Replace("]", "]]") + "]";
        }
    }
}