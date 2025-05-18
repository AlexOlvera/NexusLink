using NexusLink.AOP.Interception;
using System;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Attribute for executing SQL commands (INSERT, UPDATE, DELETE)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : SqlCommandAttribute
    {
        /// <summary>
        /// Gets or sets whether to return the number of affected rows
        /// </summary>
        public bool ReturnAffectedRows { get; set; } = true;

        public CommandAttribute(string commandText) : base(commandText) { }

        public override IMethodInterceptor CreateInterceptor()
        {
            return new CommandInterceptor(this);
        }

        private class CommandInterceptor : MethodInterceptor
        {
            private readonly CommandAttribute _attribute;

            public CommandInterceptor(CommandAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Get connection
                string connectionString = ConfigManager.GetConnectionString(_attribute.ConnectionName);

                // Format the command
                string formattedCommand = _attribute.FormatCommandText(invocation.Arguments);

                // Create and execute command
                using (SqlCommand command = _attribute.CreateCommand(connectionString, formattedCommand, invocation.Arguments))
                {
                    int affectedRows = command.ExecuteNonQuery();

                    Type returnType = invocation.ReturnType;

                    // If void return type
                    if (returnType == typeof(void))
                        return null;

                    // If boolean return type, return true if any rows affected
                    if (returnType == typeof(bool))
                        return affectedRows > 0;

                    // If numeric return type, return affected rows
                    if (returnType == typeof(int))
                        return affectedRows;

                    // Default
                    return affectedRows;
                }
            }
        }
    }
}