using System;

namespace NexusLink.Attributes.ValidationAttributes
{
    /// <summary>
    /// Atributo para implementar validación personalizada
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class CustomValidationAttribute : Attribute
    {
        /// <summary>
        /// Tipo que contiene el método de validación
        /// </summary>
        public Type ValidatorType { get; }

        /// <summary>
        /// Nombre del método de validación
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Mensaje de error personalizado
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor con tipo y método de validación
        /// </summary>
        public CustomValidationAttribute(Type validatorType, string method)
        {
            ValidatorType = validatorType;
            Method = method;
            ErrorMessage = "El valor no es válido";
        }

        /// <summary>
        /// Ejecuta el método de validación personalizado
        /// </summary>
        public ValidationResult Validate(object value)
        {
            if (ValidatorType == null || string.IsNullOrEmpty(Method))
                return new ValidationResult { IsValid = false, ErrorMessage = "Configuración de validación inválida" };

            try
            {
                var methodInfo = ValidatorType.GetMethod(Method);

                if (methodInfo == null)
                    return new ValidationResult { IsValid = false, ErrorMessage = $"Método de validación '{Method}' no encontrado" };

                var parameters = methodInfo.GetParameters();

                if (parameters.Length != 1)
                    return new ValidationResult { IsValid = false, ErrorMessage = "El método de validación debe tener un parámetro" };

                object result;

                if (methodInfo.IsStatic)
                {
                    result = methodInfo.Invoke(null, new[] { value });
                }
                else
                {
                    var instance = Activator.CreateInstance(ValidatorType);
                    result = methodInfo.Invoke(instance, new[] { value });
                }

                if (result is bool boolResult)
                {
                    return new ValidationResult { IsValid = boolResult, ErrorMessage = boolResult ? null : ErrorMessage };
                }
                else if (result is ValidationResult validationResult)
                {
                    return validationResult;
                }

                return new ValidationResult { IsValid = false, ErrorMessage = "El método de validación debe devolver bool o ValidationResult" };
            }
            catch (Exception ex)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Error en validación: {ex.Message}" };
            }
        }
    }

    /// <summary>
    /// Resultado de una validación
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indica si la validación fue exitosa
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Mensaje de error en caso de validación fallida
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}