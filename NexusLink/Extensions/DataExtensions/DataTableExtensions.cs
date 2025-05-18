using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace NexusLink.Extensions.DataExtensions
{
    public static class DataTableExtensions
    {
        /// <summary>
        /// Convierte una DataTable en una colección de objetos tipados
        /// </summary>
        public static IEnumerable<T> ToList<T>(this DataTable table) where T : new()
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var objType = typeof(T);
            var result = new List<T>();

            // Obtener las propiedades de la clase
            var properties = objType.GetProperties();

            // Crear un diccionario para mapeo rápido
            var columnMappings = CreateColumnMappings(table, properties);

            // Recorrer filas
            foreach (DataRow row in table.Rows)
            {
                var obj = new T();

                // Asignar valores a propiedades
                foreach (var prop in properties)
                {
                    if (columnMappings.TryGetValue(prop, out int colIndex))
                    {
                        if (!row.IsNull(colIndex))
                        {
                            var value = row[colIndex];
                            SetPropertyValue(prop, obj, value);
                        }
                    }
                }

                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Crea un diccionario para mapeo rápido entre propiedades y columnas
        /// </summary>
        private static Dictionary<PropertyInfo, int> CreateColumnMappings<T>(DataTable table, PropertyInfo[] properties)
        {
            var columnMappings = new Dictionary<PropertyInfo, int>();

            foreach (var prop in properties)
            {
                // Intentar encontrar columna por nombre de propiedad
                if (table.Columns.Contains(prop.Name))
                {
                    columnMappings[prop] = table.Columns[prop.Name].Ordinal;
                    continue;
                }

                // Buscar atributo de columna
                var columnAttr = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
                if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name) && table.Columns.Contains(columnAttr.Name))
                {
                    columnMappings[prop] = table.Columns[columnAttr.Name].Ordinal;
                    continue;
                }

                // Intentar buscar por nombre en formato snake_case
                string snakeCase = ToSnakeCase(prop.Name);
                if (table.Columns.Contains(snakeCase))
                {
                    columnMappings[prop] = table.Columns[snakeCase].Ordinal;
                    continue;
                }
            }

            return columnMappings;
        }

        /// <summary>
        /// Convierte un string en formato PascalCase a snake_case
        /// </summary>
        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        /// <summary>
        /// Asigna un valor a una propiedad, convirtiendo el tipo si es necesario
        /// </summary>
        private static void SetPropertyValue(PropertyInfo prop, object obj, object value)
        {
            if (value == null || value == DBNull.Value)
                return;

            Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (targetType.IsEnum && value is string strValue)
            {
                prop.SetValue(obj, Enum.Parse(targetType, strValue, true));
                return;
            }

            if (targetType == typeof(Guid) && value is string guidString)
            {
                prop.SetValue(obj, Guid.Parse(guidString));
                return;
            }

            if (value.GetType() != targetType)
            {
                var convertedValue = Convert.ChangeType(value, targetType);
                prop.SetValue(obj, convertedValue);
                return;
            }

            prop.SetValue(obj, value);
        }

        /// <summary>
        /// Convierte un DataTable a formato JSON
        /// </summary>
        public static string ToJson(this DataTable table, bool formatOutput = true)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            using (var sw = new System.IO.StringWriter())
            {
                using (var writer = new Newtonsoft.Json.JsonTextWriter(sw))
                {
                    if (formatOutput)
                    {
                        writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    }

                    writer.QuoteChar = '"';

                    jsonSerializer.Serialize(writer, table);
                    return sw.ToString();
                }
            }
        }

        /// <summary>
        /// Crea un diccionario a partir de dos columnas de la tabla
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this DataTable table, string keyColumn, string valueColumn)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (string.IsNullOrEmpty(keyColumn))
                throw new ArgumentException("Key column cannot be null or empty", nameof(keyColumn));

            if (string.IsNullOrEmpty(valueColumn))
                throw new ArgumentException("Value column cannot be null or empty", nameof(valueColumn));

            if (!table.Columns.Contains(keyColumn))
                throw new ArgumentException($"Column '{keyColumn}' not found in table", nameof(keyColumn));

            if (!table.Columns.Contains(valueColumn))
                throw new ArgumentException($"Column '{valueColumn}' not found in table", nameof(valueColumn));

            var dictionary = new Dictionary<TKey, TValue>();

            foreach (DataRow row in table.Rows)
            {
                if (!row.IsNull(keyColumn) && !row.IsNull(valueColumn))
                {
                    TKey key = (TKey)Convert.ChangeType(row[keyColumn], typeof(TKey));
                    TValue value = (TValue)Convert.ChangeType(row[valueColumn], typeof(TValue));

                    dictionary[key] = value;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Devuelve una DataTable filtrada
        /// </summary>
        public static DataTable Filter(this DataTable table, string filterExpression)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (string.IsNullOrEmpty(filterExpression))
                return table.Copy();

            var result = table.Clone();
            var rows = table.Select(filterExpression);

            foreach (var row in rows)
            {
                result.ImportRow(row);
            }

            return result;
        }
    }
}