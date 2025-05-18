using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class NexusLinkEntityConfigurator : IEntityTypeConfiguration
    {
        public void Configure(EntityTypeBuilder builder)
        {
            var entityType = builder.Metadata.ClrType;

            // Busca atributos de tabla
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                builder.ToTable(tableAttr.Name, tableAttr.Schema);
            }

            // Configura propiedades basado en atributos
            foreach (var property in entityType.GetProperties())
            {
                // Clave primaria
                var pkAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
                if (pkAttr != null)
                {
                    builder.HasKey(property.Name);
                }

                // Columna
                var colAttr = property.GetCustomAttribute<ColumnAttribute>();
                if (colAttr != null)
                {
                    var propertyBuilder = builder.Property(property.Name);
                    propertyBuilder.HasColumnName(colAttr.Name);

                    if (colAttr.isRequired)
                        propertyBuilder.IsRequired();
                }

                // Clave foránea
                var fkAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (fkAttr != null)
                {
                    // Implementamos la configuración de FK aquí
                    ConfigureForeignKey(builder, property, fkAttr);
                }
            }
        }

        private void ConfigureForeignKey(EntityTypeBuilder builder, PropertyInfo property, ForeignKeyAttribute fkAttr)
        {
            // Obtenemos el tipo de entidad principal
            var entityType = builder.Metadata.ClrType;

            // El nombre de la tabla referenciada está en el atributo
            string referencedTableName = fkAttr.Name;

            // Buscamos la propiedad de navegación que corresponde a esta FK
            var navigationProperties = entityType.GetProperties()
                .Where(p => {
                    var navAttr = p.GetCustomAttribute<NavigationPropertyAttribute>();
                    return navAttr != null && navAttr.ForeignKeyProperty == property.Name;
                })
                .ToList();

            if (navigationProperties.Any())
            {
                // Existe una propiedad de navegación, configuramos la relación
                var navProperty = navigationProperties.First();

                // Obtenemos el tipo de la entidad referenciada
                Type referencedType = navProperty.PropertyType;

                // Si es una colección, obtenemos el tipo genérico
                if (referencedType.IsGenericType &&
                    (referencedType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     referencedType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    referencedType = referencedType.GetGenericArguments()[0];

                    // Es una relación uno-a-muchos
                    var navigation = builder.Metadata.FindNavigation(navProperty.Name);

                    // Configuramos la relación en EF
                    navigation.SetPropertyAccessMode(PropertyAccessMode.Field);

                    builder.HasMany(navProperty.Name)
                           .WithOne()
                           .HasForeignKey(property.Name)
                           .IsRequired(fkAttr.isRequired);

                    if (fkAttr.isUniqueKey)
                    {
                        builder.HasIndex(property.Name).IsUnique();
                    }
                }
                else
                {
                    // Es una relación uno-a-uno o muchos-a-uno
                    builder.HasOne(navProperty.Name)
                           .WithOne()
                           .HasForeignKey(entityType, property.Name)
                           .IsRequired(fkAttr.isRequired);

                    if (fkAttr.isUniqueKey)
                    {
                        builder.HasIndex(property.Name).IsUnique();
                    }
                }
            }
            else
            {
                // No hay propiedad de navegación, solo configuramos la FK
                builder.Property(property.Name)
                       .HasColumnName(fkAttr.Name)
                       .IsRequired(fkAttr.isRequired);

                if (fkAttr.isUniqueKey)
                {
                    builder.HasIndex(property.Name).IsUnique();
                }

                // Intentamos determinar la tabla referenciada por convención
                // Este código es más difícil sin la propiedad de navegación

                // Buscar todas las entidades con DbContext para encontrar coincidencias
                var modelBuilder = builder.Metadata.Model.Builder;
                var allEntityTypes = modelBuilder.Model.GetEntityTypes();

                foreach (var potentialTarget in allEntityTypes)
                {
                    // Buscar PK que coincida con el nombre de la FK
                    var pk = potentialTarget.FindPrimaryKey();
                    if (pk != null && pk.Properties.Count == 1)
                    {
                        var pkProp = pk.Properties[0];
                        if (pkProp.Name == fkAttr.Name.Replace("Id", ""))
                        {
                            // Encontramos una posible coincidencia, configuramos la FK
                            builder.HasOne(potentialTarget.ClrType)
                                   .WithMany()
                                   .HasForeignKey(property.Name)
                                   .IsRequired(fkAttr.isRequired);

                            break;
                        }
                    }
                }
            }
        }
    }
}
