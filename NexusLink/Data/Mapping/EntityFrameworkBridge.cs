using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NexusLink.Attributes;
using NexusLink.EntityFramework;
using NexusLink.Logging;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Proporciona compatibilidad entre el mapeo de NexusLink y Entity Framework
    /// </summary>
    public class EntityFrameworkBridge
    {
        private readonly ILogger _logger;
        private readonly EntityMapper _entityMapper;

        public EntityFrameworkBridge(ILogger logger, EntityMapper entityMapper)
        {
            _logger = logger;
            _entityMapper = entityMapper;
        }

        /// <summary>
        /// Configura una entidad de Entity Framework usando los atributos de NexusLink
        /// </summary>
        public void ConfigureEntityFromAttributes(object entityTypeBuilder, Type entityType)
        {
            if (entityTypeBuilder == null)
            {
                throw new ArgumentNullException(nameof(entityTypeBuilder));
            }

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            // Obtener información de tabla para la entidad
            TableInfo tableInfo;

            try
            {
                tableInfo = _entityMapper.GetTableInfo(entityType);
            }
            catch (Exception ex)
            {
                _logger.Warning($"No se pudo obtener información de tabla para {entityType.Name}: {ex.Message}");
                return;
            }

            // Reflection para invocar métodos en entityTypeBuilder
            var builderType = entityTypeBuilder.GetType();

            // Configurar nombre de tabla
            try
            {
                var toTableMethod = builderType.GetMethod("ToTable", new[] { typeof(string), typeof(string) });

                if (toTableMethod != null)
                {
                    toTableMethod.Invoke(entityTypeBuilder, new object[] { tableInfo.TableName, tableInfo.Schema });
                    _logger.Debug($"Configurado tabla {tableInfo.Schema}.{tableInfo.TableName} para {entityType.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error al configurar nombre de tabla para {entityType.Name}: {ex.Message}");
            }

            // Configurar clave primaria
            var primaryKeyProperties = tableInfo.ColumnMappings
                .Where(cm => cm.Value.IsPrimaryKey)
                .Select(cm => cm.Key)
                .ToList();

            if (primaryKeyProperties.Count > 0)
            {
                try
                {
                    if (primaryKeyProperties.Count == 1)
                    {
                        // Clave primaria simple
                        var hasKeyMethod = builderType.GetMethod("HasKey", new[] { typeof(string) });

                        if (hasKeyMethod != null)
                        {
                            hasKeyMethod.Invoke(entityTypeBuilder, new object[] { primaryKeyProperties[0].Name });
                            _logger.Debug($"Configurada clave primaria {primaryKeyProperties[0].Name} para {entityType.Name}");
                        }
                    }
                    else
                    {
                        // Clave primaria compuesta
                        var propertyNames = primaryKeyProperties.Select(p => p.Name).ToArray();
                        var hasKeyMethod = builderType.GetMethod("HasKey", new[] { typeof(string[]) });

                        if (hasKeyMethod != null)
                        {
                            hasKeyMethod.Invoke(entityTypeBuilder, new object[] { propertyNames });
                            _logger.Debug($"Configurada clave primaria compuesta para {entityType.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Error al configurar clave primaria para {entityType.Name}: {ex.Message}");
                }
            }

            // Configurar propiedades
            foreach (var mapping in tableInfo.ColumnMappings)
            {
                PropertyInfo property = mapping.Key;
                ColumnInfo columnInfo = mapping.Value;

                try
                {
                    // Obtener configuración de propiedad
                    var propertyMethod = builderType.GetMethod("Property", new[] { typeof(string) });

                    if (propertyMethod != null)
                    {
                        var propertyBuilder = propertyMethod.Invoke(entityTypeBuilder, new object[] { property.Name });

                        if (propertyBuilder != null)
                        {
                            var propertyBuilderType = propertyBuilder.GetType();

                            // Configurar nombre de columna
                            var hasColumnNameMethod = propertyBuilderType.GetMethod("HasColumnName", new[] { typeof(string) });

                            if (hasColumnNameMethod != null && !string.IsNullOrEmpty(columnInfo.ColumnName))
                            {
                                hasColumnNameMethod.Invoke(propertyBuilder, new object[] { columnInfo.ColumnName });
                                _logger.Debug($"Configurado nombre de columna {columnInfo.ColumnName} para propiedad {property.Name}");
                            }

                            // Configurar si es requerido
                            var isRequiredMethod = propertyBuilderType.GetMethod("IsRequired");

                            if (isRequiredMethod != null && columnInfo.IsRequired)
                            {
                                isRequiredMethod.Invoke(propertyBuilder, null);
                                _logger.Debug($"Configurada propiedad {property.Name} como requerida");
                            }

                            // Configurar si es identidad (generado por la base de datos)
                            if (columnInfo.IsIdentity)
                            {
                                var valueGeneratedOnAddMethod = propertyBuilderType.GetMethod("ValueGeneratedOnAdd");

                                if (valueGeneratedOnAddMethod != null)
                                {
                                    valueGeneratedOnAddMethod.Invoke(propertyBuilder, null);
                                    _logger.Debug($"Configurada propiedad {property.Name} con generación de valor en inserción");
                                }
                            }

                            // Configurar si es computado
                            if (columnInfo.IsComputed)
                            {
                                var valueGeneratedOnAddOrUpdateMethod = propertyBuilderType.GetMethod("ValueGeneratedOnAddOrUpdate");

                                if (valueGeneratedOnAddOrUpdateMethod != null)
                                {
                                    valueGeneratedOnAddOrUpdateMethod.Invoke(propertyBuilder, null);
                                    _logger.Debug($"Configurada propiedad {property.Name} como calculada");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Error al configurar propiedad {property.Name} para {entityType.Name}: {ex.Message}");
                }
            }

            // Configurar relaciones (claves foráneas)
            foreach (var mapping in tableInfo.ColumnMappings.Where(cm => cm.Value.IsForeignKey))
            {
                PropertyInfo property = mapping.Key;
                ColumnInfo columnInfo = mapping.Value;

                try
                {
                    // Buscar propiedades de navegación relacionadas
                    var navigationProperties = entityType.GetProperties()
                        .Where(p => {
                            var navAttr = p.GetCustomAttribute<NavigationPropertyAttribute>();
                            return navAttr != null && navAttr.ForeignKeyProperty == property.Name;
                        })
                        .ToList();

                    if (navigationProperties.Count > 0)
                    {
                        var navProperty = navigationProperties[0];

                        // Determinar tipo de relación
                        bool isCollection = IsCollectionType(navProperty.PropertyType);

                        if (isCollection)
                        {
                            // Relación uno a muchos
                            var hasManyMethod = builderType.GetMethod("HasMany", new[] { typeof(string) });

                            if (hasManyMethod != null)
                            {
                                var navigationBuilder = hasManyMethod.Invoke(entityTypeBuilder, new object[] { navProperty.Name });

                                if (navigationBuilder != null)
                                {
                                    var navBuilderType = navigationBuilder.GetType();

                                    // WithRequired o WithOptional
                                    var withRequiredMethod = navBuilderType.GetMethod(columnInfo.IsRequired ? "WithRequired" : "WithOptional", new Type[0]);

                                    if (withRequiredMethod != null)
                                    {
                                        var foreignKeyBuilder = withRequiredMethod.Invoke(navigationBuilder, null);

                                        if (foreignKeyBuilder != null)
                                        {
                                            var fkBuilderType = foreignKeyBuilder.GetType();

                                            // HasForeignKey
                                            var hasForeignKeyMethod = fkBuilderType.GetMethod("HasForeignKey", new[] { typeof(string) });

                                            if (hasForeignKeyMethod != null)
                                            {
                                                hasForeignKeyMethod.Invoke(foreignKeyBuilder, new object[] { property.Name });
                                                _logger.Debug($"Configurada relación uno a muchos para {navProperty.Name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Relación muchos a uno o uno a uno
                            var hasRequiredMethod = builderType.GetMethod(columnInfo.IsRequired ? "HasRequired" : "HasOptional", new[] { typeof(string) });

                            if (hasRequiredMethod != null)
                            {
                                var navigationBuilder = hasRequiredMethod.Invoke(entityTypeBuilder, new object[] { navProperty.Name });

                                if (navigationBuilder != null)
                                {
                                    var navBuilderType = navigationBuilder.GetType();

                                    // WithMany o WithOptionalPrincipal/WithRequiredPrincipal
                                    var withManyMethod = navBuilderType.GetMethod("WithMany", new Type[0]);

                                    if (withManyMethod != null)
                                    {
                                        var foreignKeyBuilder = withManyMethod.Invoke(navigationBuilder, null);

                                        if (foreignKeyBuilder != null)
                                        {
                                            var fkBuilderType = foreignKeyBuilder.GetType();

                                            // HasForeignKey
                                            var hasForeignKeyMethod = fkBuilderType.GetMethod("HasForeignKey", new[] { typeof(string) });

                                            if (hasForeignKeyMethod != null)
                                            {
                                                hasForeignKeyMethod.Invoke(foreignKeyBuilder, new object[] { property.Name });
                                                _logger.Debug($"Configurada relación muchos a uno para {navProperty.Name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Error al configurar relación para propiedad {property.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Determina si un tipo es una colección
        /// </summary>
        private bool IsCollectionType(Type type)
        {
            return type.IsGenericType &&
                  (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                   type.GetGenericTypeDefinition() == typeof(List<>) ||
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
    }
}