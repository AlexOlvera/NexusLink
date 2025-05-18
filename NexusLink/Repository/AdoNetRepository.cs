using NexusLink.Attributes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace NexusLink.Repository
{
    /// <summary>
    /// A generic repository implementation that uses attributes for mapping
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public class AdoNetRepository<TEntity, TKey> : RepositoryBase<TEntity, TKey>
        where TEntity : class
    {
        private readonly string _tableName;
        private readonly string _primaryKeyColumn;
        private readonly PropertyInfo _primaryKeyProperty;
        private readonly IDictionary<string, PropertyInfo> _propertyMap;

        public AdoNetRepository(string connectionName = "Default") : base(connectionName)
        {
            Type entityType = typeof(TEntity);

            // Get table info
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            _tableName = tableAttr?.Name ?? entityType.Name;

            // Build property map and find primary key
            _propertyMap = new Dictionary<string, PropertyInfo>();

            foreach (var property in entityType.GetProperties())
            {
                // Skip properties with [IgnoreColumn] attribute
                if (property.GetCustomAttribute<IgnoreColumnAttribute>() != null)
                    continue;

                // Get column info
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
                string columnName = columnAttr?.Name ?? property.Name;

                // Check if it's the primary key
                var keyAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (keyAttr != null)
                {
                    _primaryKeyProperty = property;
                    _primaryKeyColumn = columnName;
                }

                _propertyMap[columnName] = property;
            }

            // If no primary key is found, look for Id or [EntityName]Id property
            if (_primaryKeyProperty == null)
            {
                _primaryKeyProperty = entityType.GetProperty("Id") ??
                                    entityType.GetProperty($"{entityType.Name}Id");

                if (_primaryKeyProperty != null)
                {
                    _primaryKeyColumn = _primaryKeyProperty.Name;
                }
            }

            if (_primaryKeyProperty == null)
            {
                throw new InvalidOperationException(
                    $"Entity {entityType.Name} does not have a primary key property. " +
                    "Use [PrimaryKey] attribute or name the property 'Id' or '[EntityName]Id'.");
            }
        }

        protected override string TableName => _tableName;

        protected override string PrimaryKeyColumn => _primaryKeyColumn;

        protected override TKey GetPrimaryKeyValue(TEntity entity)
        {
            return (TKey)_primaryKeyProperty.GetValue(entity);
        }

        protected override TEntity CreateEntityFromRecord(SqlDataReader reader)
        {
            TEntity entity = Activator.CreateInstance<TEntity>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);

                if (_propertyMap.TryGetValue(columnName, out PropertyInfo property))
                {
                    if (!reader.IsDBNull(i))
                    {
                        object value = reader.GetValue(i);

                        // Handle type conversion if needed
                        if (property.PropertyType != value.GetType())
                        {
                            Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                            value = Convert.ChangeType(value, targetType);
                        }

                        property.SetValue(entity, value);
                    }
                }
                return entity;
            }
        }

        protected override (string[] Columns, string[] Parameters) GetColumnsAndParameters()
        {
            // Skip primary key if it's auto-increment
            bool skipPrimaryKey = _primaryKeyProperty.GetCustomAttribute<AutoIncrementAttribute>() != null;

            var columns = new List<string>();
            var parameters = new List<string>();

            foreach (var pair in _propertyMap)
            {
                // Skip primary key if auto-increment
                if (skipPrimaryKey && pair.Value == _primaryKeyProperty)
                    continue;

                columns.Add(pair.Key);
                parameters.Add($"@{pair.Key}");
            }

            return (columns.ToArray(), parameters.ToArray());
        }

        protected override SqlParameter[] CreateParameters(TEntity entity)
        {
            var parameters = new List<SqlParameter>();

            foreach (var pair in _propertyMap)
            {
                object value = pair.Value.GetValue(entity) ?? DBNull.Value;
                parameters.Add(new SqlParameter($"@{pair.Key}", value));
            }

            return parameters.ToArray();
        }
    }
}