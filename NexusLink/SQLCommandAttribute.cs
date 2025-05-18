using System;
using System.Data.SqlClient;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Base class for SQL-related command attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class SqlCommandAttribute : InterceptAttribute
    {
        /// <summary>
        /// The SQL command text with optional format placeholders
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// Timeout in seconds for the command execution
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// The name of the connection to use (from configuration)
        /// </summary>
        public string ConnectionName { get; set; } = "Default";

        /// <summary>
        /// Whether parameter names should be auto-generated
        /// </summary>
        public bool AutoGenerateParameterNames { get; set; } = false;

        protected SqlCommandAttribute(string commandText)
        {
            CommandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        }

        /// <summary>
        /// Formats the command text using the provided arguments
        /// </summary>
        protected string FormatCommandText(object[] args)
        {
            if (string.IsNullOrEmpty(CommandText) || args == null || args.Length == 0)
                return CommandText;

            // If using string.Format style placeholders
            if (CommandText.Contains("{0}") || CommandText.Contains("{1}"))
            {
                return string.Format(CommandText, args);
            }

            // If using @p0, @p1 style parameters
            string result = CommandText;
            for (int i = 0; i < args.Length; i++)
            {
                result = result.Replace($"@p{i}", $"@p{i}");
            }

            return result;
        }

        /// <summary>
        /// Creates a command for the given connection and command text
        /// </summary>
        protected SqlCommand CreateCommand(string connectionString, string commandText, object[] args)
        {
            var connection = new SqlConnection(connectionString);
            var command = new SqlCommand(commandText, connection);
            command.CommandTimeout = CommandTimeout;

            // Add parameters
            if (AutoGenerateParameterNames)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    command.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);
                }
            }

            connection.Open();
            return command;
        }
    }
}