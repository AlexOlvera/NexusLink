namespace NexusLink.AOP.Interception
{
    /// <summary>
    /// Represents a method invocation that can be intercepted
    /// </summary>
    public interface IMethodInvocation
    {
        /// <summary>
        /// The target instance on which the method is invoked
        /// </summary>
        object Target { get; }

        /// <summary>
        /// Information about the intercepted method
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// The arguments passed to the method
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// The return type of the method
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Method-specific additional data
        /// </summary>
        IDictionary<string, object> Data { get; }

        /// <summary>
        /// Proceeds with the invocation by calling the next interceptor or the target method
        /// </summary>
        /// <returns>The return value of the method</returns>
        object Proceed();
    }

    /// <summary>
    /// Standard implementation of a method invocation
    /// </summary>
    public class MethodInvocation : IMethodInvocation
    {
        private readonly Func<object[], object> _proceedDelegate;
        private readonly IList<IMethodInterceptor> _interceptors;
        private int _currentInterceptorIndex = 0;

        public object Target { get; }
        public MethodInfo Method { get; }
        public object[] Arguments { get; }
        public Type ReturnType => Method.ReturnType;
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();

        public MethodInvocation(
            object target,
            MethodInfo method,
            object[] arguments,
            IList<IMethodInterceptor> interceptors,
            Func<object[], object> proceedDelegate)
        {
            Target = target;
            Method = method;
            Arguments = arguments;
            _interceptors = interceptors;
            _proceedDelegate = proceedDelegate;
        }

        public object Proceed()
        {
            // If there are more interceptors, delegate to the next one
            if (_currentInterceptorIndex < _interceptors.Count)
            {
                var interceptor = _interceptors[_currentInterceptorIndex++];
                return interceptor.Intercept(this);
            }

            // Otherwise, invoke the target method
            return _proceedDelegate(Arguments);
        }
    }
}