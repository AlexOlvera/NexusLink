using System;
using System.Collections.Generic;
using System.Threading;

namespace NexusLink.Context
{
    public class AsyncLocalContext<T>
    {
        private static AsyncLocal<T> _asyncLocalData = new AsyncLocal<T>();
        private static Dictionary<string, AsyncLocal<object>> _namedContexts =
            new Dictionary<string, AsyncLocal<object>>();

        public static T Current
        {
            get => _asyncLocalData.Value;
            set => _asyncLocalData.Value = value;
        }

        public static void Set<TValue>(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            AsyncLocal<object> asyncLocal;

            lock (_namedContexts)
            {
                if (!_namedContexts.TryGetValue(key, out asyncLocal))
                {
                    asyncLocal = new AsyncLocal<object>();
                    _namedContexts[key] = asyncLocal;
                }
            }

            asyncLocal.Value = value;
        }

        public static TValue Get<TValue>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            AsyncLocal<object> asyncLocal;

            lock (_namedContexts)
            {
                if (!_namedContexts.TryGetValue(key, out asyncLocal))
                {
                    return default(TValue);
                }
            }

            if (asyncLocal.Value == null)
                return default(TValue);

            return (TValue)asyncLocal.Value;
        }

        public static void Clear()
        {
            _asyncLocalData.Value = default(T);

            lock (_namedContexts)
            {
                foreach (var asyncLocal in _namedContexts.Values)
                {
                    asyncLocal.Value = null;
                }
            }
        }
    }
}