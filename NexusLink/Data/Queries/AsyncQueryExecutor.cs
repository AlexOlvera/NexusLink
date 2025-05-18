using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Data.Parameters;
using NexusLink.Logging;

namespace NexusLink.Data.Queries
{
    /// <summary>
    /// Ejecutor de consultas asíncronas
    /// </summary>
    public class AsyncQueryExecutor
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly QueryCache _queryCache;

        public AsyncQueryExecutor(
            ILogger logger,
            ConnectionFactory connectionFactory,
            QueryCache queryCache)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _queryCache = queryCache;
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve un objeto del tipo especificado
        /// </summary>
        public async Task<T> ExecuteQuerySingleAsync<T>(string sql, params object[] parameters) where T : new()
        {
            var queryResult = await ExecuteQueryAsync<T>(sql, parameters);
            return queryResult.Count > 0 ? queryResult[0] : default(T);
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve una lista de objetos del tipo especificado
        /// </summary>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : new()
        {
            // Crear los parámetros SQL
            DbParameter[] sqlParameters = CreateParameters(parameters);

            // Verificar si el resultado está en caché
            string cacheKey = _queryCache.GenerateKey(sql, sqlParameters);
            var cachedResult = _queryCache.GetFromCache<List<T>>(cacheKey);

            if (cachedResult != null)
            {
                _logger.Debug($"Resultado obtenido de caché para consulta: {sql}");
                return cachedResult;
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    // Agregar parámetros
                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }

                    _logger.Debug($"Ejecutando consulta asíncrona: {sql}");

                    List<T> result = new List<T>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Obtener propiedades del tipo
                        PropertyInfo[] properties = typeof(T).GetProperties();

                        // Mapear columnas a propiedades por nombre
                        Dictionary<int, PropertyInfo> columnMap = new Dictionary<int, PropertyInfo>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            // Buscar propiedad por nombre (insensible a mayúsculas)
                            PropertyInfo property = Array.Find(properties, p =>
                                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                            if (property != null && property.CanWrite)
                            {
                                columnMap[i] = property;
                            }
                        }

                        // Leer filas
                        while (await reader.ReadAsync())
                        {
                            T item = new T();

                            foreach (var kvp in columnMap)
                            {
                                int columnIndex = kvp.Key;
                                PropertyInfo property = kvp.Value;

                                if (!reader.IsDBNull(columnIndex))
                                {
                                    object value = reader.GetValue(columnIndex);

                                    try
                                    {
                                        // Convertir valor si es necesario
                                        if (value != null && property.PropertyType != value.GetType())
                                        {
                                            value = Convert.ChangeType(value, property.PropertyType);
                                        }

                                        property.SetValue(item, value);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Warning($"Error al asignar valor a la propiedad {property.Name}: {ex.Message}");
                                    }
                                }
                            }

                            result.Add(item);
                        }
                    }

                    // Guardar en caché
                    _queryCache.StoreInCache(cacheKey, result);

                    return result;
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve un DataSet
        /// </summary>
        public async Task<DataSet> ExecuteDataSetAsync(string sql, params object[] parameters)
        {
            // Crear los parámetros SQL
            DbParameter[] sqlParameters = CreateParameters(parameters);

            // Verificar si el resultado está en caché
            string cacheKey = _queryCache.GenerateKey(sql, sqlParameters);
            var cachedResult = _queryCache.GetFromCache<DataSet>(cacheKey);

            if (cachedResult != null)
            {
                _logger.Debug($"Resultado DataSet obtenido de caché para consulta: {sql}");
                return cachedResult;
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    // Agregar parámetros
                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }

                    _logger.Debug($"Ejecutando consulta DataSet asíncrona: {sql}");

                    DataSet dataSet = new DataSet();

                    // DbDataAdapter no tiene métodos async, así que usamos Task.Run para ejecutarlo en otro hilo
                    await Task.Run(() =>
                    {
                        var dbFactory = DbProviderFactories.GetFactory(connection);
                        var adapter = dbFactory.CreateDataAdapter();
                        adapter.SelectCommand = command;
                        adapter.Fill(dataSet);
                    });

                    // Guardar en caché
                    _queryCache.StoreInCache(cacheKey, dataSet);

                    return dataSet;
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve un DataTable
        /// </summary>
        public async Task<DataTable> ExecuteDataTableAsync(string sql, params object[] parameters)
        {
            var dataSet = await ExecuteDataSetAsync(sql, parameters);

            if (dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }

            return new DataTable();
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve un valor escalar
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string sql, params object[] parameters)
        {
            // Crear los parámetros SQL
            DbParameter[] sqlParameters = CreateParameters(parameters);

            // Verificar si el resultado está en caché
            string cacheKey = _queryCache.GenerateKey(sql, sqlParameters);
            var cachedResult = _queryCache.GetFromCache<object>(cacheKey);

            if (cachedResult != null)
            {
                _logger.Debug($"Resultado escalar obtenido de caché para consulta: {sql}");
                return cachedResult;
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    // Agregar parámetros
                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }

                    _logger.Debug($"Ejecutando consulta escalar asíncrona: {sql}");

                    object result = await command.ExecuteScalarAsync();

                    // Guardar en caché
                    if (result != null && result != DBNull.Value)
                    {
                        _queryCache.StoreInCache(cacheKey, result);
                    }

                    return result == DBNull.Value ? null : result;
                }
            }
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que devuelve un valor escalar convertido a un tipo específico
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string sql, params object[] parameters)
        {
            object result = await ExecuteScalarAsync(sql, parameters);

            if (result == null)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Ejecuta una consulta SQL de forma asíncrona que no retorna resultados
        /// </summary>
        public async Task ExecuteNonQueryAsync(string sql, params object[] parameters)
        {
            // Crear los parámetros SQL
            DbParameter[] sqlParameters = CreateParameters(parameters);

            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;

                    // Agregar parámetros
                    if (sqlParameters != null)
                    {
                        foreach (var parameter in sqlParameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }

                    _logger.Debug($"Ejecutando consulta non-query asíncrona: {sql}");

                    await command.ExecuteNonQueryAsync();
                }
            }

            // Invalidar caché
            _queryCache.InvalidateCache(sql);
        }

