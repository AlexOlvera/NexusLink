using System;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo para validar la longitud de una cadena
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class StringLengthAttribute : Attribute
    {
        /// <summary>
        /// Longitud máxima permitida
        /// </summary>
        public int MaximumLength { get; }

        /// <summary>
        /// Longitud mínima permitida
        /// </summary>
        public int MinimumLength { get; set; }

        /// <summary>
        /// Mensaje de error personalizado
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor con longitud máxima
        /// </summary>
        public StringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
            MinimumLength = 0;
            ErrorMessage = $"La longitud debe ser de máximo {maximumLength} caracteres";
        }

        /// <summary>
        /// Establece la longitud mínima y actualiza el mensaje de error
        /// </summary>
        public StringLengthAttribute WithMinimumLength(int minimumLength)
        {
            MinimumLength = minimumLength;
            ErrorMessage = $"La longitud debe estar entre {MinimumLength} y {MaximumLength} caracteres";
            return this;
        }
    }
}