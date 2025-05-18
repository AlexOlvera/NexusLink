using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace NexusLink.Utilities.SqlSecurity
{
    /// <summary>
    /// Proporciona métodos para sanitizar entradas SQL.
    /// </summary>
    public static class SqlSanitizer
    {
        // Patrones peligrosos para detectar
        private static readonly Regex SqlInjectionPattern = new Regex(
            @"('(''|[^'])*')|(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?|FROM|WHERE|ORDER +BY|GROUP +BY|HAVING)\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Verifica si una cadena contiene posibles intentos de inyección SQL.
        /// </summary>
        public static bool MayContainSqlInjection(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return SqlInjectionPattern.IsMatch(input);
        }

        /// <summary>
        /// Sanitiza una cadena para uso seguro en consultas SQL.
        /// </summary>
        public static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Escapar comillas simples duplicándolas (estándar SQL)
            return input.Replace("'", "''");
        }

        /// <summary>
        /// Verifica y sanitiza una cadena, lanzando excepción si contiene posibles inyecciones.
        /// </summary>
        public static string SafeSanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            if (MayContainSqlInjection(input))
            {
                throw new SecurityException("Posible intento de inyección SQL detectado");
            }

            return SanitizeString(input);
        }
    }

    /// <summary>
    /// Excepción de seguridad personalizada.
    /// </summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}