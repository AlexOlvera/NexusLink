using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using NexusLink.Attributes;
using NexusLink.Logging;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Mapea entidades a tablas y viceversa
    /// </summary>
    public class EntityMapper
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Type, TableInfo> _tableInfoCache;

        public EntityMapper(ILogger logger)
        {
            _logger = logger;
            _tableInfoCache = new Dictionary<Type, TableInfo>();
        }

        /// <summary>
        /// Mapea un DataRow a una entidad
        /// </summary>
        public T MapToEntity<T>(DataRow row) where T : new()
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            T entity = new T();
            var tableInfo = GetTableInfo(typeof(T));

            foreach (var columnMapping in tableInfo.ColumnMappings)
            {
                string columnName = columnMapping.Value.ColumnName;
                PropertyInfo property = columnMapping.Key;

                if (row.Table.Columns.Contains(columnName) && !row.IsNull(columnName))
                {
                    try
                    {
                        object value = row[columnName];

                        // Convertir valor si es necesario
                        if (value != null && property.PropertyType != value.GetType())
                        {
                            value = Convert.ChangeType(value, property.PropertyType);
                        }

                        property.SetValue(entity, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Error al asignar valor a la propiedad {property.Name}: {ex.Message}");
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Mapea un DataReader a una entidad
        /// </summary>
        public T MapToEntity<T>(IDataReader reader) where T : new()
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            T entity = new T();
            var tableInfo = GetTableInfo(typeof(T));

            // Crear mapeo de columnas por nombre
            Dictionary<string, int> columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnIndexes[reader.GetName(i)] = i;
            }

            foreach (var columnMapping in tableInfo.ColumnMappings)
            {
                string columnName = columnMapping.Value.ColumnName;
                PropertyInfo property = columnMapping.Key;

                if (columnIndexes.TryGetValue(columnName, out int columnIndex) && !reader.IsDBNull(columnIndex))
                {
                    try
                    {
                        object value = reader.GetValue(columnIndex);

                        // Convertir valor si es necesario
                        if (value != null && property.PropertyType != value.GetType())
                        {
                            value = Convert.ChangeType(value, property.PropertyType);
                        }

                        property.SetValue(entity, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Error al asignar valor a la propiedad {property.Name}: {ex.Message}");
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Mapea múltiples DataRows a una lista de entidades
        /// </summary>
        public List<T> MapToEntities<T>(DataTable table) where T : new()
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var entities = new List<T>();

            foreach (DataRow row in table.Rows)
            {
                entities.Add(MapToEntity<T>(row));
            }

            return entities;
        }

        /// <summary>
        /// Mapea múltiples DataRows a una lista de entidades
        /// </summary>
        public List<T> MapToEntities<T>(IDataReader reader) where T : new()
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var entities = new List<T>();

            while (reader.Read())
            {
                entities.Add(MapToEntity<T>(reader));
            }

            return entities;
        }

        /// <summary>
        /// Mapea una entidad a un conjunto de parámetros para INSERT
        /// </summary>
        public Dictionary<string, object> MapToInsertParameters<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var tableInfo = GetTableInfo(typeof(T));
            var parameters = new Dictionary<string, object>();

            foreach (var columnMapping in tableInfo.ColumnMappings)
            {
                PropertyInfo property = columnMapping.Key;
                ColumnInfo columnInfo = columnMapping.Value;

                // No incluir columnas de identidad para INSERT
                if (columnInfo.IsIdentity)
                {
                    continue;
                }

                // No incluir columnas calculadas para INSERT
                if (columnInfo.IsComputed)
                {
                    continue;
                }

                // Obtener valor de la propiedad
                object value = property.GetValue(entity);

                // Agregar parámetro
                parameters.Add(columnInfo.ColumnName, value);
            }

            return parameters;
        }

        /// <summary>
        /// Mapea una entidad a un conjunto de parámetros para UPDATE
        /// </summary>
        public Dictionary<string, object> MapToUpdateParameters<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var tableInfo = GetTableInfo(typeof(T));
            var parameters = new Dictionary<string, object>();

            foreach (var columnMapping in tableInfo.ColumnMappings)
            {
                PropertyInfo property = columnMapping.Key;
                ColumnInfo columnInfo = columnMapping.Value;

                // No incluir columnas de identidad para UPDATE
                if (columnInfo.IsIdentity)
                {
                    continue;
                }

                // No incluir columnas calculadas para UPDATE
                if (columnInfo.IsComputed)
                {
                    continue;
                }

                // No incluir columnas de clave primaria en SET, solo en WHERE
                if (columnInfo.IsPrimaryKey)
                {
                    continue;
                }

                // Obtener valor de la propiedad
                object value = property.GetValue(entity);

                // Agregar parámetro
                parameters.Add(columnInfo.ColumnName, value);
            }

            return parameters;
        }

        /// <summary>
        /// Mapea una entidad a un conjunto de parámetros para el criterio WHERE de UPDATE o DELETE
        /// </summary>
        public Dictionary<string, object> MapToWhereParameters<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var tableInfo = GetTableInfo(typeof(T));
            var parameters = new Dictionary<string, object>();

            // Usar claves primarias para WHERE
            var primaryKeyColumns = tableInfo.ColumnMappings
                .Where(cm => cm.Value.IsPrimaryKey)
                .ToList();

            if (primaryKeyColumns.Count == 0)
            {
                throw new InvalidOperationException($"La entidad {typeof(T).Name} no tiene definida una clave primaria");
            }

            foreach (var columnMapping in primaryKeyColumns)
            {
                PropertyInfo property = columnMapping.Key;
                ColumnInfo columnInfo = columnMapping.Value;

                // Obtener valor de la propiedad
                object value = property.GetValue(entity);

                // Agregar parámetro
                parameters.Add(columnInfo.ColumnName, value);
            }

            return parameters;
        }

        /// <summary>
        /// Genera una consulta SQL SELECT para una entidad
        /// </summary>
        public string GenerateSelectQuery<T>() where T : class
        {
            var tableInfo = GetTableInfo(typeof(T));

            string columns = string.Join(", ", tableInfo.ColumnMappings
                .Select(cm => $"[{cm.Value.ColumnName}]"));

            return $"SELECT {columns} FROM [{tableInfo.Schema}].[{tableInfo.TableName}]";
        }

        /// <summary>
        /// Genera una consulta SQL SELECT por ID para una entidad
        /// </summary>
        public string GenerateSelectByIdQuery<T>() where T : class
        {
            var tableInfo = GetTableInfo(typeof(T));

            string columns = string.Join(", ", tableInfo.ColumnMappings
                .Select(cm => $"[{cm.Value.ColumnName}]"));

            string whereClause = string.Join(" AND ", tableInfo.ColumnMappings
                .Where(cm => cm.Value.IsPrimaryKey)
                .Select(cm => $"[{cm.Value.ColumnName}] = @{cm.Value.ColumnName}"));

            return $"SELECT {columns} FROM [{tableInfo.Schema}].[{tableInfo.TableName}] WHERE {whereClause}";
        }

        /// <summary>
        /// Genera una consulta SQL INSERT para una entidad
        /// </summary>
        public string GenerateInsertQuery<T>() where T : class
        {
            var tableInfo = GetTableInfo(typeof(T));

            var insertColumns = tableInfo.ColumnMappings
                .Where(cm => !cm.Value.IsIdentity && !cm.Value.IsComputed)
                .ToList();

            string columns = string.Join(", ", insertColumns
                .Select(cm => $"[{cm.Value.ColumnName}]"));

            string parameters = string.Join(", ", insertColumns
                .Select(cm => $"@{cm.Value.ColumnName}"));

            return $"INSERT INTO [{tableInfo.Schema}].[{tableInfo.TableName}] ({columns}) VALUES ({parameters})";
        }

        /// <summary>
        /// Genera una consulta SQL UPDATE para una entidad
        /// </summary>
        public string GenerateUpdateQuery<T>() where T : class
        {
            var tableInfo = GetTableInfo(typeof(T));

            var updateColumns = tableInfo.ColumnMappings
                .Where(cm => !cm.Value.IsIdentity && !cm.Value.IsComputed && !cm.Value.IsPrimaryKey)
                .ToList();

            string setClause = string.Join(", ", updateColumns
                .Select(cm => $"[{cm.Value.ColumnName}] = @{cm.Value.ColumnName}"));

            string whereClause = string.Join(" AND ", tableInfo.ColumnMappings
                .Where(cm => cm.Value.IsPrimaryKey)
                .Select(cm => $"[{cm.Value.ColumnName}] = @{cm.Value.ColumnName}"));

            return $"UPDATE [{tableInfo.Schema}].[{tableInfo.TableName}] SET {setClause} WHERE {whereClause}";
        }

        /// <summary>
        /// Genera una consulta SQL DELETE para una entidad
        /// </summary>
        public string GenerateDeleteQuery<T>() where T : class
        {
            var tableInfo = GetTableInfo(typeof(T));

            string whereClause = string.Join(" AND ", tableInfo.ColumnMappings
                .Where(cm => cm.Value.IsPrimaryKey)
                .Select(cm => $"[{cm.Value.ColumnName}] = @{cm.Value.ColumnName}"));

            return $"DELETE FROM [{tableInfo.Schema}].[{tableInfo.TableName}] WHERE {whereClause}";
        }

        /// <summary>
        /// Obtiene información de tabla para un tipo de entidad
        /// </summary>
        public TableInfo GetTableInfo(Type entityType)
        {
            if (_tableInfoCache.TryGetValue(entityType, out TableInfo tableInfo))
            {
                return tableInfo;
            }

            tableInfo = BuildTableInfo(entityType);
            _tableInfoCache[entityType] = tableInfo;

            return tableInfo;
        }

        /// <summary>
        /// Construye información de tabla para un tipo de entidad
        /// </summary>
        private TableInfo BuildTableInfo(Type entityType)
        {
            // Obtener atributo de tabla
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            if (tableAttribute == null)
            {
                throw new InvalidOperationException($"El tipo {entityType.Name} no tiene un atributo TableAttribute");
            }

            var tableInfo = new TableInfo
            {
                TableName = tableAttribute.Name,
                Schema = tableAttribute.Schema ?? "dbo",
                ColumnMappings = new Dictionary<PropertyInfo, ColumnInfo>()
            };

            if (string.IsNullOrEmpty(tableInfo.TableName))
            {
                // Si no se especifica un nombre de tabla, usar el nombre del tipo
                tableInfo.TableName = entityType.Name;
            }

            // Analizar propiedades para mapeos de columnas
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                // Buscar atributos de columna
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                var uniqueKeyAttribute = property.GetCustomAttribute<UniqueKeyAttribute>();

                // Si no hay atributos de columna, saltar
                if (columnAttribute == null && primaryKeyAttribute == null &&
                    foreignKeyAttribute == null && uniqueKeyAttribute == null)
                {
                    continue;
                }

                var columnInfo = new ColumnInfo();

                // Configurar con ColumnAttribute
                if (columnAttribute != null)
                {
                    columnInfo.ColumnName = columnAttribute.Name;
                    columnInfo.IsRequired = columnAttribute.isRequired;
                    columnInfo.IsCriterial = columnAttribute.isCriterial;
                    columnInfo.IsIdentity = columnAttribute.isAddedAutomatically;
                }

                // Configurar con PrimaryKeyAttribute
                if (primaryKeyAttribute != null)
                {
                    columnInfo.ColumnName = primaryKeyAttribute.Name;
                    columnInfo.IsPrimaryKey = true;
                    columnInfo.IsIdentity = primaryKeyAttribute.isAddedAutomatically;
                    columnInfo.IsRequired = true;
                    columnInfo.IsUniqueKey = true;
                }

                // Configurar con ForeignKeyAttribute
                if (foreignKeyAttribute != null)
                {
                    columnInfo.ColumnName = foreignKeyAttribute.Name;
                    columnInfo.IsForeignKey = true;
                    columnInfo.IsRequired = foreignKeyAttribute.isRequired;
                    columnInfo.IsUniqueKey = foreignKeyAttribute.isUniqueKey;
                    columnInfo.IsCriterial = foreignKeyAttribute.isCriterial;
                }

                // Configurar con UniqueKeyAttribute
                if (uniqueKeyAttribute != null)
                {
                    columnInfo.ColumnName = uniqueKeyAttribute.Name;
                    columnInfo.IsUniqueKey = true;
                    columnInfo.IsRequired = uniqueKeyAttribute.isRequired;
                    columnInfo.IsCriterial = uniqueKeyAttribute.isCriterial;
                }

                // Si no se especifica un nombre de columna, usar el nombre de la propiedad
                if (string.IsNullOrEmpty(columnInfo.ColumnName))
                {
                    columnInfo.ColumnName = property.Name;
                }

                // Agregar mapeo de columna
                tableInfo.ColumnMappings[property] = columnInfo;
            }

            return tableInfo;
        }
    }
}