using NexusLink.AOP.Interception;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspect that provides validation capabilities for method parameters
    /// </summary>
    public class ValidationAspect : MethodInterceptor
    {
        private readonly IValidator _validator;

        public ValidationAspect(IValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        protected override void BeforeInvocation(IMethodInvocation invocation)
        {
            ParameterInfo[] parameters = invocation.Method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                object value = invocation.Arguments[i];

                // Skip validation for null values on reference types
                if (value == null && !parameter.ParameterType.IsValueType)
                    continue;

                // Validate the parameter
                ValidationResult validationResult = _validator.Validate(value, parameter.Name);

                if (!validationResult.IsValid)
                {
                    throw new ArgumentValidationException(parameter.Name, validationResult.Errors);
                }
            }
        }
    }

    /// <summary>
    /// Interface for validators
    /// </summary>
    public interface IValidator
    {
        ValidationResult Validate(object value, string name);
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public IList<string> Errors { get; } = new List<string>();
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ArgumentValidationException : ArgumentException
    {
        public IList<string> ValidationErrors { get; }

        public ArgumentValidationException(string paramName, IList<string> errors)
            : base($"Validation failed for parameter '{paramName}': {string.Join(", ", errors)}", paramName)
        {
            ValidationErrors = errors;
        }
    }
}