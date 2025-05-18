using NexusLink.AOP.Interception;
using NexusLink.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Attribute for executing stored procedures
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method)]
    public class StoredProcedureAttribute : InterceptAttribute
    {
        /// <summary>
        /// The name of the stored procedure
        /// </summary>
        public string ProcedureName { get; }

        /// <summary>
        /// The connection name from configuration
        /// </summary>
        public string ConnectionName { get; set; } = "Default";

        /// <summary>
        /// Command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Parameter mappings from method parameters to stored procedure parameters
        /// </summary>
        public IDictionary<string, string> ParameterMappings { get; set; } = new Dictionary<string, string>();

        public StoredProcedureAttribute(string procedureName)
        {
            ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
        }

        public override IMethodInterceptor CreateInterceptor()
        {
            return new StoredProcedureInterceptor(this);
        }

        private class StoredProcedureInterceptor : MethodInterceptor
        {
            private readonly StoredProcedureAttribute _attribute;

            public StoredProcedureInterceptor(StoredProcedureAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Get connection
                string connectionString = ConfigManager.GetConnectionString(_attribute.ConnectionName);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(_attribute.ProcedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = _attribute.CommandTimeout;

                        // Add parameters
                        AddParameters(command, invocation);

                        // Execute based on return type
                        Type returnType = invocation.ReturnType;

                        // For scalar results
                        if (IsPrimitiveOrString(returnType))
                        {
                            object result = command.ExecuteScalar();
                            return result == DBNull.Value ? GetDefaultValue(returnType) :
                                   Convert.ChangeType(result, returnType);
                        }

                        // For complex objects or collections
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            return MapReaderToReturnType(reader, returnType);
                        }
                    }
                }
            }

            private void AddParameters(SqlCommand command, IMethodInvocation invocation)
            {
                ParameterInfo[] parameters = invocation.Method.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo param = parameters[i];
                    object value = invocation.Arguments[i] ?? DBNull.Value;

                    // Check if there is a custom mapping
                    if (_attribute.ParameterMappings.TryGetValue(param.Name, out string spParamName))
                    {
                        command.Parameters.AddWithValue(spParamName, value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@" + param.Name, value);
                    }
                }
            }

            // Other helper methods (MapReaderToReturnType, IsPrimitiveOrString, etc.) would be similar to QueryAttribute
        }
    }
}