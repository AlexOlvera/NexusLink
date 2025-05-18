using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusLink.Attributes;

namespace NexusLink.EntityFramework.ModelBuilder
{
    /// <summary>
    /// Constructor de modelos fluido para Entity Framework.
    /// Proporciona una API fluida para configurar modelos EF Core utilizando atributos NexusLink.
    /// </summary>
    public class FluentModelBuilder
    {
        private readonly Microsoft.EntityFrameworkCore.ModelBuilder _modelBuilder;

        /// <summary>
        /// Crea una nueva instancia de FluentModelBuilder
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder de Entity Framework</param>
        public FluentModelBuilder(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
        }

        /// <summary>
        /// Configura una entidad
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> Entity<T>() where T : class
        {
            return new EntityBuilder<T>(_modelBuilder.Entity<T>());
        }

        /// <summary>
        /// Ignora una entidad
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <returns>Constructor de modelo fluido</returns>
        public FluentModelBuilder Ignore<T>() where T : class
        {
            _modelBuilder.Ignore<T>();
            return this;
        }

        /// <summary>
        /// Obtiene el ModelBuilder subyacente
        /// </summary>
        /// <returns>ModelBuilder de Entity Framework</returns>
        public Microsoft.EntityFrameworkCore.ModelBuilder GetModelBuilder()
        {
            return _modelBuilder;
        }
    }

    /// <summary>
    /// Constructor de entidad fluido
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    public class EntityBuilder<T> where T : class
    {
        private readonly EntityTypeBuilder<T> _entityTypeBuilder;

        /// <summary>
        /// Crea una nueva instancia de EntityBuilder
        /// </summary>
        /// <param name="entityTypeBuilder">EntityTypeBuilder de Entity Framework</param>
        public EntityBuilder(EntityTypeBuilder<T> entityTypeBuilder)
        {
            _entityTypeBuilder = entityTypeBuilder ?? throw new ArgumentNullException(nameof(entityTypeBuilder));
        }

        /// <summary>
        /// Configura la tabla
        /// </summary>
        /// <param name="name">Nombre de la tabla</param>
        /// <param name="schema">Esquema de la tabla</param>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> ToTable(string name, string schema = null)
        {
            _entityTypeBuilder.ToTable(name, schema);
            return this;
        }

        /// <summary>
        /// Configura la clave primaria
        /// </summary>
        /// <typeparam name="TKey">Tipo de la clave</typeparam>
        /// <param name="keyExpression">Expresión para la clave</param>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> HasKey<TKey>(Expression<Func<T, TKey>> keyExpression)
        {
            _entityTypeBuilder.HasKey(keyExpression);
            return this;
        }

        /// <summary>
        /// Configura una propiedad
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="propertyExpression">Expresión para la propiedad</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return new PropertyBuilder<T, TProperty>(this, _entityTypeBuilder.Property(propertyExpression));
        }

        /// <summary>
        /// Configura una relación uno a muchos
        /// </summary>
        /// <typeparam name="TRelated">Tipo de la entidad relacionada</typeparam>
        /// <param name="navigationExpression">Expresión para la navegación</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasMany<TRelated>(Expression<Func<T, IEnumerable<TRelated>>> navigationExpression)
            where TRelated : class
        {
            return new RelationshipBuilder<T, TRelated>(this, _entityTypeBuilder.HasMany(navigationExpression));
        }

        /// <summary>
        /// Configura una relación uno a uno
        /// </summary>
        /// <typeparam name="TRelated">Tipo de la entidad relacionada</typeparam>
        /// <param name="navigationExpression">Expresión para la navegación</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasOne<TRelated>(Expression<Func<T, TRelated>> navigationExpression)
            where TRelated : class
        {
            return new RelationshipBuilder<T, TRelated>(this, _entityTypeBuilder.HasOne(navigationExpression));
        }

