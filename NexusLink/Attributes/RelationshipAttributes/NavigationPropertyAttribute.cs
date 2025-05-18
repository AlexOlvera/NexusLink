using System;

namespace NexusLink.Attributes.RelationshipAttributes
{
    /// <summary>
    /// Atributo que identifica una propiedad de navegación y la asocia con una propiedad de clave foránea.
    /// Este atributo se utiliza para configurar relaciones entre entidades en Entity Framework.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NavigationPropertyAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la propiedad que contiene la clave foránea asociada a esta relación.
        /// </summary>
        public string ForeignKeyProperty { get; set; }

        /// <summary>
        /// Indica si esta navegación es la principal en una relación uno-a-uno.
        /// </summary>
        public bool IsPrincipal { get; set; }

        /// <summary>
        /// Tipo de cascada para operaciones de eliminación.
        /// </summary>
        public CascadeType DeleteBehavior { get; set; } = CascadeType.Restrict;

        /// <summary>
        /// Constructor con nombre de propiedad de clave foránea.
        /// </summary>
        /// <param name="foreignKeyProperty">Nombre de la propiedad que contiene la clave foránea.</param>
        public NavigationPropertyAttribute(string foreignKeyProperty)
        {
            ForeignKeyProperty = foreignKeyProperty;
        }

        /// <summary>
        /// Constructor con nombre de propiedad de clave foránea y comportamiento de cascada.
        /// </summary>
        /// <param name="foreignKeyProperty">Nombre de la propiedad que contiene la clave foránea.</param>
        /// <param name="deleteBehavior">Comportamiento de cascada para eliminaciones.</param>
        public NavigationPropertyAttribute(string foreignKeyProperty, CascadeType deleteBehavior)
        {
            ForeignKeyProperty = foreignKeyProperty;
            DeleteBehavior = deleteBehavior;
        }
    }

    /// <summary>
    /// Define los tipos de comportamiento de cascada para las relaciones entre entidades.
    /// </summary>
    public enum CascadeType
    {
        /// <summary>
        /// No se permiten eliminaciones si existen referencias.
        /// </summary>
        Restrict,

        /// <summary>
        /// La eliminación de la entidad principal también elimina las entidades dependientes.
        /// </summary>
        Cascade,

        /// <summary>
        /// La eliminación de la entidad principal establece las propiedades FK a null en las entidades dependientes.
        /// </summary>
        SetNull,

        /// <summary>
        /// La eliminación de la entidad principal no afecta a las entidades dependientes.
        /// </summary>
        NoAction
    }
}