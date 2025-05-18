using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Extensions.SqlExtensions
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Establece el tiempo de espera para un comando SQL
        /// </summary>
        public static SqlCommand WithTimeout(this SqlCommand command, int timeoutSeconds)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.CommandTimeout = timeoutSeconds;
            return command;
        }

        /// <summary>
        /// Establece el tipo de comando
        /// </summary>
        public static SqlCommand AsStoredProcedure(this SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.CommandType = CommandType.StoredProcedure;
            return command;
        }

        /// <summary>
        /// Agrega un parámetro al comando
        /// </summary>
        public static SqlCommand WithParameter(this SqlCommand command, string name, object value)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return command;
        }

        /// <summary>
        /// Agrega múltiples parámetros al comando
        /// </summary>
        public static SqlCommand WithParameters(this SqlCommand command, IDictionary<string, object> parameters)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            return command;
        }

        /// <summary>
        /// Ejecuta un comando y devuelve un valor escalar de forma asíncrona
        /// </summary>
        public static async Task<T> ExecuteScalarAsync<T>(this SqlCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta un comando y devuelve un valor escalar
        /// </summary>
        public static T ExecuteScalar<T>(this SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta un comando y rellena un DataSet
        /// </summary>
        public static DataSet ExecuteDataSet(this SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var dataset = new DataSet();
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dataset);
            }

            return dataset;
        }

        /// <summary>
        /// Ejecuta un comando y rellena un DataTable
        /// </summary>
        public static DataTable ExecuteDataTable(this SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var table = new DataTable();
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(table);
            }

            return table;
        }

        /// <summary>
        /// Ejecuta un comando y devuelve un objeto tipado 
        /// que es el primer registro del resultado
        /// </summary>
        public static T ExecuteEntity<T>(this SqlCommand command) where T : new()
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.ToEntity<T>();
                }

                return default(T);
            }
        }

        /// <summary>
        /// Ejecuta un comando y devuelve una lista de objetos tipados
        /// a partir de los resultados
        /// </summary>
        public static List<T> ExecuteEntities<T>(this SqlCommand command) where T : new()
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var result = new List<T>();

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(reader.ToEntity<T>());
                }
            }

            return result;
        }
    }
}