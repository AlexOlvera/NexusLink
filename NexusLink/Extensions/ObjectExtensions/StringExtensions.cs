using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NexusLink.Extensions.ObjectExtensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Verifica si una cadena es nula o está vacía
        /// </summary>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Verifica si una cadena es nula, está vacía o sólo contiene espacios
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Trunca una cadena a una longitud máxima
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Trunca una cadena a una longitud máxima y agrega puntos suspensivos
        /// </summary>
        public static string TruncateWithEllipsis(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= maxLength)
                return value;

            // Reservar espacio para los puntos suspensivos
            maxLength = Math.Max(0, maxLength - 3);
            return value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Convierte una cadena en formato snake_case a camelCase
        /// </summary>
        public static string SnakeToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var parts = value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return string.Empty;

            var result = parts[0].ToLower();

            for (int i = 1; i < parts.Length; i++)
            {
                result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }

            return result;
        }

        /// <summary>
        /// Convierte una cadena en formato snake_case a PascalCase
        /// </summary>
        public static string SnakeToPascalCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var parts = value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return string.Empty;

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }

            return string.Join("", parts);
        }

        /// <summary>
        /// Convierte una cadena en formato camelCase o PascalCase a snake_case
        /// </summary>
        public static string ToSnakeCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
        }

        /// <summary>
        /// Convierte una cadena a formato seguro para URL
        /// </summary>
        public static string ToUrlFriendly(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Reemplazar espacios con guiones
            value = value.Replace(" ", "-");

            // Normalizar acentos y otros caracteres especiales
            value = value.Normalize(NormalizationForm.FormD);

            var chars = value.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            value = new string(chars).Normalize(NormalizationForm.FormC);

            // Eliminar caracteres no válidos para URL
            value = Regex.Replace(value, @"[^a-zA-Z0-9\-]", "");

            // Eliminar guiones múltiples
            value = Regex.Replace(value, @"-{2,}", "-");

            // Eliminar guiones al inicio y final
            value = value.Trim('-');

            return value.ToLower();
        }

        /// <summary>
        /// Convierte una cadena a su representación en Base64
        /// </summary>
        public static string ToBase64(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodifica una cadena en Base64
        /// </summary>
        public static string FromBase64(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            try
            {
                var bytes = Convert.FromBase64String(value);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// Verifica si una cadena es una dirección de email válida
        /// </summary>
        public static bool IsValidEmail(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(value);
                return addr.Address == value;
            }
            catch
            {
                return false;
            }
        }
    }
}