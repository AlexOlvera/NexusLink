using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink.Utilities.SqlSecurity
{
    /// <summary>
    /// Proporciona métodos para verificar y validar parámetros SQL.
    /// </summary>
    public static class ParameterGuard
    {
        /// <summary>
        /// Valida un arreglo de parámetros SQL para uso seguro.
        /// </summary>
        public static void ValidateParameters(SqlParameter[] parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                ValidateParameter(param);
            }
        }

        /// <summary>
        /// Valida un parámetro SQL para uso seguro.
        /// </summary>
        public static void ValidateParameter(SqlParameter parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            // Validar nombre del parámetro
            if (string.IsNullOrEmpty(parameter.ParameterName))
            {
                throw new ArgumentException("El nombre del parámetro no puede estar vacío");
            }

            // Asegurar que el nombre del parámetro comience con @
            if (!parameter.ParameterName.StartsWith("@"))
            {
                parameter.ParameterName = "@" + parameter.ParameterName;
            }

            // Validar que el valor no sea excesivamente grande
            if (parameter.Value is string strValue)
            {
                if (strValue.Length > 8000)
                {
                    throw new ArgumentException($"El valor del parámetro '{parameter.ParameterName}' excede la longitud máxima permitida");
                }
            }
        }

        /// <summary>
        /// Crea un parámetro SQL seguro.
        /// </summary>
        public static SqlParameter CreateSafeParameter(string name, object value)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("El nombre del parámetro no puede estar vacío");

            // Asegurar que el nombre comience con @
            if (!name.StartsWith("@"))
            {
                name = "@" + name;
            }

            var parameter = new SqlParameter(name, value ?? DBNull.Value);
            ValidateParameter(parameter);

            return parameter;
        }
    }
}
