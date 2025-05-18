using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink.Serialization.FormatProviders
{
    /// <summary>
    /// Proveedor de formato para fechas y horas.
    /// </summary>
    public class DateTimeFormatProvider : CustomFormatProvider
    {
        private readonly CultureInfo _culture;
        private readonly string _defaultFormat;

        public DateTimeFormatProvider(CultureInfo culture = null, string defaultFormat = "yyyy-MM-dd")
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

            if (arg is DateTime dateTime)
            {
                if (string.IsNullOrEmpty(format))
                    format = _defaultFormat;

                return dateTime.ToString(format, _culture);
            }

            if (arg is IFormattable formattable)
                return formattable.ToString(format, _culture);

            return arg.ToString();
        }

        /// <summary>
        /// Formatea un valor DateTime utilizando este proveedor.
        /// </summary>
        public string FormatDateTime(DateTime value, string format = null)
        {
            if (string.IsNullOrEmpty(format))
                format = _defaultFormat;

            return Format(format, value, this);
        }

        /// <summary>
        /// Intenta convertir una cadena a DateTime utilizando este proveedor.
        /// </summary>
        public bool TryParseDateTime(string dateTimeString, out DateTime result, string format = null)
        {
            if (string.IsNullOrEmpty(format))
                format = _defaultFormat;

            return DateTime.TryParseExact(dateTimeString, format, _culture,
                DateTimeStyles.None, out result);
        }
    }
}
