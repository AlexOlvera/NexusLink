using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using NexusLink.Logging;

namespace NexusLink.Core.Transactions
{
    /// <summary>
    /// Gestiona los niveles de aislamiento para transacciones
    /// </summary>
    public class IsolationLevelManager
    {
        private readonly ILogger _logger;
        private readonly AsyncLocal<IsolationLevel> _currentIsolationLevel = new AsyncLocal<IsolationLevel>();
        private readonly Dictionary<Type, IsolationLevel> _entityIsolationLevels;
        private readonly Dictionary<string, IsolationLevel> _operationIsolationLevels;

        public IsolationLevelManager(ILogger logger)
        {
            _logger = logger;
            _currentIsolationLevel.Value = IsolationLevel.ReadCommitted; // Nivel predeterminado
            _entityIsolationLevels = new Dictionary<Type, IsolationLevel>();
            _operationIsolationLevels = new Dictionary<string, IsolationLevel>(StringComparer.OrdinalIgnoreCase);

            // Configurar niveles predeterminados para operaciones comunes
            ConfigureDefaultLevels();
        }

        /// <summary>
        /// Obtiene o establece el nivel de aislamiento actual
        /// </summary>
        public IsolationLevel CurrentIsolationLevel
        {
            get => _currentIsolationLevel.Value;
            set => _currentIsolationLevel.Value = value;
        }

        /// <summary>
        /// Configura el nivel de aislamiento para un tipo de entidad específico
        /// </summary>
        public void ConfigureEntityIsolationLevel(Type entityType, IsolationLevel isolationLevel)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            _entityIsolationLevels[entityType] = isolationLevel;
            _logger.Debug($"Nivel de aislamiento para {entityType.Name} configurado como {isolationLevel}");
        }

        /// <summary>
        /// Configura el nivel de aislamiento para un tipo de entidad específico
        /// </summary>
        public void ConfigureEntityIsolationLevel<T>(IsolationLevel isolationLevel)
        {
            ConfigureEntityIsolationLevel(typeof(T), isolationLevel);
        }

        /// <summary>
        /// Configura el nivel de aislamiento para una operación específica
        /// </summary>
        public void ConfigureOperationIsolationLevel(string operationName, IsolationLevel isolationLevel)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("El nombre de la operación no puede estar vacío", nameof(operationName));
            }

            _operationIsolationLevels[operationName] = isolationLevel;
            _logger.Debug($"Nivel de aislamiento para operación '{operationName}' configurado como {isolationLevel}");
        }

        /// <summary>
        /// Obtiene el nivel de aislamiento para un tipo de entidad específico
        /// </summary>
        public IsolationLevel GetIsolationLevelForEntity(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (_entityIsolationLevels.TryGetValue(entityType, out IsolationLevel level))
            {
                return level;
            }

            // Si no hay configuración específica, usar el nivel predeterminado
            return CurrentIsolationLevel;
        }

        /// <summary>
        /// Obtiene el nivel de aislamiento para un tipo de entidad específico
        /// </summary>
        public IsolationLevel GetIsolationLevelForEntity<T>()
        {
            return GetIsolationLevelForEntity(typeof(T));
        }

        /// <summary>
        /// Obtiene el nivel de aislamiento para una operación específica
        /// </summary>
        public IsolationLevel GetIsolationLevelForOperation(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("El nombre de la operación no puede estar vacío", nameof(operationName));
            }

            if (_operationIsolationLevels.TryGetValue(operationName, out IsolationLevel level))
            {
                return level;
            }

            // Si no hay configuración específica, usar el nivel predeterminado
            return CurrentIsolationLevel;
        }

        /// <summary>
        /// Ejecuta una acción con un nivel de aislamiento específico
        /// </summary>
        public void ExecuteWithIsolationLevel(IsolationLevel isolationLevel, Action action)
        {
            IsolationLevel previousLevel = CurrentIsolationLevel;
            try
            {
                CurrentIsolationLevel = isolationLevel;
                _logger.Debug($"Cambiando nivel de aislamiento a {isolationLevel} temporalmente");
                action();
            }
            finally
            {
                CurrentIsolationLevel = previousLevel;
                _logger.Debug($"Restaurando nivel de aislamiento a {previousLevel}");
            }
        }

        /// <summary>
        /// Ejecuta una función con un nivel de aislamiento específico
        /// </summary>
        public T ExecuteWithIsolationLevel<T>(IsolationLevel isolationLevel, Func<T> func)
        {
            IsolationLevel previousLevel = CurrentIsolationLevel;
            try
            {
                CurrentIsolationLevel = isolationLevel;
                _logger.Debug($"Cambiando nivel de aislamiento a {isolationLevel} temporalmente");
                return func();
            }
            finally
            {
                CurrentIsolationLevel = previousLevel;
                _logger.Debug($"Restaurando nivel de aislamiento a {previousLevel}");
            }
        }

        /// <summary>
        /// Configura los niveles de aislamiento predeterminados para operaciones comunes
        /// </summary>
        private void ConfigureDefaultLevels()
        {
            // Operaciones de lectura con nivel menos restrictivo
            ConfigureOperationIsolationLevel("GetById", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("GetAll", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Find", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Search", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Count", IsolationLevel.ReadCommitted);

            // Operaciones de escritura con nivel más restrictivo
            ConfigureOperationIsolationLevel("Insert", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Update", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Delete", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Save", IsolationLevel.ReadCommitted);

            // Operaciones batch con nivel más restrictivo
            ConfigureOperationIsolationLevel("BulkInsert", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("BulkUpdate", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("BulkDelete", IsolationLevel.ReadCommitted);

            // Operaciones de reporting con nivel menos restrictivo
            ConfigureOperationIsolationLevel("Report", IsolationLevel.ReadCommitted);
            ConfigureOperationIsolationLevel("Export", IsolationLevel.ReadCommitted);
        }
    }
}