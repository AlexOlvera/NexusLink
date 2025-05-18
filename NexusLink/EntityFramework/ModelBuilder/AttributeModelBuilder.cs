using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusLink.Attributes;
using NexusLink.Attributes.KeyAttributes;
using NexusLink.Attributes.RelationshipAttributes;
using NexusLink.Attributes.BehaviorAttributes;
using NexusLink.Attributes.ValidationAttributes;
using NexusLink.Logging;

namespace NexusLink.EntityFramework.ModelBuilder
{
    /// <summary>
    /// Constructor de modelos basado en atributos para Entity Framework
    /// </summary>
    public class AttributeModelBuilder
    {
        private readonly Microsoft.EntityFrameworkCore.ModelBuilder _modelBuilder;
        private readonly ILogger _logger;
        private readonly HashSet<Type> _processedTypes;

        public AttributeModelBuilder(
            Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder,
            ILogger logger = null)
        {
            _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            _logger = logger;
            _processedTypes = new HashSet<Type>();
        }

        /// <summary>
        /// Aplica la configuración de atributos a todos los tipos que derivan de TBaseEntity
        /// </summary>
        public AttributeModelBuilder ApplyAttributesFromAssembly<TBaseEntity>(Assembly assembly)
        {
            Type baseType = typeof(TBaseEntity);

            // Encuentra todos los tipos derivados de TBaseEntity
            var entityTypes = assembly.GetTypes()
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t != baseType);

            _logger?.Info($"Encontrados {entityTypes.Count()} tipos derivados de {baseType.Name} en el ensamblado {assembly.GetName().Name}");

            // Aplica configuración a cada tipo
            foreach (var entityType in entityTypes)
            {
                ApplyConfiguration(entityType);
            }

            return this;
        }

        /// <summary>
        /// Aplica configuración basada en atributos a un tipo de entidad
        /// </summary>
        public AttributeModelBuilder ApplyConfiguration<TEntity>() where TEntity : class
        {
            return ApplyConfiguration(typeof(TEntity));
        }

        /// <summary>
        /// Aplica configuración basada en atributos a un tipo de entidad
        /// </summary>
        public AttributeModelBuilder ApplyConfiguration(Type entityType)
        {
            if (_processedTypes.Contains(entityType))
                return this;

            _processedTypes.Add(entityType);

            _logger?.Debug($"Aplicando configuración para {entityType.Name}");

            // Configurar tabla
            ConfigureTable(entityType);

            // Configurar propiedades
            ConfigureProperties(entityType);

            // Configurar relaciones
            ConfigureRelationships(entityType);

            return this;
        }

        /// <summary>
        /// Configura la tabla basada en atributos
        /// </summary>
        private void ConfigureTable(Type entityType)
        {
            // Obtener atributo de tabla
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                var entityTypeBuilder = _modelBuilder.Entity(entityType);

                // Nombre de tabla
                if (!string.IsNullOrEmpty(tableAttr.Name))
                {
                    if (!string.IsNullOrEmpty(tableAttr.Schema))
                    {
                        entityTypeBuilder.ToTable(tableAttr.Name, tableAttr.Schema);
                        _logger?.Debug($"Configurada tabla {entityType.Name} como {tableAttr.Schema}.{tableAttr.Name}");
                    }
                    else
                    {
                        entityTypeBuilder.ToTable(tableAttr.Name);
                        _logger?.Debug($"Configurada tabla {entityType.Name} como {tableAttr.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Configura las propiedades basadas en atributos
        /// </summary>
        private void ConfigureProperties(Type entityType)
        {
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                // Ignorar propiedad
                if (property.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    _modelBuilder.Entity(entityType).Ignore(property.Name);
                    continue;
                }

                var propertyBuilder = _modelBuilder.Entity(entityType).Property(property.PropertyType, property.Name);

                // Columna
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr != null)
                {
                    if (!string.IsNullOrEmpty(columnAttr.Name))
                    {
                        propertyBuilder.HasColumnName(columnAttr.Name);
                    }

                    // Tipo de columna
                    if (!string.IsNullOrEmpty(columnAttr.TypeName))
                    {
                        propertyBuilder.HasColumnType(columnAttr.TypeName);
                    }
                }

                // Clave primaria
                var pkAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (pkAttr != null)
                {
                    _modelBuilder.Entity(entityType).HasKey(property.Name);

                    // Autoincremento
                    if (property.GetCustomAttribute<AutoIncrementAttribute>() != null)
                    {
                        propertyBuilder.ValueGeneratedOnAdd();
                    }
                }

                // Valor por defecto
                var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttr != null)
                {
                    propertyBuilder.HasDefaultValue(defaultValueAttr.Value);
                }

                // Requerido
                var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttr != null)
                {
                    propertyBuilder.IsRequired();
                }

                // Longitud máxima
                var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                if (stringLengthAttr != null)
                {
                    propertyBuilder.HasMaxLength(stringLengthAttr.MaxLength);
                }
            }
        }

        /// <summary>
        /// Configura las relaciones basadas en atributos
        /// </summary>
        private void ConfigureRelationships(Type entityType)
        {
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                // Clave foránea
                var fkAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                {
                    // El nombre de la propiedad de navegación
                    string navigationPropertyName = fkAttr.Name;

                    if (!string.IsNullOrEmpty(navigationPropertyName))
                    {
                        // Buscar la propiedad de navegación correspondiente
                        var navigationProperty = properties.FirstOrDefault(p => p.Name == navigationPropertyName);

                        if (navigationProperty != null)
                        {
                            // Configurar relación
                            _modelBuilder.Entity(entityType)
                                .HasOne(navigationProperty.PropertyType)
                                .WithMany()
                                .HasForeignKey(property.Name);
                        }
                    }
                }

                // Relación uno a muchos
                var oneToManyAttr = property.GetCustomAttribute<OneToManyAttribute>();
                if (oneToManyAttr != null && property.PropertyType.IsGenericType)
                {
                    Type relatedType = property.PropertyType.GenericTypeArguments[0];

                    _modelBuilder.Entity(entityType)
                        .HasMany(relatedType)
                        .WithOne()
                        .HasForeignKey(oneToManyAttr.ForeignKeyProperty);
                }

                // Relación muchos a uno
                var manyToOneAttr = property.GetCustomAttribute<ManyToOneAttribute>();
                if (manyToOneAttr != null)
                {
                    Type relatedType = property.PropertyType;

                    _modelBuilder.Entity(entityType)
                        .HasOne(relatedType)
                        .WithMany()
                        .HasForeignKey(manyToOneAttr.ForeignKeyProperty);
                }

                // Relación muchos a muchos
                var manyToManyAttr = property.GetCustomAttribute<ManyToManyAttribute>();
                if (manyToManyAttr != null && property.PropertyType.IsGenericType)
                {
                    Type relatedType = property.PropertyType.GenericTypeArguments[0];

                    _modelBuilder.Entity(entityType)
                        .HasMany(relatedType)
                        .WithMany()
                        .UsingEntity(manyToManyAttr.JoinTableName,
                            l => l.HasOne(relatedType).WithMany().HasForeignKey(manyToManyAttr.RightKeyColumn),
                            r => r.HasOne(entityType).WithMany().HasForeignKey(manyToManyAttr.LeftKeyColumn));
                }
            }
        }
    }
}