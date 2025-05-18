namespace NexusLink.AOP.Interception
{
    /// <summary>
    /// Factory for creating dynamic proxies that apply method interception
    /// </summary>
    public class ProxyFactory
    {
        private readonly IDynamicProxyGenerator _proxyGenerator;

        public ProxyFactory(IDynamicProxyGenerator proxyGenerator)
        {
            _proxyGenerator = proxyGenerator;
        }

        /// <summary>
        /// Creates a proxy for the specified interface type
        /// </summary>
        /// <typeparam name="TInterface">The interface to proxy</typeparam>
        /// <param name="target">The target instance implementing the interface</param>
        /// <param name="interceptors">Optional explicit interceptors</param>
        /// <returns>A proxy implementing the interface</returns>
        public TInterface CreateInterfaceProxy<TInterface>(object target, params IMethodInterceptor[] interceptors)
            where TInterface : class
        {
            Type interfaceType = typeof(TInterface);

            if (!interfaceType.IsInterface)
                throw new ArgumentException($"{interfaceType.Name} is not an interface");

            return (TInterface)CreateProxy(target, interfaceType, null, interceptors);
        }

        /// <summary>
        /// Creates a proxy for the specified class type
        /// </summary>
        /// <typeparam name="T">The class to proxy</typeparam>
        /// <param name="target">The target instance</param>
        /// <param name="interceptors">Optional explicit interceptors</param>
        /// <returns>A proxy extending the class</returns>
        public T CreateClassProxy<T>(T target, params IMethodInterceptor[] interceptors)
            where T : class
        {
            Type classType = typeof(T);

            if (classType.IsSealed)
                throw new ArgumentException($"Cannot create proxy for sealed class {classType.Name}");

            return (T)CreateProxy(target, null, classType, interceptors);
        }

        /// <summary>
        /// Internal method to create a proxy using the proxy generator
        /// </summary>
        private object CreateProxy(object target, Type interfaceType, Type classType, IMethodInterceptor[] interceptors)
        {
            // Create context and add interceptors
            var context = new InterceptionContext();

            foreach (var interceptor in interceptors)
            {
                context.AddInterceptor(interceptor);
            }

            // If it's an interface proxy
            if (interfaceType != null)
            {
                // Add attribute-based interceptors from interface methods
                foreach (var method in interfaceType.GetMethods())
                {
                    context.AddInterceptorsFromAttributes(method);
                }

                return _proxyGenerator.GenerateInterfaceProxy(target, interfaceType, context);
            }

            // If it's a class proxy
            if (classType != null)
            {
                // Add attribute-based interceptors from class methods
                foreach (var method in classType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    context.AddInterceptorsFromAttributes(method);
                }

                return _proxyGenerator.GenerateClassProxy(target, classType, context);
            }

            throw new ArgumentException("Either interfaceType or classType must be specified");
        }
    }
}