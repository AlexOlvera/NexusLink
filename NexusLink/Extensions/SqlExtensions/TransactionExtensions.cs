using System;
using System.Data;
using System.Data.SqlClient;

namespace NexusLink.Extensions.SqlExtensions
{
    public static class TransactionExtensions
    {
        /// <summary>
        /// Crea un nuevo comando SQL asociado a la transacción
        /// </summary>
        public static SqlCommand CreateCommand(this SqlTransaction transaction, string commandText)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var command = transaction.Connection.CreateCommand();
            command.CommandText = commandText;
            command.Transaction = transaction;

            return command;
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente dentro de la transacción
        /// </summary>
        public static int ExecuteNonQuery(this SqlTransaction transaction, string commandText)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var command = transaction.CreateCommand(commandText))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente dentro de la transacción y devuelve un escalar
        /// </summary>
        public static T ExecuteScalar<T>(this SqlTransaction transaction, string commandText)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var command = transaction.CreateCommand(commandText))
            {
                var result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return default(T);

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente dentro de la transacción y devuelve un SqlDataReader
        /// </summary>
        public static SqlDataReader ExecuteReader(this SqlTransaction transaction, string commandText,
            CommandBehavior behavior = CommandBehavior.Default)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var command = transaction.CreateCommand(commandText);
            return command.ExecuteReader(behavior);
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente dentro de la transacción y rellena un DataTable
        /// </summary>
        public static DataTable ExecuteDataTable(this SqlTransaction transaction, string commandText)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var command = transaction.CreateCommand(commandText))
            {
                var table = new DataTable();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }

                return table;
            }
        }

        /// <summary>
        /// Ejecuta un comando SQL directamente dentro de la transacción y rellena un DataSet
        /// </summary>
        public static DataSet ExecuteDataSet(this SqlTransaction transaction, string commandText)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using (var command = transaction.CreateCommand(commandText))
            {
                var dataset = new DataSet();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dataset);
                }

                return dataset;
            }
        }

        /// <summary>
        /// Crea un punto de guardado dentro de la transacción
        /// </summary>
        public static void CreateSavepoint(this SqlTransaction transaction, string savePointName)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (string.IsNullOrEmpty(savePointName))
                throw new ArgumentException("Savepoint name cannot be null or empty", nameof(savePointName));

            transaction.Save(savePointName);
        }

        /// <summary>
        /// Retrocede hasta un punto de guardado específico dentro de la transacción
        /// </summary>
        public static void RollbackToSavepoint(this SqlTransaction transaction, string savePointName)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (string.IsNullOrEmpty(savePointName))
                throw new ArgumentException("Savepoint name cannot be null or empty", nameof(savePointName));

            transaction.Rollback(savePointName);
        }
    }
}