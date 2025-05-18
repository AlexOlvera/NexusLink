using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NexusLink.Core.Connection;
using NexusLink.Logging;

namespace NexusLink.Data.Commands
{
    /// <summary>
    /// Proporciona operaciones en lote (bulk) para SQL Server
    /// </summary>
    public class BulkCommand
    {
        private readonly ILogger _logger;
        private readonly ConnectionFactory _connectionFactory;
        private string _tableName;
        private string _schema;
        private int _batchSize;
        private int _timeout;
        private bool _keepIdentity;
        private bool _checkConstraints;
        private bool _fireTableTriggers;
        private Dictionary<string, string> _columnMappings;

        public BulkCommand(ILogger logger, ConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _schema = "dbo";
            _batchSize = 1000;
            _timeout = 30;
            _keepIdentity = false;
            _checkConstraints = true;
            _fireTableTriggers = true;
            _columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Establece la tabla de destino
        /// </summary>
        public BulkCommand ToTable(string tableName)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            return this;
        }

        /// <summary>
        /// Establece el esquema de la tabla de destino
        /// </summary>
        public BulkCommand WithSchema(string schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            return this;
        }

        /// <summary>
        /// Establece el tamaño del lote
        /// </summary>
        public BulkCommand WithBatchSize(int batchSize)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentException("El tamaño del lote debe ser mayor que cero", nameof(batchSize));
            }

            _batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Establece el timeout en segundos
        /// </summary>
        public BulkCommand WithTimeout(int seconds)
        {
            if (seconds <= 0)
            {
                throw new ArgumentException("El timeout debe ser mayor que cero", nameof(seconds));
            }

            _timeout = seconds;
            return this;
        }

        /// <summary>
        /// Establece si se deben conservar los valores de identidad
        /// </summary>
        public BulkCommand KeepIdentity(bool keepIdentity)
        {
            _keepIdentity = keepIdentity;
            return this;
        }

        /// <summary>
        /// Establece si se deben verificar las restricciones
        /// </summary>
        public BulkCommand CheckConstraints(bool checkConstraints)
        {
            _checkConstraints = checkConstraints;
            return this;
        }

        /// <summary>
        /// Establece si se deben disparar los triggers de la tabla
        /// </summary>
        public BulkCommand FireTriggers(bool fireTriggers)
        {
            _fireTableTriggers = fireTriggers;
            return this;
        }

        /// <summary>
        /// Agrega un mapeo de columna
        /// </summary>
        public BulkCommand WithColumnMapping(string sourceColumn, string destinationColumn)
        {
            if (string.IsNullOrEmpty(sourceColumn))
            {
                throw new ArgumentException("La columna de origen no puede estar vacía", nameof(sourceColumn));
            }

            if (string.IsNullOrEmpty(destinationColumn))
            {
                throw new ArgumentException("La columna de destino no puede estar vacía", nameof(destinationColumn));
            }

            _columnMappings[sourceColumn] = destinationColumn;
            return this;
        }

