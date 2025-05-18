using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NexusLink.Extensions.ObjectExtensions
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Obtiene propiedades con un atributo específico
        /// </summary>
        public static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<T>(this Type type) where T : Attribute
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(T), true).Any());
        }

        /// <summary>
        /// Obtiene un atributo específico de un miembro
        /// </summary>
        public static T GetAttribute<T>(this MemberInfo member) where T : Attribute
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            return (T)member.GetCustomAttributes(typeof(T), true).FirstOrDefault();
        }

        /// <summary>
        /// Obtiene un valor de propiedad de forma segura
        /// </summary>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.Name}'");

            return property.GetValue(obj);
        }

        /// <summary>
        /// Establece un valor de propiedad de forma segura
        /// </summary>
        public static void SetPropertyValue(this object obj, string propertyName, object value)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.Name}'");

            if (!property.CanWrite)
                throw new ArgumentException($"Property '{propertyName}' on type '{type.Name}' is read-only");

            // Convertir el valor si es necesario
            if (value != null && property.PropertyType != value.GetType())
            {
                var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                value = Convert.ChangeType(value, underlyingType);
            }

            property.SetValue(obj, value);
        }

        /// <summary>
        /// Crea una instancia de un tipo usando su constructor con los parámetros especificados
        /// </summary>
        public static object CreateInstance(this Type type, params object[] parameters)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return Activator.CreateInstance(type, parameters);
        }

        /// <summary>
        /// Convierte un objeto en otro tipo mediante mapeo de propiedades
        /// </summary>
        public static T MapTo<T>(this object source) where T : new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var result = new T();
            var sourceType = source.GetType();
            var targetType = typeof(T);

            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var targetProp in targetProperties)
            {
                var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);

                if (sourceProp != null && sourceProp.CanRead)
                {
                    var value = sourceProp.GetValue(source);

                    if (value != null && targetProp.PropertyType != sourceProp.PropertyType)
                    {
                        try
                        {
                            var underlyingType = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;
                            value = Convert.ChangeType(value, underlyingType);
                        }
                        catch
                        {
                            // No se puede convertir, omitir esta propiedad
                            continue;
                        }
                    }

                    targetProp.SetValue(result, value);
                }
            }

            return result;
        }
    }
}