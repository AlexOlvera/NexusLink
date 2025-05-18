using System;

namespace NexusLink.Attributes.RelationshipAttributes
{
    /// <summary>
    /// Atributo para definir una relación muchos a muchos entre entidades
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ManyToManyAttribute : Attribute
    {
        /// <summary>
        /// Tipo de la entidad relacionada
        /// </summary>
        public Type RelatedEntityType { get; }

        /// <summary>
        /// Nombre de la tabla de unión
        /// </summary>
        public string JoinTableName { get; set; }

        /// <summary>
        /// Nombre de la columna de clave foránea que apunta a esta entidad
        /// </summary>
        public string JoinColumnName { get; set; }

        /// <summary>
        /// Nombre de la columna de clave foránea que apunta a la entidad relacionada
        /// </summary>
        public string InverseJoinColumnName { get; set; }

        /// <summary>
        /// Nombre de la propiedad de navegación inversa
        /// </summary>
        public string InverseProperty { get; set; }

        /// <summary>
        /// Constructor con tipo de entidad relacionada
        /// </summary>
        public ManyToManyAttribute(Type relatedEntityType)
        {
            RelatedEntityType = relatedEntityType;
        }

        /// <summary>
        /// Constructor con tipo de entidad relacionada y nombre de tabla de unión
        /// </summary>
        public ManyToManyAttribute(Type relatedEntityType, string joinTableName)
        {
            RelatedEntityType = relatedEntityType;
            JoinTableName = joinTableName;
        }
    }
}