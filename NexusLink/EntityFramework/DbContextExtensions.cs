using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusLink.Attributes;
using NexusLink.Attributes.RelationshipAttributes;
using NexusLink.Data.Mapping;

namespace NexusLink.EntityFramework
{
    /// <summary>
    /// Extensiones para DbContext que facilitan la integración con NexusLink.
    /// Proporciona métodos para configurar el modelo a partir de atributos NexusLink.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Configura el modelo del DbContext utilizando atributos NexusLink
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder de Entity Framework</param>
        /// <param name="assemblies">Ensamblados a escanear (null para el ensamblado actual)</param>
        /// <returns>ModelBuilder para encadenamiento</returns>
        public static ModelBuilder ConfigureFromNexusLinkAttributes(
            this ModelBuilder modelBuilder,
            params Assembly[] assemblies)
        {
            if (modelBuilder == null)
                throw new ArgumentNullException(nameof(modelBuilder));

            // Obtener ensamblados
            var targetAssemblies = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetCallingAssembly() };

            // Obtener tipos con atributo TableAttribute
            var entityTypes = targetAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes<TableAttribute>(true).Any());

            foreach (var entityType in entityTypes)
            {
                ConfigureEntity(modelBuilder, entityType);
            }

            return modelBuilder;
        }

        /// <summary>
        /// Configura una entidad en el modelo
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder de Entity Framework</param>
        /// <param name="entityType">Tipo de entidad</param>
        private static void ConfigureEntity(ModelBuilder modelBuilder, Type entityType)
        {
            // Obtener atributo de tabla
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>(true);

            if (tableAttribute == null)
                return;

            // Configurar entidad
            var entityBuilder = modelBuilder.Entity(entityType);

            // Configurar tabla
            entityBuilder.ToTable(tableAttribute.Name, tableAttribute.Schema);

            // Configurar propiedades
            foreach (var property in entityType.GetProperties())
            {
                ConfigureProperty(entityBuilder, property);
            }

            // Configurar relaciones
            ConfigureRelationships(modelBuilder, entityType);
        }

        /// <summary>
        /// Configura una propiedad en el modelo
        /// </summary>
        /// <param name="entityBuilder">EntityTypeBuilder de Entity Framework</param>
        /// <param name="property">Propiedad a configurar</param>
        private static void ConfigureProperty(IMutableEntityType entityBuilder, PropertyInfo property)
        {
            // Obtener configuración de columna
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>(true);

            if (columnAttribute != null)
            {
                // Configurar columna
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.SetColumnName(columnAttribute.Name);

                    // Configurar como requerido
                    if (columnAttribute.IsRequired)
                    {
                        propertyBuilder.IsRequired = true;
                    }
                }
            }

            // Configurar clave primaria
            var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>(true);

            if (primaryKeyAttribute != null)
            {
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.SetColumnName(primaryKeyAttribute.Name);
                    entityBuilder.SetPrimaryKey(property.Name);

                    // Configurar como autoincremental
                    if (primaryKeyAttribute.IsAddedAutomatically)
                    {
                        propertyBuilder.ValueGenerated = ValueGenerated.OnAdd;
                    }
                }
            }

            // Configurar clave única
            var uniqueKeyAttribute = property.GetCustomAttribute<UniqueKeyAttribute>(true);

            if (uniqueKeyAttribute != null)
            {
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.SetColumnName(uniqueKeyAttribute.Name);
                    entityBuilder.AddKey(property.Name);

                    // Configurar como requerido
                    if (uniqueKeyAttribute.IsRequired)
                    {
                        propertyBuilder.IsRequired = true;
                    }
                }
            }

            // Configurar clave foránea
            var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>(true);

            if (foreignKeyAttribute != null)
            {
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.SetColumnName(foreignKeyAttribute.Name);

                    // Configurar como requerido
                    if (foreignKeyAttribute.IsRequired)
                    {
                        propertyBuilder.IsRequired = true;
                    }
                }
            }

            // Configurar valores predeterminados
            var defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>(true);

            if (defaultValueAttribute != null)
            {
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.SetDefaultValue(defaultValueAttribute.Value);
                }
            }

            // Configurar propiedades calculadas
            var computedAttribute = property.GetCustomAttribute<ComputedAttribute>(true);

            if (computedAttribute != null)
            {
                var propertyBuilder = entityBuilder.FindProperty(property.Name);

                if (propertyBuilder != null)
                {
                    propertyBuilder.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                    propertyBuilder.SetDefaultValueSql(computedAttribute.Expression);
                }
            }

            // Configurar propiedades ignoradas
            var ignoreAttribute = property.GetCustomAttribute<IgnoreAttribute>(true);

            if (ignoreAttribute != null)
            {
                entityBuilder.RemoveProperty(property.Name);
            }
        }

        /// <summary>
        /// Configura relaciones en el modelo
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder de Entity Framework</param>
        /// <param name="entityType">Tipo de entidad</param>
        private static void ConfigureRelationships(ModelBuilder modelBuilder, Type entityType)
        {
            // Configurar relaciones 1:N
            foreach (var property in entityType.GetProperties())
            {
                var oneToManyAttribute = property.GetCustomAttribute<OneToManyAttribute>(true);

                if (oneToManyAttribute != null)
                {
                    var principalType = oneToManyAttribute.PrincipalType;
                    var dependentProperty = oneToManyAttribute.DependentProperty;

                    modelBuilder.Entity(entityType)
                        .HasMany(principalType)
                        .WithOne(entityType)
                        .HasForeignKey(dependentProperty)
                        .OnDelete(oneToManyAttribute.DeleteBehavior);
                }

                var manyToOneAttribute = property.GetCustomAttribute<ManyToOneAttribute>(true);

                if (manyToOneAttribute != null)
                {
                    var principalType = manyToOneAttribute.PrincipalType;
                    var dependentProperty = property.Name;

                    modelBuilder.Entity(entityType)
                        .HasOne(principalType)
                        .WithMany()
                        .HasForeignKey(dependentProperty)
                        .OnDelete(manyToOneAttribute.DeleteBehavior);
                }

                var manyToManyAttribute = property.GetCustomAttribute<ManyToManyAttribute>(true);

                if (manyToManyAttribute != null)
                {
                    var principalType = manyToManyAttribute.PrincipalType;
                    var joinTableName = manyToManyAttribute.JoinTableName;

                    modelBuilder.Entity(entityType)
                        .HasMany(principalType)
                        .WithMany()
                        .UsingEntity(joinEntityBuilder =>
                        {
                            joinEntityBuilder.ToTable(joinTableName);

                            // Configurar claves
                            var leftKey = $"{entityType.Name}Id";
                            var rightKey = $"{principalType.Name}Id";

                            joinEntityBuilder.Property<int>(leftKey);
                            joinEntityBuilder.Property<int>(rightKey);

                            joinEntityBuilder.HasKey(leftKey, rightKey);
                        });
                }
            }
        }

        /// <summary>
        /// Configura un DbContext para usar NexusLink
        /// </summary>
        /// <param name="dbContext">DbContext a configurar</param>
        /// <returns>DbContext para encadenamiento</returns>
        public static DbContext ConfigureForNexusLink(this DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            // Configurar EF para usar atributos NexusLink
            dbContext.Database.AutoTransactionsEnabled = false;

            return dbContext;
        }

        /// <summary>
        /// Migra la base de datos a partir de atributos NexusLink
        /// </summary>
        /// <param name="dbContext">DbContext a migrar</param>
        /// <returns>True si la migración fue exitosa, false en caso contrario</returns>
        public static bool MigrateFromNexusLinkAttributes(this DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            try
            {
                // Aplicar migraciones
                dbContext.Database.EnsureCreated();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Migra la base de datos a partir de atributos NexusLink (asíncrono)
        /// </summary>
        /// <param name="dbContext">DbContext a migrar</param>
        /// <returns>True si la migración fue exitosa, false en caso contrario</returns>
        public static async Task<bool> MigrateFromNexusLinkAttributesAsync(this DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            try
            {
                // Aplicar migraciones
                await dbContext.Database.EnsureCreatedAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Genera scripts SQL a partir de atributos NexusLink
        /// </summary>
        /// <param name="dbContext">DbContext para el que generar scripts</param>
        /// <returns>Script SQL generado</returns>
        public static string GenerateSqlScriptFromNexusLinkAttributes(this DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            return dbContext.Database.GenerateCreateScript();
        }
    }
}