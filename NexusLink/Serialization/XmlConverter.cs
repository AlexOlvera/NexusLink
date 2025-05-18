using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NexusLink.Serialization
{
    /// <summary>
    /// Clase para convertir objetos a y desde formato XML
    /// </summary>
    public class XmlConverter
    {
        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;

        /// <summary>
        /// Constructor predeterminado con configuración estándar
        /// </summary>
        public XmlConverter()
        {
            _writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };

            _readerSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };
        }

        /// <summary>
        /// Constructor con configuración personalizada
        /// </summary>
        public XmlConverter(XmlWriterSettings writerSettings, XmlReaderSettings readerSettings)
        {
            _writerSettings = writerSettings ?? throw new ArgumentNullException(nameof(writerSettings));
            _readerSettings = readerSettings ?? throw new ArgumentNullException(nameof(readerSettings));
        }

        /// <summary>
        /// Serializa un objeto a XML
        /// </summary>
        public string Serialize<T>(T value)
        {
            if (value == null)
                return null;

            var serializer = new XmlSerializer(typeof(T));

            using (var sw = new StringWriter())
            {
                using (var writer = XmlWriter.Create(sw, _writerSettings))
                {
                    serializer.Serialize(writer, value);
                    return sw.ToString();
                }
            }
        }

        /// <summary>
        /// Serializa un objeto a XML y lo escribe en un stream
        /// </summary>
        public void Serialize<T>(Stream stream, T value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (value == null)
                return;

            var serializer = new XmlSerializer(typeof(T));

            using (var writer = XmlWriter.Create(stream, _writerSettings))
            {
                serializer.Serialize(writer, value);
            }
        }

        /// <summary>
        /// Deserializa XML a un objeto
        /// </summary>
        public T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            var serializer = new XmlSerializer(typeof(T));

            using (var sr = new StringReader(xml))
            {
                using (var reader = XmlReader.Create(sr, _readerSettings))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
        }

        /// <summary>
        /// Deserializa XML desde un stream a un objeto
        /// </summary>
        public T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var serializer = new XmlSerializer(typeof(T));

            using (var reader = XmlReader.Create(stream, _readerSettings))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializa XML a un tipo especificado en tiempo de ejecución
        /// </summary>
        public object Deserialize(string xml, Type type)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var serializer = new XmlSerializer(type);

            using (var sr = new StringReader(xml))
            {
                using (var reader = XmlReader.Create(sr, _readerSettings))
                {
                    return serializer.Deserialize(reader);
                }
            }
        }

        /// <summary>
        /// Validates if a string is valid XML
        /// </summary>
        public bool IsValidXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return false;

            try
            {
                using (var sr = new StringReader(xml))
                {
                    using (var reader = XmlReader.Create(sr, _readerSettings))
                    {
                        while (reader.Read()) { }
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}