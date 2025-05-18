using NexusLink.AOP.Interception;
using System;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Base attribute for method interception
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptAttribute : Attribute
    {
        /// <summary>
        /// The order in which this interceptor should be applied (lower values run first)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Creates the interceptor instance for this attribute
        /// </summary>
        public abstract IMethodInterceptor CreateInterceptor();
    }
}