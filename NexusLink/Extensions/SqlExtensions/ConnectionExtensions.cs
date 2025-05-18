using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Extensions.SqlExtensions
{
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Crea un nuevo comando SQL asociado a la conexión
        /// </summary>
        public static SqlCommand CreateCommand(this SqlConnection connection, string commandText)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return new SqlCommand(commandText, connection);
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente
        /// </summary>
        public static int ExecuteNonQuery(this SqlConnection connection, string commandText)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            bool wasOpen = (connection.State == ConnectionState.Open);

            if (!wasOpen)
                connection.Open();

            try
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente y devuelve un escalar
        /// </summary>
        public static T ExecuteScalar<T>(this SqlConnection connection, string commandText)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            bool wasOpen = (connection.State == ConnectionState.Open);

            if (!wasOpen)
                connection.Open();

            try
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    var result = command.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return default(T);

                    return (T)Convert.ChangeType(result, typeof(T));
                }
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente y devuelve un SqlDataReader
        /// </summary>
        public static SqlDataReader ExecuteReader(this SqlConnection connection, string commandText,
            CommandBehavior behavior = CommandBehavior.Default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != ConnectionState.Open)
                connection.Open();

            var command = new SqlCommand(commandText, connection);
            return command.ExecuteReader(behavior);
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente y rellena un DataTable
        /// </summary>
        public static DataTable ExecuteDataTable(this SqlConnection connection, string commandText)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            bool wasOpen = (connection.State == ConnectionState.Open);

            if (!wasOpen)
                connection.Open();

            try
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    var table = new DataTable();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }

                    return table;
                }
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente y rellena un DataSet
        /// </summary>
        public static DataSet ExecuteDataSet(this SqlConnection connection, string commandText)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            bool wasOpen = (connection.State == ConnectionState.Open);

            if (!wasOpen)
                connection.Open();

            try
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    var dataset = new DataSet();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataset);
                    }

                    return dataset;
                }
            }
            finally
            {
                if (!wasOpen)
                    connection.Close();
            }
        }

        /// <summary>
        /// Abre la conexión de manera segura
        /// </summary>
        public static SqlConnection EnsureOpen(this SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != ConnectionState.Open)
                connection.Open();

            return connection;
        }

        /// <summary>
        /// Abre la conexión de manera segura y asíncrona
        /// </summary>
        public static async Task<SqlConnection> EnsureOpenAsync(this SqlConnection connection,
            CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            return connection;
        }
    }
}
