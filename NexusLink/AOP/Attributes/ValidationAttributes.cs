using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo base para validación.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class ValidationAttribute : Attribute
    {
        public string ErrorMessage { get; set; }

        public abstract bool IsValid(object value);
    }

    /// <summary>
    /// Valida que un valor sea requerido (no nulo).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute
    {
        public RequiredAttribute()
        {
            ErrorMessage = "Este campo es requerido";
        }

        public override bool IsValid(object value)
        {
            return value != null && (!(value is string) || !string.IsNullOrWhiteSpace((string)value));
        }
    }
}