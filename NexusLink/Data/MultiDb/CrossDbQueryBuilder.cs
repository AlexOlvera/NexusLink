using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using NexusLink.Data.Parameters;

namespace NexusLink.Data.MultiDb
{
    /// <summary>
    /// Constructor de consultas que pueden ejecutarse en diferentes bases de datos
    /// </summary>
    public class CrossDbQueryBuilder
    {
        private readonly StringBuilder _query;
        private readonly ParameterCollection _parameters;
        private readonly List<string> _targetDatabases;

        public CrossDbQueryBuilder()
        {
            _query = new StringBuilder();
            _parameters = new ParameterCollection();
            _targetDatabases = new List<string>();
        }

        /// <summary>
        /// Añade una base de datos objetivo para la consulta
        /// </summary>
        public CrossDbQueryBuilder ForDatabase(string databaseName)
        {
            if (!_targetDatabases.Contains(databaseName))
            {
                _targetDatabases.Add(databaseName);
            }
            return this;
        }

        /// <summary>
        /// Añade texto SQL a la consulta
        /// </summary>
        public CrossDbQueryBuilder Append(string sql)
        {
            _query.Append(sql);
            return this;
        }

        /// <summary>
        /// Añade un parámetro a la consulta
        /// </summary>
        public CrossDbQueryBuilder AddParameter(string name, object value)
        {
            _parameters.Add(name, value);
            return this;
        }

        /// <summary>
        /// Ejecuta la consulta en todas las bases de datos objetivo y combina los resultados
        /// </summary>
        public DataSet ExecuteQuery(DbContextManager contextManager)
        {
            var combinedResults = new DataSet();

            foreach (var database in _targetDatabases)
            {
                var result = contextManager.ExecuteWith(database, () => {
                    var factory = contextManager.GetCurrentFactory();
                    using (var connection = factory.CreateConnection())
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = _query.ToString();
                            foreach (var param in _parameters)
                            {
                                var dbParam = factory.CreateParameter(param.Name, param.Value);
                                command.Parameters.Add(dbParam);
                            }

                            var adapter = factory.CreateDataAdapter();
                            adapter.SelectCommand = command;

                            var ds = new DataSet();
                            adapter.Fill(ds);

                            // Renombrar tablas para incluir base de datos
                            foreach (DataTable table in ds.Tables)
                            {
                                table.TableName = $"{database}_{table.TableName}";
                            }

                            return ds;
                        }
                    }
                });

                // Combinar con resultados existentes
                foreach (DataTable table in result.Tables)
                {
                    combinedResults.Tables.Add(table.Copy());
                }
            }

            return combinedResults;
        }
    }
}