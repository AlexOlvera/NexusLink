using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusLink.Core.Configuration;
using NexusLink.Core.Connection;
using NexusLink.Core.Transactions;
using NexusLink.Data.Commands;
using NexusLink.Data.Queries;
using NexusLink.Data.Parameters;
using NexusLink.Data.Mapping;
using NexusLink.Data.MultiDb;
using NexusLink.AOP.Interception;
using NexusLink.Logging;
using NexusLink.Repository;

namespace NexusLink.DependencyInjection
{
    /// <summary>
    /// Proporciona registro modular de componentes de NexusLink en un contenedor de DI
    /// </summary>
    public class ModuleRegistration
    {
        private readonly IServiceCollection _services;
        private readonly Dictionary<Type, ServiceLifetime> _registeredServices;

        public ModuleRegistration(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _registeredServices = new Dictionary<Type, ServiceLifetime>();
        }

        /// <summary>
        /// Registra los módulos principales de NexusLink
        /// </summary>
        public ModuleRegistration RegisterCoreModules()
        {
            // Registro de componentes principales
            RegisterConfiguration();
            RegisterConnections();
            RegisterTransactions();

            return this;
        }

        /// <summary>
        /// Registra módulos de acceso a datos
        /// </summary>
        public ModuleRegistration RegisterDataModules()
        {
            // Registro de componentes de datos
            RegisterCommands();
            RegisterQueries();
            RegisterParameters();
            RegisterMapping();

            return this;
        }

        /// <summary>
        /// Registra módulos para soporte multi-base de datos
        /// </summary>
        public ModuleRegistration RegisterMultiDbModules()
        {
            // Registro de componentes multi-BD
            _services.AddSingleton<DbContextManager>();
            _services.AddSingleton<DbProviderFactory>();
            _services.AddScoped<CrossDbQueryBuilder>();
            _services.AddScoped<DatabaseRouter>();

            return this;
        }

        /// <summary>
        /// Registra módulos de AOP
        /// </summary>
        public ModuleRegistration RegisterAopModules()
        {
            // Registro de componentes AOP
            _services.AddSingleton<ProxyFactory>();
            _services.AddSingleton<MethodInterceptor>();
            _services.AddScoped<InterceptionContext>();
            _services.AddSingleton<DynamicProxyGenerator>();

            return this;
        }

        /// <summary>
        /// Registra módulos de repositorio
        /// </summary>
        public ModuleRegistration RegisterRepositoryModules()
        {
            // Registra componentes de repositorio
            _services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            _services.AddScoped(typeof(ICrudRepository<,>), typeof(CrudRepository<,>));
            _services.AddScoped<RepositoryBase>();

            return this;
        }

        /// <summary>
        /// Registra módulos de logging
        /// </summary>
        public ModuleRegistration RegisterLoggingModules()
        {
            // Registra componentes de logging
            _services.AddSingleton<ILogger, TraceLogger>();
            _services.AddSingleton<NexusTraceAdapter>();
            _services.AddSingleton<LoggerFactory>();

            return this;
        }

        /// <summary>
        /// Registra todas las clases que implementan una interfaz específica
        /// </summary>
        public ModuleRegistration RegisterAllImplementations<TInterface>(
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            Type interfaceType = typeof(TInterface);

            // Buscar todas las clases que implementan la interfaz
            var implementationTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && interfaceType.IsAssignableFrom(t));

            foreach (var implementationType in implementationTypes)
            {
                RegisterService(interfaceType, implementationType, lifetime);
            }

            return this;
        }

        /// <summary>
        /// Registra un servicio específico
        /// </summary>
        public ModuleRegistration RegisterService(
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.AddSingleton(serviceType, implementationType);
                    break;
                case ServiceLifetime.Scoped:
                    _services.AddScoped(serviceType, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    _services.AddTransient(serviceType, implementationType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime));
            }

