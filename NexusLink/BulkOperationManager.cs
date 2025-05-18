using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public class BulkOperationManager
{
    public async Task BulkInsertAsync<T>(string connectionString, IEnumerable<T> entities,
        string tableName, int batchSize = 1000)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            // Crear DataTable a partir de entidades
            var dataTable = new DataTable();
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToArray();

            // Configurar columnas
            foreach (var prop in properties)
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var columnName = columnAttr?.Name ?? prop.Name;
                dataTable.Columns.Add(columnName, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            // Poblar filas
            foreach (var entity in entities)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    var columnName = columnAttr?.Name ?? prop.Name;
                    row[columnName] = prop.GetValue(entity) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            // Realizar bulk copy
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BatchSize = batchSize;

                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }
    }
}