using NexusLink.Core.Transactions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class TransactionAttribute : ActionFilterAttribute
    {
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        private TransactionScope _transactionScope;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Inicia una transacción antes de ejecutar la acción
            var options = new TransactionOptions
            {
                IsolationLevel = this.IsolationLevel,
                Timeout = TimeSpan.FromSeconds(60)
            };

            _transactionScope = new TransactionScope(TransactionScopeOption.Required, options);

            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            try
            {
                // Completa la transacción si no hay excepciones
                if (filterContext.Exception == null)
                {
                    _transactionScope.Complete();
                }
            }
            finally
            {
                // Garantiza la disposición del scope
                _transactionScope.Dispose();
            }
        }
    }
}
