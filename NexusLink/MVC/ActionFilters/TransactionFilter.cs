using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using NexusLink.Core.Transactions;

namespace NexusLink.MVC.ActionFilters
{
    /// <summary>
    /// Filtro de acción que proporciona soporte transaccional para acciones MVC.
    /// Inicia una transacción antes de la acción y la confirma o revierte según el resultado.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TransactionFilterAttribute : Attribute, IAsyncActionFilter
    {
        private readonly IsolationLevel _isolationLevel;
        private readonly TransactionTimeout _timeout;

        /// <summary>
        /// Crea una nueva instancia del filtro con nivel de aislamiento predeterminado
        /// </summary>
        public TransactionFilterAttribute()
            : this(IsolationLevel.ReadCommitted, TransactionTimeout.Default)
        {
        }

        /// <summary>
        /// Crea una nueva instancia del filtro con nivel de aislamiento personalizado
        /// </summary>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        public TransactionFilterAttribute(IsolationLevel isolationLevel)
            : this(isolationLevel, TransactionTimeout.Default)
        {
        }

        /// <summary>
        /// Crea una nueva instancia del filtro con nivel de aislamiento y timeout personalizados
        /// </summary>
        /// <param name="isolationLevel">Nivel de aislamiento</param>
        /// <param name="timeout">Timeout en segundos</param>
        public TransactionFilterAttribute(IsolationLevel isolationLevel, TransactionTimeout timeout)
        {
            _isolationLevel = isolationLevel;
            _timeout = timeout;
        }

        /// <summary>
        /// Ejecuta el filtro
        /// </summary>
        /// <param name="context">Contexto de la acción</param>
        /// <param name="next">Delegado para el siguiente filtro</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Obtener el gestor de transacciones
            var transactionManager = (ITransactionManager)context.HttpContext.RequestServices
                .GetService(typeof(ITransactionManager));

            if (transactionManager == null)
            {
                // Si no hay gestor de transacciones, ejecutar la acción sin transacción
                await next();
                return;
            }

            // Iniciar la transacción
            using (var scope = transactionManager.BeginTransaction(_isolationLevel, _timeout))
            {
                // Ejecutar la acción
                var actionExecutedContext = await next();

                if (actionExecutedContext.Exception != null && !actionExecutedContext.ExceptionHandled)
                {
                    // Si hay una excepción no manejada, revertir la transacción
                    scope.Rollback();
                }
                else
                {
                    // Si todo va bien, confirmar la transacción
                    scope.Commit();
                }
            }
        }
    }
}