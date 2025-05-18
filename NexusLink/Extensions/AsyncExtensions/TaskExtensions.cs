using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLink.Extensions.AsyncExtensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Ejecuta una colección de tareas en paralelo con un límite de concurrencia
        /// </summary>
        public static async Task ForEachAsync<T>(this IEnumerable<T> source,
            int maxDegreeOfParallelism, Func<T, Task> body)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (maxDegreeOfParallelism <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism),
                    "Max degree of parallelism must be greater than zero");

            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            foreach (var item in source)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await body(item);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Ejecuta una operación con reintentos
        /// </summary>
        public static async Task<T> WithRetryAsync<T>(this Func<Task<T>> operation,
            int retryCount, TimeSpan delay, Func<Exception, bool> retryPredicate = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount),
                    "Retry count must be greater than or equal to zero");

            Exception lastException = null;

            for (int retry = 0; retry <= retryCount; retry++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (retry < retryCount && (retryPredicate == null || retryPredicate(ex)))
                    {
                        await Task.Delay(delay);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            throw lastException;
        }

        /// <summary>
        /// Convierte un Task en Task<T> con un valor por defecto si la tarea se completa correctamente
        /// </summary>
        public static async Task<T> WithResult<T>(this Task task, T result)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            await task;
            return result;
        }

        /// <summary>
        /// Espera a que se complete una tarea con un timeout
        /// </summary>
        public static async Task<bool> WaitAsync(this Task task, TimeSpan timeout)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var delayTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, delayTask);

            return completedTask == task;
        }

        /// <summary>
        /// Procesa los resultados de varias tareas a medida que se completan
        /// </summary>
        public static async Task WhenEach<T>(this IEnumerable<Task<T>> tasks, Action<T> action)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var tasksCopy = tasks.ToList();

            while (tasksCopy.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasksCopy);
                tasksCopy.Remove(completedTask);
                action(await completedTask);
            }
        }

        /// <summary>
        /// Ejecuta una operación de forma segura, devolviendo un valor por defecto si falla
        /// </summary>
        public static async Task<T> SafeExecuteAsync<T>(this Task<T> task, T defaultValue = default(T))
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            try
            {
                return await task;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}