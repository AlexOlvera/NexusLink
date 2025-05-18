using System;
using System.Collections.Generic;
using System.Data;

namespace NexusLink.Extensions.DataExtensions
{
    public static class DataRowExtensions
    {
        /// <summary>
        /// Obtiene el valor de una columna con un tipo específico, maneja valores nulos
        /// </summary>
        public static T Field<T>(this DataRow row, string columnName)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

            if (!row.Table.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist in the DataRow", nameof(columnName));

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
                return default(T);

            Type targetType = typeof(T);

            // Si el tipo destino es nullable, obtener el tipo subyacente
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Manejar tipos comunes
            if (underlyingType == typeof(bool) && value is int)
            {
                value = Convert.ToInt32(value) != 0;
            }
            else if (underlyingType.IsEnum && value is string)
            {
                value = Enum.Parse(underlyingType, (string)value, true);
            }
            else if (underlyingType == typeof(Guid) && value is string)
            {
                value = Guid.Parse((string)value);
            }

            return (T)Convert.ChangeType(value, underlyingType);
        }

        /// <summary>
        /// Verifica si una columna específica es nula o DBNull
        /// </summary>
        public static bool IsNull(this DataRow row, string columnName)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

            if (!row.Table.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist in the DataRow", nameof(columnName));

            return row[columnName] == null || row[columnName] is DBNull;
        }

        /// <summary>
        /// Crea un diccionario a partir de un DataRow
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var dict = new Dictionary<string, object>();

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column];

                if (value is DBNull)
                {
                    dict[column.ColumnName] = null;
                }
                else
                {
                    dict[column.ColumnName] = value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Crea un objeto dinámico a partir de un DataRow
        /// </summary>
        public static dynamic ToDynamic(this DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var expando = new System.Dynamic.ExpandoObject();
            var expandoDict = expando as IDictionary<string, object>;

            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column];

                if (value is DBNull)
                {
                    expandoDict[column.ColumnName] = null;
                }
                else
                {
                    expandoDict[column.ColumnName] = value;
                }
            }

            return expando;
        }
    }
}