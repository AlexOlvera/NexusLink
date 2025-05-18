using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NexusLink.Dynamic.Expando
{
    /// <summary>
    /// Contenedor de propiedades dinámicas serializable a XML
    /// </summary>
    [Serializable]
    public class DynamicPropertyBag : Dictionary<string, object>, IXmlSerializable
    {
        /// <summary>
        /// Crea un contenedor de propiedades vacío
        /// </summary>
        public DynamicPropertyBag()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Crea un contenedor de propiedades a partir de un diccionario
        /// </summary>
        public DynamicPropertyBag(IDictionary<string, object> dictionary)
            : base(dictionary, StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Obtiene un esquema XML (no implementado)
        /// </summary>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Serializa a XML
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            foreach (string key in Keys)
            {
                object value = this[key];

                writer.WriteStartElement("Property");
                writer.WriteAttributeString("Name", key);

                if (value == null)
                {
                    writer.WriteAttributeString("Type", "null");
                }
                else
                {
                    Type valueType = value.GetType();
                    writer.WriteAttributeString("Type", valueType.FullName);

                    // Serializar tipos simples directamente
                    if (valueType.IsPrimitive || valueType == typeof(string) ||
                        valueType == typeof(DateTime) || valueType.IsEnum)
                    {
                        writer.WriteString(Convert.ToString(value));
                    }
                    else
                    {
                        // Usar XmlSerializer para tipos complejos
                        XmlSerializer serializer = new XmlSerializer(valueType);
                        serializer.Serialize(writer, value);
                    }
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Deserializa desde XML
        /// </summary>
        public void ReadXml(XmlReader reader)
        {
            Clear();

            // Avanzar al primer elemento
            if (reader.IsEmptyElement)
            {
                return;
            }

            reader.Read();

            while (reader.NodeType == XmlNodeType.Element && reader.Name == "Property")
            {
                string name = reader.GetAttribute("Name");
                string typeStr = reader.GetAttribute("Type");

                if (typeStr == "null")
                {
                    this[name] = null;
                    reader.Skip();
                }
                else
                {
                    // Obtener el tipo
                    Type type = Type.GetType(typeStr);
                    if (type == null)
                    {
                        // Intentar buscar el tipo en todos los ensamblados cargados
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = assembly.GetType(typeStr);
                            if (type != null)
                                break;
                        }

                        if (type == null)
                        {
                            // No se pudo encontrar el tipo, omitir la propiedad
                            reader.Skip();
                            continue;
                        }
                    }

                    // Leer el valor
                    object value;

                    // Tipos simples
                    if (type.IsPrimitive || type == typeof(string) ||
                        type == typeof(DateTime) || type.IsEnum)
                    {
                        reader.Read();
                        string textValue = reader.Value;

                        if (type == typeof(string))
                        {
                            value = textValue;
                        }
                        else if (type == typeof(int))
                        {
                            value = int.Parse(textValue);
                        }
                        else if (type == typeof(long))
                        {
                            value = long.Parse(textValue);
                        }
                        else if (type == typeof(float))
                        {
                            value = float.Parse(textValue);
                        }
                        else if (type == typeof(double))
                        {
                            value = double.Parse(textValue);
                        }
                        else if (type == typeof(bool))
                        {
                            value = bool.Parse(textValue);
                        }
                        else if (type == typeof(DateTime))
                        {
                            value = DateTime.Parse(textValue);
                        }
                        else if (type.IsEnum)
                        {
                            value = Enum.Parse(type, textValue);
                        }
                        else
                        {
                            value = Convert.ChangeType(textValue, type);
                        }

                        reader.Read(); // Leer el final del elemento
                    }
                    else
                    {
                        // Tipos complejos
                        XmlSerializer serializer = new XmlSerializer(type);
                        reader.ReadStartElement(); // Leer el inicio del contenido
                        value = serializer.Deserialize(reader);
                    }

                    this[name] = value;

                    // Si aún no estamos al final del elemento, avanzar
                    if (reader.NodeType != XmlNodeType.EndElement)
                    {
                        reader.ReadEndElement();
                    }
                }

                // Avanzar al siguiente elemento
                reader.MoveToContent();
            }

            // Leer el final del elemento contenedor
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                reader.ReadEndElement();
            }
        }
    }
}