        /// <summary>
        /// Ignora una propiedad
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="propertyExpression">Expresión para la propiedad</param>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> Ignore<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            _entityTypeBuilder.Ignore(propertyExpression);
            return this;
        }

        /// <summary>
        /// Obtiene el EntityTypeBuilder subyacente
        /// </summary>
        /// <returns>EntityTypeBuilder de Entity Framework</returns>
        public EntityTypeBuilder<T> GetEntityTypeBuilder()
        {
            return _entityTypeBuilder;
        }
    }

    /// <summary>
    /// Constructor de propiedad fluido
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    /// <typeparam name="TProperty">Tipo de propiedad</typeparam>
    public class PropertyBuilder<T, TProperty> where T : class
    {
        private readonly EntityBuilder<T> _entityBuilder;
        private readonly Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<TProperty> _propertyBuilder;

        /// <summary>
        /// Crea una nueva instancia de PropertyBuilder
        /// </summary>
        /// <param name="entityBuilder">Constructor de entidad</param>
        /// <param name="propertyBuilder">PropertyBuilder de Entity Framework</param>
        public PropertyBuilder(
            EntityBuilder<T> entityBuilder,
            Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<TProperty> propertyBuilder)
        {
            _entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
            _propertyBuilder = propertyBuilder ?? throw new ArgumentNullException(nameof(propertyBuilder));
        }

        /// <summary>
        /// Configura la columna
        /// </summary>
        /// <param name="name">Nombre de la columna</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> HasColumnName(string name)
        {
            _propertyBuilder.HasColumnName(name);
            return this;
        }

        /// <summary>
        /// Configura el tipo de columna
        /// </summary>
        /// <param name="typeName">Nombre del tipo de columna</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> HasColumnType(string typeName)
        {
            _propertyBuilder.HasColumnType(typeName);
            return this;
        }

        /// <summary>
        /// Configura la longitud máxima
        /// </summary>
        /// <param name="maxLength">Longitud máxima</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> HasMaxLength(int maxLength)
        {
            _propertyBuilder.HasMaxLength(maxLength);
            return this;
        }

        /// <summary>
        /// Configura la propiedad como requerida
        /// </summary>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> IsRequired()
        {
            _propertyBuilder.IsRequired();
            return this;
        }

        /// <summary>
        /// Configura la propiedad como clave primaria
        /// </summary>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> IsPrimaryKey()
        {
            _propertyBuilder.IsRequired();
            _propertyBuilder.ValueGeneratedOnAdd();
            return this;
        }

        /// <summary>
        /// Configura la propiedad como clave única
        /// </summary>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> IsUniqueKey()
        {
            _propertyBuilder.IsRequired();
            return this;
        }

        /// <summary>
        /// Configura la propiedad con un valor predeterminado
        /// </summary>
        /// <param name="value">Valor predeterminado</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> HasDefaultValue(object value)
        {
            _propertyBuilder.HasDefaultValue(value);
            return this;
        }

        /// <summary>
        /// Configura la propiedad con una expresión SQL para valor predeterminado
        /// </summary>
        /// <param name="sql">Expresión SQL</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> HasDefaultValueSql(string sql)
        {
            _propertyBuilder.HasDefaultValueSql(sql);
            return this;
        }

        /// <summary>
        /// Configura la propiedad como calculada
        /// </summary>
        /// <param name="sql">Expresión SQL</param>
        /// <returns>Constructor de propiedad</returns>
        public PropertyBuilder<T, TProperty> IsComputed(string sql)
        {
            _propertyBuilder.HasComputedColumnSql(sql);
            return this;
        }

        /// <summary>
        /// Vuelve al constructor de entidad
        /// </summary>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> Entity()
        {
            return _entityBuilder;
        }
    }

    /// <summary>
    /// Constructor de relación fluido
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    /// <typeparam name="TRelated">Tipo de entidad relacionada</typeparam>
    public class RelationshipBuilder<T, TRelated>
        where T : class
        where TRelated : class
    {
        private readonly EntityBuilder<T> _entityBuilder;
        private readonly object _navigationBuilder;

        /// <summary>
        /// Crea una nueva instancia de RelationshipBuilder
        /// </summary>
        /// <param name="entityBuilder">Constructor de entidad</param>
        /// <param name="navigationBuilder">Referencia al NavigationBuilder de EF Core</param>
        public RelationshipBuilder(EntityBuilder<T> entityBuilder, object navigationBuilder)
        {
            _entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
            _navigationBuilder = navigationBuilder ?? throw new ArgumentNullException(nameof(navigationBuilder));
        }

        /// <summary>
        /// Configura la relación con uno
        /// </summary>
        /// <param name="navigationExpression">Expresión para la navegación</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> WithOne(Expression<Func<TRelated, T>> navigationExpression = null)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                if (navigationExpression != null)
                {
                    collectionBuilder.WithOne(navigationExpression);
                }
                else
                {
                    collectionBuilder.WithOne();
                }
            }

            return this;
        }

        /// <summary>
        /// Configura la relación con muchos
        /// </summary>
        /// <param name="navigationExpression">Expresión para la navegación</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> WithMany(Expression<Func<TRelated, IEnumerable<T>>> navigationExpression = null)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                if (navigationExpression != null)
                {
                    referenceBuilder.WithMany(navigationExpression);
                }
                else
                {
                    referenceBuilder.WithMany();
                }
            }

            return this;
        }

        /// <summary>
        /// Configura la clave foránea
        /// </summary>
        /// <param name="foreignKeyExpression">Expresión para la clave foránea</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasForeignKey<TDependentKey>(
            Expression<Func<TRelated, TDependentKey>> foreignKeyExpression)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.HasForeignKey(foreignKeyExpression);
            }
            else if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.HasForeignKey(foreignKeyExpression);
            }

            return this;
        }

        /// <summary>
        /// Configura la clave principal
        /// </summary>
        /// <param name="keyExpression">Expresión para la clave principal</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasPrincipalKey<TPrincipalKey>(
            Expression<Func<T, TPrincipalKey>> keyExpression)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.HasPrincipalKey(keyExpression);
            }
            else if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.HasPrincipalKey(keyExpression);
            }

            return this;
        }

        /// <summary>
        /// Configura la restricción de eliminación
        /// </summary>
        /// <param name="deleteBehavior">Comportamiento de eliminación</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> OnDelete(DeleteBehavior deleteBehavior)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.OnDelete(deleteBehavior);
            }
            else if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.OnDelete(deleteBehavior);
            }

            return this;
        }

        /// <summary>
        /// Configura la carga automática
        /// </summary>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> AutoInclude()
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.AutoInclude();
            }
            else if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.AutoInclude();
            }

            return this;
        }

        /// <summary>
        /// Configura una relación muchos a muchos
        /// </summary>
        /// <param name="joinTableName">Nombre de la tabla de unión</param>
        /// <param name="leftKeyName">Nombre de la clave izquierda</param>
        /// <param name="rightKeyName">Nombre de la clave derecha</param>
        /// <param name="schema">Esquema de la tabla</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> UsingJoinTable(
            string joinTableName,
            string leftKeyName,
            string rightKeyName,
            string schema = null)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.UsingEntity(builder =>
                {
                    builder.ToTable(joinTableName, schema);

                    builder.Property<int>(leftKeyName);
                    builder.Property<int>(rightKeyName);

                    builder.HasKey(leftKeyName, rightKeyName);

                    builder.HasOne<T>()
                        .WithMany()
                        .HasForeignKey(leftKeyName);

                    builder.HasOne<TRelated>()
                        .WithMany()
                        .HasForeignKey(rightKeyName);
                });
            }

            return this;
        }

        /// <summary>
        /// Vuelve al constructor de entidad
        /// </summary>
        /// <returns>Constructor de entidad</returns>
        public EntityBuilder<T> Entity()
        {
            return _entityBuilder;
        }

        /// <summary>
        /// Configura la consulta de filtro global
        /// </summary>
        /// <param name="filterExpression">Expresión de filtro</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasQueryFilter(Expression<Func<TRelated, bool>> filterExpression)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                var entityType = collectionBuilder.OwnedEntityType;
                entityType.Builder.HasQueryFilter(filterExpression);
            }

            return this;
        }

        /// <summary>
        /// Configura el nombre de la restricción de clave foránea
        /// </summary>
        /// <param name="name">Nombre de la restricción</param>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> HasConstraintName(string name)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.CollectionNavigationBuilder<T, TRelated> collectionBuilder)
            {
                collectionBuilder.HasConstraintName(name);
            }
            else if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.HasConstraintName(name);
            }

            return this;
        }

        /// <summary>
        /// Configura la propiedad como opcional
        /// </summary>
        /// <returns>Constructor de relación</returns>
        public RelationshipBuilder<T, TRelated> IsRequired(bool required = true)
        {
            if (_navigationBuilder is Microsoft.EntityFrameworkCore.Metadata.Builders.ReferenceNavigationBuilder<T, TRelated> referenceBuilder)
            {
                referenceBuilder.IsRequired(required);
            }

            return this;
        }
    }
}