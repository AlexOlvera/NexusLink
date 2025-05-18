using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Extensions.AsyncExtensions
{
    public static class AsyncQueryExtensions
    {
        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona y devuelve un DataTable
        /// </summary>
        public static async Task<DataTable> ExecuteTableAsync(this SqlCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var table = new DataTable();

            using (var adapter = new SqlDataAdapter(command))
            {
                var task = Task.Run(() => adapter.Fill(table), cancellationToken);
                await task;
            }

            return table;
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona y devuelve un DataSet
        /// </summary>
        public static async Task<DataSet> ExecuteDataSetAsync(this SqlCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var dataset = new DataSet();

            using (var adapter = new SqlDataAdapter(command))
            {
                var task = Task.Run(() => adapter.Fill(dataset), cancellationToken);
                await task;
            }

            return dataset;
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona y devuelve una colección de entidades
        /// </summary>
        public static async Task<List<T>> QueryAsync<T>(this SqlCommand command,
            CancellationToken cancellationToken = default) where T : new()
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var result = new List<T>();

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = new T();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var prop = typeof(T).GetProperty(columnName);

                        if (prop != null && prop.CanWrite)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);

                                if (prop.PropertyType != value.GetType() && value != null)
                                {
                                    value = Convert.ChangeType(value, prop.PropertyType);
                                }

                                prop.SetValue(entity, value);
                            }
                        }
                    }

                    result.Add(entity);
                }
            }

            return result;
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona y devuelve la primera entidad
        /// </summary>
        public static async Task<T> QueryFirstOrDefaultAsync<T>(this SqlCommand command,
            CancellationToken cancellationToken = default) where T : new()
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    var entity = new T();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var prop = typeof(T).GetProperty(columnName);

                        if (prop != null && prop.CanWrite)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                var value = reader.GetValue(i);

                                if (prop.PropertyType != value.GetType() && value != null)
                                {
                                    value = Convert.ChangeType(value, prop.PropertyType);
                                }

                                prop.SetValue(entity, value);
                            }
                        }
                    }

                    return entity;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona y procesa los resultados mediante un delegado
        /// </summary>
        public static async Task<List<T>> QueryAsync<T>(this SqlCommand command,
            Func<SqlDataReader, T> map, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var result = new List<T>();

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(map(reader));
                }
            }

            return result;
        }
    }
}
