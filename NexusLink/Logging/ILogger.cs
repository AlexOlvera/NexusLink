using System;

namespace NexusLink.Logging
{
    /// <summary>
    /// Interfaz para servicios de logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Registra un mensaje de depuración
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Debug(string message, params object[] args);

        /// <summary>
        /// Registra un mensaje de información
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Warning(string message, params object[] args);

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Error(string message, params object[] args);

        /// <summary>
        /// Registra un mensaje de error con excepción
        /// </summary>
        /// <param name="exception">Excepción</param>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Error(Exception exception, string message, params object[] args);

        /// <summary>
        /// Registra un mensaje crítico
        /// </summary>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Critical(string message, params object[] args);

        /// <summary>
        /// Registra un mensaje crítico con excepción
        /// </summary>
        /// <param name="exception">Excepción</param>
        /// <param name="message">Mensaje</param>
        /// <param name="args">Argumentos de formato</param>
        void Critical(Exception exception, string message, params object[] args);

        /// <summary>
        /// Inicia un ámbito de logging
        /// </summary>
        /// <typeparam name="TState">Tipo del estado</typeparam>
        /// <param name="state">Estado</param>
        /// <returns>Ámbito que se puede disponer</returns>
        IDisposable BeginScope<TState>(TState state);
    }
}