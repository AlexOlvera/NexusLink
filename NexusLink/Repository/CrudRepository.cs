using NexusLink.AOP.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NexusLink.Repository
{
    /// <summary>
    /// A repository interface that uses attributes for SQL operations
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public interface ICrudRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its primary key
        /// </summary>
        [Query("SELECT * FROM {0} WHERE {1} = @p0")]
        TEntity GetById(TKey id);

        /// <summary>
        /// Gets all entities
        /// </summary>
        [Query("SELECT * FROM {0}")]
        IEnumerable<TEntity> GetAll();

        /// <summary>
        /// Adds a new entity
        /// </summary>
        [Command("INSERT INTO {0} ({1}) VALUES ({2})")]
        [Transactional]
        int Add(TEntity entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        [Command("UPDATE {0} SET {1} WHERE {2} = @id")]
        [Transactional]
        bool Update(TEntity entity);

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        [Command("DELETE FROM {0} WHERE {1} = @p0")]
        [Transactional]
        bool Delete(TKey id);
    }

    /// <summary>
    /// Base implementation of the ICrudRepository interface
    /// </summary>
    public abstract class CrudRepositoryBase<TEntity, TKey> : ICrudRepository<TEntity, TKey>
        where TEntity : class
    {
        protected abstract string TableName { get; }
        protected abstract string PrimaryKeyColumn { get; }
        protected abstract string InsertColumns { get; }
        protected abstract string InsertValues { get; }
        protected abstract string UpdateColumns { get; }

        public TEntity GetById(TKey id)
        {
            // Implementation provided by [Query] attribute
            throw new NotImplementedException("This method should be intercepted by the Query attribute");
        }

        public IEnumerable<TEntity> GetAll()
        {
            // Implementation provided by [Query] attribute
            throw new NotImplementedException("This method should be intercepted by the Query attribute");
        }

        public int Add(TEntity entity)
        {
            // Implementation provided by [Command] attribute
            throw new NotImplementedException("This method should be intercepted by the Command attribute");
        }

        public bool Update(TEntity entity)
        {
            // Implementation provided by [Command] attribute
            throw new NotImplementedException("This method should be intercepted by the Command attribute");
        }

        public bool Delete(TKey id)
        {
            // Implementation provided by [Command] attribute
            throw new NotImplementedException("This method should be intercepted by the Command attribute");
        }

        // The interceptor will look at these methods to get SQL fragments
        protected virtual string GetFormattedSql(string sql, params object[] args)
        {
            return string.Format(sql, args);
        }
    }

    /// <summary>
    /// Implementation of CrudRepositoryBase that auto-detects entity mappings
    /// </summary>
    public class CrudRepository<TEntity, TKey> : CrudRepositoryBase<TEntity, TKey>
        where TEntity : class, new()
    {
        private readonly string _tableName;
        private readonly string _primaryKeyColumn;
        private readonly PropertyInfo _primaryKeyProperty;
        private readonly Dictionary<string, PropertyInfo> _propertyMap;

        public CrudRepository()
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
        }

        protected override string TableName => _tableName;

        protected override string PrimaryKeyColumn => _primaryKeyColumn;

        protected override string InsertColumns
        {
            get
            {
                bool skipPrimaryKey = _primaryKeyProperty.GetCustomAttribute<AutoIncrementAttribute>() != null;

                var columns = new List<string>();
                foreach (var pair in _propertyMap)
                {
                    if (skipPrimaryKey && pair.Value == _primaryKeyProperty)
                        continue;

                    columns.Add(pair.Key);
                }

                return string.Join(", ", columns);
            }
        }

        protected override string InsertValues
        {
            get
            {
                bool skipPrimaryKey = _primaryKeyProperty.GetCustomAttribute<AutoIncrementAttribute>() != null;

                var parameters = new List<string>();
                int index = 0;

                foreach (var pair in _propertyMap)
                {
                    if (skipPrimaryKey && pair.Value == _primaryKeyProperty)
                        continue;

                    parameters.Add($"@p{index++}");
                }

                return string.Join(", ", parameters);
            }
        }

        protected override string UpdateColumns
        {
            get
            {
                var setColumns = new List<string>();
                int index = 0;

                foreach (var pair in _propertyMap)
                {
                    if (pair.Value != _primaryKeyProperty)
                    {
                        setColumns.Add($"{pair.Key} = @p{index++}");
                    }
                }

                return string.Join(", ", setColumns);
            }
        }
    }
}