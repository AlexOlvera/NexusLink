using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NexusLink.Serialization
{
    /// <summary>
    /// Clase para convertir objetos a y desde formato JSON
    /// </summary>
    public class JsonConverter
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Constructor predeterminado con configuración estándar
        /// </summary>
        public JsonConverter()
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        /// <summary>
        /// Constructor con configuración personalizada
        /// </summary>
        public JsonConverter(JsonSerializerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Serializa un objeto a JSON
        /// </summary>
        public string Serialize(object value)
        {
            if (value == null)
                return null;

            return JsonConvert.SerializeObject(value, _settings);
        }

        /// <summary>
        /// Serializa un objeto a JSON y lo escribe en un stream
        /// </summary>
        public void Serialize(Stream stream, object value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (value == null)
                return;

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var serializer = JsonSerializer.Create(_settings);
                    serializer.Serialize(jsonWriter, value);
                    jsonWriter.Flush();
                }
            }
        }

        /// <summary>
        /// Deserializa JSON a un objeto
        /// </summary>
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default(T);

            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        /// <summary>
        /// Deserializa JSON desde un stream a un objeto
        /// </summary>
        public T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = JsonSerializer.Create(_settings);
                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }

        /// <summary>
        /// Deserializa JSON a un tipo especificado en tiempo de ejecución
        /// </summary>
        public object Deserialize(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        /// <summary>
        /// Combina dos objetos JSON
        /// </summary>
        public string Merge(string json1, string json2)
        {
            if (string.IsNullOrEmpty(json1))
                return json2;

            if (string.IsNullOrEmpty(json2))
                return json1;

            var obj1 = JObject.Parse(json1);
            var obj2 = JObject.Parse(json2);

            obj1.Merge(obj2, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });

            return obj1.ToString();
        }
    }
}