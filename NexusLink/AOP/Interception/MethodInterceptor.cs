namespace NexusLink.AOP.Interception
{
    /// <summary>
    /// Base interface for all method interceptors
    /// </summary>
    public interface IMethodInterceptor
    {
        /// <summary>
        /// Intercepts a method invocation
        /// </summary>
        /// <param name="invocation">The method invocation context</param>
        /// <returns>The return value of the method</returns>
        object Intercept(IMethodInvocation invocation);
    }

    /// <summary>
    /// Abstract base implementation for method interceptors
    /// </summary>
    public abstract class MethodInterceptor : IMethodInterceptor
    {
        public virtual object Intercept(IMethodInvocation invocation)
        {
            BeforeInvocation(invocation);

            object result;
            try
            {
                // Execute the actual method or delegate to the next interceptor
                result = invocation.Proceed();

                // After successful execution
                AfterInvocation(invocation, result);
            }
            catch (Exception ex)
            {
                // Handle exception
                result = OnException(invocation, ex);

                // Rethrow if not handled
                if (ShouldRethrow(ex))
                    throw;
            }
            finally
            {
                // Always executed cleanup
                FinallyInvocation(invocation);
            }

            return result;
        }

        /// <summary>
        /// Called before the target method is invoked
        /// </summary>
        protected virtual void BeforeInvocation(IMethodInvocation invocation) { }

        /// <summary>
        /// Called after the target method is successfully invoked
        /// </summary>
        protected virtual void AfterInvocation(IMethodInvocation invocation, object result) { }

        /// <summary>
        /// Called when an exception occurs during method invocation
        /// </summary>
        /// <returns>Alternative return value if exception is handled</returns>
        protected virtual object OnException(IMethodInvocation invocation, Exception ex) { throw ex; }

        /// <summary>
        /// Called in a finally block after the invocation
        /// </summary>
        protected virtual void FinallyInvocation(IMethodInvocation invocation) { }

        /// <summary>
        /// Determines whether the exception should be rethrown
        /// </summary>
        protected virtual bool ShouldRethrow(Exception ex) => true;
    }
}