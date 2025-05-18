// Archivo: NexusLink/Context/ThreadContextManager.cs
using System;
using System.Collections.Generic;
using System.Threading;

namespace NexusLink.Context
{
    public class ThreadContextManager<T>
    {
        private static ThreadLocal<T> _threadLocalData = new ThreadLocal<T>();
        private static Dictionary<string, ThreadLocal<object>> _namedContexts =
            new Dictionary<string, ThreadLocal<object>>();

        public static T Current
        {
            get => _threadLocalData.Value;
            set => _threadLocalData.Value = value;
        }

        public static void Set<TValue>(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            ThreadLocal<object> threadLocal;

            lock (_namedContexts)
            {
                if (!_namedContexts.TryGetValue(key, out threadLocal))
                {
                    threadLocal = new ThreadLocal<object>();
                    _namedContexts[key] = threadLocal;
                }
            }

            threadLocal.Value = value;
        }

        public static TValue Get<TValue>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            ThreadLocal<object> threadLocal;

            lock (_namedContexts)
            {
                if (!_namedContexts.TryGetValue(key, out threadLocal))
                {
                    return default(TValue);
                }
            }

            if (threadLocal.Value == null)
                return default(TValue);

            return (TValue)threadLocal.Value;
        }

        public static void Clear()
        {
            _threadLocalData.Value = default(T);

            lock (_namedContexts)
            {
                foreach (var threadLocal in _namedContexts.Values)
                {
                    threadLocal.Value = null;
                }
            }
        }
    }
}
