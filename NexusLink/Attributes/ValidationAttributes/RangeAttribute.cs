using System;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo para validar que un valor esté dentro de un rango
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        /// <summary>
        /// Valor mínimo del rango
        /// </summary>
        public object Minimum { get; }

        /// <summary>
        /// Valor máximo del rango
        /// </summary>
        public object Maximum { get; }

        /// <summary>
        /// Tipo del rango
        /// </summary>
        public Type OperandType { get; }

        /// <summary>
        /// Mensaje de error personalizado
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor con rango numérico
        /// </summary>
        public RangeAttribute(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(double);
            ErrorMessage = $"El valor debe estar entre {minimum} y {maximum}";
        }

        /// <summary>
        /// Constructor con rango entero
        /// </summary>
        public RangeAttribute(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(int);
            ErrorMessage = $"El valor debe estar entre {minimum} y {maximum}";
        }

        /// <summary>
        /// Constructor con rango de tipo personalizado
        /// </summary>
        public RangeAttribute(Type type, string minimum, string maximum)
        {
            OperandType = type;
            Minimum = minimum;
            Maximum = maximum;
            ErrorMessage = $"El valor debe estar entre {minimum} y {maximum}";
        }
    }
}