using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NexusLink.Utilities.SqlResources
{
    /// <summary>
    /// Cargador de scripts SQL desde archivos
    /// </summary>
    public class ScriptLoader
    {
        private readonly string _scriptDirectory;
        private readonly Dictionary<string, string> _scriptCache;

        /// <summary>
        /// Constructor con directorio predeterminado
        /// </summary>
        public ScriptLoader()
        {
            _scriptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlScripts");
            _scriptCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Constructor con directorio personalizado
        /// </summary>
        public ScriptLoader(string scriptDirectory)
        {
            _scriptDirectory = scriptDirectory ?? throw new ArgumentNullException(nameof(scriptDirectory));
            _scriptCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene todos los archivos de script SQL
        /// </summary>
        public IEnumerable<string> GetAllScriptFiles()
        {
            if (!Directory.Exists(_scriptDirectory))
                yield break;

            foreach (string file in Directory.GetFiles(_scriptDirectory, "*.sql", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        /// <summary>
        /// Obtiene el contenido de un script SQL desde un archivo
        /// </summary>
        public string LoadSqlScript(string scriptName)
        {
            Guard.NotNullOrEmpty(scriptName, nameof(scriptName));

            if (!scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                scriptName += ".sql";

            if (_scriptCache.TryGetValue(scriptName, out string cachedScript))
                return cachedScript;

            string scriptPath = Path.Combine(_scriptDirectory, scriptName);

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script SQL '{scriptName}' no encontrado en {_scriptDirectory}");

            string script = File.ReadAllText(scriptPath);
            _scriptCache[scriptName] = script;

            return script;
        }

        /// <summary>
        /// Obtiene el contenido de un script SQL desde un archivo y aplica parámetros
        /// </summary>
        public string LoadSqlScript(string scriptName, Dictionary<string, object> parameters)
        {
            string script = LoadSqlScript(scriptName);

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
        /// Guarda un script SQL en un archivo
        /// </summary>
        public void SaveSqlScript(string scriptName, string scriptContent)
        {
            Guard.NotNullOrEmpty(scriptName, nameof(scriptName));
            Guard.NotNull(scriptContent, nameof(scriptContent));

            if (!scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                scriptName += ".sql";

            string scriptPath = Path.Combine(_scriptDirectory, scriptName);

            // Crear directorio si no existe
            string directory = Path.GetDirectoryName(scriptPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(scriptPath, scriptContent);

            // Actualizar caché
            _scriptCache[scriptName] = scriptContent;
        }

        /// <summary>
        /// Obtiene scripts SQL agrupados por categoría
        /// </summary>
        public Dictionary<string, List<string>> GetScriptsByCategory()
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(_scriptDirectory))
                return result;

            foreach (string file in Directory.GetFiles(_scriptDirectory, "*.sql", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(_scriptDirectory, file);
                string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);

                if (pathParts.Length < 2)
                {
                    string fileName = Path.GetFileName(file);
                    AddScriptToCategory(result, "General", fileName);
                }
                else
                {
                    string category = pathParts[0];
                    string fileName = Path.GetFileName(file);
                    AddScriptToCategory(result, category, fileName);
                }
            }

            return result;
        }

        private void AddScriptToCategory(Dictionary<string, List<string>> categories, string category, string scriptName)
        {
            if (!categories.TryGetValue(category, out List<string> scripts))
            {
                scripts = new List<string>();
                categories[category] = scripts;
            }

            scripts.Add(scriptName);
        }
    }
}
