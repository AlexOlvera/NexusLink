using System.Collections.Generic;
using System.Reflection;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Información de tabla para mapeo de entidades
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// Nombre de la tabla
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Esquema de la tabla
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Base de datos de la tabla
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Mapeos de propiedades a columnas
        /// </summary>
        public Dictionary<PropertyInfo, ColumnInfo> ColumnMappings { get; set; }

        /// <summary>
        /// Índice de la tabla (si lo tiene)
        /// </summary>
        public int? TableIndex { get; set; }

        /// <summary>
        /// Obtiene el nombre completo de la tabla con esquema
        /// </summary>
        public string GetFullTableName()
        {
            return $"[{Schema}].[{TableName}]";
        }

        /// <summary>
        /// Obtiene el nombre completo de la tabla con base de datos y esquema
        /// </summary>
        public string GetFullTableNameWithDatabase()
        {
            if (string.IsNullOrEmpty(Database))
            {
                return GetFullTableName();
            }

            return $"[{Database}].[{Schema}].[{TableName}]";
        }
    }
}