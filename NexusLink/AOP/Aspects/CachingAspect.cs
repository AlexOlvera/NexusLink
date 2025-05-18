using NexusLink.AOP.Interception;
using System;
using System.Collections.Generic;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspect that provides caching capabilities for method results
    /// </summary>
    public class CachingAspect : MethodInterceptor
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly Func<IMethodInvocation, string> _keyGenerator;
        private readonly TimeSpan _cacheDuration;

        public CachingAspect(ICacheProvider cacheProvider,
                            Func<IMethodInvocation, string> keyGenerator,
                            TimeSpan cacheDuration)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _cacheDuration = cacheDuration;
        }

        public override object Intercept(IMethodInvocation invocation)
        {
            // Skip caching for void methods
            if (invocation.Method.ReturnType == typeof(void))
                return invocation.Proceed();

            // Generate cache key
            string cacheKey = _keyGenerator(invocation);

            // Try to get from cache
            if (_cacheProvider.TryGetValue(cacheKey, out object cachedResult))
            {
                return cachedResult;
            }

            // Execute method
            object result = invocation.Proceed();

            // Cache result
            if (result != null)
            {
                _cacheProvider.Set(cacheKey, result, _cacheDuration);
            }

            return result;
        }
    }

    /// <summary>
    /// Interface for cache providers
    /// </summary>
    public interface ICacheProvider
    {
        bool TryGetValue(string key, out object value);
        void Set(string key, object value, TimeSpan duration);
        void Remove(string key);
    }

    /// <summary>
    /// Simple in-memory implementation of ICacheProvider
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime Expiration { get; set; }
        }

        private readonly Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();
        private readonly object _lock = new object();

        public bool TryGetValue(string key, out object value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out CacheItem item))
                {
                    if (DateTime.UtcNow < item.Expiration)
                    {
                        value = item.Value;
                        return true;
                    }
                    else
                    {
                        _cache.Remove(key);
                    }
                }

                value = null;
                return false;
            }
        }

        public void Set(string key, object value, TimeSpan duration)
        {
            lock (_lock)
            {
                _cache[key] = new CacheItem
                {
                    Value = value,
                    Expiration = DateTime.UtcNow.Add(duration)
                };
            }
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }
        }
    }
}