using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink.EntityFramework
{
    public class NexusLinkDbContext : DbContext
    {
        private readonly DatabaseSelector _databaseSelector;

        public NexusLinkDbContext(DatabaseSelector databaseSelector)
            : base("Default")
        {
            _databaseSelector = databaseSelector;

            // Configura el DbContext para usar la conexión actual
            this.Database.Connection.ConnectionString =
                _databaseSelector.CurrentConnection.ConnectionString;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Aplicamos configuraciones base
            base.OnModelCreating(modelBuilder);

            // Escanear todas las entidades en el ensamblado actual
            var entityTypes = this.GetType().Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TableAttribute>(true).Any())
                .ToList();

            foreach (var entityType in entityTypes)
            {
                // Aplicar configuración para cada entidad encontrada
                var entityConfigurator = new NexusLinkEntityConfigurator();

                // En versiones antiguas de EF, esto era diferente
                // EF Core:
                modelBuilder.Entity(entityType, b => entityConfigurator.Configure(b));

                // Para EF 6 (requiere ajustes):
                var entityConfig = modelBuilder.Entity(entityType);
                ConfigureEntityWithAttributes(entityConfig, entityType);
            }
        }

        // Método específico para EF 6
        private void ConfigureEntityWithAttributes(EntityTypeConfiguration entityConfig, Type entityType)
        {
            // Configuración de tabla
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                entityConfig.ToTable(tableAttr.Name, tableAttr.Schema);
            }

            // Configurar propiedades
            foreach (var property in entityType.GetProperties())
            {
                // Clave primaria
                var pkAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (pkAttr != null)
                {
                    entityConfig.HasKey(property.Name);
                }

                // Columna
                var colAttr = property.GetCustomAttribute<ColumnAttribute>();
                if (colAttr != null)
                {
                    entityConfig.Property(property.Name)
                        .HasColumnName(colAttr.Name)
                        .IsRequired(colAttr.isRequired);
                }

                // Clave foránea
                var fkAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                {
                    ConfigureForeignKeyRelationship(entityConfig, entityType, property, fkAttr);
                }
            }
        }

        private void ConfigureForeignKeyRelationship(
            EntityTypeConfiguration entityConfig,
            Type entityType,
            PropertyInfo property,
            ForeignKeyAttribute fkAttr)
        {
            // Buscar propiedad de navegación
            var navProperties = entityType.GetProperties()
                .Where(p => {
                    var navAttr = p.GetCustomAttribute<NavigationPropertyAttribute>();
                    return navAttr != null && navAttr.ForeignKeyProperty == property.Name;
                })
                .ToList();

            if (navProperties.Any())
            {
                var navProperty = navProperties.First();
                var navPropertyType = navProperty.PropertyType;

                // Determinar tipo de relación
                if (navPropertyType.IsGenericType &&
                    (navPropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     navPropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    // Es una relación uno-a-muchos (colección)
                    var elementType = navPropertyType.GetGenericArguments()[0];

                    // En EF 6, la configuración es diferente
                    entityConfig.HasMany(navProperty.Name)
                        .WithRequired() // o WithOptional si !fkAttr.isRequired
                        .HasForeignKey(property.Name);
                }
                else
                {
                    // Es una relación uno-a-uno o muchos-a-uno
                    entityConfig.HasRequired(navProperty.Name) // o HasOptional si !fkAttr.isRequired
                        .WithMany() // o WithRequiredPrincipal o WithOptionalPrincipal si es 1:1
                        .HasForeignKey(property.Name);
                }

                if (fkAttr.isUniqueKey)
                {
                    // En EF6, configurar índice único
                    entityConfig.Property(property.Name)
                        .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_" + property.Name) { IsUnique = true }));
                }
            }
            else
            {
                // Sólo configurar la columna de FK sin relación de navegación
                entityConfig.Property(property.Name)
                    .HasColumnName(fkAttr.Name)
                    .IsRequired(fkAttr.isRequired);

                if (fkAttr.isUniqueKey)
                {
                    entityConfig.Property(property.Name)
                        .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_" + property.Name) { IsUnique = true }));
                }
            }
        }

        // Override para soportar cambio de base de datos
        public void UseDatabase(string databaseName)
        {
            _databaseSelector.CurrentDatabaseName = databaseName;
            this.Database.Connection.ConnectionString =
                _databaseSelector.CurrentConnection.ConnectionString;
        }

        // Métodos para trabajar con transacciones NexusLink
        public void UseTransaction(System.Transactions.Transaction transaction)
        {
            this.Database.UseTransaction(
                transaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete)
                as SqlTransaction);
        }
    }
}
