using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NexusLink.Dynamic.DynamicProxy
{
    /// <summary>
    /// Caché de tipos proxy generados dinámicamente.
    /// Mejora el rendimiento almacenando tipos generados para reutilizarlos.
    /// </summary>
    public class ProxyCache
    {
        private readonly ConcurrentDictionary<Type, Type> _proxyTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Obtiene un tipo proxy almacenado en caché
        /// </summary>
        /// <param name="baseType">Tipo base para el que se generó el proxy</param>
        /// <returns>Tipo proxy en caché o null si no existe</returns>
        public Type GetProxyType(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            _proxyTypes.TryGetValue(baseType, out Type proxyType);
            return proxyType;
        }

        /// <summary>
        /// Almacena un tipo proxy en caché
        /// </summary>
        /// <param name="baseType">Tipo base para el que se generó el proxy</param>
        /// <param name="proxyType">Tipo proxy generado</param>
        public void CacheProxyType(Type baseType, Type proxyType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (proxyType == null)
                throw new ArgumentNullException(nameof(proxyType));

            _proxyTypes.TryAdd(baseType, proxyType);
        }

        /// <summary>
        /// Elimina un tipo proxy de la caché
        /// </summary>
        /// <param name="baseType">Tipo base para el que se generó el proxy</param>
        /// <returns>True si se eliminó el tipo, false si no existía en caché</returns>
        public bool RemoveProxyType(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            return _proxyTypes.TryRemove(baseType, out _);
        }

        /// <summary>
        /// Limpia la caché de tipos proxy
        /// </summary>
        public void Clear()
        {
            _proxyTypes.Clear();
        }

        /// <summary>
        /// Comprueba si un tipo proxy está en caché
        /// </summary>
        /// <param name="baseType">Tipo base para el que se generó el proxy</param>
        /// <returns>True si el tipo está en caché, false en caso contrario</returns>
        public bool ContainsProxyType(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            return _proxyTypes.ContainsKey(baseType);
        }

        /// <summary>
        /// Obtiene la cantidad de tipos proxy en caché
        /// </summary>
        public int Count => _proxyTypes.Count;

        /// <summary>
        /// Obtiene o crea un tipo proxy, almacenándolo en caché si es creado
        /// </summary>
        /// <param name="baseType">Tipo base para el que se generó el proxy</param>
        /// <param name="proxyFactory">Función para crear el tipo proxy si no está en caché</param>
        /// <returns>Tipo proxy en caché o creado</returns>
        public Type GetOrCreateProxyType(Type baseType, Func<Type, Type> proxyFactory)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (proxyFactory == null)
                throw new ArgumentNullException(nameof(proxyFactory));

            return _proxyTypes.GetOrAdd(baseType, type => proxyFactory(type));
        }

        /// <summary>
        /// Verifica si el tipo proporcionado es un tipo proxy generado
        /// </summary>
        /// <param name="type">Tipo a verificar</param>
        /// <returns>True si el tipo es un proxy generado, false en caso contrario</returns>
        public bool IsProxyType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return _proxyTypes.Values.Contains(type);
        }

        /// <summary>
        /// Intenta obtener el tipo base para un tipo proxy
        /// </summary>
        /// <param name="proxyType">Tipo proxy generado</param>
        /// <param name="baseType">Tipo base (salida)</param>
        /// <returns>True si se encontró el tipo base, false en caso contrario</returns>
        public bool TryGetBaseType(Type proxyType, out Type baseType)
        {
            if (proxyType == null)
                throw new ArgumentNullException(nameof(proxyType));

            foreach (var pair in _proxyTypes)
            {
                if (pair.Value == proxyType)
                {
                    baseType = pair.Key;
                    return true;
                }
            }

            baseType = null;
            return false;
        }

        /// <summary>
        /// Enumera todos los tipos base para los que se han generado proxies
        /// </summary>
        /// <returns>Enumeración de tipos base</returns>
        public IEnumerable<Type> GetBaseTypes()
        {
            return _proxyTypes.Keys;
        }

        /// <summary>
        /// Enumera todos los tipos proxy generados
        /// </summary>
        /// <returns>Enumeración de tipos proxy</returns>
        public IEnumerable<Type> GetProxyTypes()
        {
            return _proxyTypes.Values;
        }

        /// <summary>
        /// Enumera todos los pares de tipo base y tipo proxy
        /// </summary>
        /// <returns>Enumeración de pares (tipo base, tipo proxy)</returns>
        public IEnumerable<KeyValuePair<Type, Type>> GetProxyTypePairs()
        {
            return _proxyTypes;
        }
    }
}