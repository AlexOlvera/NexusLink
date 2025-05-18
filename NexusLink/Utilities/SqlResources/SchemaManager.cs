using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NexusLink.Utilities
{
    public class SchemaManager
    {
        private readonly string _connectionString;

        public SchemaManager(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool SchemaExists(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty", nameof(schemaName));

            string sql = "SELECT SCHEMA_ID(@SchemaName) AS SchemaId";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@SchemaName", schemaName);
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
        }

        public void CreateSchema(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty", nameof(schemaName));

            if (SchemaExists(schemaName))
                return;

            string sql = $"CREATE SCHEMA [{schemaName}]";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<string> GetAllSchemas()
        {
            List<string> schemas = new List<string>();
            string sql = "SELECT name FROM sys.schemas WHERE schema_id > 4";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            schemas.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return schemas;
        }

        public bool TableExists(string schemaName, string tableName)
        {
            if (string.IsNullOrEmpty(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty", nameof(schemaName));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            string sql = "SELECT OBJECT_ID(@FullTableName, 'U') AS TableId";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@FullTableName", $"{schemaName}.{tableName}");
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
        }

        public DataTable GetTableSchema(string schemaName, string tableName)
        {
            if (string.IsNullOrEmpty(schemaName))
                throw new ArgumentException("Schema name cannot be null or empty", nameof(schemaName));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Get table schema
                string commandText = @"
                    SELECT 
                        c.column_id AS ColumnId,
                        c.name AS ColumnName,
                        t.name AS DataType,
                        c.max_length AS MaxLength,
                        c.precision AS Precision,
                        c.scale AS Scale,
                        c.is_nullable AS IsNullable,
                        c.is_identity AS IsIdentity,
                        CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    INNER JOIN sys.tables tbl ON c.object_id = tbl.object_id
                    INNER JOIN sys.schemas s ON tbl.schema_id = s.schema_id
                    LEFT JOIN (
                        SELECT ic.column_id, ic.object_id
                        FROM sys.index_columns ic
                        INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                        WHERE i.is_primary_key = 1
                    ) pk ON c.column_id = pk.column_id AND c.object_id = pk.object_id
                    WHERE s.name = @SchemaName AND tbl.name = @TableName
                    ORDER BY c.column_id";

                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@SchemaName", schemaName);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
        }
    }
}