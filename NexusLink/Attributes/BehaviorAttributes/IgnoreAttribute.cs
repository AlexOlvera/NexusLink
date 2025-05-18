using System;

namespace NexusLink.Attributes.BehaviorAttributes
{
    /// <summary>
    /// Atributo para ignorar una propiedad en operaciones específicas
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        /// <summary>
        /// Indica si la propiedad debe ignorarse en operaciones de inserción
        /// </summary>
        public bool OnInsert { get; set; }

        /// <summary>
        /// Indica si la propiedad debe ignorarse en operaciones de actualización
        /// </summary>
        public bool OnUpdate { get; set; }

        /// <summary>
        /// Indica si la propiedad debe ignorarse en operaciones de selección
        /// </summary>
        public bool OnSelect { get; set; }

        /// <summary>
        /// Constructor predeterminado que ignora la propiedad en todas las operaciones
        /// </summary>
        public IgnoreAttribute()
        {
            OnInsert = true;
            OnUpdate = true;
            OnSelect = true;
        }

        /// <summary>
        /// Constructor que permite especificar las operaciones en las que se ignora
        /// </summary>
        public IgnoreAttribute(bool onInsert, bool onUpdate, bool onSelect)
        {
            OnInsert = onInsert;
            OnUpdate = onUpdate;
            OnSelect = onSelect;
        }
    }
}