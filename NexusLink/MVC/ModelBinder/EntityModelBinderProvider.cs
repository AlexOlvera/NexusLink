using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using NexusLink.Attributes;
using NexusLink.Data.Mapping;
using NexusLink.Extensions.ObjectExtensions;

namespace NexusLink.MVC.ModelBinder
{
    /// <summary>
    /// Proveedor de model binders para entidades mapeadas con NexusLink.
    /// Facilita la integración con ASP.NET Core MVC.
    /// </summary>
    public class EntityModelBinderProvider : IModelBinderProvider
    {
        private readonly EntityMapper _entityMapper;
        private readonly Dictionary<Type, IModelBinder> _binders;

        /// <summary>
        /// Crea una nueva instancia del proveedor
        /// </summary>
        /// <param name="entityMapper">Mapeador de entidades</param>
        public EntityModelBinderProvider(EntityMapper entityMapper)
        {
            _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
            _binders = new Dictionary<Type, IModelBinder>();
        }

        /// <summary>
        /// Obtiene un model binder para un tipo específico
        /// </summary>
        /// <param name="context">Contexto del model binder</param>
        /// <returns>Model binder para el tipo o null</returns>
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Verificar si ya tenemos un binder para este tipo
            if (_binders.TryGetValue(context.Metadata.ModelType, out var cachedBinder))
            {
                return cachedBinder;
            }

            // Verificar si el tipo es una entidad mapeada
            if (IsNexusLinkEntity(context.Metadata.ModelType))
            {
                var binder = new EntityModelBinder(_entityMapper);
                _binders[context.Metadata.ModelType] = binder;
                return binder;
            }

            // Verificar si es una colección de entidades mapeadas
            if (IsCollectionOfNexusLinkEntities(context.Metadata.ModelType, out Type elementType))
            {
                var binder = new EntityCollectionModelBinder(_entityMapper, elementType);
                _binders[context.Metadata.ModelType] = binder;
                return binder;
            }

            return null;
        }

        /// <summary>
        /// Verifica si un tipo es una entidad mapeada con NexusLink
        /// </summary>
        /// <param name="type">Tipo a verificar</param>
        /// <returns>True si es una entidad mapeada, false en caso contrario</returns>
        private bool IsNexusLinkEntity(Type type)
        {
            // Verificar si el tipo tiene el atributo TableAttribute
            var tableAttributes = type.GetCustomAttributes(typeof(TableAttribute), true);
            return tableAttributes.Length > 0;
        }

        /// <summary>
        /// Verifica si un tipo es una colección de entidades mapeadas
        /// </summary>
        /// <param name="type">Tipo a verificar</param>
        /// <param name="elementType">Tipo de elementos de la colección (salida)</param>
        /// <returns>True si es una colección de entidades mapeadas, false en caso contrario</returns>
        private bool IsCollectionOfNexusLinkEntities(Type type, out Type elementType)
        {
            elementType = null;

            // Verificar si es un IEnumerable<T>
            if (!type.IsGenericType)
                return false;

            if (type.GetGenericTypeDefinition() != typeof(IEnumerable<>) &&
                !type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return false;
            }

            // Obtener tipo de elemento
            elementType = type.GetGenericArguments()[0];
            if (type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                var enumerableInterface = type.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                elementType = enumerableInterface.GetGenericArguments()[0];
            }

            // Verificar si el tipo de elemento es una entidad mapeada
            return IsNexusLinkEntity(elementType);
        }
    }

    /// <summary>
    /// Model binder para colecciones de entidades mapeadas
    /// </summary>
    public class EntityCollectionModelBinder : IModelBinder
    {
        private readonly EntityMapper _entityMapper;
        private readonly Type _elementType;
        private readonly CollectionModelBinder _collectionBinder;

        /// <summary>
        /// Crea una nueva instancia del binder
        /// </summary>
        /// <param name="entityMapper">Mapeador de entidades</param>
        /// <param name="elementType">Tipo de elementos de la colección</param>
        public EntityCollectionModelBinder(EntityMapper entityMapper, Type elementType)
        {
            _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
            _elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));

            // Crear un binder para el elemento
            var elementBinder = new EntityModelBinder(_entityMapper);

            // Crear el binder de colección
            _collectionBinder = new CollectionModelBinder(
                elementBinder,
                NullLoggerFactory.Instance);
        }

        /// <summary>
        /// Realiza el binding del modelo
        /// </summary>
        /// <param name="bindingContext">Contexto del binding</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return _collectionBinder.BindModelAsync(bindingContext);
        }
    }

    // Implementación simple de NullLoggerFactory para evitar dependencias externas
    internal class NullLoggerFactory : ILoggerFactory
    {
        public static readonly NullLoggerFactory Instance = new NullLoggerFactory();

        public ILogger CreateLogger(string categoryName) => new NullLogger();
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    internal class NullLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => new NullDisposable();
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }

    internal class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }

    // Tipos simplificados para compatibilidad
    internal enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    internal interface ILogger
    {
        IDisposable BeginScope<TState>(TState state);
        bool IsEnabled(LogLevel logLevel);
        void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
    }

    internal interface ILoggerFactory : IDisposable
    {
        ILogger CreateLogger(string categoryName);
        void AddProvider(ILoggerProvider provider);
    }

    internal interface ILoggerProvider : IDisposable
    {
        ILogger CreateLogger(string categoryName);
    }

    internal struct EventId
    {
        public EventId(int id, string name = null)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}