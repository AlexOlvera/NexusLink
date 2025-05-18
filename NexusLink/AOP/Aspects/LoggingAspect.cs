using NexusLink.AOP.Interception;
using NexusLink.Logging;
using System;
using System.Reflection;
using System.Text;

namespace NexusLink.AOP.Aspects
{
    /// <summary>
    /// Aspect that provides logging capabilities for method execution
    /// </summary>
    public class LoggingAspect : MethodInterceptor
    {
        private readonly ILogger _logger;
        private readonly LogLevel _entryLevel;
        private readonly LogLevel _exitLevel;
        private readonly LogLevel _exceptionLevel;
        private readonly bool _logParameters;
        private readonly bool _logReturnValue;

        public LoggingAspect(ILogger logger,
                            LogLevel entryLevel = LogLevel.Debug,
                            LogLevel exitLevel = LogLevel.Debug,
                            LogLevel exceptionLevel = LogLevel.Error,
                            bool logParameters = true,
                            bool logReturnValue = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entryLevel = entryLevel;
            _exitLevel = exitLevel;
            _exceptionLevel = exceptionLevel;
            _logParameters = logParameters;
            _logReturnValue = logReturnValue;
        }

        protected override void BeforeInvocation(IMethodInvocation invocation)
        {
            if (!_logger.IsEnabled(_entryLevel))
                return;

            string methodName = GetMethodSignature(invocation);
            string message = $"Entering method: {methodName}";

            if (_logParameters && invocation.Arguments?.Length > 0)
            {
                StringBuilder paramLog = new StringBuilder();
                ParameterInfo[] parameters = invocation.Method.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) paramLog.Append(", ");

                    paramLog.Append(parameters[i].Name).Append("=");

                    if (invocation.Arguments[i] == null)
                    {
                        paramLog.Append("null");
                    }
                    else if (IsSensitiveParameter(parameters[i]))
                    {
                        paramLog.Append("***");
                    }
                    else
                    {
                        paramLog.Append(invocation.Arguments[i]);
                    }
                }

                message += $" with parameters: {paramLog}";
            }

            _logger.Log(_entryLevel, message);
        }

        protected override void AfterInvocation(IMethodInvocation invocation, object result)
        {
            if (!_logger.IsEnabled(_exitLevel))
                return;

            string methodName = GetMethodSignature(invocation);
            string message = $"Exiting method: {methodName}";

            if (_logReturnValue && invocation.Method.ReturnType != typeof(void))
            {
                string resultValue = result != null ? result.ToString() : "null";
                message += $" with result: {resultValue}";
            }

            _logger.Log(_exitLevel, message);
        }

        protected override object OnException(IMethodInvocation invocation, Exception ex)
        {
            if (_logger.IsEnabled(_exceptionLevel))
            {
                string methodName = GetMethodSignature(invocation);
                _logger.Log(_exceptionLevel, ex, $"Exception in method: {methodName}");
            }

            throw ex;
        }

        private string GetMethodSignature(IMethodInvocation invocation)
        {
            MethodInfo method = invocation.Method;
            return $"{method.DeclaringType.Name}.{method.Name}";
        }

        private bool IsSensitiveParameter(ParameterInfo parameter)
        {
            // Check if parameter has sensitive data attributes or naming conventions
            string name = parameter.Name.ToLowerInvariant();
            return name.Contains("password") || name.Contains("secret") ||
                   name.Contains("key") || name.Contains("token");
        }
    }
}