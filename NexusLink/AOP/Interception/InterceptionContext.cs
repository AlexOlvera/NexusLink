namespace NexusLink.AOP.Interception
{
    /// <summary>
    /// Provides context and configuration for method interception
    /// </summary>
    public class InterceptionContext
    {
        /// <summary>
        /// Gets the collection of interceptors to apply
        /// </summary>
        public IList<IMethodInterceptor> Interceptors { get; } = new List<IMethodInterceptor>();

        /// <summary>
        /// Optional tag to identify the interception context
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Additional data related to the interception
        /// </summary>
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds an interceptor to the context
        /// </summary>
        public InterceptionContext AddInterceptor(IMethodInterceptor interceptor)
        {
            Interceptors.Add(interceptor);
            return this;
        }

        /// <summary>
        /// Adds interceptors from attributes on the method
        /// </summary>
        public InterceptionContext AddInterceptorsFromAttributes(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(typeof(InterceptAttribute), true);

            foreach (InterceptAttribute attribute in attributes.OrderBy(a => a.Order))
            {
                AddInterceptor(attribute.CreateInterceptor());
            }

            return this;
        }
    }
}