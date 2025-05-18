using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Repository
{
    /// <summary>
    /// Asynchronous version of the repository interface
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public interface IAsyncRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its primary key asynchronously
        /// </summary>
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities asynchronously
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity asynchronously
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity asynchronously
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity asynchronously
        /// </summary>
        Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its primary key asynchronously
        /// </summary>
        Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching the specified predicate asynchronously
        /// </summary>
        Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base implementation of IAsyncRepository with common functionality
    /// </summary>
    public abstract class AsyncRepositoryBase<TEntity, TKey> :
        RepositoryBase<TEntity, TKey>,
        IAsyncRepository<TEntity, TKey>
        where TEntity : class
    {
        protected AsyncRepositoryBase(string connectionName = "Default")
            : base(connectionName)
        {
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            string query = $"SELECT * FROM {TableName} WHERE {PrimaryKeyColumn} = @Id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return CreateEntityFromRecord(reader);
                        }
                    }
                }
            }

            return null;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            string query = $"SELECT * FROM {TableName}";
            var entities = new List<TEntity>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            entities.Add(CreateEntityFromRecord(reader));
                        }
                    }
                }
            }

            return entities;
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var (columns, parameters) = GetColumnsAndParameters();

            string columnList = string.Join(", ", columns);
            string parameterList = string.Join(", ", parameters);

            string query = $"INSERT INTO {TableName} ({columnList}) VALUES ({parameterList}); " +
                           $"SELECT SCOPE_IDENTITY();";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(CreateParameters(entity));

                    // Execute and get the inserted ID
                    decimal id = (decimal)await command.ExecuteScalarAsync(cancellationToken);

                    // Reload the entity to get any generated values
                    return await GetByIdAsync((TKey)Convert.ChangeType(id, typeof(TKey)), cancellationToken);
                }
            }
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
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
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(CreateParameters(entity));
                    command.Parameters.AddWithValue("@Id", GetPrimaryKeyValue(entity));

                    int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                    if (rowsAffected > 0)
                    {
                        return entity;
                    }
                }
            }

            return null;
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return await DeleteAsync(GetPrimaryKeyValue(entity), cancellationToken);
        }

        public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            string query = $"DELETE FROM {TableName} WHERE {PrimaryKeyColumn} = @Id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                    return rowsAffected > 0;
                }
            }
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default)
        {
            // For base implementation, we'll get all and filter in memory
            // Override in derived classes for more efficient implementations
            var all = await GetAllAsync(cancellationToken);
            return all.Where(predicate);
        }
    }
}