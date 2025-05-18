using NexusLink.Core.Connection;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Repository
{
    public class AsyncRepository<T> : IAsyncRepository<T> where T : class
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly IEntityMapper<T> _entityMapper;

        public AsyncRepository(
            ConnectionFactory connectionFactory,
            IEntityMapper<T> entityMapper)
        {
            _connectionFactory = connectionFactory;
            _entityMapper = entityMapper;
        }

        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                // Genera consulta basada en mapeo
                var tableName = _entityMapper.GetTableName();
                var pkName = _entityMapper.GetPrimaryKeyName();

                var query = $"SELECT * FROM {tableName} WHERE {pkName} = @Id";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.Add(new SqlParameter("@Id", id));

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return _entityMapper.MapFromDataReader(reader);
                        }

                        return null;
                    }
                }
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<T>();

            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                var tableName = _entityMapper.GetTableName();
                var query = $"SELECT * FROM {tableName}";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            results.Add(_entityMapper.MapFromDataReader(reader));
                        }
                    }
                }
            }

            return results;
        }

        public async Task<int> InsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                // Genera INSERT basado en mapeo
                var (query, parameters) = _entityMapper.GenerateInsertStatement(entity);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    }

                    // Si la tabla tiene una PK con autoincremento, retorna el nuevo ID
                    if (_entityMapper.HasAutoIncrementPrimaryKey())
                    {
                        command.CommandText += "; SELECT SCOPE_IDENTITY();";
                        var result = await command.ExecuteScalarAsync(cancellationToken);
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken);
                        return 0;
                    }
                }
            }
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                // Genera UPDATE basado en mapeo
                var (query, parameters) = _entityMapper.GenerateUpdateStatement(entity);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    }

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateConnectionAsync())
            {
                // Genera DELETE basado en mapeo
                var (query, parameters) = _entityMapper.GenerateDeleteStatement(entity);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    }

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }
    }
}