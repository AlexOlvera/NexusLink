using System;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo para marcar una propiedad como requerida
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : Attribute
    {
        /// <summary>
        /// Mensaje de error personalizado
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Indica si se deben permitir cadenas vacías
        /// </summary>
        public bool AllowEmptyStrings { get; set; }

        /// <summary>
        /// Constructor predeterminado
        /// </summary>
        public RequiredAttribute()
        {
            ErrorMessage = "El campo es requerido";
            AllowEmptyStrings = false;
        }
    }
}