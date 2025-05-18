using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace NexusLink.Serialization
{
    /// <summary>
    /// Proporciona métodos para la serialización y deserialización de objetos en diferentes formatos.
    /// </summary>
    public static class SerializationManager
    {
        #region JSON Serialization

        /// <summary>
        /// Serializa un objeto a formato JSON.
        /// </summary>
        public static string ToJson<T>(T obj, bool indented = true)
        {
            if (obj == null) return null;

            try
            {
                return JsonConvert.SerializeObject(obj,
                    indented ? Formatting.Indented : Formatting.None,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    });
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error serializing object to JSON", ex);
            }
        }

        /// <summary>
        /// Deserializa una cadena JSON a un objeto del tipo especificado.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error deserializing JSON to object", ex);
            }
        }

        #endregion

        #region XML Serialization

        /// <summary>
        /// Serializa un objeto a formato XML.
        /// </summary>
        public static string ToXml<T>(T obj)
        {
            if (obj == null) return null;

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                    {
                        serializer.Serialize(xmlWriter, obj);
                        return stringWriter.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error serializing object to XML", ex);
            }
        }

        /// <summary>
        /// Deserializa una cadena XML a un objeto del tipo especificado.
        /// </summary>
        public static T FromXml<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return default;

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var stringReader = new StringReader(xml))
                {
                    return (T)serializer.Deserialize(stringReader);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error deserializing XML to object", ex);
            }
        }

        #endregion

        #region Binary Serialization

        /// <summary>
        /// Serializa un objeto a formato binario.
        /// </summary>
        public static byte[] ToBinary<T>(T obj) where T : Serializable
        {
            if (obj == null) return null;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(memoryStream, obj);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error serializing object to binary", ex);
            }
        }

        /// <summary>
        /// Deserializa datos binarios a un objeto del tipo especificado.
        /// </summary>
        public static T FromBinary<T>(byte[] data) where T : Serializable
        {
            if (data == null || data.Length == 0) return default;

            try
            {
                using (var memoryStream = new MemoryStream(data))
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    return (T)formatter.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error deserializing binary to object", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Excepción personalizada para errores de serialización.
    /// </summary>
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message) { }
        public SerializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}