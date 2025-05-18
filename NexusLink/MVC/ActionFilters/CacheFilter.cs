using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace NexusLink.MVC.ActionFilters
{
    /// <summary>
    /// Filtro de acción que proporciona caché de resultados para acciones MVC.
    /// Almacena los resultados en caché y los reutiliza en solicitudes posteriores.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CacheFilterAttribute : Attribute, IAsyncActionFilter, IAsyncResultFilter
    {
        private readonly int _duration;
        private readonly bool _varyByUser;
        private readonly bool _varyByParams;
        private readonly string[] _varyByHeaders;
        private readonly string[] _cacheProfileProperties;

        /// <summary>
        /// Crea una nueva instancia del filtro con duración predeterminada
        /// </summary>
        public CacheFilterAttribute()
            : this(60, false, true, null, null)
        {
        }

        /// <summary>
        /// Crea una nueva instancia del filtro con duración personalizada
        /// </summary>
        /// <param name="duration">Duración en segundos</param>
        public CacheFilterAttribute(int duration)
            : this(duration, false, true, null, null)
        {
        }

        /// <summary>
        /// Crea una nueva instancia del filtro con opciones personalizadas
        /// </summary>
        /// <param name="duration">Duración en segundos</param>
        /// <param name="varyByUser">True para variar por usuario</param>
        /// <param name="varyByParams">True para variar por parámetros</param>
        /// <param name="varyByHeaders">Encabezados para variar</param>
        /// <param name="cacheProfileProperties">Propiedades del perfil de caché</param>
        public CacheFilterAttribute(
            int duration,
            bool varyByUser,
            bool varyByParams,
            string[] varyByHeaders,
            string[] cacheProfileProperties)
        {
            _duration = duration;
            _varyByUser = varyByUser;
            _varyByParams = varyByParams;
            _varyByHeaders = varyByHeaders ?? Array.Empty<string>();
            _cacheProfileProperties = cacheProfileProperties ?? Array.Empty<string>();
        }

        /// <summary>
        /// Ejecuta el filtro de acción
        /// </summary>
        /// <param name="context">Contexto de la acción</param>
        /// <param name="next">Delegado para el siguiente filtro</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Obtener la caché
            var cache = (IMemoryCache)context.HttpContext.RequestServices.GetService(typeof(IMemoryCache));

            if (cache == null)
            {
                // Si no hay caché, ejecutar la acción sin cacheado
                await next();
                return;
            }

            // Generar clave de caché
            string cacheKey = GenerateCacheKey(context);

            // Intentar obtener resultado de la caché
            if (cache.TryGetValue(cacheKey, out ActionExecutedContext cachedResult))
            {
                // Si hay resultado en caché, reutilizarlo
                context.Result = cachedResult.Result;
                return;
            }

            // Ejecutar la acción
            var executedContext = await next();

            // Guardar resultado en caché si no hay errores
            if (executedContext.Exception == null && executedContext.Result != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_duration));

                cache.Set(cacheKey, executedContext, cacheEntryOptions);
            }
        }

        /// <summary>
        /// Ejecuta el filtro de resultado
        /// </summary>
        /// <param name="context">Contexto del resultado</param>
        /// <param name="next">Delegado para el siguiente filtro</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Establecer encabezados de caché
            SetCacheHeaders(context.HttpContext.Response, _duration);

            await next();
        }

        /// <summary>
        /// Genera una clave de caché única para la acción
        /// </summary>
        /// <param name="context">Contexto de la acción</param>
        /// <returns>Clave de caché</returns>
        private string GenerateCacheKey(ActionExecutingContext context)
        {
            var keyBuilder = new StringBuilder();

            // Agregar nombre del controlador y la acción
            keyBuilder.Append($"{context.Controller.GetType().Name}_{context.ActionDescriptor.DisplayName}");

            // Agregar identificador de usuario si es necesario
            if (_varyByUser && context.HttpContext.User.Identity.IsAuthenticated)
            {
                keyBuilder.Append($"_User_{context.HttpContext.User.Identity.Name}");
            }

            // Agregar parámetros si es necesario
            if (_varyByParams)
            {
                foreach (var parameter in context.ActionArguments)
                {
                    keyBuilder.Append($"_{parameter.Key}_{parameter.Value?.ToString() ?? "null"}");
                }
            }

            // Agregar encabezados si es necesario
            foreach (var header in _varyByHeaders)
            {
                if (context.HttpContext.Request.Headers.TryGetValue(header, out StringValues values))
                {
                    keyBuilder.Append($"_{header}_{string.Join("_", values)}");
                }
            }

            // Agregar propiedades del perfil de caché si es necesario
            foreach (var property in _cacheProfileProperties)
            {
                if (TryGetPropertyValue(context.Controller, property, out object value))
                {
                    keyBuilder.Append($"_{property}_{value?.ToString() ?? "null"}");
                }
            }

            return keyBuilder.ToString();
        }

        /// <summary>
        /// Establece encabezados de caché en la respuesta
        /// </summary>
        /// <param name="response">Respuesta HTTP</param>
        /// <param name="duration">Duración en segundos</param>
        private void SetCacheHeaders(HttpResponse response, int duration)
        {
            // Establecer encabezados de caché
            response.Headers.Add("Cache-Control", $"public, max-age={duration}");
            response.Headers.Add("Expires", DateTime.UtcNow.AddSeconds(duration).ToString("R"));
            response.Headers.Add("Vary", GetVaryHeaderValue());
        }

        /// <summary>
        /// Obtiene el valor para el encabezado Vary
        /// </summary>
        /// <returns>Valor del encabezado Vary</returns>
        private string GetVaryHeaderValue()
        {
            var varyBuilder = new StringBuilder();

            if (_varyByUser)
            {
                varyBuilder.Append("Authorization");
            }

            foreach (var header in _varyByHeaders)
            {
                if (varyBuilder.Length > 0)
                {
                    varyBuilder.Append(", ");
                }

                varyBuilder.Append(header);
            }

            return varyBuilder.ToString();
        }

        /// <summary>
        /// Intenta obtener el valor de una propiedad
        /// </summary>
        /// <param name="obj">Objeto</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <param name="value">Valor de la propiedad (salida)</param>
        /// <returns>True si se pudo obtener, false en caso contrario</returns>
        private bool TryGetPropertyValue(object obj, string propertyName, out object value)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
            {
                value = null;
                return false;
            }

            try
            {
                var property = obj.GetType().GetProperty(propertyName);

                if (property != null)
                {
                    value = property.GetValue(obj);
                    return true;
                }
            }
            catch
            {
                // Ignorar errores
            }

            value = null;
            return false;
        }
    }
}