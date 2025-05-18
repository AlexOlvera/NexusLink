using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public static class RepositoryServiceExtensions
    {
        public static IServiceCollection AddRepository<TEntity, TRepository>(
            this IServiceCollection services)
            where TRepository : class, IRepository<TEntity>
            where TEntity : class
        {
            // Registra repositorio genérico
            services.AddScoped<IRepository<TEntity>, TRepository>();

            return services;
        }

        public static IServiceCollection AddGenericRepository(
            this IServiceCollection services)
        {
            // Registra GenericRepository para cualquier entidad
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            // También registra CrudRepository
            services.AddScoped(typeof(ICrudRepository<>), typeof(CrudRepository<>));

            return services;
        }
    }
}
