using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace NexusLink.Dynamic.Expando
{
    /// <summary>
    /// Versión tipada de ExpandoObject que permite acceso estático y dinámico
    /// </summary>
    /// <typeparam name="T">Tipo base</typeparam>
    public class TypedExpando<T> : DynamicObject where T : class, new()
    {
        private readonly T _instance;
        private readonly Dictionary<string, object> _properties;

        /// <summary>
        /// Objeto subyacente
        /// </summary>
        public T Instance => _instance;

        /// <summary>
        /// Crea un nuevo TypedExpando con una instancia por defecto
        /// </summary>
        public TypedExpando()
        {
            _instance = new T();
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Crea un TypedExpando a partir de una instancia existente
        /// </summary>
        public TypedExpando(T instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Intenta obtener un miembro
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // Verificar propiedades dinámicas
            if (_properties.TryGetValue(binder.Name, out result))
            {
                return true;
            }

            // Verificar propiedades de la instancia
            PropertyInfo property = typeof(T).GetProperty(binder.Name);
            if (property != null)
            {
                result = property.GetValue(_instance);
                return true;
            }

            // Verificar campos de la instancia
            FieldInfo field = typeof(T).GetField(binder.Name);
            if (field != null)
            {
                result = field.GetValue(_instance);
                return true;
            }

            // Miembro no encontrado
            result = null;
            return false;
        }

        /// <summary>
        /// Intenta establecer un miembro
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // Verificar propiedades de la instancia
            PropertyInfo property = typeof(T).GetProperty(binder.Name);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_instance, value);
                return true;
            }

            // Verificar campos de la instancia
            FieldInfo field = typeof(T).GetField(binder.Name);
            if (field != null)
            {
                field.SetValue(_instance, value);
                return true;
            }

            // Establecer propiedad dinámica
            _properties[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Intenta invocar un método
        /// </summary>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            // Buscar el método en la instancia
            MethodInfo method = typeof(T).GetMethod(
                binder.Name,
                args.Select(a => a?.GetType() ?? typeof(object)).ToArray());

            if (method != null)
            {
                result = method.Invoke(_instance, args);
                return true;
            }

            // Verificar si es un delegado
            if (_properties.TryGetValue(binder.Name, out object value) && value is Delegate del)
            {
                result = del.DynamicInvoke(args);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Devuelve los nombres de los miembros dinámicos
        /// </summary>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            // Propiedades dinámicas
            foreach (string key in _properties.Keys)
            {
                yield return key;
            }

            // Propiedades y campos de la instancia
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                yield return property.Name;
            }

            foreach (FieldInfo field in typeof(T).GetFields())
            {
                yield return field.Name;
            }
        }

        /// <summary>
        /// Obtiene el valor de una propiedad dinámica
        /// </summary>
        public object GetDynamicProperty(string name)
        {
            return _properties.TryGetValue(name, out object value) ? value : null;
        }

        /// <summary>
        /// Establece el valor de una propiedad dinámica
        /// </summary>
        public void SetDynamicProperty(string name, object value)
        {
            _properties[name] = value;
        }

        /// <summary>
        /// Comprueba si existe una propiedad dinámica
        /// </summary>
        public bool HasDynamicProperty(string name)
        {
            return _properties.ContainsKey(name);
        }

        /// <summary>
        /// Elimina una propiedad dinámica
        /// </summary>
        public bool RemoveDynamicProperty(string name)
        {
            return _properties.Remove(name);
        }

        /// <summary>
        /// Obtiene todas las propiedades dinámicas
        /// </summary>
        public IReadOnlyDictionary<string, object> GetDynamicProperties()
        {
            return _properties;
        }

        /// <summary>
        /// Limpia todas las propiedades dinámicas
        /// </summary>
        public void ClearDynamicProperties()
        {
            _properties.Clear();
        }

        /// <summary>
        /// Convierte implícitamente a T
        /// </summary>
        public static implicit operator T(TypedExpando<T> expando)
        {
            return expando._instance;
        }
    }
}