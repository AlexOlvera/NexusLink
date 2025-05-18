using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace NexusLink.Extensions.ObjectExtensions
{
    /// <summary>
    /// Proporciona métodos de extensión para objetos generales.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Convierte un objeto a formato JSON.
        /// </summary>
        public static string ToJson(this object obj, bool indented = true)
        {
            return Serialization.SerializationManager.ToJson(obj, indented);
        }

        /// <summary>
        /// Convierte un objeto a formato XML.
        /// </summary>
        public static string ToXml(this object obj)
        {
            return Serialization.SerializationManager.ToXml(obj);
        }

        /// <summary>
        /// Convierte un objeto a una representación de diccionario.
        /// </summary>
        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            if (obj == null) return null;

            try
            {
                return obj.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead)
                    .ToDictionary(
                        prop => prop.Name,
                        prop => prop.GetValue(obj, null)
                    );
            }
            catch (Exception ex)
            {
                throw new Exception("Error converting object to dictionary", ex);
            }
        }

        /// <summary>
        /// Clona un objeto mediante serialización.
        /// </summary>
        public static T Clone<T>(this T obj) where T : class
        {
            if (obj == null) return null;

            try
            {
                var json = Serialization.SerializationManager.ToJson(obj, false);
                return Serialization.SerializationManager.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                throw new Exception("Error cloning object", ex);
            }
        }

        /// <summary>
        /// Convierte un objeto a una representación de DataTable.
        /// </summary>
        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            if (items == null) return null;

            try
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var table = new DataTable(typeof(T).Name);

                // Crear columnas
                foreach (var prop in properties)
                {
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }

                // Añadir filas
                foreach (var item in items)
                {
                    var row = table.NewRow();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item, null);
                        row[prop.Name] = value ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }

                return table;
            }
            catch (Exception ex)
            {
                throw new Exception("Error converting objects to DataTable", ex);
            }
        }
    }
}