using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NexusLink.Utilities.SqlResources
{
    /// <summary>
    /// Administrador de recursos SQL embebidos
    /// </summary>
    public class ResourceManager
    {
        private readonly Assembly _assembly;
        private readonly string _resourceNamespace;
        private readonly Dictionary<string, string> _scriptCache;

        /// <summary>
        /// Constructor con assembly y namespace predeterminados
        /// </summary>
        public ResourceManager()
        {
            _assembly = Assembly.GetExecutingAssembly();
            _resourceNamespace = "NexusLink.SqlScripts";
            _scriptCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Constructor con assembly y namespace personalizados
        /// </summary>
        public ResourceManager(Assembly assembly, string resourceNamespace)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceNamespace = resourceNamespace ?? throw new ArgumentNullException(nameof(resourceNamespace));
            _scriptCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene todos los nombres de recursos embebidos
        /// </summary>
        public IEnumerable<string> GetAllResourceNames()
        {
            return _assembly.GetManifestResourceNames();
        }

        /// <summary>
        /// Obtiene el contenido de un script SQL embebido
        /// </summary>
        public string GetSqlScript(string scriptName)
        {
            Guard.NotNullOrEmpty(scriptName, nameof(scriptName));

            if (!scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                scriptName += ".sql";

            if (_scriptCache.TryGetValue(scriptName, out string cachedScript))
                return cachedScript;

            string resourceName = $"{_resourceNamespace}.{scriptName}";

            using (Stream stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Script SQL '{scriptName}' no encontrado en {_resourceNamespace}");

                using (StreamReader reader = new StreamReader(stream))
                {
                    string script = reader.ReadToEnd();
                    _scriptCache[scriptName] = script;
                    return script;
                }
            }
        }

        /// <summary>
        /// Obtiene el contenido de un script SQL embebido y aplica parámetros
        /// </summary>
        public string GetSqlScript(string scriptName, Dictionary<string, object> parameters)
        {
            string script = GetSqlScript(scriptName);

            if (parameters != null && parameters.Count > 0)
            {
                script = ApplyParameters(script, parameters);
            }

            return script;
        }

        /// <summary>
        /// Reemplaza parámetros en un script SQL
        /// </summary>
        private string ApplyParameters(string script, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                string pattern = $@"@{param.Key}\b";
                string value = param.Value?.ToString() ?? "NULL";

                if (param.Value is string)
                    value = $"'{value}'";
                else if (param.Value is DateTime)
                    value = $"'{((DateTime)param.Value).ToString("yyyy-MM-dd HH:mm:ss")}'";
                else if (param.Value is bool)
                    value = (bool)param.Value ? "1" : "0";
                else if (param.Value == null)
                    value = "NULL";

                script = Regex.Replace(script, pattern, value);
            }

            return script;
        }

        /// <summary>
        /// Obtiene scripts SQL agrupados por categoría
        /// </summary>
        public Dictionary<string, List<string>> GetScriptsByCategory()
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string resourceName in GetAllResourceNames())
            {
                if (!resourceName.StartsWith(_resourceNamespace) || !resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    continue;

                string relativePath = resourceName.Substring(_resourceNamespace.Length + 1);
                string[] parts = relativePath.Split('.');

                if (parts.Length < 3)
                    continue;

                string category = parts[0];

                if (!result.TryGetValue(category, out List<string> scripts))
                {
                    scripts = new List<string>();
                    result[category] = scripts;
                }

                scripts.Add(relativePath);
            }

            return result;
        }
    }
}
