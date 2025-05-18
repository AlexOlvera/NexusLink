using System;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspect that provides automatic retry capabilities for methods
    /// </summary>
    public class RetryAspect : MethodInterceptor
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _delay;
        private readonly Func<Exception, bool> _retryPredicate;
        private readonly bool _useExponentialBackoff;

        public RetryAspect(int maxAttempts = 3,
                          int delayMilliseconds = 100,
                          Func<Exception, bool> retryPredicate = null,
                          bool useExponentialBackoff = true)
        {
            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be greater than zero");

            if (delayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "Cannot be negative");

            _maxAttempts = maxAttempts;
            _delay = TimeSpan.FromMilliseconds(delayMilliseconds);
            _retryPredicate = retryPredicate ?? (ex => true);
            _useExponentialBackoff = useExponentialBackoff;
        }

        public override object Intercept(IMethodInvocation invocation)
        {
            int attempt = 0;

            while (true)
            {
                try
                {
                    attempt++;
                    return invocation.Proceed();
                }
                catch (Exception ex)
                {
                    // Check if we should retry this exception
                    if (!_retryPredicate(ex))
                        throw;

                    // Check if we've exceeded max attempts
                    if (attempt >= _maxAttempts)
                        throw;

                    // Calculate delay
                    TimeSpan currentDelay = _useExponentialBackoff
                        ? TimeSpan.FromMilliseconds(_delay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                        : _delay;

                    // Log the retry
                    NexusTraceAdapter.LogWarning(
                        $"Retry attempt {attempt}/{_maxAttempts} for {invocation.Method.Name} after {currentDelay.TotalMilliseconds}ms");

                    // Wait before retrying
                    Thread.Sleep(currentDelay);
                }
            }
        }
    }
}