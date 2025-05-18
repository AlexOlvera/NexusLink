using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace NexusLink.Extensions
{
    /// <summary>
    /// Proporciona métodos de extensión para DbDataReader.
    /// </summary>
    public static class DataReaderExtensions
    {
        /// <summary>
        /// Convierte un DataReader a una lista de objetos tipados.
        /// </summary>
        public static List<T> ToList<T>(this DbDataReader reader) where T : class, new()
        {
            var result = new List<T>();
            var properties = typeof(T).GetProperties();

            // Mapeo de propiedades a índices de columnas
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnMap[reader.GetName(i)] = i;
            }

            while (reader.Read())
            {
                var item = new T();
                foreach (var property in properties)
                {
                    if (columnMap.TryGetValue(property.Name, out int ordinal))
                    {
                        if (!reader.IsDBNull(ordinal))
                        {
                            object value = reader.GetValue(ordinal);
                            try
                            {
                                // Intentar conversión de tipo
                                if (property.PropertyType.IsEnum && value is string)
                                {
                                    property.SetValue(item, Enum.Parse(property.PropertyType, (string)value));
                                }
                                else
                                {
                                    property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                                }
                            }
                            catch
                            {
                                // Ignorar errores de conversión, mantener valor predeterminado
                            }
                        }
                    }
                }
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Convierte un DataReader a una lista de objetos dinámicos.
        /// </summary>
        public static IEnumerable<dynamic> ToDynamic(this DbDataReader reader)
        {
            var result = new List<dynamic>();

            while (reader.Read())
            {
                var expandoObject = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    expandoObject[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                result.Add((dynamic)expandoObject);
            }

            return result;
        }

        /// <summary>
        /// Obtiene el valor de una columna con seguridad de tipos.
        /// </summary>
        public static T GetValueOrDefault<T>(this IDataRecord record, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = record.GetOrdinal(columnName);
                if (record.IsDBNull(ordinal))
                {
                    return defaultValue;
                }

                object value = record.GetValue(ordinal);
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Obtiene el valor de una columna con seguridad de tipos.
        /// </summary>
        public static T GetValueOrDefault<T>(this IDataRecord record, int ordinal, T defaultValue = default)
        {
            try
            {
                if (record.IsDBNull(ordinal))
                {
                    return defaultValue;
                }

                object value = record.GetValue(ordinal);
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Convierte un DataReader a un DataTable.
        /// </summary>
        public static DataTable ToDataTable(this DbDataReader reader)
        {
            var result = new DataTable();
            result.Load(reader);
            return result;
        }
    }
}