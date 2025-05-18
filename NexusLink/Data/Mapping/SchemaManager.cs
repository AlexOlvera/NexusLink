using NexusLink.Core.Connection;
using NexusLink.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace NexusLink.Data.Mapping
{
    /// <summary>
    /// Administra operaciones relacionadas con esquemas de base de datos,
    /// incluyendo creación, modificación y validación.
    /// </summary>
    public class SchemaManager
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly EntityMapper _entityMapper;
        private readonly ILogger _logger;

        public SchemaManager(ConnectionFactory connectionFactory, EntityMapper entityMapper, ILogger logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Crea una tabla en la base de datos basada en una entidad.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad para generar el esquema</typeparam>
        /// <param name="ifNotExists">Si es true, añade una cláusula IF NOT EXISTS</param>
        /// <returns>True si la operación fue exitosa</returns>
        public bool CreateTableFromEntity<T>(bool ifNotExists = true) where T : class, new()
        {
            try
            {
                var tableInfo = _entityMapper.GetTableInfo<T>();
                string createTableSql = GenerateCreateTableSql(tableInfo, ifNotExists);

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = createTableSql;
                        command.ExecuteNonQuery();
                    }
                }

                _logger.Info($"Tabla {tableInfo.TableName} creada exitosamente.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creando tabla para entidad {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Verifica si el esquema de la base de datos coincide con las entidades mapeadas.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad para validar</typeparam>
        /// <returns>Lista de discrepancias encontradas</returns>
        public IEnumerable<SchemaMismatch> ValidateSchema<T>() where T : class, new()
        {
            var tableInfo = _entityMapper.GetTableInfo<T>();
            var mismatches = new List<SchemaMismatch>();

            try
            {
                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();

                    // Verifica si la tabla existe
                    bool tableExists = CheckTableExists(connection, tableInfo.TableName, tableInfo.SchemaName);
                    if (!tableExists)
                    {
                        mismatches.Add(new SchemaMismatch
                        {
                            EntityType = typeof(T),
                            MismatchType = MismatchType.TableMissing,
                            Message = $"Tabla {tableInfo.TableName} no existe en la base de datos."
                        });
                        return mismatches;
                    }

                    // Verifica cada columna
                    var dbColumns = GetDatabaseColumns(connection, tableInfo.TableName, tableInfo.SchemaName);

                    foreach (var column in tableInfo.Columns)
                    {
                        if (!dbColumns.ContainsKey(column.ColumnName))
                        {
                            mismatches.Add(new SchemaMismatch
                            {
                                EntityType = typeof(T),
                                MismatchType = MismatchType.ColumnMissing,
                                PropertyName = column.PropertyName,
                                Message = $"Columna {column.ColumnName} no existe en la tabla {tableInfo.TableName}."
                            });
                            continue;
                        }

                        var dbColumn = dbColumns[column.ColumnName];
                        if (!IsColumnTypeCompatible(column.SqlType, dbColumn.DataType))
                        {
                            mismatches.Add(new SchemaMismatch
                            {
                                EntityType = typeof(T),
                                MismatchType = MismatchType.TypeMismatch,
                                PropertyName = column.PropertyName,
                                Message = $"Tipo incompatible para columna {column.ColumnName}: esperado {column.SqlType}, actual {dbColumn.DataType}."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error validando esquema para {typeof(T).Name}: {ex.Message}");
                throw;
            }

            return mismatches;
        }

        /// <summary>
        /// Actualiza el esquema de la base de datos para que coincida con las entidades mapeadas.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad para actualizar</typeparam>
        /// <param name="addMissingColumns">Si es true, agrega columnas faltantes</param>
        /// <param name="updateMismatchedTypes">Si es true, intenta actualizar tipos incompatibles</param>
        /// <returns>True si la operación fue exitosa</returns>
        public bool UpdateSchema<T>(bool addMissingColumns = true, bool updateMismatchedTypes = false) where T : class, new()
        {
            var tableInfo = _entityMapper.GetTableInfo<T>();

            try
            {
                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();

                    // Verifica si la tabla existe
                    bool tableExists = CheckTableExists(connection, tableInfo.TableName, tableInfo.SchemaName);
                    if (!tableExists)
                    {
                        // Crea la tabla si no existe
                        string createTableSql = GenerateCreateTableSql(tableInfo, false);
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = createTableSql;
                            command.ExecuteNonQuery();
                        }
                        return true;
                    }

                    // Verifica columnas existentes
                    var dbColumns = GetDatabaseColumns(connection, tableInfo.TableName, tableInfo.SchemaName);

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Agrega columnas faltantes
                            if (addMissingColumns)
                            {
                                foreach (var column in tableInfo.Columns)
                                {
                                    if (!dbColumns.ContainsKey(column.ColumnName))
                                    {
                                        string alterSql = $"ALTER TABLE {tableInfo.SchemaName}.{tableInfo.TableName} ADD {column.ColumnName} {column.SqlType}";
                                        using (var command = connection.CreateCommand())
                                        {
                                            command.CommandText = alterSql;
                                            command.Transaction = transaction;
                                            command.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            // Actualiza tipos incompatibles
                            if (updateMismatchedTypes)
                            {
                                foreach (var column in tableInfo.Columns)
                                {
                                    if (dbColumns.ContainsKey(column.ColumnName))
                                    {
                                        var dbColumn = dbColumns[column.ColumnName];
                                        if (!IsColumnTypeCompatible(column.SqlType, dbColumn.DataType))
                                        {
                                            string alterSql = $"ALTER TABLE {tableInfo.SchemaName}.{tableInfo.TableName} ALTER COLUMN {column.ColumnName} {column.SqlType}";
                                            using (var command = connection.CreateCommand())
                                            {
                                                command.CommandText = alterSql;
                                                command.Transaction = transaction;
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.Error($"Error actualizando esquema para {typeof(T).Name}: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error actualizando esquema para {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }

        #region Helper Methods

        private string GenerateCreateTableSql(TableInfo tableInfo, bool ifNotExists)
        {
            string ifNotExistsClause = ifNotExists ? "IF NOT EXISTS " : "";

            var columnDefinitions = tableInfo.Columns.Select(c =>
                $"{c.ColumnName} {c.SqlType}{(c.IsNullable ? "" : " NOT NULL")}{(c.IsIdentity ? " IDENTITY(1,1)" : "")}{(c.DefaultValue != null ? $" DEFAULT {c.DefaultValue}" : "")}");

            var primaryKeyColumns = tableInfo.Columns
                .Where(c => c.IsPrimaryKey)
                .Select(c => c.ColumnName);

            string primaryKeyConstraint = primaryKeyColumns.Any()
                ? $", CONSTRAINT PK_{tableInfo.TableName} PRIMARY KEY ({string.Join(", ", primaryKeyColumns)})"
                : "";

            return $@"CREATE TABLE {ifNotExistsClause}{tableInfo.SchemaName}.{tableInfo.TableName} (
                {string.Join(",\n", columnDefinitions)}{primaryKeyConstraint}
            )";
        }

        private bool CheckTableExists(DbConnection connection, string tableName, string schemaName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = @TableName
                    AND TABLE_SCHEMA = @SchemaName";

                var tableNameParam = command.CreateParameter();
                tableNameParam.ParameterName = "@TableName";
                tableNameParam.Value = tableName;
                command.Parameters.Add(tableNameParam);

                var schemaNameParam = command.CreateParameter();
                schemaNameParam.ParameterName = "@SchemaName";
                schemaNameParam.Value = schemaName;
                command.Parameters.Add(schemaNameParam);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private Dictionary<string, DbColumn> GetDatabaseColumns(DbConnection connection, string tableName, string schemaName)
        {
            var result = new Dictionary<string, DbColumn>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT,
                        CASE WHEN COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') = 1 THEN 1 ELSE 0 END AS IS_IDENTITY
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    AND TABLE_SCHEMA = @SchemaName";

                var tableNameParam = command.CreateParameter();
                tableNameParam.ParameterName = "@TableName";
                tableNameParam.Value = tableName;
                command.Parameters.Add(tableNameParam);

                var schemaNameParam = command.CreateParameter();
                schemaNameParam.ParameterName = "@SchemaName";
                schemaNameParam.Value = schemaName;
                command.Parameters.Add(schemaNameParam);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var column = new DbColumn
                        {
                            ColumnName = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = reader.GetString(2) == "YES",
                            DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsIdentity = reader.GetInt32(4) == 1
                        };

                        result[column.ColumnName] = column;
                    }
                }
            }

            return result;
        }

        private bool IsColumnTypeCompatible(string modelType, string dbType)
        {
            // Implementación simple, podría expandirse para manejar más tipos y compatibilidades
            modelType = modelType.ToLowerInvariant();
            dbType = dbType.ToLowerInvariant();

            if (modelType == dbType) return true;

            // Algunas compatibilidades comunes
            if (modelType.Contains("varchar") && dbType.Contains("varchar")) return true;
            if (modelType.Contains("int") && dbType.Contains("int")) return true;
            if (modelType.Contains("decimal") && dbType.Contains("decimal")) return true;
            if (modelType.Contains("float") && dbType.Contains("float")) return true;
            if (modelType.Contains("date") && dbType.Contains("date")) return true;

            return false;
        }

        #endregion
    }

    public class DbColumn
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; }
        public bool IsIdentity { get; set; }
    }

    public class SchemaMismatch
    {
        public Type EntityType { get; set; }
        public MismatchType MismatchType { get; set; }
        public string PropertyName { get; set; }
        public string Message { get; set; }
    }

    public enum MismatchType
    {
        TableMissing,
        ColumnMissing,
        TypeMismatch,
        NullabilityMismatch,
        DefaultValueMismatch,
        IdentityMismatch
    }
}