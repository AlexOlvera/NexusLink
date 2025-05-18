using System;

namespace NexusLink.Attributes.RelationshipAttributes
{
    /// <summary>
    /// Atributo para definir una tabla de unión para relaciones muchos a muchos
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JoinTableAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la tabla de unión
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Esquema de la tabla de unión
        /// </summary>
        public string Schema { get; set; } = "dbo";

        /// <summary>
        /// Nombre de la columna de clave foránea que apunta a esta entidad
        /// </summary>
        public string JoinColumn { get; set; }

        /// <summary>
        /// Nombre de la columna de clave foránea que apunta a la entidad relacionada
        /// </summary>
        public string InverseJoinColumn { get; set; }

        /// <summary>
        /// Constructor con nombre de tabla de unión
        /// </summary>
        public JoinTableAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Constructor con nombre de tabla de unión y columnas
        /// </summary>
        public JoinTableAttribute(string name, string joinColumn, string inverseJoinColumn)
        {
            Name = name;
            JoinColumn = joinColumn;
            InverseJoinColumn = inverseJoinColumn;
        }
    }
}