using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace NexusLink.Utilities
{
    /// <summary>
    /// Clase de utilidad para validación de contratos
    /// </summary>
    public static class Ensure
    {
        /// <summary>
        /// Verifica que una condición sea verdadera
        /// </summary>
        [DebuggerStepThrough]
        public static void That(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Verifica que una condición sea verdadera con formato de mensaje
        /// </summary>
        [DebuggerStepThrough]
        public static void That(bool condition, string message, params object[] args)
        {
            if (!condition)
                throw new InvalidOperationException(string.Format(message, args));
        }

        /// <summary>
        /// Verifica que un objeto no sea nulo
        /// </summary>
        [DebuggerStepThrough]
        public static void NotNull(object value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Verifica que una cadena no sea nula o vacía
        /// </summary>
        [DebuggerStepThrough]
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", parameterName);
        }

        /// <summary>
        /// Verifica que una colección no sea nula o vacía
        /// </summary>
        [DebuggerStepThrough]
        public static void NotNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
        {
            NotNull(collection, parameterName);

            if (!collection.Any())
                throw new ArgumentException("Collection cannot be empty.", parameterName);
        }

        /// <summary>
        /// Verifica que un valor esté en un rango
        /// </summary>
        [DebuggerStepThrough]
        public static void InRange<T>(T value, T min, T max, string parameterName) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(parameterName, value,
                    $"Value must be between {min} and {max}.");
        }

        /// <summary>
        /// Verifica que un valor sea mayor que un mínimo
        /// </summary>
        [DebuggerStepThrough]
        public static void GreaterThan<T>(T value, T min, string parameterName) where T : IComparable<T>
        {
            if (value.CompareTo(min) <= 0)
                throw new ArgumentOutOfRangeException(parameterName, value,
                    $"Value must be greater than {min}.");
        }

        /// <summary>
        /// Verifica que un valor sea menor que un máximo
        /// </summary>
        [DebuggerStepThrough]
        public static void LessThan<T>(T value, T max, string parameterName) where T : IComparable<T>
        {
            if (value.CompareTo(max) >= 0)
                throw new ArgumentOutOfRangeException(parameterName, value,
                    $"Value must be less than {max}.");
        }

        /// <summary>
        /// Lanza una excepción con un mensaje personalizado
        /// </summary>
        [DebuggerStepThrough]
        public static void Fail(string message)
        {
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Lanza una excepción con un mensaje personalizado formateado
        /// </summary>
        [DebuggerStepThrough]
        public static void Fail(string message, params object[] args)
        {
            throw new InvalidOperationException(string.Format(message, args));
        }

        /// <summary>
        /// Verifica que un objeto sea de un tipo específico
        /// </summary>
        [DebuggerStepThrough]
        public static void IsType<T>(object value, string parameterName)
        {
            NotNull(value, parameterName);

            if (!(value is T))
                throw new ArgumentException($"Value must be of type {typeof(T).Name}.", parameterName);
        }
    }
}