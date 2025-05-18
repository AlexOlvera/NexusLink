using System;

namespace NexusLink.Attributes.BehaviorAttributes
{
    /// <summary>
    /// Atributo para marcar una propiedad como auto-incrementable
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AutoIncrementAttribute : Attribute
    {
        /// <summary>
        /// Valor inicial para el auto-incremento
        /// </summary>
        public int Seed { get; set; } = 1;

        /// <summary>
        /// Incremento para cada nueva fila
        /// </summary>
        public int Increment { get; set; } = 1;

        /// <summary>
        /// Constructor predeterminado
        /// </summary>
        public AutoIncrementAttribute()
        {
        }

        /// <summary>
        /// Constructor con semilla e incremento
        /// </summary>
        public AutoIncrementAttribute(int seed, int increment)
        {
            Seed = seed;
            Increment = increment;
        }
    }
}