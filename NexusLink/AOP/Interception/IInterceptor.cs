using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NexusLink.AOP.Interception
{
    /// <summary>
    /// Define la interfaz base para todos los interceptores en NexusLink.
    /// Los interceptores permiten agregar comportamiento antes y después de la invocación de métodos.
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Intercepta una invocación de método y opcionalmente ejecuta lógica antes y después de la invocación.
        /// </summary>
        /// <param name="invocation">Información sobre la invocación del método.</param>
        /// <returns>El resultado de la invocación.</returns>
        object Intercept(IInvocation invocation);

        /// <summary>
        /// Intercepta una invocación asíncrona de método y opcionalmente ejecuta lógica antes y después de la invocación.
        /// </summary>
        /// <param name="invocation">Información sobre la invocación del método.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        Task<object> InterceptAsync(IInvocation invocation);

        /// <summary>
        /// Determina si este interceptor puede interceptar el método especificado.
        /// </summary>
        /// <param name="method">El método a evaluar.</param>
        /// <returns>Verdadero si este interceptor puede interceptar el método; de lo contrario, falso.</returns>
        bool CanIntercept(MethodInfo method);

        /// <summary>
        /// Obtiene la prioridad de ejecución de este interceptor.
        /// Los interceptores con mayor prioridad se ejecutan antes que los de menor prioridad.
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// Define la información sobre una invocación de método que está siendo interceptada.
    /// </summary>
    public interface IInvocation
    {
        /// <summary>
        /// Obtiene el objeto de destino en el que se invoca el método.
        /// </summary>
        object Target { get; }

        /// <summary>
        /// Obtiene el método que se está invocando.
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// Obtiene los argumentos pasados a la invocación del método.
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// Obtiene o establece el valor devuelto por la invocación del método.
        /// </summary>
        object ReturnValue { get; set; }

        /// <summary>
        /// Obtiene o establece la excepción generada durante la invocación del método.
        /// Si no se produce ninguna excepción, este valor es nulo.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Procede con la invocación original del método o el siguiente interceptor en la cadena.
        /// </summary>
        void Proceed();

        /// <summary>
        /// Procede con la invocación original asíncrona del método o el siguiente interceptor en la cadena.
        /// </summary>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        Task ProceedAsync();

        /// <summary>
        /// Obtiene un valor que indica si el método interceptado es asíncrono.
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Obtiene metadatos adicionales asociados con la invocación.
        /// </summary>
        IDictionary<string, object> Metadata { get; }
    }
}