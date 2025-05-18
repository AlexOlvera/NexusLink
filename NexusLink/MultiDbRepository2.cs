using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class MultiDbRepository2<T> : IRepository<T> where T : class
    {
        private readonly DatabaseSelector _databaseSelector;
        private readonly IRepository<T> _baseRepository;

        public MultiDbRepository(
            DatabaseSelector databaseSelector,
            IRepository<T> baseRepository)
        {
            _databaseSelector = databaseSelector;
            _baseRepository = baseRepository;
        }

        public T GetById(int id)
        {
            // Usa el repositorio base con la conexión actual
            return _baseRepository.GetById(id);
        }

        public IEnumerable<T> GetAll()
        {
            return _baseRepository.GetAll();
        }

        public void Insert(T entity)
        {
            _baseRepository.Insert(entity);
        }

        public void Update(T entity)
        {
            _baseRepository.Update(entity);
        }

        public void Delete(T entity)
        {
            _baseRepository.Delete(entity);
        }

        // Método para cambiar de base de datos
        public T WithDatabase<TResult>(string databaseName, Func<IRepository<T>, TResult> action)
        {
            return _databaseSelector.ExecuteWith(databaseName, () => action(_baseRepository));
        }
    }
}