        /// <summary>
        /// Ejecuta una operación bulk insert
        /// </summary>
        public void BulkInsert(DataTable dataTable)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("La tabla de destino no ha sido especificada");
            }

            _logger.Info($"Iniciando BulkInsert en {_schema}.{_tableName}. Filas: {dataTable.Rows.Count}");

            using (var connection = _connectionFactory.CreateConnection() as SqlConnection)
            {
                if (connection == null)
                {
                    throw new InvalidOperationException("La operación bulk solo está soportada para SQL Server");
                }

                connection.Open();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    ConfigureBulkCopy(bulkCopy, dataTable);

                    try
                    {
                        bulkCopy.WriteToServer(dataTable);
                        _logger.Info($"BulkInsert completado. Filas insertadas: {dataTable.Rows.Count}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error en BulkInsert: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ejecuta una operación bulk insert de forma asíncrona
        /// </summary>
        public async Task BulkInsertAsync(DataTable dataTable)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("La tabla de destino no ha sido especificada");
            }

            _logger.Info($"Iniciando BulkInsert asíncrono en {_schema}.{_tableName}. Filas: {dataTable.Rows.Count}");

            using (var connection = _connectionFactory.CreateConnection() as SqlConnection)
            {
                if (connection == null)
                {
                    throw new InvalidOperationException("La operación bulk solo está soportada para SQL Server");
                }

                await connection.OpenAsync();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    ConfigureBulkCopy(bulkCopy, dataTable);

                    try
                    {
                        await bulkCopy.WriteToServerAsync(dataTable);
                        _logger.Info($"BulkInsert asíncrono completado. Filas insertadas: {dataTable.Rows.Count}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error en BulkInsert asíncrono: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Configura el objeto SqlBulkCopy con las opciones especificadas
        /// </summary>
        private void ConfigureBulkCopy(SqlBulkCopy bulkCopy, DataTable dataTable)
        {
            bulkCopy.DestinationTableName = $"[{_schema}].[{_tableName}]";
            bulkCopy.BatchSize = _batchSize;
            bulkCopy.BulkCopyTimeout = _timeout;
            bulkCopy.EnableStreaming = true;

            // Configurar opciones
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default;

            if (_keepIdentity)
            {
                options |= SqlBulkCopyOptions.KeepIdentity;
            }

            if (_checkConstraints)
            {
                options |= SqlBulkCopyOptions.CheckConstraints;
            }

            if (_fireTableTriggers)
            {
                options |= SqlBulkCopyOptions.FireTriggers;
            }

            // Aplicar mapeos de columnas
            if (_columnMappings.Count > 0)
            {
                foreach (var mapping in _columnMappings)
                {
                    // Verificar que la columna exista en la tabla de origen
                    if (dataTable.Columns.Contains(mapping.Key))
                    {
                        bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                    }
                    else
                    {
                        _logger.Warning($"Columna de origen no encontrada: {mapping.Key}");
                    }
                }
            }
            else
            {
                // Si no hay mapeos explícitos, crear mapeos automáticos por nombre
                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
            }
        }

        /// <summary>
        /// Ejecuta una operación bulk merge (upsert)
        /// </summary>
        public void BulkMerge(DataTable dataTable, string[] keyColumns)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (keyColumns == null || keyColumns.Length == 0)
            {
                throw new ArgumentException("Debe especificar al menos una columna clave", nameof(keyColumns));
            }

            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("La tabla de destino no ha sido especificada");
            }

            _logger.Info($"Iniciando BulkMerge en {_schema}.{_tableName}. Filas: {dataTable.Rows.Count}");

            // Crear tabla temporal
            string tempTableName = $"#Temp_{_tableName}_{Guid.NewGuid().ToString().Replace("-", "")}";

            using (var connection = _connectionFactory.CreateConnection() as SqlConnection)
            {
                if (connection == null)
                {
                    throw new InvalidOperationException("La operación bulk solo está soportada para SQL Server");
                }

                connection.Open();

                // Crear tabla temporal con la misma estructura
                CreateTempTable(connection, tempTableName, dataTable);

                // Insertar datos en la tabla temporal
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BatchSize = _batchSize;
                    bulkCopy.BulkCopyTimeout = _timeout;

                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.WriteToServer(dataTable);
                }

                // Ejecutar MERGE
                ExecuteMerge(connection, tempTableName, keyColumns);

                // Eliminar tabla temporal
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"DROP TABLE {tempTableName}";
                    command.ExecuteNonQuery();
                }

                _logger.Info($"BulkMerge completado. Filas procesadas: {dataTable.Rows.Count}");
            }
        }

        /// <summary>
        /// Crea una tabla temporal con la misma estructura que la tabla de origen
        /// </summary>
        private void CreateTempTable(SqlConnection connection, string tempTableName, DataTable dataTable)
        {
            using (var command = connection.CreateCommand())
            {
                var sql = new System.Text.StringBuilder();
                sql.AppendLine($"CREATE TABLE {tempTableName} (");

                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var column = dataTable.Columns[i];
                    string sqlType = GetSqlType(column.DataType);

                    sql.Append($"[{column.ColumnName}] {sqlType}");

                    if (i < dataTable.Columns.Count - 1)
                    {
                        sql.AppendLine(",");
                    }
                    else
                    {
                        sql.AppendLine();
                    }
                }

                sql.AppendLine(")");

                command.CommandText = sql.ToString();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta la operación MERGE entre la tabla temporal y la tabla destino
        /// </summary>
        private void ExecuteMerge(SqlConnection connection, string tempTableName, string[] keyColumns)
        {
            using (var command = connection.CreateCommand())
            {
                var sql = new System.Text.StringBuilder();

                // Obtener columnas de la tabla destino
                DataTable schema = GetTableSchema(connection);

                if (schema.Rows.Count == 0)
                {
                    throw new InvalidOperationException($"No se pudo obtener el esquema de la tabla {_schema}.{_tableName}");
                }

                // Construir sentencia MERGE
                sql.AppendLine($"MERGE [{_schema}].[{_tableName}] AS target");
                sql.AppendLine($"USING {tempTableName} AS source");
                sql.Append("ON (");

                for (int i = 0; i < keyColumns.Length; i++)
                {
                    sql.Append($"target.[{keyColumns[i]}] = source.[{keyColumns[i]}]");

                    if (i < keyColumns.Length - 1)
                    {
                        sql.Append(" AND ");
                    }
                }

                sql.AppendLine(")");

                // WHEN MATCHED
                sql.AppendLine("WHEN MATCHED THEN");
                sql.Append("UPDATE SET ");

                List<string> updateColumns = new List<string>();

                foreach (DataRow row in schema.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString();

                    // No actualizar columnas clave
                    if (!keyColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                    {
                        updateColumns.Add($"target.[{columnName}] = source.[{columnName}]");
                    }
                }

                sql.AppendLine(string.Join(", ", updateColumns));

                // WHEN NOT MATCHED
                sql.AppendLine("WHEN NOT MATCHED THEN");
                sql.Append("INSERT (");

                List<string> columnNames = new List<string>();
                List<string> sourceColumns = new List<string>();

                foreach (DataRow row in schema.Rows)
                {
                    string columnName = row["COLUMN_NAME"].ToString();
                    columnNames.Add($"[{columnName}]");
                    sourceColumns.Add($"source.[{columnName}]");
                }

                sql.AppendLine(string.Join(", ", columnNames) + ")");
                sql.AppendLine("VALUES (" + string.Join(", ", sourceColumns) + ")");
                sql.AppendLine(";");

                command.CommandText = sql.ToString();
                command.CommandTimeout = _timeout;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Obtiene el esquema de la tabla destino
        /// </summary>
        private DataTable GetTableSchema(SqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION";

                command.Parameters.Add(new SqlParameter("@schema", _schema));
                command.Parameters.Add(new SqlParameter("@tableName", _tableName));

                var schemaTable = new DataTable();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(schemaTable);
                }

                return schemaTable;
            }
        }

        /// <summary>
        /// Convierte un tipo .NET a un tipo SQL Server
        /// </summary>
        private string GetSqlType(Type type)
        {
            if (type == typeof(string))
                return "NVARCHAR(MAX)";
            else if (type == typeof(int) || type == typeof(Int32))
                return "INT";
            else if (type == typeof(long) || type == typeof(Int64))
                return "BIGINT";
            else if (type == typeof(short) || type == typeof(Int16))
                return "SMALLINT";
            else if (type == typeof(byte))
                return "TINYINT";
            else if (type == typeof(decimal))
                return "DECIMAL(18, 6)";
            else if (type == typeof(float))
                return "REAL";
            else if (type == typeof(double))
                return "FLOAT";
            else if (type == typeof(bool) || type == typeof(Boolean))
                return "BIT";
            else if (type == typeof(DateTime))
                return "DATETIME2";
            else if (type == typeof(Guid))
                return "UNIQUEIDENTIFIER";
            else if (type == typeof(byte[]))
                return "VARBINARY(MAX)";
            else if (type == typeof(TimeSpan))
                return "TIME";
            else
                return "NVARCHAR(MAX)";
        }
    }
}