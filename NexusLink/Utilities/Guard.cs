using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusLink.Utilities
{
    /// <summary>
    /// Clase de utilidad para validación de parámetros
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Verifica que un argumento no sea nulo
        /// </summary>
        public static void NotNull(object argument, string parameterName)
        {
            if (argument == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Verifica que una cadena no sea nula o vacía
        /// </summary>
        public static void NotNullOrEmpty(string argument, string parameterName)
        {
            if (string.IsNullOrEmpty(argument))
                throw new ArgumentException("El argumento no puede ser nulo o vacío.", parameterName);
        }

        /// <summary>
        /// Verifica que una cadena no sea nula, vacía o solo espacios
        /// </summary>
        public static void NotNullOrWhitespace(string argument, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(argument))
                throw new ArgumentException("El argumento no puede ser nulo, vacío o solo espacios.", parameterName);
        }

        /// <summary>
        /// Verifica que una colección no sea nula o vacía
        /// </summary>
        public static void NotNullOrEmpty<T>(IEnumerable<T> argument, string parameterName)
        {
            NotNull(argument, parameterName);

            if (!argument.Any())
                throw new ArgumentException("La colección no puede estar vacía.", parameterName);
        }

        /// <summary>
        /// Verifica que un valor esté en un rango
        /// </summary>
        public static void InRange<T>(T argument, string parameterName, T minimum, T maximum) where T : IComparable<T>
        {
            if (argument.CompareTo(minimum) < 0 || argument.CompareTo(maximum) > 0)
                throw new ArgumentOutOfRangeException(parameterName, argument,
                    $"El valor debe estar entre {minimum} y {maximum}.");
        }

        /// <summary>
        /// Verifica que un valor sea mayor que un mínimo
        /// </summary>
        public static void GreaterThan<T>(T argument, string parameterName, T minimum) where T : IComparable<T>
        {
            if (argument.CompareTo(minimum) <= 0)
                throw new ArgumentOutOfRangeException(parameterName, argument,
                    $"El valor debe ser mayor que {minimum}.");
        }

        /// <summary>
        /// Verifica que un valor sea mayor o igual que un mínimo
        /// </summary>
        public static void GreaterThanOrEqual<T>(T argument, string parameterName, T minimum) where T : IComparable<T>
        {
            if (argument.CompareTo(minimum) < 0)
                throw new ArgumentOutOfRangeException(parameterName, argument,
                    $"El valor debe ser mayor o igual que {minimum}.");
        }

        /// <summary>
        /// Verifica que un valor sea menor que un máximo
        /// </summary>
        public static void LessThan<T>(T argument, string parameterName, T maximum) where T : IComparable<T>
        {
            if (argument.CompareTo(maximum) >= 0)
                throw new ArgumentOutOfRangeException(parameterName, argument,
                    $"El valor debe ser menor que {maximum}.");
        }

        /// <summary>
        /// Verifica que un valor sea menor o igual que un máximo
        /// </summary>
        public static void LessThanOrEqual<T>(T argument, string parameterName, T maximum) where T : IComparable<T>
        {
            if (argument.CompareTo(maximum) > 0)
                throw new ArgumentOutOfRangeException(parameterName, argument,
                    $"El valor debe ser menor o igual que {maximum}.");
        }

        /// <summary>
        /// Verifica que una condición sea verdadera
        /// </summary>
        public static void IsTrue(bool condition, string message, string parameterName)
        {
            if (!condition)
                throw new ArgumentException(message, parameterName);
        }

        /// <summary>
        /// Verifica que un GUID no sea vacío
        /// </summary>
        public static void NotEmpty(Guid argument, string parameterName)
        {
            if (argument == Guid.Empty)
                throw new ArgumentException("El GUID no puede ser vacío.", parameterName);
        }
    }
}
