using System;
using System.Collections.Generic;

namespace NexusLink.Repository
{
    /// <summary>
    /// Base interface for all repositories
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its primary key
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>The entity or null if not found</returns>
        TEntity GetById(TKey id);

        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <returns>All entities</returns>
        IEnumerable<TEntity> GetAll();

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <returns>The added entity</returns>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity</returns>
        TEntity Update(TEntity entity);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        bool Delete(TEntity entity);

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>True if deleted, false otherwise</returns>
        bool Delete(TKey id);

        /// <summary>
        /// Finds entities matching the specified predicate
        /// </summary>
        /// <param name="predicate">The predicate to filter on</param>
        /// <returns>Matching entities</returns>
        IEnumerable<TEntity> Find(Func<TEntity, bool> predicate);
    }
}