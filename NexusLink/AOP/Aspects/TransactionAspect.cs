using System;
using System.Data.SqlClient;
using System.Transactions;
using NexusLink.AOP.Attributes;
using NexusLink.Core.Transactions;
using NexusLink.Logging;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspecto que maneja transacciones automáticamente para métodos
    /// </summary>
    public class TransactionAspect : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;
        private readonly IsolationLevel _isolationLevel;
        private readonly TransactionScopeOption _scopeOption;
        private readonly TimeSpan _timeout;

        public TransactionAspect(ILogger logger,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            int timeoutSeconds = 60)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isolationLevel = isolationLevel;
            _scopeOption = scopeOption;
            _timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            var attribute = args.Method.GetCustomAttributes(typeof(TransactionalAttribute), true)
                .FirstOrDefault() as TransactionalAttribute;

            // Usar valores de atributo si están disponibles, o los predeterminados
            var isolationLevel = attribute?.IsolationLevel ?? _isolationLevel;
            var scopeOption = attribute?.ScopeOption ?? _scopeOption;
            var timeout = attribute?.TimeoutSeconds > 0
                ? TimeSpan.FromSeconds(attribute.TimeoutSeconds)
                : _timeout;

            _logger.LogDebug($"Iniciando transacción para método {args.Method.Name} " +
                $"con aislamiento {isolationLevel}, opción {scopeOption} y timeout {timeout.TotalSeconds}s");

            // Crear opciones de transacción
            var options = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout
            };

            // Crear y guardar el TransactionScope en los argumentos
            var scope = new TransactionScope(scopeOption, options);
            args.SetArgument("TransactionScope", scope);
        }

        public override void OnSuccess(MethodExecutionArgs args)
        {
            _logger.LogDebug($"Método {args.Method.Name} completado con éxito, confirmando transacción");

            // Obtener y completar el TransactionScope
            if (args.TryGetArgument("TransactionScope", out TransactionScope scope))
            {
                scope.Complete();
            }
        }

        public override void OnException(MethodExecutionArgs args)
        {
            _logger.LogWarning($"Excepción en método {args.Method.Name}: {args.Exception.Message}. Revertiendo transacción");

            // No llamamos a Complete(), lo que provocará un rollback automático
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            // Obtener y disponer el TransactionScope
            if (args.TryGetArgument("TransactionScope", out TransactionScope scope))
            {
                scope.Dispose();
                _logger.LogDebug($"Transacción de método {args.Method.Name} finalizada");
            }
        }
    }
}