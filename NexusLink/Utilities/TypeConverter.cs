using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace NexusLink.Utilities
{
    /// <summary>
    /// Proporciona métodos para convertir tipos de datos de forma segura.
    /// </summary>
    public static class TypeConverter
    {
        private static readonly Dictionary<Type, TypeConverter> Converters = new Dictionary<Type, TypeConverter>();

        /// <summary>
        /// Convierte un valor al tipo especificado.
        /// </summary>
        public static T ConvertTo<T>(object value)
        {
            return (T)ConvertTo(value, typeof(T));
        }

        /// <summary>
        /// Convierte un valor al tipo especificado.
        /// </summary>
        public static object ConvertTo(object value, Type destinationType)
        {
            if (value == null || value == DBNull.Value)
            {
                return destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;
            }

            Type sourceType = value.GetType();

            // Si los tipos son idénticos o el destino puede contener el origen
            if (destinationType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            // Si el tipo de destino es nullable, obtener tipo subyacente
            Type underlyingType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingType != null)
            {
                destinationType = underlyingType;
            }

            // Conversión específica para enumeraciones
            if (destinationType.IsEnum)
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(destinationType, stringValue, true);
                }
                else if (value is int || value is byte || value is long || value is short)
                {
                    return Enum.ToObject(destinationType, value);
                }
            }

            // Conversión específica para fechas
            if (destinationType == typeof(DateTime) && value is string dateString)
            {
                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
            }

            // Conversión específica para booleanos
            if (destinationType == typeof(bool) && value is string boolString)
            {
                if (bool.TryParse(boolString, out bool result))
                {
                    return result;
                }

                // Valores comunes
                switch (boolString.ToLowerInvariant())
                {
                    case "1":
                    case "y":
                    case "yes":
                    case "s":
                    case "si":
                    case "sí":
                    case "true":
                    case "verdadero":
                        return true;
                    case "0":
                    case "n":
                    case "no":
                    case "false":
                    case "falso":
                        return false;
                }
            }

            // Intentar conversión directa
            try
            {
                return Convert.ChangeType(value, destinationType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // Intentar usar TypeConverter
                try
                {
                    TypeConverter converter;
                    if (!Converters.TryGetValue(destinationType, out converter))
                    {
                        converter = TypeDescriptor.GetConverter(destinationType);
                        Converters[destinationType] = converter;
                    }

                    if (converter.CanConvertFrom(sourceType))
                    {
                        return converter.ConvertFrom(value);
                    }
                }
                catch
                {
                    // Ignorar errores, fallar con valor predeterminado
                }
            }

            // Si todo falla, devolver valor predeterminado
            return destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;
        }

        /// <summary>
        /// Intenta convertir un valor al tipo especificado.
        /// </summary>
        public static bool TryConvertTo<T>(object value, out T result)
        {
            try
            {
                result = ConvertTo<T>(value);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Intenta convertir un valor al tipo especificado.
        /// </summary>
        public static bool TryConvertTo(object value, Type destinationType, out object result)
        {
            try
            {
                result = ConvertTo(value, destinationType);
                return true;
            }
            catch
            {
                result = destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;
                return false;
            }
        }

        /// <summary>
        /// Evalúa si un valor puede convertirse al tipo especificado.
        /// </summary>
        public static bool CanConvertTo<T>(object value)
        {
            return TryConvertTo<T>(value, out _);
        }

        /// <summary>
        /// Evalúa si un valor puede convertirse al tipo especificado.
        /// </summary>
        public static bool CanConvertTo(object value, Type destinationType)
        {
            return TryConvertTo(value, destinationType, out _);
        }
    }
}