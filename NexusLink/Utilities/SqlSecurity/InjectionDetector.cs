using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NexusLink.Utilities.SqlSecurity
{
    /// <summary>
    /// Proporciona métodos para detectar y prevenir inyecciones SQL.
    /// </summary>
    public static class InjectionDetector
    {
        private static readonly HashSet<string> DangerousKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "EXEC", "EXECUTE",
            "UNION", "JOIN", "FROM", "WHERE", "HAVING", "ORDER BY", "GROUP BY", "TRUNCATE", "MERGE"
        };

        /// <summary>
        /// Verifica si una consulta SQL contiene comandos potencialmente peligrosos.
        /// </summary>
        public static bool ContainsDangerousCommands(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;

            // Eliminar comentarios SQL
            query = Regex.Replace(query, @"--.*", string.Empty);
            query = Regex.Replace(query, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            // Verificar palabras clave peligrosas
            foreach (var keyword in DangerousKeywords)
            {
                var pattern = $@"\b{keyword}\b";
                if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            // Buscar intentos de comentar el resto de la consulta
            if (query.Contains("--") || query.Contains("/*"))
            {
                return true;
            }

            // Buscar múltiples consultas
            if (Regex.IsMatch(query, @";\s*\w+", RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica consulta SQL y lanza excepción si se detectan comandos peligrosos.
        /// </summary>
        public static void VerifyQuery(string query)
        {
            if (ContainsDangerousCommands(query))
            {
                throw new SecurityException("La consulta contiene comandos SQL potencialmente peligrosos");
            }
        }
    }
}
