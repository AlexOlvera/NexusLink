using System;
using System.Collections.Generic;
using System.Reflection;
using NexusLink.Context;

namespace NexusLink.Data.MultiDb
{
    /// <summary>
    /// Enruta operaciones a bases de datos específicas basado en atributos o reglas
    /// </summary>
    public class DatabaseRouter
    {
        private readonly Dictionary<Type, string> _typeDbMappings;
        private readonly Dictionary<MethodInfo, string> _methodDbMappings;

        public DatabaseRouter()
        {
            _typeDbMappings = new Dictionary<Type, string>();
            _methodDbMappings = new Dictionary<MethodInfo, string>();
        }

        /// <summary>
        /// Registra una entidad para una base de datos específica
        /// </summary>
        public void RegisterEntity<T>(string databaseName)
        {
            _typeDbMappings[typeof(T)] = databaseName;
        }

        /// <summary>
        /// Registra un método para una base de datos específica
        /// </summary>
        public void RegisterMethod(MethodInfo method, string databaseName)
        {
            _methodDbMappings[method] = databaseName;
        }

        /// <summary>
        /// Determina la base de datos para un método específico
        /// </summary>
        public string GetDatabaseForMethod(MethodInfo method)
        {
            // Verificar mapeo explícito de método
            if (_methodDbMappings.TryGetValue(method, out string dbForMethod))
            {
                return dbForMethod;
            }

            // Verificar atributo de base de datos
            var dbAttr = method.GetCustomAttribute<DatabaseAttribute>();
            if (dbAttr != null)
            {
                return dbAttr.DatabaseName;
            }

            // Verificar mapeo de entidad del tipo de retorno
            Type returnType = method.ReturnType;
            if (returnType.IsGenericType)
            {
                returnType = returnType.GetGenericArguments()[0];
            }

            if (_typeDbMappings.TryGetValue(returnType, out string dbForType))
            {
                return dbForType;
            }

            // Verificar atributo en el tipo de retorno
            dbAttr = returnType.GetCustomAttribute<DatabaseAttribute>();
            if (dbAttr != null)
            {
                return dbAttr.DatabaseName;
            }

            // Usar la base de datos actual
            return DatabaseContext.Current.CurrentDatabaseName;
        }

        /// <summary>
        /// Determina la base de datos para un tipo específico
        /// </summary>
        public string GetDatabaseForType(Type type)
        {
            // Verificar mapeo explícito
            if (_typeDbMappings.TryGetValue(type, out string dbName))
            {
                return dbName;
            }

            // Verificar atributo
            var dbAttr = type.GetCustomAttribute<DatabaseAttribute>();
            if (dbAttr != null)
            {
                return dbAttr.DatabaseName;
            }

            // Usar la base de datos actual
            return DatabaseContext.Current.CurrentDatabaseName;
        }
    }
}