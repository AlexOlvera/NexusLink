using System;
using System.Diagnostics;
using NexusLink.AOP.Attributes;
using NexusLink.Logging;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspecto que mide el rendimiento de métodos
    /// </summary>
    public class PerformanceAspect : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;
        private readonly int _warningThresholdMs;

        public PerformanceAspect(ILogger logger, int warningThresholdMs = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _warningThresholdMs = warningThresholdMs;
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            // Crear y guardar un cronómetro
            var stopwatch = Stopwatch.StartNew();
            args.SetArgument("Stopwatch", stopwatch);

            _logger.LogDebug($"Iniciando medición de rendimiento para método {args.Method.Name}");
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            // Recuperar el cronómetro
            if (args.TryGetArgument("Stopwatch", out Stopwatch stopwatch))
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                var methodName = args.Method.DeclaringType.FullName + "." + args.Method.Name;

                if (elapsedMs > _warningThresholdMs)
                {
                    _logger.LogWarning($"¡Rendimiento lento! Método {methodName} tomó {elapsedMs}ms (umbral: {_warningThresholdMs}ms)");
                }
                else
                {
                    _logger.LogDebug($"Método {methodName} completado en {elapsedMs}ms");
                }
            }
        }
    }
}
