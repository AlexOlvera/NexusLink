using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class CompiledQueryManager
    {
        private readonly ConcurrentDictionary<string, Delegate> _compiledQueries =
            new ConcurrentDictionary<string, Delegate>();

        public Func<SqlConnection, TParam, IEnumerable<TResult>> GetOrCompileQuery<TParam, TResult>(
            string queryText)
        {
            return (Func<SqlConnection, TParam, IEnumerable<TResult>>)
                _compiledQueries.GetOrAdd(queryText, _ => CompileQuery<TParam, TResult>(queryText));
        }

        private Func<SqlConnection, TParam, IEnumerable<TResult>> CompileQuery<TParam, TResult>(
            string queryText)
        {
            // Compilar expresión para procesar resultados de manera óptima
            return (connection, param) =>
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = queryText;

                    // Agregar parámetros basados en las propiedades de TParam
                    foreach (var prop in typeof(TParam).GetProperties())
                    {
                        var value = prop.GetValue(param);
                        command.Parameters.Add(new SqlParameter($"@{prop.Name}", value ?? DBNull.Value));
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        return MapResults<TResult>(reader);
                    }
                }
            };
        }

        private IEnumerable<TResult> MapResults<TResult>(SqlDataReader reader)
        {
            var results = new List<TResult>();
            var resultType = typeof(TResult);
            var constructor = resultType.GetConstructor(Type.EmptyTypes);

            // Preparar mapeador de propiedades (solo se hace una vez)
            var propertyMappers = new List<Action<SqlDataReader, TResult>>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                var property = resultType.GetProperty(columnName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property != null && property.CanWrite)
                {
                    int ordinal = i; // Capturar para el lambda
                    propertyMappers.Add((r, obj) =>
                    {
                        if (!r.IsDBNull(ordinal))
                        {
                            object value = r.GetValue(ordinal);

                            // Convertir si es necesario
                            if (value != null && value.GetType() != property.PropertyType)
                            {
                                value = Convert.ChangeType(value, property.PropertyType);
                            }

                            property.SetValue(obj, value);
                        }
                    });
                }
            }

            // Procesar resultados
            while (reader.Read())
            {
                var result = (TResult)constructor.Invoke(null);

                foreach (var mapper in propertyMappers)
                {
                    mapper(reader, result);
                }

                results.Add(result);
            }

            return results;
        }
    }
}
