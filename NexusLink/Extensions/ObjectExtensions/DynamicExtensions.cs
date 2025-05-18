using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace NexusLink.Extensions.ObjectExtensions
{
    public static class DynamicExtensions
    {
        /// <summary>
        /// Convierte un objeto a un ExpandoObject
        /// </summary>
        public static ExpandoObject ToExpando(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var expando = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)expando;

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dictionary[property.Name] = property.GetValue(obj);
            }

            return expando;
        }

        /// <summary>
        /// Convierte un diccionario a un ExpandoObject
        /// </summary>
        public static ExpandoObject ToExpando(this IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            var expando = new ExpandoObject();
            var expandoDictionary = (IDictionary<string, object>)expando;

            foreach (var item in dictionary)
            {
                expandoDictionary[item.Key] = item.Value;
            }

            return expando;
        }

        /// <summary>
        /// Agrega propiedades dinámicas desde otro objeto
        /// </summary>
        public static dynamic AddProperties(this ExpandoObject expando, object source)
        {
            if (expando == null)
                throw new ArgumentNullException(nameof(expando));

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var dictionary = (IDictionary<string, object>)expando;

            foreach (var property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dictionary[property.Name] = property.GetValue(source);
            }

            return expando;
        }

        /// <summary>
        /// Recupera un valor desde un objeto dinámico de manera segura
        /// </summary>
        public static T GetValue<T>(this ExpandoObject expando, string key, T defaultValue = default(T))
        {
            if (expando == null)
                throw new ArgumentNullException(nameof(expando));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var dictionary = (IDictionary<string, object>)expando;

            if (dictionary.TryGetValue(key, out object value))
            {
                if (value == null)
                    return defaultValue;

                if (value is T)
                    return (T)value;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Verifica si un objeto dinámico contiene una propiedad
        /// </summary>
        public static bool HasProperty(this ExpandoObject expando, string name)
        {
            if (expando == null)
                throw new ArgumentNullException(nameof(expando));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Property name cannot be null or empty", nameof(name));

            return ((IDictionary<string, object>)expando).ContainsKey(name);
        }
    }
}
