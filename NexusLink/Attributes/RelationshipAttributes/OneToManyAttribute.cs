using System;

namespace NexusLink.Attributes.RelationshipAttributes
{
    /// <summary>
    /// Atributo para definir una relación uno a muchos entre entidades
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OneToManyAttribute : Attribute
    {
        /// <summary>
        /// Tipo de la entidad relacionada
        /// </summary>
        public Type RelatedEntityType { get; }

        /// <summary>
        /// Nombre de la propiedad de clave foránea en la entidad relacionada
        /// </summary>
        public string ForeignKeyProperty { get; set; }

        /// <summary>
        /// Nombre de la propiedad de navegación inversa
        /// </summary>
        public string InverseProperty { get; set; }

        /// <summary>
        /// Acción en cascada para eliminaciones
        /// </summary>
        public CascadeType DeleteBehavior { get; set; } = CascadeType.Restrict;

        /// <summary>
        /// Constructor con tipo de entidad relacionada
        /// </summary>
        public OneToManyAttribute(Type relatedEntityType)
        {
            RelatedEntityType = relatedEntityType;
        }

        /// <summary>
        /// Constructor con tipo de entidad relacionada y propiedad de clave foránea
        /// </summary>
        public OneToManyAttribute(Type relatedEntityType, string foreignKeyProperty)
        {
            RelatedEntityType = relatedEntityType;
            ForeignKeyProperty = foreignKeyProperty;
        }
    }
}
