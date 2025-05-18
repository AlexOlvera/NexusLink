using System;
using System.Collections.Generic;
using System.Reflection;
using NexusLink.Attributes;
using NexusLink.Logging;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Mapa de relaciones entre entidades
    /// </summary>
    public class RelationshipMap
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Type, List<Relationship>> _relationships;

        public RelationshipMap(ILogger logger)
        {
            _logger = logger;
            _relationships = new Dictionary<Type, List<Relationship>>();
        }

        /// <summary>
        /// Analiza las relaciones de una entidad
        /// </summary>
        public void AnalyzeRelationships(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (_relationships.ContainsKey(entityType))
            {
                return; // Ya analizado
            }

            var relationships = new List<Relationship>();

            // Analizar propiedades de navegación
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                var navigationAttr = property.GetCustomAttribute<NavigationPropertyAttribute>();

                if (navigationAttr != null)
                {
                    // Obtener propiedad de clave foránea
                    var foreignKeyProperty = entityType.GetProperty(navigationAttr.ForeignKeyProperty);

                    if (foreignKeyProperty == null)
                    {
                        _logger.Warning($"Propiedad de clave foránea {navigationAttr.ForeignKeyProperty} no encontrada en {entityType.Name}");
                        continue;
                    }

                    // Obtener atributos de clave foránea
                    var foreignKeyAttr = foreignKeyProperty.GetCustomAttribute<ForeignKeyAttribute>();

                    if (foreignKeyAttr == null)
                    {
                        _logger.Warning($"Atributo ForeignKeyAttribute no encontrado en {navigationAttr.ForeignKeyProperty}");
                        continue;
                    }

                    // Determinar tipo de relación
                    RelationshipType relationshipType;
                    Type targetType;

                    if (IsCollectionType(property.PropertyType))
                    {
                        // Relación uno a muchos
                        relationshipType = RelationshipType.OneToMany;
                        targetType = property.PropertyType.GetGenericArguments()[0];
                    }
                    else
                    {
                        // Relación muchos a uno o uno a uno
                        targetType = property.PropertyType;

                        // Verificar si es uno a uno (la clave foránea también es única)
                        if (foreignKeyAttr.isUniqueKey)
                        {
                            relationshipType = RelationshipType.OneToOne;
                        }
                        else
                        {
                            relationshipType = RelationshipType.ManyToOne;
                        }
                    }

                    // Crear relación
                    var relationship = new Relationship
                    {
                        SourceType = entityType,
                        TargetType = targetType,
                        RelationshipType = relationshipType,
                        NavigationProperty = property,
                        ForeignKeyProperty = foreignKeyProperty,
                        IsRequired = foreignKeyAttr.isRequired
                    };

                    relationships.Add(relationship);
                    _logger.Debug($"Relación encontrada: {entityType.Name}.{property.Name} ({relationshipType}) -> {targetType.Name}");
                }
            }

            // Guardar relaciones
            _relationships[entityType] = relationships;
        }

        /// <summary>
        /// Obtiene todas las relaciones de una entidad
        /// </summary>
        public List<Relationship> GetRelationships(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (!_relationships.ContainsKey(entityType))
            {
                AnalyzeRelationships(entityType);
            }

            return _relationships[entityType];
        }

        /// <summary>
        /// Obtiene todas las relaciones de una entidad del tipo especificado
        /// </summary>
        public List<Relationship> GetRelationships<T>()
        {
            return GetRelationships(typeof(T));
        }

        /// <summary>
        /// Obtiene las relaciones de una entidad con otra entidad específica
        /// </summary>
        public List<Relationship> GetRelationshipsWith(Type sourceType, Type targetType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            var relationships = GetRelationships(sourceType);

            return relationships.FindAll(r => r.TargetType == targetType);
        }

        /// <summary>
        /// Obtiene las relaciones de una entidad con otra entidad específica
        /// </summary>
        public List<Relationship> GetRelationshipsWith<TSource, TTarget>()
        {
            return GetRelationshipsWith(typeof(TSource), typeof(TTarget));
        }

        /// <summary>
        /// Determina si existe una relación entre dos entidades
        /// </summary>
        public bool HasRelationship(Type sourceType, Type targetType)
        {
            return GetRelationshipsWith(sourceType, targetType).Count > 0;
        }

        /// <summary>
        /// Determina si existe una relación entre dos entidades
        /// </summary>
        public bool HasRelationship<TSource, TTarget>()
        {
            return HasRelationship(typeof(TSource), typeof(TTarget));
        }

        /// <summary>
        /// Determina si un tipo es una colección
        /// </summary>
        private bool IsCollectionType(Type type)
        {
            return type.IsGenericType &&
                  (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                   type.GetGenericTypeDefinition() == typeof(List<>) ||
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
    }

    /// <summary>
    /// Representa una relación entre entidades
    /// </summary>
    public class Relationship
    {
        /// <summary>
        /// Tipo de entidad origen
        /// </summary>
        public Type SourceType { get; set; }

        /// <summary>
        /// Tipo de entidad destino
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Tipo de relación
        /// </summary>
        public RelationshipType RelationshipType { get; set; }

        /// <summary>
        /// Propiedad de navegación
        /// </summary>
        public PropertyInfo NavigationProperty { get; set; }

        /// <summary>
        /// Propiedad de clave foránea
        /// </summary>
        public PropertyInfo ForeignKeyProperty { get; set; }

        /// <summary>
        /// Indica si la relación es requerida
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Obtiene un valor que indica si la relación es de colección (uno a muchos o muchos a muchos)
        /// </summary>
        public bool IsCollection
        {
            get
            {
                return RelationshipType == RelationshipType.OneToMany ||
                       RelationshipType == RelationshipType.ManyToMany;
            }
        }
    }

    /// <summary>
    /// Tipo de relación entre entidades
    /// </summary>
    public enum RelationshipType
    {
        /// <summary>
        /// Relación uno a uno
        /// </summary>
        OneToOne,

        /// <summary>
        /// Relación uno a muchos
        /// </summary>
        OneToMany,

        /// <summary>
        /// Relación muchos a uno
        /// </summary>
        ManyToOne,

        /// <summary>
        /// Relación muchos a muchos
        /// </summary>
        ManyToMany
    }
}