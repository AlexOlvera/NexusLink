using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NexusLink.Serialization
{
    /// <summary>
    /// Clase para convertir objetos a y desde formato binario
    /// </summary>
    public class BinaryConverter
    {
        private readonly BinaryFormatter _formatter;

        /// <summary>
        /// Constructor predeterminado
        /// </summary>
        public BinaryConverter()
        {
            _formatter = new BinaryFormatter();
        }

        /// <summary>
        /// Constructor con formateador personalizado
        /// </summary>
        public BinaryConverter(BinaryFormatter formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        /// <summary>
        /// Serializa un objeto a un array de bytes
        /// </summary>
        public byte[] Serialize(object value)
        {
            if (value == null)
                return null;

            using (var ms = new MemoryStream())
            {
                _formatter.Serialize(ms, value);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Serializa un objeto y lo escribe en un stream
        /// </summary>
        public void Serialize(Stream stream, object value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (value == null)
                return;

            _formatter.Serialize(stream, value);
        }

        /// <summary>
        /// Deserializa un array de bytes a un objeto
        /// </summary>
        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default(T);

            using (var ms = new MemoryStream(data))
            {
                return (T)_formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Deserializa desde un stream a un objeto
        /// </summary>
        public T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return (T)_formatter.Deserialize(stream);
        }

        /// <summary>
        /// Deserializa a un tipo especificado en tiempo de ejecución
        /// </summary>
        public object Deserialize(byte[] data, Type type)
        {
            if (data == null || data.Length == 0)
                return null;

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            using (var ms = new MemoryStream(data))
            {
                var result = _formatter.Deserialize(ms);

                if (type.IsInstanceOfType(result))
                {
                    return result;
                }

                throw new InvalidCastException($"Cannot convert deserialized object to type {type.FullName}");
            }
        }
    }
}