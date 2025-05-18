using System;
using System.Security.Principal;
using System.Threading;
using NexusLink.AOP.Attributes;
using NexusLink.Logging;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspecto que maneja la autorización para métodos
    /// </summary>
    public class AuthorizationAspect : OnMethodBoundaryAspect
    {
        private readonly ILogger _logger;

        public AuthorizationAspect(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            // Obtener la identidad actual
            IPrincipal principal = Thread.CurrentPrincipal;
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Intento de acceso no autorizado: usuario no autenticado");
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            // Verificar los roles requeridos
            var attributes = args.Method.GetCustomAttributes(typeof(RequireRoleAttribute), true);
            foreach (RequireRoleAttribute roleAttr in attributes)
            {
                if (!string.IsNullOrEmpty(roleAttr.RoleName) && !principal.IsInRole(roleAttr.RoleName))
                {
                    _logger.LogWarning($"Acceso denegado: usuario {principal.Identity.Name} no tiene el rol {roleAttr.RoleName}");
                    throw new UnauthorizedAccessException($"Se requiere el rol {roleAttr.RoleName}");
                }
            }

            _logger.LogDebug($"Autorización concedida a {principal.Identity.Name} para método {args.Method.Name}");
        }
    }

    /// <summary>
    /// Atributo para requerir un rol específico
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequireRoleAttribute : Attribute
    {
        public string RoleName { get; }

        public RequireRoleAttribute(string roleName)
        {
            RoleName = roleName;
        }
    }
}