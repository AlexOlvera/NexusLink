using System;
using System.Text.RegularExpressions;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo para validar una cadena contra una expresión regular
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegexAttribute : Attribute
    {
        /// <summary>
        /// Patrón de expresión regular
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Opciones de expresión regular
        /// </summary>
        public RegexOptions Options { get; set; }

        /// <summary>
        /// Mensaje de error personalizado
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor con patrón
        /// </summary>
        public RegexAttribute(string pattern)
        {
            Pattern = pattern;
            Options = RegexOptions.None;
            ErrorMessage = "El valor no coincide con el formato requerido";
        }

        /// <summary>
        /// Constructor con patrón y opciones
        /// </summary>
        public RegexAttribute(string pattern, RegexOptions options)
        {
            Pattern = pattern;
            Options = options;
            ErrorMessage = "El valor no coincide con el formato requerido";
        }

        /// <summary>
        /// Valida una cadena contra la expresión regular
        /// </summary>
        public bool IsValid(string value)
        {
            if (value == null)
                return false;

            return Regex.IsMatch(value, Pattern, Options);
        }
    }
}