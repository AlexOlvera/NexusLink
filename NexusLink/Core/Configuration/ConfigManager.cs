using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace NexusLink.Core.Configuration
{
    /// <summary>
    /// Administra la configuración de NexusLink con compatibilidad para .NET Framework y .NET Core/.NET 5+
    /// </summary>
    public static class ConfigManager
    {
        private static IConfiguration _configuration;

        /// <summary>
        /// Inicializa el ConfigManager con una configuración de .NET Core/.NET 5+
        /// </summary>
        /// <param name="configuration">La configuración de la aplicación</param>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene una cadena de conexión por nombre, compatible con ambos frameworks
        /// </summary>
        /// <param name="name">Nombre de la cadena de conexión</param>
        /// <returns>La cadena de conexión o null si no se encuentra</returns>
        public static string GetConnectionString(string name)
        {
            // Intenta .NET Core/.NET 5+ primero
            if (_configuration != null)
            {
                string connStr = _configuration.GetConnectionString(name);
                if (!string.IsNullOrEmpty(connStr)) return connStr;
            }

            // Fallback a .NET Framework
            try
            {
                var settings = ConfigurationManager.ConnectionStrings[name];
                return settings?.ConnectionString;
            }
            catch
            {
                // Último intento con ConfigurationSettings (obsoleto pero compatible)
                try
                {
                    return ConfigurationSettings.AppSettings[name];
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Obtiene un valor de configuración por clave
        /// </summary>
        /// <param name="key">Clave de configuración</param>
        /// <returns>El valor de configuración o null si no se encuentra</returns>
        public static string GetAppSetting(string key)
        {
            // Intenta .NET Core/.NET 5+ primero
            if (_configuration != null)
            {
                string value = _configuration[key];
                if (!string.IsNullOrEmpty(value)) return value;
            }

            // Fallback a .NET Framework
            try
            {
                return ConfigurationManager.AppSettings[key];
            }
            catch
            {
                try
                {
                    return ConfigurationSettings.AppSettings[key];
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Obtiene un valor tipado de configuración
        /// </summary>
        /// <typeparam name="T">Tipo del valor</typeparam>
        /// <param name="key">Clave de configuración</param>
        /// <param name="defaultValue">Valor predeterminado si no se encuentra</param>
        /// <returns>El valor convertido al tipo especificado</returns>
        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            string value = GetAppSetting(key);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                // Convertir tipos comunes
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value);
                else if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);
                else if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(value);
                else if (typeof(T) == typeof(TimeSpan))
                    return (T)(object)TimeSpan.Parse(value);
                else
                    return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}