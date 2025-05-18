using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace NexusLink.Dynamic.Expando
{
    /// <summary>
    /// Versión mejorada de ExpandoObject con funcionalidades adicionales
    /// </summary>
    public class ExpandoObject : DynamicObject, IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _properties;
        private readonly object _instance;
        private readonly Type _instanceType;

        /// <summary>
        /// Crea un nuevo ExpandoObject vacío
        /// </summary>
        public ExpandoObject()
        {
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Crea un ExpandoObject que envuelve un objeto existente
        /// </summary>
        public ExpandoObject(object instance)
        {
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _instance = instance;
            _instanceType = instance?.GetType();
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

            // Verificar propiedades de instancia
            if (_instance != null)
            {
                try
                {
                    PropertyInfo property = _instanceType.GetProperty(binder.Name);
                    if (property != null)
                    {
                        result = property.GetValue(_instance);
                        return true;
                    }

                    FieldInfo field = _instanceType.GetField(binder.Name);
                    if (field != null)
                    {
                        result = field.GetValue(_instance);
                        return true;
                    }
                }
                catch
                {
                    // Ignorar errores de acceso
                }
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
            // Verificar propiedades de instancia
            if (_instance != null)
            {
                try
                {
                    PropertyInfo property = _instanceType.GetProperty(binder.Name);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(_instance, value);
                        return true;
                    }

                    FieldInfo field = _instanceType.GetField(binder.Name);
                    if (field != null)
                    {
                        field.SetValue(_instance, value);
                        return true;
                    }
                }
                catch
                {
                    // Ignorar errores de acceso
                }
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
            // Verificar si es un método de instancia
            if (_instance != null)
            {
                try
                {
                    MethodInfo method = _instanceType.GetMethod(
                        binder.Name,
                        args.Select(a => a?.GetType() ?? typeof(object)).ToArray());

                    if (method != null)
                    {
                        result = method.Invoke(_instance, args);
                        return true;
                    }
                }
                catch
                {
                    // Ignorar errores de acceso
                }
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

            // Propiedades y métodos de instancia
            if (_instance != null)
            {
                foreach (PropertyInfo property in _instanceType.GetProperties())
                {
                    yield return property.Name;
                }

                foreach (MethodInfo method in _instanceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!method.IsSpecialName)
                    {
                        yield return method.Name;
                    }
                }
            }
        }

        #region IDictionary<string, object> Implementation

        public object this[string key]
        {
            get
            {
                if (_properties.TryGetValue(key, out object value))
                {
                    return value;
                }

                if (_instance != null)
                {
                    try
                    {
                        PropertyInfo property = _instanceType.GetProperty(key);
                        if (property != null)
                        {
                            return property.GetValue(_instance);
                        }

                        FieldInfo field = _instanceType.GetField(key);
                        if (field != null)
                        {
                            return field.GetValue(_instance);
                        }
                    }
                    catch
                    {
                        // Ignorar errores de acceso
                    }
                }

                throw new KeyNotFoundException($"The property '{key}' does not exist");
            }
            set
            {
                if (_instance != null)
                {
                    try
                    {
                        PropertyInfo property = _instanceType.GetProperty(key);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(_instance, value);
                            return;
                        }

                        FieldInfo field = _instanceType.GetField(key);
                        if (field != null)
                        {
                            field.SetValue(_instance, value);
                            return;
                        }
                    }
                    catch
                    {
                        // Ignorar errores de acceso
                    }
                }

                _properties[key] = value;
            }
        }

        public ICollection<string> Keys =>
            GetDynamicMemberNames().ToList();

        public ICollection<object> Values
        {
            get
            {
                var values = new List<object>();
                foreach (string key in Keys)
                {
                    values.Add(this[key]);
                }
                return values;
            }
        }

        public int Count => Keys.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException($"An item with the key '{key}' already exists");
            }

            this[key] = value;
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _properties.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ContainsKey(item.Key) && this[item.Key].Equals(item.Value);
        }

        public bool ContainsKey(string key)
        {
            if (_properties.ContainsKey(key))
            {
                return true;
            }

            if (_instance != null)
            {
                PropertyInfo property = _instanceType.GetProperty(key);
                if (property != null)
                {
                    return true;
                }

                FieldInfo field = _instanceType.GetField(key);
                if (field != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The number of elements in the source is greater than the available space in the array.");
            }

            int i = arrayIndex;
            foreach (var item in this)
            {
                array[i++] = item;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (string key in Keys)
            {
                yield return new KeyValuePair<string, object>(key, this[key]);
            }
        }

        public bool Remove(string key)
        {
            return _properties.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}