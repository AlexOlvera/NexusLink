using System;

namespace NexusLink.Attributes.BehaviorAttributes
{
    /// <summary>
    /// Atributo para marcar una propiedad como calculada en la base de datos
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ComputedAttribute : Attribute
    {
        /// <summary>
        /// Expresión SQL para el cálculo
        /// </summary>
        public string SqlExpression { get; }

        /// <summary>
        /// Indica si el valor calculado se almacena en la base de datos
        /// </summary>
        public bool IsPersisted { get; set; }

        /// <summary>
        /// Constructor con expresión SQL
        /// </summary>
        public ComputedAttribute(string sqlExpression)
        {
            SqlExpression = sqlExpression;
            IsPersisted = false;
        }

        /// <summary>
        /// Constructor con expresión SQL y persistencia
        /// </summary>
        public ComputedAttribute(string sqlExpression, bool isPersisted)
        {
            SqlExpression = sqlExpression;
            IsPersisted = isPersisted;
        }
    }
}