        /// <summary>
        /// Ejecuta una consulta paginada de forma asíncrona
        /// </summary>
        public async Task<PagedResult<T>> ExecutePagedQueryAsync<T>(
            string sql,
            int pageNumber,
            int pageSize,
            string sortColumn,
            string sortDirection,
            params object[] parameters) where T : new()
        {
            // Validar parámetros
            if (pageNumber < 1)
            {
                throw new ArgumentException("El número de página debe ser mayor que cero", nameof(pageNumber));
            }

            if (pageSize < 1)
            {
                throw new ArgumentException("El tamaño de página debe ser mayor que cero", nameof(pageSize));
            }

            // Modificar la consulta para incluir paginación
            string pagedSql = GeneratePagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);

            // Ejecutar consulta paginada
            var results = await ExecuteQueryAsync<T>(pagedSql, parameters);

            // Ejecutar consulta de conteo
            string countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
            int totalCount = await ExecuteScalarAsync<int>(countSql, parameters);

            // Calcular páginas totales
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Crear resultado paginado
            var pagedResult = new PagedResult<T>
            {
                Items = results,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };

            return pagedResult;
        }

        /// <summary>
        /// Cancela una consulta asíncrona en progreso
        /// </summary>
        public bool CancelQuery(CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Consulta cancelada por solicitud del usuario");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Crea parámetros SQL a partir de valores de parámetros
        /// </summary>
        private DbParameter[] CreateParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return null;
            }

            List<DbParameter> sqlParameters = new List<DbParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                // Si el parámetro ya es un DbParameter, usarlo directamente
                if (parameters[i] is DbParameter)
                {
                    sqlParameters.Add((DbParameter)parameters[i]);
                }
                else
                {
                    // Crear un nuevo parámetro
                    var parameter = _connectionFactory.CreateParameter();
                    parameter.ParameterName = $"@p{i}";
                    parameter.Value = parameters[i] ?? DBNull.Value;
                    sqlParameters.Add(parameter);
                }
            }

            return sqlParameters.ToArray();
        }

        /// <summary>
        /// Genera una consulta SQL con paginación
        /// </summary>
        private string GeneratePagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            // Aplicar paginación según el proveedor de base de datos
            string providerName = _connectionFactory.GetProviderName();

            if (providerName.Contains("SqlClient"))
            {
                // SQL Server
                return GenerateSqlServerPagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);
            }
            else if (providerName.Contains("MySql"))
            {
                // MySQL
                return GenerateMySqlPagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);
            }
            else if (providerName.Contains("Oracle"))
            {
                // Oracle
                return GenerateOraclePagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);
            }
            else if (providerName.Contains("Npgsql"))
            {
                // PostgreSQL
                return GeneratePostgreSqlPagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);
            }
            else
            {
                // Paginación genérica
                return GenerateGenericPagedQuery(sql, pageNumber, pageSize, sortColumn, sortDirection);
            }
        }

        /// <summary>
        /// Genera una consulta paginada para SQL Server
        /// </summary>
        private string GenerateSqlServerPagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            int offset = (pageNumber - 1) * pageSize;

            // Asegurarse de que la consulta original tenga una cláusula ORDER BY
            if (string.IsNullOrEmpty(sortColumn))
            {
                // Si no se especifica una columna de ordenamiento, agregar un ORDER BY genérico
                return $"{sql} ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }
            else
            {
                // Si la consulta ya tiene ORDER BY, se respeta, de lo contrario se agrega
                if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                }
                else
                {
                    string direction = !string.IsNullOrEmpty(sortDirection) && sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                        ? "DESC"
                        : "ASC";

                    return $"{sql} ORDER BY {sortColumn} {direction} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                }
            }
        }

        /// <summary>
        /// Genera una consulta paginada para MySQL
        /// </summary>
        private string GenerateMySqlPagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            int offset = (pageNumber - 1) * pageSize;

            // Asegurarse de que la consulta original tenga una cláusula ORDER BY
            if (string.IsNullOrEmpty(sortColumn))
            {
                // Si no se especifica una columna de ordenamiento, no se agrega ORDER BY
                return $"{sql} LIMIT {offset}, {pageSize}";
            }
            else
            {
                // Si la consulta ya tiene ORDER BY, se respeta, de lo contrario se agrega
                if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{sql} LIMIT {offset}, {pageSize}";
                }
                else
                {
                    string direction = !string.IsNullOrEmpty(sortDirection) && sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                        ? "DESC"
                        : "ASC";

                    return $"{sql} ORDER BY {sortColumn} {direction} LIMIT {offset}, {pageSize}";
                }
            }
        }

        /// <summary>
        /// Genera una consulta paginada para Oracle
        /// </summary>
        private string GenerateOraclePagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            int offset = (pageNumber - 1) * pageSize;
            int limit = pageSize;
            string innerSql;

            // Asegurarse de que la consulta original tenga una cláusula ORDER BY
            if (string.IsNullOrEmpty(sortColumn))
            {
                // Si no se especifica una columna de ordenamiento, usar ROWNUM
                innerSql = sql;
            }
            else
            {
                // Si la consulta ya tiene ORDER BY, se respeta, de lo contrario se agrega
                if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                {
                    innerSql = sql;
                }
                else
                {
                    string direction = !string.IsNullOrEmpty(sortDirection) && sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                        ? "DESC"
                        : "ASC";

                    innerSql = $"{sql} ORDER BY {sortColumn} {direction}";
                }
            }

            // Consulta paginada en Oracle usando ROWNUM
            return $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        {innerSql}
                    ) a WHERE ROWNUM <= {offset + limit}
                ) WHERE rnum > {offset}";
        }

        /// <summary>
        /// Genera una consulta paginada para PostgreSQL
        /// </summary>
        private string GeneratePostgreSqlPagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            int offset = (pageNumber - 1) * pageSize;

            // Asegurarse de que la consulta original tenga una cláusula ORDER BY
            if (string.IsNullOrEmpty(sortColumn))
            {
                // Si no se especifica una columna de ordenamiento, no se agrega ORDER BY
                return $"{sql} LIMIT {pageSize} OFFSET {offset}";
            }
            else
            {
                // Si la consulta ya tiene ORDER BY, se respeta, de lo contrario se agrega
                if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{sql} LIMIT {pageSize} OFFSET {offset}";
                }
                else
                {
                    string direction = !string.IsNullOrEmpty(sortDirection) && sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                        ? "DESC"
                        : "ASC";

                    return $"{sql} ORDER BY {sortColumn} {direction} LIMIT {pageSize} OFFSET {offset}";
                }
            }
        }

        /// <summary>
        /// Genera una consulta paginada genérica (funciona para la mayoría de bases de datos)
        /// </summary>
        private string GenerateGenericPagedQuery(string sql, int pageNumber, int pageSize, string sortColumn, string sortDirection)
        {
            int offset = (pageNumber - 1) * pageSize;

            // Consulta con subconsulta y ROW_NUMBER()
            string orderByClause;

            if (string.IsNullOrEmpty(sortColumn))
            {
                // Si no se especifica una columna de ordenamiento, usar un campo constante
                orderByClause = "(SELECT 1)";
            }
            else
            {
                string direction = !string.IsNullOrEmpty(sortDirection) && sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                    ? "DESC"
                    : "ASC";

                orderByClause = $"{sortColumn} {direction}";
            }

            // Esta consulta debería funcionar en la mayoría de bases de datos modernas
            return $@"
                SELECT * FROM (
                    SELECT ROW_NUMBER() OVER (ORDER BY {orderByClause}) AS RowNum, * FROM (
                        {sql}
                    ) AS InnerQuery
                ) AS OuterQuery
                WHERE RowNum > {offset} AND RowNum <= {offset + pageSize}";
        }
    }

    /// <summary>
    /// Resultado paginado de una consulta
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// Lista de elementos de la página actual
        /// </summary>
        public List<T> Items { get; set; }

        /// <summary>
        /// Número de página actual
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Número total de elementos
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Número total de páginas
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indica si hay una página siguiente
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Indica si hay una página anterior
        /// </summary>
        public bool HasPreviousPage { get; set; }
    }
}