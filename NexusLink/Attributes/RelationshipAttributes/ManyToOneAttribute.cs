using System;

namespace NexusLink.Attributes.RelationshipAttributes
{
    /// <summary>
    /// Atributo para definir una relación muchos a uno entre entidades
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ManyToOneAttribute : Attribute
    {
        /// <summary>
        /// Tipo de la entidad relacionada
        /// </summary>
        public Type RelatedEntityType { get; }

        /// <summary>
        /// Nombre de la propiedad de clave foránea en esta entidad
        /// </summary>
        public string ForeignKeyProperty { get; set; }

        /// <summary>
        /// Nombre de la propiedad de navegación inversa
        /// </summary>
        public string InverseProperty { get; set; }

        /// <summary>
        /// Constructor con tipo de entidad relacionada
        /// </summary>
        public ManyToOneAttribute(Type relatedEntityType)
        {
            RelatedEntityType = relatedEntityType;
        }

        /// <summary>
        /// Constructor con tipo de entidad relacionada y propiedad de clave foránea
        /// </summary>
        public ManyToOneAttribute(Type relatedEntityType, string foreignKeyProperty)
        {
            RelatedEntityType = relatedEntityType;
            ForeignKeyProperty = foreignKeyProperty;
        }
    }
}