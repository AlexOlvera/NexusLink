using NexusLink.Core.Configuration;
using NexusLink.Core.Connection;
using NexusLink.Core.Transactions;
using NexusLink.Dynamic.Emit;
using NexusLink.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public static class NexusLinkServiceCollectionExtensions
    {
        public static IServiceCollection AddNexusLink(this IServiceCollection services, Action<NexusLinkOptions> setupAction = null)
        {
            // Configura opciones
            var options = new NexusLinkOptions();
            setupAction?.Invoke(options);

            // Registra servicios core
            services.AddSingleton<MultiDatabaseConfig>(options.DatabaseConfig);
            services.AddSingleton<DatabaseSelector>();
            services.AddSingleton<ConnectionFactory>();
            services.AddSingleton<ConnectionPool>();

            // Registra servicios transientes
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            // Registra loggers
            services.AddSingleton<ILogger, TraceLogger>();

            // Registra generadores dinámicos
            services.AddSingleton<ProxyGenerator>();
            services.AddSingleton<TypeBuilder>();

            return services;
        }

        // Extensión para configuración de base de datos
        public static IServiceCollection AddNexusLinkDatabase(
            this IServiceCollection services,
            string name,
            string connectionString,
            DatabaseProviderType providerType = DatabaseProviderType.SqlServer)
        {
            // Obtiene la configuración
            var dbConfig = services.BuildServiceProvider().GetRequiredService<MultiDatabaseConfig>();

            // Añade la conexión
            dbConfig.AddConnection(new ConnectionSettings
            {
                Name = name,
                ConnectionString = connectionString,
                ProviderType = providerType
            });

            return services;
        }
    }
}
