using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using NexusLink.Logging;
using NexusLink.Utilities.SqlSecurity;

namespace NexusLink.Data.Parameters
{
    /// <summary>
    /// Valida parámetros SQL para prevenir SQL Injection y otros problemas
    /// </summary>
    public class ParameterValidator
    {
        private readonly ILogger _logger;
        private readonly SqlSanitizer _sqlSanitizer;
        private readonly InjectionDetector _injectionDetector;

        public ParameterValidator(
            ILogger logger,
            SqlSanitizer sqlSanitizer,
            InjectionDetector injectionDetector)
        {
            _logger = logger;
            _sqlSanitizer = sqlSanitizer;
            _injectionDetector = injectionDetector;
        }

        /// <summary>
        /// Valida un único parámetro
        /// </summary>
        public bool ValidateParameter(DbParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            // Validar el nombre del parámetro
            bool isNameValid = ValidateParameterName(parameter.ParameterName);

            if (!isNameValid)
            {
                _logger.Warning($"Nombre de parámetro inválido: {parameter.ParameterName}");
                return false;
            }

            // Validar el valor del parámetro según su tipo
            bool isValueValid = ValidateParameterValue(parameter);

            if (!isValueValid)
            {
                _logger.Warning($"Valor de parámetro inválido para {parameter.ParameterName}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida múltiples parámetros
        /// </summary>
        public bool ValidateParameters(IEnumerable<DbParameter> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            bool isValid = true;

            foreach (var parameter in parameters)
            {
                if (!ValidateParameter(parameter))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Valida el nombre de un parámetro
        /// </summary>
        public bool ValidateParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            // El nombre debe comenzar con @
            if (!parameterName.StartsWith("@"))
            {
                return false;
            }

            // Validar que el nombre solo contenga caracteres alfanuméricos, guiones bajos y el símbolo @
            string pattern = @"^@[a-zA-Z0-9_]+$";
            return Regex.IsMatch(parameterName, pattern);
        }

        /// <summary>
        /// Valida el valor de un parámetro según su tipo
        /// </summary>
        public bool ValidateParameterValue(DbParameter parameter)
        {
            if (parameter.Value == null || parameter.Value == DBNull.Value)
            {
                // Los valores nulos son válidos
                return true;
            }

            // Para parámetros de salida, no es necesario validar el valor inicial
            if (parameter.Direction == ParameterDirection.Output ||
                parameter.Direction == ParameterDirection.ReturnValue)
            {
                return true;
            }

            switch (parameter.DbType)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return ValidateStringValue(parameter.Value.ToString());

                case DbType.Binary:
                    return ValidateBinaryValue(parameter.Value as byte[]);

                case DbType.Boolean:
                    return true; // Los booleanos siempre son válidos

                case DbType.Byte:
                case DbType.Currency:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.SByte:
                case DbType.Single:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                    return ValidateNumericValue(parameter.Value);

                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                case DbType.Time:
                    return ValidateDateTimeValue(parameter.Value);

                case DbType.Guid:
                    return true; // Los GUIDs siempre son válidos

                case DbType.Object:
                    // Para objetos, validar según el tipo real
                    return ValidateObjectValue(parameter.Value);

                case DbType.Xml:
                    return ValidateXmlValue(parameter.Value.ToString());

                default:
                    _logger.Warning($"Tipo de parámetro desconocido: {parameter.DbType}");
                    return false;
            }
        }

        /// <summary>
        /// Valida un valor de tipo string
        /// </summary>
        private bool ValidateStringValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            // Detectar posibles intentos de SQL Injection
            if (_injectionDetector.ContainsSqlInjection(value))
            {
                _logger.Warning($"Posible intento de SQL Injection detectado: {value}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida un valor binario
        /// </summary>
        private bool ValidateBinaryValue(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return true;
            }

            // Las validaciones de datos binarios son mínimas
            if (value.Length > 10 * 1024 * 1024) // Tamaño máximo de 10 MB
            {
                _logger.Warning($"Tamaño de datos binarios demasiado grande: {value.Length} bytes");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida un valor numérico
        /// </summary>
        private bool ValidateNumericValue(object value)
        {
            // La mayoría de los valores numéricos son seguros
            if (value is decimal decimalValue)
            {
                if (decimalValue < decimal.MinValue / 2 || decimalValue > decimal.MaxValue / 2)
                {
                    _logger.Warning($"Valor decimal fuera de rango: {decimalValue}");
                    return false;
                }
            }
            else if (value is double doubleValue)
            {
                if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                {
                    _logger.Warning($"Valor double inválido: {doubleValue}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Valida un valor de fecha y hora
        /// </summary>
        private bool ValidateDateTimeValue(object value)
        {
            if (value is DateTime dateTime)
            {
                // Validar que la fecha esté en un rango razonable
                if (dateTime < new DateTime(1900, 1, 1) || dateTime > new DateTime(9999, 12, 31))
                {
                    _logger.Warning($"Fecha fuera de rango: {dateTime}");
                    return false;
                }
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                // Validar que la fecha esté en un rango razonable
                if (dateTimeOffset < new DateTimeOffset(new DateTime(1900, 1, 1)) ||
                    dateTimeOffset > new DateTimeOffset(new DateTime(9999, 12, 31)))
                {
                    _logger.Warning($"Fecha con offset fuera de rango: {dateTimeOffset}");
                    return false;
                }
            }
            else if (value is TimeSpan timeSpan)
            {
                // Validar que el tiempo esté en un rango razonable
                if (timeSpan < TimeSpan.MinValue / 2 || timeSpan > TimeSpan.MaxValue / 2)
                {
                    _logger.Warning($"TimeSpan fuera de rango: {timeSpan}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Valida un valor de tipo objeto
        /// </summary>
        private bool ValidateObjectValue(object value)
        {
            // Para objetos, validar según el tipo real
            if (value is string stringValue)
            {
                return ValidateStringValue(stringValue);
            }
            else if (value is byte[] byteArrayValue)
            {
                return ValidateBinaryValue(byteArrayValue);
            }
            else if (value is int || value is long || value is short || value is decimal || value is double || value is float)
            {
                return ValidateNumericValue(value);
            }
            else if (value is DateTime || value is DateTimeOffset || value is TimeSpan)
            {
                return ValidateDateTimeValue(value);
            }
            else if (value is bool)
            {
                return true; // Los booleanos siempre son válidos
            }
            else if (value is Guid)
            {
                return true; // Los GUIDs siempre son válidos
            }
            else if (value is DataTable)
            {
                // Las tablas se validarán internamente por SQL Server
                return true;
            }
            else
            {
                // Para otros tipos de objetos, se considera una advertencia
                _logger.Warning($"Tipo de objeto no validado específicamente: {value.GetType().Name}");
                return true;
            }
        }

        /// <summary>
        /// Valida un valor XML
        /// </summary>
        private bool ValidateXmlValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            try
            {
                // Intentar cargar el XML para validar su formato
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(value);
                return true;
            }
            catch (System.Xml.XmlException ex)
            {
                _logger.Warning($"XML inválido: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sanitiza un conjunto de parámetros para prevenir SQL Injection
        /// </summary>
        public IEnumerable<DbParameter> SanitizeParameters(IEnumerable<DbParameter> parameters)
        {
            var sanitizedParameters = new List<DbParameter>();

            foreach (var parameter in parameters)
            {
                var sanitizedParameter = SanitizeParameter(parameter);
                sanitizedParameters.Add(sanitizedParameter);
            }

            return sanitizedParameters;
        }

        /// <summary>
        /// Sanitiza un parámetro para prevenir SQL Injection
        /// </summary>
        public DbParameter SanitizeParameter(DbParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            // Para valores string, aplicar sanitización
            if (parameter.Value is string stringValue)
            {
                parameter.Value = _sqlSanitizer.SanitizeString(stringValue);
            }

            return parameter;
        }
    }
}