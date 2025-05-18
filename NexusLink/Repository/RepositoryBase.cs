using NexusLink.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace NexusLink.Repository
{
    /// <summary>
    /// Base implementation of IRepository with common functionality
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : class
    {
        protected readonly string _connectionName;

        protected RepositoryBase(string connectionName = "Default")
        {
            _connectionName = connectionName;
        }

        /// <summary>
        /// Gets the connection string for this repository
        /// </summary>
        protected string ConnectionString => ConfigManager.GetConnectionString(_connectionName);

        /// <summary>
        /// Gets the table name for the entity
        /// </summary>
        protected abstract string TableName { get; }

        /// <summary>
        /// Gets the primary key column name
        /// </summary>
        protected abstract string PrimaryKeyColumn { get; }

        /// <summary>
        /// Extracts the primary key value from an entity
        /// </summary>
        protected abstract TKey GetPrimaryKeyValue(TEntity entity);

        /// <summary>
        /// Creates an entity from a database record
        /// </summary>
        protected abstract TEntity CreateEntityFromRecord(SqlDataReader reader);

        /// <summary>
        /// Gets the columns and parameter names for inserts and updates
        /// </summary>
        protected abstract (string[] Columns, string[] Parameters) GetColumnsAndParameters();

        /// <summary>
        /// Creates parameters for an entity
        /// </summary>
        protected abstract SqlParameter[] CreateParameters(TEntity entity);

        public virtual TEntity GetById(TKey id)
        {
            string query = $"SELECT * FROM {TableName} WHERE {PrimaryKeyColumn} = @Id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return CreateEntityFromRecord(reader);
                        }
                    }
                }
            }

            return null;
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            List<TEntity> entities = new List<TEntity>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entities.Add(CreateEntityFromRecord(reader));
                        }
                    }
                }
            }

            return entities;
        }

        public virtual TEntity Add(TEntity entity)
        {
            var (columns, parameters) = GetColumnsAndParameters();

            string columnList = string.Join(", ", columns);
            string parameterList = string.Join(", ", parameters);

            string query = $"INSERT INTO {TableName} ({columnList}) VALUES ({parameterList}); " +
                           $"SELECT SCOPE_IDENTITY();";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(CreateParameters(entity));

                    // Execute and get the inserted ID
                    decimal id = (decimal)command.ExecuteScalar();

                    // Reload the entity to get any generated values
                    return GetById((TKey)Convert.ChangeType(id, typeof(TKey)));
                }
            }
        }

        public virtual TEntity Update(TEntity entity)
        {
            var (columns, parameters) = GetColumnsAndParameters();

            StringBuilder setClause = new StringBuilder();

            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) setClause.Append(", ");
                setClause.Append($"{columns[i]} = {parameters[i]}");
            }

            string query = $"UPDATE {TableName} SET {setClause} WHERE {PrimaryKeyColumn} = @Id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(CreateParameters(entity));
                    command.Parameters.AddWithValue("@Id", GetPrimaryKeyValue(entity));

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return entity;
                    }
                }
            }

            return null;
        }

        public virtual bool Delete(TEntity entity)
        {
            return Delete(GetPrimaryKeyValue(entity));
        }

        public virtual bool Delete(TKey id)
        {
            string query = $"DELETE FROM {TableName} WHERE {PrimaryKeyColumn} = @Id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public virtual IEnumerable<TEntity> Find(Func<TEntity, bool> predicate)
        {
            // For base implementation, we'll get all and filter in memory
            // Override in derived classes for more efficient implementations
            return GetAll().Where(predicate);
        }
    }
}