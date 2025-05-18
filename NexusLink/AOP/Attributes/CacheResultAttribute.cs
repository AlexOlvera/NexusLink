using NexusLink.AOP.Interception;
using System;
using System.Collections.Generic;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Caches the result of a method for a specified duration
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheResultAttribute : InterceptAttribute
    {
        /// <summary>
        /// Cache duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; } = 60;

        /// <summary>
        /// Cache key prefix to use
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Whether to include method parameter values in the cache key
        /// </summary>
        public bool IncludeParametersInCacheKey { get; set; } = true;

        public CacheResultAttribute()
        {
            // Set order to a high value so it executes after other interceptors
            Order = 1000;
        }

        public override IMethodInterceptor CreateInterceptor()
        {
            return new CachingInterceptor(this);
        }

        private class CachingInterceptor : MethodInterceptor
        {
            private readonly CacheResultAttribute _attribute;
            private static readonly object _cacheLock = new object();
            private static readonly Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();

            private class CacheItem
            {
                public object Value { get; set; }
                public DateTime Expiration { get; set; }

                public bool IsExpired => DateTime.UtcNow > Expiration;
            }

            public CachingInterceptor(CacheResultAttribute attribute)
            {
                _attribute = attribute;
            }

            public override object Intercept(IMethodInvocation invocation)
            {
                // Generate cache key
                string cacheKey = GenerateCacheKey(invocation);

                // Check if result is in cache
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(cacheKey, out CacheItem cachedItem) && !cachedItem.IsExpired)
                    {
                        return cachedItem.Value;
                    }
                }

                // Execute the method
                object result = invocation.Proceed();

                // Cache the result
                lock (_cacheLock)
                {
                    _cache[cacheKey] = new CacheItem
                    {
                        Value = result,
                        Expiration = DateTime.UtcNow.AddSeconds(_attribute.DurationSeconds)
                    };
                }

                return result;
            }

            private string GenerateCacheKey(IMethodInvocation invocation)
            {
                StringBuilder keyBuilder = new StringBuilder();

                // Add prefix if specified
                if (!string.IsNullOrEmpty(_attribute.KeyPrefix))
                {
                    keyBuilder.Append(_attribute.KeyPrefix).Append(':');
                }

                // Add type and method name
                keyBuilder.Append(invocation.Method.DeclaringType.FullName)
                         .Append(':')
                         .Append(invocation.Method.Name);

                // Add parameters if requested
                if (_attribute.IncludeParametersInCacheKey && invocation.Arguments?.Length > 0)
                {
                    keyBuilder.Append(':');

                    for (int i = 0; i < invocation.Arguments.Length; i++)
                    {
                        if (i > 0) keyBuilder.Append('_');

                        object arg = invocation.Arguments[i];
                        keyBuilder.Append(arg != null ? arg.ToString() : "null");
                    }
                }

                return keyBuilder.ToString();
            }
        }
    }
}