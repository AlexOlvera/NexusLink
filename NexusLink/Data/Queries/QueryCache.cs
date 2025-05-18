using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NexusLink.Data.Queries
{
    public class QueryCache
    {
        private readonly MemoryCache _cache;
        private readonly int _defaultExpirationSeconds;

        public QueryCache(int defaultExpirationSeconds = 60)
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _defaultExpirationSeconds = defaultExpirationSeconds;
        }

        public T Get<T>(string cacheKey, Func<T> dataRetriever, int? expirationSeconds = null)
        {
            if (_cache.TryGetValue(cacheKey, out T cachedResult))
            {
                return cachedResult;
            }

            // Obtener datos
            T result = dataRetriever();

            // Almacenar en caché
            var expiration = TimeSpan.FromSeconds(expirationSeconds ?? _defaultExpirationSeconds);
            _cache.Set(cacheKey, result, expiration);

            return result;
        }

        public async Task<T> GetAsync<T>(string cacheKey, Func<Task<T>> dataRetriever, int? expirationSeconds = null)
        {
            if (_cache.TryGetValue(cacheKey, out T cachedResult))
            {
                return cachedResult;
            }

            // Obtener datos asincrónicamente
            T result = await dataRetriever();

            // Almacenar en caché
            var expiration = TimeSpan.FromSeconds(expirationSeconds ?? _defaultExpirationSeconds);
            _cache.Set(cacheKey, result, expiration);

            return result;
        }

        public void Invalidate(string cacheKey)
        {
            _cache.Remove(cacheKey);
        }

        public void InvalidatePattern(string pattern)
        {
            // Obtener todas las claves que coinciden con el patrón
            var keysToRemove = _cache.GetKeys<string>()
                .Where(k => Regex.IsMatch(k, pattern))
                .ToList();

            // Eliminar todas las claves coincidentes
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }
    }
}
