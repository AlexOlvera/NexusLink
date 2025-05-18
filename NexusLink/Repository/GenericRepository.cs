using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using NexusLink.Data.Mapping;
using NexusLink.Logging;

namespace NexusLink.Repository
{
    public class GenericRepository<T> : RepositoryBase<T> where T : class
    {
        private readonly DbContext _dbContext;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger _logger;
        private readonly EntityMapper _entityMapper;

        public GenericRepository(DbContext dbContext, ILogger logger, EntityMapper entityMapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
            _dbSet = _dbContext.Set<T>();
        }

        public override T GetById(int id)
        {
            _logger.Debug($"Retrieving {typeof(T).Name} by id: {id}");
            return _dbSet.Find(id);
        }

        public override IEnumerable<T> GetAll()
        {
            _logger.Debug($"Retrieving all {typeof(T).Name} entities");
            return _dbSet.ToList();
        }

        public override IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            _logger.Debug($"Finding {typeof(T).Name} entities with predicate");
            return _dbSet.Where(predicate).ToList();
        }

        public override void Insert(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.Debug($"Inserting {typeof(T).Name} entity");
            _dbSet.Add(entity);
            _dbContext.SaveChanges();
        }

        public override void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.Debug($"Updating {typeof(T).Name} entity");
            _dbContext.Entry(entity).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public override void Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.Debug($"Deleting {typeof(T).Name} entity");
            _dbSet.Remove(entity);
            _dbContext.SaveChanges();
        }

        public override int Count()
        {
            return _dbSet.Count();
        }

        public override bool Any(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Any(predicate);
        }
    }
}