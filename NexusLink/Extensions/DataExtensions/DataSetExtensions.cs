using System;
using System.Data;
using System.IO;
using System.Xml;

namespace NexusLink.Extensions.DataExtensions
{
    public static class DataSetExtensions
    {
        /// <summary>
        /// Convierte un DataSet a formato JSON
        /// </summary>
        public static string ToJson(this DataSet dataSet, bool formatOutput = true)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet));

            var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            using (var sw = new StringWriter())
            {
                using (var writer = new Newtonsoft.Json.JsonTextWriter(sw))
                {
                    if (formatOutput)
                    {
                        writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    }

                    writer.QuoteChar = '"';

                    jsonSerializer.Serialize(writer, dataSet);
                    return sw.ToString();
                }
            }
        }

        /// <summary>
        /// Convierte un DataSet a formato XML
        /// </summary>
        public static string ToXml(this DataSet dataSet)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet));

            using (var ms = new MemoryStream())
            {
                dataSet.WriteXml(ms);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Crea una copia del DataSet
        /// </summary>
        public static DataSet Clone(this DataSet source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Copy();
        }

        /// <summary>
        /// Crea una copia profunda del DataSet incluyendo los datos
        /// </summary>
        public static DataSet Copy(this DataSet source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var copy = new DataSet();
            copy.Merge(source);
            return copy;
        }

        /// <summary>
        /// Verifica si el DataSet está vacío (no tiene tablas o todas las tablas están vacías)
        /// </summary>
        public static bool IsEmpty(this DataSet dataSet)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet));

            if (dataSet.Tables.Count == 0)
                return true;

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.Rows.Count > 0)
                    return false;
            }

            return true;
        }
    }
}
