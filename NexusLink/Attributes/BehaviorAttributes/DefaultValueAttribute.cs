using System;

namespace NexusLink.Attributes.BehaviorAttributes
{
    /// <summary>
    /// Atributo para especificar un valor predeterminado para una propiedad
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Valor predeterminado
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Expresión SQL para el valor predeterminado
        /// </summary>
        public string SqlExpression { get; private set; }

        /// <summary>
        /// Indica si el valor predeterminado es una expresión SQL
        /// </summary>
        public bool IsSqlExpression { get; private set; }

        /// <summary>
        /// Constructor con valor explícito
        /// </summary>
        public DefaultValueAttribute(object value)
        {
            Value = value;
            IsSqlExpression = false;
        }

        /// <summary>
        /// Constructor con expresión SQL
        /// </summary>
        public static DefaultValueAttribute FromSqlExpression(string sqlExpression)
        {
            var attr = new DefaultValueAttribute(null);
            attr.SqlExpression = sqlExpression;
            attr.IsSqlExpression = true;
            return attr;
        }

        /// <summary>
        /// Constructor para valor predeterminado de fecha actual
        /// </summary>
        public static DefaultValueAttribute CurrentDate()
        {
            return FromSqlExpression("GETDATE()");
        }

        /// <summary>
        /// Constructor para valor predeterminado de GUID
        /// </summary>
        public static DefaultValueAttribute NewGuid()
        {
            return FromSqlExpression("NEWID()");
        }

        /// <summary>
        /// Constructor para valor predeterminado de usuario actual
        /// </summary>
        public static DefaultValueAttribute CurrentUser()
        {
            return FromSqlExpression("CURRENT_USER");
        }
    }
}