using System;
using NexusLink.AOP.Attributes;
using NexusLink.Context;
using NexusLink.Core.Connection;
using NexusLink.Logging;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspecto que maneja el enrutamiento de base de datos para métodos
    /// </summary>
    public class DatabaseRouterAspect : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;
        private readonly DatabaseSelector _databaseSelector;

        public DatabaseRouterAspect(ILogger logger, DatabaseSelector databaseSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseSelector = databaseSelector ?? throw new ArgumentNullException(nameof(databaseSelector));
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            // Buscar el atributo DatabaseAttribute
            var attribute = args.Method.GetCustomAttributes(typeof(DatabaseAttribute), true)
                .FirstOrDefault() as DatabaseAttribute;

            if (attribute == null)
            {
                return;
            }

            // Guardar la base de datos actual
            string previousDatabase = _databaseSelector.CurrentDatabaseName;
            args.SetArgument("PreviousDatabase", previousDatabase);

            // Establecer la base de datos según el atributo
            string targetDatabase = attribute.DatabaseName;

            // Si el nombre de la base de datos es un parámetro, obtenerlo
            if (attribute.IsDatabaseNameParameter && !string.IsNullOrEmpty(attribute.DatabaseNameParameter))
            {
                // Buscar el parámetro por nombre
                for (int i = 0; i < args.Method.GetParameters().Length; i++)
                {
                    var param = args.Method.GetParameters()[i];
                    if (param.Name == attribute.DatabaseNameParameter)
                    {
                        if (args.Arguments != null && args.Arguments.Count > i)
                        {
                            var paramValue = args.Arguments[i];
                            if (paramValue != null)
                            {
                                targetDatabase = paramValue.ToString();
                            }
                        }
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(targetDatabase))
            {
                _logger.LogDebug($"Cambiando base de datos a '{targetDatabase}' para método {args.Method.Name}");
                _databaseSelector.CurrentDatabaseName = targetDatabase;
            }
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            // Restaurar la base de datos anterior
            if (args.TryGetArgument("PreviousDatabase", out string previousDatabase))
            {
                _logger.LogDebug($"Restaurando base de datos a '{previousDatabase}' para método {args.Method.Name}");
                _databaseSelector.CurrentDatabaseName = previousDatabase;
            }
        }
    }
}
