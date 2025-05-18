using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NexusLink.Attributes;
using NexusLink.Attributes.RelationshipAttributes;
using NexusLink.Logging;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Convenciones de mapeo para entidades
    /// </summary>
    public class MappingConvention
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, Func<string, string>> _nameTransformations;

        public MappingConvention(ILogger logger)
        {
            _logger = logger;
            _nameTransformations = new Dictionary<string, Func<string, string>>();

            // Configurar transformaciones predeterminadas
            ConfigureDefaultTransformations();
        }

        /// <summary>
        /// Configura las transformaciones predeterminadas
        /// </summary>
        private void ConfigureDefaultTransformations()
        {
            // Transformación: PascalCase -> snake_case
            _nameTransformations["PascalToSnakeCase"] = name =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
            };

            // Transformación: PascalCase -> camelCase
            _nameTransformations["PascalToCamelCase"] = name =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return char.ToLowerInvariant(name[0]) + name.Substring(1);
            };

            // Transformación: Singular -> Plural
            _nameTransformations["SingularToPlural"] = name =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                // Reglas básicas de pluralización en inglés
                if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(0, name.Length - 1) + "ies";
                }
                else if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
                {
                    return name + "es";
                }
                else
                {
                    return name + "s";
                }
            };
        }

        /// <summary>
        /// Registra una transformación de nombres personalizada
        /// </summary>
        public void RegisterTransformation(string name, Func<string, string> transformation)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El nombre de la transformación no puede estar vacío", nameof(name));
            }

            if (transformation == null)
            {
                throw new ArgumentNullException(nameof(transformation));
            }

            _nameTransformations[name] = transformation;
        }

        /// <summary>
        /// Aplica una transformación de nombres
        /// </summary>
        public string ApplyTransformation(string name, string transformationName)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (string.IsNullOrEmpty(transformationName))
            {
                return name;
            }

            if (_nameTransformations.TryGetValue(transformationName, out var transformation))
            {
                return transformation(name);
            }

            return name;
        }

        /// <summary>
        /// Aplica la convención de mapeo a un tipo de entidad
        /// </summary>
        public TableInfo ApplyConvention(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            // Obtener o crear atributos de mapeo
            var tableInfo = new TableInfo();

            // Nombre de tabla a partir del nombre del tipo
            string tableName = entityType.Name;

            // Aplicar transformación para pluralizar
            tableName = ApplyTransformation(tableName, "SingularToPlural");

            // Esquema predeterminado
            string schema = "dbo";

            // Buscar atributo de tabla existente
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            if (tableAttribute != null)
            {
                // Si hay atributo, usar sus valores
                if (!string.IsNullOrEmpty(tableAttribute.Name))
                {
                    tableName = tableAttribute.Name;
                }

                if (!string.IsNullOrEmpty(tableAttribute.Schema))
                {
                    schema = tableAttribute.Schema;
                }

                tableInfo.Database = tableAttribute.Database;
            }

            tableInfo.TableName = tableName;
            tableInfo.Schema = schema;
            tableInfo.ColumnMappings = new Dictionary<PropertyInfo, ColumnInfo>();

            // Analizar propiedades
            foreach (var property in entityType.GetProperties())
            {
                // Saltar propiedades de navegación sin atributo de columna
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                var uniqueKeyAttribute = property.GetCustomAttribute<UniqueKeyAttribute>();
                var navigationAttribute = property.GetCustomAttribute<NavigationPropertyAttribute>();

                // Si es propiedad de navegación sin atributo de columna, saltar
                if (navigationAttribute != null && columnAttribute == null &&
                    primaryKeyAttribute == null && foreignKeyAttribute == null &&
                    uniqueKeyAttribute == null)
                {
                    continue;
                }

                // Crear info de columna
                var columnInfo = new ColumnInfo();

                // Nombre de columna a partir del nombre de la propiedad
                string columnName = property.Name;

                // Detectar si es clave primaria por convención
                bool isPrimaryKey = string.Equals(columnName, "Id", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(columnName, entityType.Name + "Id", StringComparison.OrdinalIgnoreCase);

                // Detectar si es clave foránea por convención
                bool isForeignKey = columnName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                                    !isPrimaryKey;

                // Aplicar atributos existentes
                if (columnAttribute != null)
                {
                    if (!string.IsNullOrEmpty(columnAttribute.Name))
                    {
                        columnName = columnAttribute.Name;
                    }

                    columnInfo.IsRequired = columnAttribute.isRequired;
                    columnInfo.IsCriterial = columnAttribute.isCriterial;
                    columnInfo.IsIdentity = columnAttribute.isAddedAutomatically;
                }

                if (primaryKeyAttribute != null)
                {
                    isPrimaryKey = true;

                    if (!string.IsNullOrEmpty(primaryKeyAttribute.Name))
                    {
                        columnName = primaryKeyAttribute.Name;
                    }

                    columnInfo.IsIdentity = primaryKeyAttribute.isAddedAutomatically;
                    columnInfo.IsRequired = true;
                }

                if (foreignKeyAttribute != null)
                {
                    isForeignKey = true;

                    if (!string.IsNullOrEmpty(foreignKeyAttribute.Name))
                    {
                        columnName = foreignKeyAttribute.Name;
                    }

                    columnInfo.IsRequired = foreignKeyAttribute.isRequired;
                    columnInfo.IsUniqueKey = foreignKeyAttribute.isUniqueKey;
                    columnInfo.IsCriterial = foreignKeyAttribute.isCriterial = foreignKeyAttribute.isCriterial;
                    columnInfo.ReferenceTable = foreignKeyAttribute.ReferenceTableName;
                }

                if (uniqueKeyAttribute != null)
                {
                    columnInfo.IsUniqueKey = true;

                    if (!string.IsNullOrEmpty(uniqueKeyAttribute.Name))
                    {
                        columnName = uniqueKeyAttribute.Name;
                    }

                    columnInfo.IsRequired = uniqueKeyAttribute.isRequired;
                    columnInfo.IsCriterial = uniqueKeyAttribute.isCriterial;
                }

                // Asignar configuración
                columnInfo.ColumnName = columnName;
                columnInfo.IsPrimaryKey = isPrimaryKey;
                columnInfo.IsForeignKey = isForeignKey;
                columnInfo.PropertyType = property.PropertyType;

                // Registrar columna
                tableInfo.ColumnMappings[property] = columnInfo;

                // Identificar clave primaria
                if (isPrimaryKey)
                {
                    tableInfo.PrimaryKeyProperty = property;
                }
            }

            _logger.LogDebug($"Convención aplicada a entidad {entityType.Name} -> Tabla {schema}.{tableName}");

            return tableInfo;
        }
    }
}