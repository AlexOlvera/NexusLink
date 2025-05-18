using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Core.Connection;

namespace NexusLink.Data.Queries
{


    /// <summary>
    /// Extensiones para SqlDataReader
    /// </summary>
    internal static class SqlDataReaderExtensions
    {
        /// <summary>
        /// Convierte un SqlDataReader a un objeto dinámico
        /// </summary>
        /// <param name="reader">SqlDataReader</param>
        /// <returns>Objeto dinámico</returns>
        public static dynamic ToExpando(this SqlDataReader reader)
        {
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                expandoObject.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
            }

            return expandoObject;
        }

        /// <summary>
        /// Convierte un SqlDataReader a una lista de objetos dinámicos
        /// </summary>
        /// <param name="reader">SqlDataReader</param>
        /// <returns>Lista de objetos dinámicos</returns>
        public static List<dynamic> ToDynamicList(this SqlDataReader reader)
        {
            var result = new List<dynamic>();

            while (reader.Read())
            {
                result.Add(reader.ToExpando());
            }

            return result;
        }

        /// <summary>
        /// Convierte un SqlDataReader a una lista de objetos dinámicos de forma asíncrona
        /// </summary>
        /// <param name="reader">SqlDataReader</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de objetos dinámicos</returns>
        public static async Task<List<dynamic>> ToDynamicListAsync(this SqlDataReader reader, CancellationToken cancellationToken = default)
        {
            var result = new List<dynamic>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                result.Add(reader.ToExpando());
            }

            return result;
        }
    }
}