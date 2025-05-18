using NexusLink.AOP.Interception;
using NexusLink.Core.Connection;
using System;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Specifies which database to use for a method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public class DatabaseAttribute : InterceptAttribute
    {
        /// <summary>
        /// The name of the database to use
        /// </summary>
        public string DatabaseName { get; }

        public DatabaseAttribute(string databaseName)
        {
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            // Set order to -100 so it executes before other interceptors
            Order = -100;
        }

        public override IMethodInterceptor CreateInterceptor()
        {
            return new DatabaseSelectorInterceptor(this);
        }

        private class DatabaseSelectorInterceptor : MethodInterceptor
        {
            private readonly DatabaseAttribute _attribute;

            public DatabaseSelectorInterceptor(DatabaseAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Get the database selector from the service locator
                var databaseSelector = ServiceLocator.Current.GetService<DatabaseSelector>();

                // Execute in the context of the specified database
                return databaseSelector.ExecuteWith(_attribute.DatabaseName, () => invocation.Proceed());
            }
        }
    }
}