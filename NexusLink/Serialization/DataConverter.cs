using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NexusLink.Serialization
{
    /// <summary>
    /// Proporciona métodos para convertir tipos de datos de ADO.NET a otros formatos.
    /// </summary>
    public static class DataConverter
    {
        #region DataTable Conversions

        /// <summary>
        /// Convierte un DataTable a una cadena JSON.
        /// </summary>
        public static string DataTableToJson(DataTable dataTable, bool indented = true)
        {
            if (dataTable == null) return null;

            try
            {
                var rows = new List<Dictionary<string, object>>();

                foreach (DataRow dr in dataTable.Rows)
                {
                    var row = new Dictionary<string, object>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        row[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                    }
                    rows.Add(row);
                }

                return JsonConvert.SerializeObject(rows,
                    indented ? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex)
            {
                throw new ConversionException("Error converting DataTable to JSON", ex);
            }
        }

        /// <summary>
        /// Convierte un DataTable a una lista de objetos dinámicos.
        /// </summary>
        public static IEnumerable<dynamic> DataTableToDynamic(DataTable dataTable)
        {
            if (dataTable == null) return Enumerable.Empty<dynamic>();

            try
            {
                return dataTable.AsEnumerable().Select(row => {
                    var expandoObject = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        expandoObject[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                    }
                    return (dynamic)expandoObject;
                });
            }
            catch (Exception ex)
            {
                throw new ConversionException("Error converting DataTable to dynamic objects", ex);
            }
        }

        /// <summary>
        /// Convierte un DataTable a una lista de objetos tipados.
        /// </summary>
        public static List<T> DataTableToList<T>(DataTable dataTable) where T : class, new()
        {
            if (dataTable == null) return new List<T>();

            try
            {
                var properties = typeof(T).GetProperties();
                var result = new List<T>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var item = new T();
                    foreach (var property in properties)
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            var value = row[property.Name];
                            if (value != DBNull.Value)
                            {
                                property.SetValue(item, Convert.ChangeType(value, property.PropertyType), null);
                            }
                        }
                    }
                    result.Add(item);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new ConversionException("Error converting DataTable to List<T>", ex);
            }
        }

        #endregion

        #region DataSet Conversions

        /// <summary>
        /// Convierte un DataSet a una cadena JSON.
        /// </summary>
        public static string DataSetToJson(DataSet dataSet, bool indented = true)
        {
            if (dataSet == null) return null;

            try
            {
                var result = new Dictionary<string, object>();

                foreach (DataTable table in dataSet.Tables)
                {
                    result[table.TableName] = DataTableToDynamic(table);
                }

                return JsonConvert.SerializeObject(result,
                    indented ? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex)
            {
                throw new ConversionException("Error converting DataSet to JSON", ex);
            }
        }

        #endregion

        #region JSON Conversions

        /// <summary>
        /// Convierte una cadena JSON a un DataTable.
        /// </summary>
        public static DataTable JsonToDataTable(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (items == null || items.Count == 0) return null;

                var dataTable = new DataTable();
                var columns = items[0].Keys;

                // Crear columnas
                foreach (var column in columns)
                {
                    dataTable.Columns.Add(column);
                }

                // Añadir filas
                foreach (var item in items)
                {
                    var row = dataTable.NewRow();
                    foreach (var key in item.Keys)
                    {
                        row[key] = item[key] ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                throw new ConversionException("Error converting JSON to DataTable", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Excepción personalizada para errores de conversión.
    /// </summary>
    public class ConversionException : Exception
    {
        public ConversionException(string message) : base(message) { }
        public ConversionException(string message, Exception innerException) : base(message, innerException) { }
    }
}