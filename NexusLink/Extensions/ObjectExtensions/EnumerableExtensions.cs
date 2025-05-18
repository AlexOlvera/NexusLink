using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusLink.Extensions.ObjectExtensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Verifica si una colección es nula o está vacía
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        /// <summary>
        /// Ejecuta una acción por cada elemento de la colección
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Ejecuta una acción por cada elemento de la colección con un índice
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            int index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }

        /// <summary>
        /// Divide una colección en lotes de tamaño especificado
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return GetBatch(enumerator, batchSize);
                }
            }
        }

        private static IEnumerable<T> GetBatch<T>(IEnumerator<T> enumerator, int size)
        {
            yield return enumerator.Current;

            for (int i = 1; i < size && enumerator.MoveNext(); i++)
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// Devuelve una colección vacía si la fuente es nula
        /// </summary>
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Obtiene elementos distintos basados en una clave
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                var key = keySelector(element);
                if (seenKeys.Add(key))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// Compara dos secuencias para determinar si son iguales
        /// </summary>
        public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
        {
            if (first == null || second == null)
                return first == second;

            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            using (var e1 = first.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!e2.MoveNext() || !comparer(e1.Current, e2.Current))
                        return false;
                }

                return !e2.MoveNext();
            }
        }
    }
}
