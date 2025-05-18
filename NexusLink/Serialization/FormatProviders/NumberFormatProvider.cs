using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink.Serialization.FormatProviders
{
    /// <summary>
    /// Proveedor de formato para números.
    /// </summary>
    public class NumberFormatProvider : CustomFormatProvider
    {
        private readonly CultureInfo _culture;
        private readonly string _defaultFormat;

        public NumberFormatProvider(CultureInfo culture = null, string defaultFormat = "N2")
        {
            _culture = culture ?? CultureInfo.CurrentCulture;
            _defaultFormat = defaultFormat;
        }

        public override object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;

            return null;
        }

        public override string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null) return string.Empty;

            if (arg is IFormattable formattable)
            {
                if (string.IsNullOrEmpty(format))
                    format = _defaultFormat;

                return formattable.ToString(format, _culture);
            }

            return arg.ToString();
        }

        /// <summary>
        /// Formatea un valor numérico utilizando este proveedor.
        /// </summary>
        public string FormatNumber<T>(T value, string format = null) where T : struct, IFormattable
        {
            if (string.IsNullOrEmpty(format))
                format = _defaultFormat;

            return Format(format, value, this);
        }

        /// <summary>
        /// Intenta convertir una cadena a un valor numérico utilizando este proveedor.
        /// </summary>
        public bool TryParseDecimal(string numberString, out decimal result)
        {
            return decimal.TryParse(numberString, NumberStyles.Any, _culture, out result);
        }

        /// <summary>
        /// Intenta convertir una cadena a un valor numérico utilizando este proveedor.
        /// </summary>
        public bool TryParseDouble(string numberString, out double result)
        {
            return double.TryParse(numberString, NumberStyles.Any, _culture, out result);
        }
    }
}
