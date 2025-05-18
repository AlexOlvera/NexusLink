using NexusLink.AOP.Interception;
using NexusLink.Attributes;
using NexusLink.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Attribute for SQL SELECT queries
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryAttribute : SqlCommandAttribute
    {
        public QueryAttribute(string queryText) : base(queryText) { }

        public override IMethodInterceptor CreateInterceptor()
        {
            return new QueryInterceptor(this);
        }

        private class QueryInterceptor : MethodInterceptor
        {
            private readonly QueryAttribute _attribute;

            public QueryInterceptor(QueryAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Get connection
                string connectionString = ConfigManager.GetConnectionString(_attribute.ConnectionName);

                // Format the query
                string formattedQuery = _attribute.FormatCommandText(invocation.Arguments);

                // Create command
                using (SqlCommand command = _attribute.CreateCommand(connectionString, formattedQuery, invocation.Arguments))
                {
                    // Determine return type
                    Type returnType = invocation.ReturnType;

                    // Execute query and map results
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return MapReaderToReturnType(reader, returnType);
                    }
                }
            }

            private object MapReaderToReturnType(SqlDataReader reader, Type returnType)
            {
                // Handle scalar types (int, string, etc.)
                if (IsPrimitiveOrString(returnType))
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull(0) ? GetDefaultValue(returnType) : Convert.ChangeType(reader[0], returnType);
                    }
                    return GetDefaultValue(returnType);
                }

                // Handle collection types (List<T>, IEnumerable<T>)
                if (IsGenericCollection(returnType, out Type elementType))
                {
                    // Create list of the element type
                    Type listType = typeof(List<>).MakeGenericType(elementType);
                    IList list = (IList)Activator.CreateInstance(listType);

                    // Map reader rows to elements
                    while (reader.Read())
                    {
                        object item;

                        // For primitive type collections
                        if (IsPrimitiveOrString(elementType))
                        {
                            item = reader.IsDBNull(0) ? GetDefaultValue(elementType) : Convert.ChangeType(reader[0], elementType);
                        }
                        // For complex type collections
                        else
                        {
                            item = MapReaderRowToObject(reader, elementType);
                        }

                        list.Add(item);
                    }

                    return list;
                }

                // Handle single complex object
                if (reader.Read())
                {
                    return MapReaderRowToObject(reader, returnType);
                }

                return GetDefaultValue(returnType);
            }

            private object MapReaderRowToObject(SqlDataReader reader, Type objectType)
            {
                object instance = Activator.CreateInstance(objectType);

                // Get data schema
                var schemaTable = reader.GetSchemaTable();

                // Maps column names to column ordinals
                Dictionary<string, int> columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnMap[reader.GetName(i)] = i;
                }

                // Map properties using appropriate attributes
                foreach (var property in objectType.GetProperties())
                {
                    // Look for column attribute
                    var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
                    string columnName = columnAttr?.Name ?? property.Name;

                    if (columnMap.TryGetValue(columnName, out int ordinal))
                    {
                        if (!reader.IsDBNull(ordinal))
                        {
                            object value = reader[ordinal];

                            // Handle type conversion
                            if (property.PropertyType != value.GetType() && value != DBNull.Value)
                            {
                                value = Convert.ChangeType(value, property.PropertyType);
                            }

                            property.SetValue(instance, value);
                        }
                    }
                }

                return instance;
            }

            private bool IsPrimitiveOrString(Type type)
            {
                return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                       type == typeof(DateTime) || type == typeof(Guid) ||
                       Nullable.GetUnderlyingType(type) != null;
            }

            private bool IsGenericCollection(Type type, out Type elementType)
            {
                elementType = null;

                if (!type.IsGenericType)
                    return false;

                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }

                return false;
            }

            private object GetDefaultValue(Type type)
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
        }
    }
}