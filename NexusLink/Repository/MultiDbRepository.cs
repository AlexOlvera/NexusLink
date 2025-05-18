using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Context;
using NexusLink.Core.Connection;

namespace NexusLink.Repository
{
    /// <summary>
    /// Implementación de repositorio que soporta operaciones en múltiples bases de datos.
    /// Utiliza el DatabaseSelector para determinar dinámicamente la base de datos objetivo.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad administrada por el repositorio</typeparam>
    public class MultiDbRepository<T> : IRepository<T> where T : class, new()
    {
        private readonly DatabaseSelector _databaseSelector;
        private readonly IRepositoryFactory _repositoryFactory;

        public MultiDbRepository(DatabaseSelector databaseSelector, IRepositoryFactory repositoryFactory)
        {
            _databaseSelector = databaseSelector ?? throw new ArgumentNullException(nameof(databaseSelector));
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        /// <summary>
        /// Obtiene el repositorio específico para la base de datos actual
        /// </summary>
        protected IRepository<T> CurrentRepository =>
            _repositoryFactory.CreateRepository<T>(_databaseSelector.CurrentDatabaseName);

        public T GetById(object id)
        {
            return CurrentRepository.GetById(id);
        }

        public async Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await CurrentRepository.GetByIdAsync(id, cancellationToken);
        }

        public IEnumerable<T> GetAll()
        {
            return CurrentRepository.GetAll();
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await CurrentRepository.GetAllAsync(cancellationToken);
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return CurrentRepository.Find(predicate);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await CurrentRepository.FindAsync(predicate, cancellationToken);
        }

        public void Add(T entity)
        {
            CurrentRepository.Add(entity);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await CurrentRepository.AddAsync(entity, cancellationToken);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            CurrentRepository.AddRange(entities);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await CurrentRepository.AddRangeAsync(entities, cancellationToken);
        }

        public void Update(T entity)
        {
            CurrentRepository.Update(entity);
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await CurrentRepository.UpdateAsync(entity, cancellationToken);
        }

        public void Remove(T entity)
        {
            CurrentRepository.Remove(entity);
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            await CurrentRepository.RemoveAsync(entity, cancellationToken);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            CurrentRepository.RemoveRange(entities);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await CurrentRepository.RemoveRangeAsync(entities, cancellationToken);
        }

        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            return CurrentRepository.Count(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            return await CurrentRepository.CountAsync(predicate, cancellationToken);
        }

        public bool Any(Expression<Func<T, bool>> predicate = null)
        {
            return CurrentRepository.Any(predicate);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            return await CurrentRepository.AnyAsync(predicate, cancellationToken);
        }

        /// <summary>
        /// Ejecuta una operación en una base de datos específica
        /// </summary>
        /// <typeparam name="TResult">Tipo de resultado de la operación</typeparam>
        /// <param name="databaseName">Nombre de la base de datos</param>
        /// <param name="operation">Operación a ejecutar</param>
        /// <returns>Resultado de la operación</returns>
        public TResult ExecuteInDatabase<TResult>(string databaseName, Func<IRepository<T>, TResult> operation)
        {
            using (var scope = new DatabaseScope(_databaseSelector, databaseName))
            {
                return operation(CurrentRepository);
            }
        }

        /// <summary>
        /// Ejecuta una operación asíncrona en una base de datos específica
        /// </summary>
        /// <typeparam name="TResult">Tipo de resultado de la operación</typeparam>
        /// <param name="databaseName">Nombre de la base de datos</param>
        /// <param name="operation">Operación asíncrona a ejecutar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        public async Task<TResult> ExecuteInDatabaseAsync<TResult>(
            string databaseName,
            Func<IRepository<T>, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            using (var scope = new DatabaseScope(_databaseSelector, databaseName))
            {
                return await operation(CurrentRepository);
            }
        }

        /// <summary>
        /// Ejecuta una operación que afecta a múltiples bases de datos 
        /// de manera transaccional usando una transacción distribuida
        /// </summary>
        /// <param name="operations">Diccionario de operaciones por base de datos</param>
        /// <returns>True si todas las operaciones fueron exitosas</returns>
        public bool ExecuteMultiDbOperation(Dictionary<string, Action<IRepository<T>>> operations)
        {
            using (var transactionScope = new System.Transactions.TransactionScope())
            {
                foreach (var operation in operations)
                {
                    using (var scope = new DatabaseScope(_databaseSelector, operation.Key))
                    {
                        operation.Value(CurrentRepository);
                    }
                }

                transactionScope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Ejecuta una operación asíncrona que afecta a múltiples bases de datos 
        /// de manera transaccional usando una transacción distribuida
        /// </summary>
        /// <param name="operations">Diccionario de operaciones por base de datos</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si todas las operaciones fueron exitosas</returns>
        public async Task<bool> ExecuteMultiDbOperationAsync(
            Dictionary<string, Func<IRepository<T>, Task>> operations,
            CancellationToken cancellationToken = default)
        {
            using (var transactionScope = new System.Transactions.TransactionScope(
                System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var operation in operations)
                {
                    using (var scope = new DatabaseScope(_databaseSelector, operation.Key))
                    {
                        await operation.Value(CurrentRepository);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return false;
                        }
                    }
                }

                transactionScope.Complete();
                return true;
            }
        }
    }

    /// <summary>
    /// Fábrica para crear repositorios basados en el nombre de la base de datos
    /// </summary>
    public interface IRepositoryFactory
    {
        IRepository<T> CreateRepository<T>(string databaseName) where T : class, new();
    }
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NexusLink.Repository
//{
//    public class MultiDbRepository<T> : IRepository<T> where T : class
//    {
//        private readonly DatabaseSelector _databaseSelector;
//        private readonly IRepository<T> _baseRepository;

//        public MultiDbRepository(
//            DatabaseSelector databaseSelector,
//            IRepository<T> baseRepository)
//        {
//            _databaseSelector = databaseSelector;
//            _baseRepository = baseRepository;
//        }

//        public T GetById(int id)
//        {
//            // Usa el repositorio base con la conexión actual
//            return _baseRepository.GetById(id);
//        }

//        public IEnumerable<T> GetAll()
//        {
//            return _baseRepository.GetAll();
//        }

//        public void Insert(T entity)
//        {
//            _baseRepository.Insert(entity);
//        }

//        public void Update(T entity)
//        {
//            _baseRepository.Update(entity);
//        }

//        public void Delete(T entity)
//        {
//            _baseRepository.Delete(entity);
//        }

//        // Método para cambiar de base de datos
//        public TResult WithDatabase<TResult>(string databaseName, Func<IRepository<T>, TResult> action)
//        {
//            return _databaseSelector.ExecuteWith(databaseName, () => action(_baseRepository));
//        }
//    }
//}