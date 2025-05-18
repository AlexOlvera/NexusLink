using System;
using System.Globalization;

namespace NexusLink.Serialization.FormatProviders
{
    /// <summary>
    /// Base para proveedores de formato personalizados.
    /// </summary>
    public abstract class CustomFormatProvider : IFormatProvider, ICustomFormatter
    {
        public abstract object GetFormat(Type formatType);
        public abstract string Format(string format, object arg, IFormatProvider formatProvider);
    }
}