            _registeredServices[implementationType] = lifetime;
            return this;
        }

        /// <summary>
        /// Registra un servicio específico con una implementación de fábrica
        /// </summary>
        public ModuleRegistration RegisterService<TService>(
            Func<IServiceProvider, TService> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.AddSingleton(implementationFactory);
                    break;
                case ServiceLifetime.Scoped:
                    _services.AddScoped(implementationFactory);
                    break;
                case ServiceLifetime.Transient:
                    _services.AddTransient(implementationFactory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime));
            }

            _registeredServices[typeof(TService)] = lifetime;
            return this;
        }

        /// <summary>
        /// Registra todas las clases decoradas con una anotación específica
        /// </summary>
        public ModuleRegistration RegisterWithAttribute<TAttribute>(
            Assembly assembly,
            ServiceLifetime defaultLifetime = ServiceLifetime.Scoped) where TAttribute : Attribute
        {
            // Buscar todos los tipos con el atributo
            var typesWithAttribute = assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.GetCustomAttribute<TAttribute>() != null);

            foreach (var type in typesWithAttribute)
            {
                // Buscar interfaces implementadas para registro
                var interfaces = type.GetInterfaces();

                if (interfaces.Any())
                {
                    // Registrar contra la primera interfaz
                    RegisterService(interfaces[0], type, defaultLifetime);
                }
                else
                {
                    // Registrar como implementación concreta
                    RegisterService(type, type, defaultLifetime);
                }
            }

            return this;
        }

        /// <summary>
        /// Registra componentes de configuración
        /// </summary>
        private void RegisterConfiguration()
        {
            _services.AddSingleton<ConfigManager>();
            _services.AddSingleton<ConnectionSettings>();
            _services.AddSingleton<MultiDatabaseConfig>();
            _services.AddSingleton<SettingsProvider>();
        }

        /// <summary>
        /// Registra componentes de conexión
        /// </summary>
        private void RegisterConnections()
        {
            _services.AddSingleton<ConnectionFactory>();
            _services.AddSingleton<ConnectionPool>();
            _services.AddSingleton<RetryPolicy>();
            _services.AddSingleton<ConnectionMonitor>();
            _services.AddScoped<ConnectionResolver>();
            _services.AddScoped<DatabaseSelector>();
            _services.AddScoped<MultiTenantConnection>();
            _services.AddScoped<SafeConnection>();
        }

        /// <summary>
        /// Registra componentes de transacción
        /// </summary>
        private void RegisterTransactions()
        {
            _services.AddScoped<TransactionScope>();
            _services.AddScoped<UnitOfWork>();
            _services.AddScoped<TransactionManager>();
            _services.AddSingleton<IsolationLevelManager>();
        }

        /// <summary>
        /// Registra componentes de comandos
        /// </summary>
        private void RegisterCommands()
        {
            _services.AddScoped<CommandBuilder>();
            _services.AddScoped<StoredProcedure>();
            _services.AddScoped<ScalarFunction>();
            _services.AddScoped<BulkCommand>();
            _services.AddScoped<CommandExecutor>();
        }

        /// <summary>
        /// Registra componentes de consultas
        /// </summary>
        private void RegisterQueries()
        {
            _services.AddScoped<QueryBuilder>();
            _services.AddScoped<QueryResult>();
            _services.AddScoped<DynamicQuery>();
            _services.AddScoped<AsyncQueryExecutor>();
            _services.AddSingleton<QueryCache>();
        }

        /// <summary>
        /// Registra componentes de parámetros
        /// </summary>
        private void RegisterParameters()
        {
            _services.AddScoped<ParameterCollection>();
            _services.AddScoped<TypedParameter>();
            _services.AddScoped<ParameterValidator>();
            _services.AddSingleton<SqlParameterFactory>();
        }

        /// <summary>
        /// Registra componentes de mapeo
        /// </summary>
        private void RegisterMapping()
        {
            _services.AddScoped<EntityMapper>();
            _services.AddSingleton<TableInfo>();
            _services.AddScoped<EntityFrameworkBridge>();
            _services.AddScoped<RelationshipMap>();
            _services.AddSingleton<MappingConvention>();
            _services.AddScoped<SchemaManager>();
        }

        /// <summary>
        /// Registra los repositorios en la base de datos especificada
        /// </summary>
        public ModuleRegistration RegisterRepositoriesForDatabase<TContext>(
            string connectionName,
            Assembly repositoryAssembly,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            // Buscar todos los repositorios en el ensamblado
            var repositoryTypes = repositoryAssembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IRepository).IsAssignableFrom(t));

            foreach (var repositoryType in repositoryTypes)
            {
                // Registrar el repositorio con la conexión específica
                _services.Add(new ServiceDescriptor(
                    repositoryType,
                    sp => {
                        var instance = ActivatorUtilities.CreateInstance(sp, repositoryType);

                        // Configurar la conexión
                        var databaseSelector = sp.GetRequiredService<DatabaseSelector>();
                        databaseSelector.CurrentDatabaseName = connectionName;

                        return instance;
                    },
                    lifetime));

                _registeredServices[repositoryType] = lifetime;

                // Registrar también contra sus interfaces
                foreach (var interfaceType in repositoryType.GetInterfaces()
                    .Where(i => i != typeof(IRepository) && typeof(IRepository).IsAssignableFrom(i)))
                {
                    _services.Add(new ServiceDescriptor(
                        interfaceType,
                        sp => sp.GetService(repositoryType),
                        lifetime));

                    _registeredServices[interfaceType] = lifetime;
                }
            }

            return this;
        }

        /// <summary>
        /// Registra valores de opciones para un tipo de opciones específico
        /// </summary>
        public ModuleRegistration RegisterOptions<TOptions>(Action<TOptions> configureOptions)
            where TOptions : class, new()
        {
            _services.Configure(configureOptions);
            return this;
        }

        /// <summary>
        /// Registra factorías para tipos específicos
        /// </summary>
        public ModuleRegistration RegisterFactory<TService, TImplementation>(
            Func<IServiceProvider, TImplementation> factory)
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddSingleton<Func<TService>>(sp => () => factory(sp));
            return this;
        }

        /// <summary>
        /// Verifica que todos los tipos requeridos estén registrados
        /// </summary>
        public ModuleRegistration ValidateRequiredServices(params Type[] requiredTypes)
        {
            var missingTypes = requiredTypes
                .Where(t => !_registeredServices.ContainsKey(t))
                .ToList();

            if (missingTypes.Any())
            {
                throw new InvalidOperationException(
                    $"Los siguientes servicios requeridos no han sido registrados: {string.Join(", ", missingTypes.Select(t => t.Name))}");
            }

            return this;
        }

        /// <summary>
        /// Aplica la configuración a una función para obtener el proveedor de servicios
        /// </summary>
        public IServiceProvider BuildServiceProvider()
        {
            return _services.BuildServiceProvider();
        }
    }
}