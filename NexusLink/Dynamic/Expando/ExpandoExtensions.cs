using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace NexusLink.Dynamic.Expando
{
    /// <summary>
    /// Extensiones para objetos dinámicos
    /// </summary>
    public static class ExpandoExtensions
    {
        /// <summary>
        /// Convierte un objeto a ExpandoObject
        /// </summary>
        public static ExpandoObject ToExpando(this object obj)
        {
            return new ExpandoObject(obj);
        }

        /// <summary>
        /// Convierte un objeto a TypedExpando
        /// </summary>
        public static TypedExpando<T> ToTypedExpando<T>(this T obj) where T : class, new()
        {
            return new TypedExpando<T>(obj);
        }

        /// <summary>
        /// Convierte un diccionario a ExpandoObject
        /// </summary>
        public static ExpandoObject ToExpando(this IDictionary<string, object> dictionary)
        {
            var expando = new ExpandoObject();

            foreach (var kvp in dictionary)
            {
                expando[kvp.Key] = kvp.Value;
            }

            return expando;
        }

        /// <summary>
        /// Convierte un DataRow a ExpandoObject
        /// </summary>
        public static ExpandoObject ToExpando(this DataRow row)
        {
            var expando = new ExpandoObject();

            foreach (DataColumn column in row.Table.Columns)
            {
                expando[column.ColumnName] = row[column];
            }

            return expando;
        }

        /// <summary>
        /// Convierte un DataTable a una lista de ExpandoObject
        /// </summary>
        public static List<ExpandoObject> ToExpandoList(this DataTable table)
        {
            var list = new List<ExpandoObject>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(row.ToExpando());
            }

            return list;
        }

        /// <summary>
        /// Mezcla dos objetos expandibles
        /// </summary>
        public static ExpandoObject Merge(this ExpandoObject expandoA, ExpandoObject expandoB)
        {
            var result = new ExpandoObject();

            // Copiar propiedades del primer objeto
            foreach (var kvp in expandoA)
            {
                result[kvp.Key] = kvp.Value;
            }

            // Añadir o sobrescribir propiedades del segundo objeto
            foreach (var kvp in expandoB)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// Convierte un ExpandoObject a un tipo específico
        /// </summary>
        public static T ToObject<T>(this ExpandoObject expando) where T : new()
        {
            T result = new T();

            // Obtener propiedades del tipo destino
            Dictionary<string, PropertyInfo> properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // Establecer valores
            foreach (var kvp in expando)
            {
                if (properties.TryGetValue(kvp.Key, out PropertyInfo property))
                {
                    try
                    {
                        // Convertir valor si es necesario
                        object convertedValue = kvp.Value;

                        if (kvp.Value != null && kvp.Value.GetType() != property.PropertyType)
                        {
                            convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        }

                        property.SetValue(result, convertedValue);
                    }
                    catch
                    {
                        // Ignorar errores de conversión
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Copia todas las propiedades entre dos objetos
        /// </summary>
        public static void CopyPropertiesTo(this object source, object target)
        {
            // Si la fuente es dinámica, tratarla especialmente
            if (source is IDynamicMetaObjectProvider dynamicSource)
            {
                var expandoSource = dynamicSource as IDictionary<string, object>
                    ?? new ExpandoObject(source);

                // Si el destino es dinámico, copiar directamente
                if (target is IDynamicMetaObjectProvider)
                {
                    var expandoTarget = target as IDictionary<string, object>
                        ?? new ExpandoObject(target);

                    foreach (var kvp in expandoSource)
                    {
                        expandoTarget[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Si el destino no es dinámico, usar reflexión
                    Type targetType = target.GetType();
                    foreach (var kvp in expandoSource)
                    {
                        PropertyInfo property = targetType.GetProperty(kvp.Key);
                        if (property != null && property.CanWrite)
                        {
                            try
                            {
                                property.SetValue(target, kvp.Value);
                            }
                            catch
                            {
                                // Ignorar errores de conversión
                            }
                        }
                    }
                }

                return;
            }

            // Copia normal a través de reflexión
            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            foreach (PropertyInfo sourceProperty in sourceType.GetProperties())
            {
                PropertyInfo targetProperty = targetType.GetProperty(sourceProperty.Name);
                if (targetProperty != null && targetProperty.CanWrite)
                {
                    try
                    {
                        object value = sourceProperty.GetValue(source);
                        targetProperty.SetValue(target, value);
                    }
                    catch
                    {
                        // Ignorar errores de conversión
                    }
                }
            }
        }
    }
}