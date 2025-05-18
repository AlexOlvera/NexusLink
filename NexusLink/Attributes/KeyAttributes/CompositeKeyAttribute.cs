using System;

namespace NexusLink.Attributes.KeyAttributes
{
    /// <summary>
    /// Atributo para marcar una propiedad como parte de una clave compuesta
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CompositeKeyAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la columna en la base de datos
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Nombre del grupo de clave compuesta al que pertenece esta propiedad
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Orden de la columna dentro de la clave compuesta
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Indica si la columna es requerida (NOT NULL)
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Constructor con nombre de columna
        /// </summary>
        public CompositeKeyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Constructor con nombre de columna, nombre de clave y orden
        /// </summary>
        public CompositeKeyAttribute(string name, string keyName, int order)
        {
            Name = name;
            KeyName = keyName;
            Order = order;
        }
    }
}