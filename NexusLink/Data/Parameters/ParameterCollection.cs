using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using NexusLink.Core.Connection;

namespace NexusLink.Data.Parameters
{
    /// <summary>
    /// Colección optimizada de parámetros SQL
    /// </summary>
    public class ParameterCollection : IEnumerable<DbParameter>
    {
        private readonly List<DbParameter> _parameters;
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// Inicializa una nueva instancia de ParameterCollection
        /// </summary>
        public ParameterCollection(ConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _parameters = new List<DbParameter>();
        }

        /// <summary>
        /// Obtiene el número de parámetros en la colección
        /// </summary>
        public int Count => _parameters.Count;

        /// <summary>
        /// Obtiene o establece un parámetro por índice
        /// </summary>
        public DbParameter this[int index]
        {
            get
            {
                if (index < 0 || index >= _parameters.Count)
                {
                    throw new IndexOutOfRangeException("Índice fuera de rango");
                }

                return _parameters[index];
            }
            set
            {
                if (index < 0 || index >= _parameters.Count)
                {
                    throw new IndexOutOfRangeException("Índice fuera de rango");
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _parameters[index] = value;
            }
        }

        /// <summary>
        /// Obtiene o establece un parámetro por nombre
        /// </summary>
        public DbParameter this[string name]
        {
            get
            {
                var parameter = _parameters.FirstOrDefault(p =>
                    p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    p.ParameterName.Equals("@" + name, StringComparison.OrdinalIgnoreCase));

                if (parameter == null)
                {
                    throw new ArgumentException($"Parámetro no encontrado: {name}");
                }

                return parameter;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int index = _parameters.FindIndex(p =>
                    p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    p.ParameterName.Equals("@" + name, StringComparison.OrdinalIgnoreCase));

                if (index >= 0)
                {
                    _parameters[index] = value;
                }
                else
                {
                    throw new ArgumentException($"Parámetro no encontrado: {name}");
                }
            }
        }

        /// <summary>
        /// Agrega un parámetro a la colección
        /// </summary>
        public void Add(DbParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            // Asegurarse de que el nombre empiece con @
            if (!parameter.ParameterName.StartsWith("@"))
            {
                parameter.ParameterName = "@" + parameter.ParameterName;
            }

            _parameters.Add(parameter);
        }

        /// <summary>
        /// Agrega un parámetro a la colección con el nombre y valor especificados
        /// </summary>
        public void Add(string name, object value)
        {
            var parameter = _connectionFactory.CreateParameter();

            // Asegurarse de que el nombre empiece con @
            if (!name.StartsWith("@"))
            {
                parameter.ParameterName = "@" + name;
            }
            else
            {
                parameter.ParameterName = name;
            }

            parameter.Value = value ?? DBNull.Value;

            _parameters.Add(parameter);
        }

        /// <summary>
        /// Agrega un parámetro a la colección con el nombre, valor y tipo especificados
        /// </summary>
        public void Add(string name, object value, DbType dbType)
        {
            var parameter = _connectionFactory.CreateParameter();

            // Asegurarse de que el nombre empiece con @
            if (!name.StartsWith("@"))
            {
                parameter.ParameterName = "@" + name;
            }
            else
            {
                parameter.ParameterName = name;
            }

            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;

            _parameters.Add(parameter);
        }

        /// <summary>
        /// Agrega un parámetro a la colección con el nombre, valor, tipo y dirección especificados
        /// </summary>
        public void Add(string name, object value, DbType dbType, ParameterDirection direction)
        {
            var parameter = _connectionFactory.CreateParameter();

            // Asegurarse de que el nombre empiece con @
            if (!name.StartsWith("@"))
            {
                parameter.ParameterName = "@" + name;
            }
            else
            {
                parameter.ParameterName = name;
            }

            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = direction;

            _parameters.Add(parameter);
        }

        /// <summary>
        /// Agrega un parámetro a la colección con el nombre, valor, tipo, dirección y tamaño especificados
        /// </summary>
        public void Add(string name, object value, DbType dbType, ParameterDirection direction, int size)
        {
            var parameter = _connectionFactory.CreateParameter();

            // Asegurarse de que el nombre empiece con @
            if (!name.StartsWith("@"))
            {
                parameter.ParameterName = "@" + name;
            }
            else
            {
                parameter.ParameterName = name;
            }

            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;
            parameter.Direction = direction;
            parameter.Size = size;

            _parameters.Add(parameter);
        }

        /// <summary>
        /// Agrega una colección de parámetros a esta colección
        /// </summary>
        public void AddRange(IEnumerable<DbParameter> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            foreach (var parameter in parameters)
            {
                Add(parameter);
            }
        }

        /// <summary>
        /// Agrega múltiples parámetros de entrada a la colección
        /// </summary>
        public void AddInputParameters(params (string name, object value)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                Add(name, value, DbType.Object, ParameterDirection.Input);
            }
        }

        /// <summary>
        /// Agrega múltiples parámetros de salida a la colección
        /// </summary>
        public void AddOutputParameters(params (string name, DbType dbType)[] parameters)
        {
            foreach (var (name, dbType) in parameters)
            {
                Add(name, null, dbType, ParameterDirection.Output);
            }
        }

        /// <summary>
        /// Limpia todos los parámetros de la colección
        /// </summary>
        public void Clear()
        {
            _parameters.Clear();
        }

        /// <summary>
        /// Determina si la colección contiene un parámetro con el nombre especificado
        /// </summary>
        public bool Contains(string name)
        {
            return _parameters.Any(p =>
                p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                p.ParameterName.Equals("@" + name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determina si la colección contiene el parámetro especificado
        /// </summary>
        public bool Contains(DbParameter parameter)
        {
            return _parameters.Contains(parameter);
        }

        /// <summary>
        /// Copia los parámetros a un arreglo
        /// </summary>
        public void CopyTo(DbParameter[] array, int arrayIndex)
        {
            _parameters.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Elimina un parámetro de la colección
        /// </summary>
        public bool Remove(DbParameter parameter)
        {
            return _parameters.Remove(parameter);
        }

        /// <summary>
        /// Elimina un parámetro de la colección por nombre
        /// </summary>
        public bool Remove(string name)
        {
            int index = _parameters.FindIndex(p =>
                p.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                p.ParameterName.Equals("@" + name, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _parameters.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Obtiene el valor de un parámetro de salida
        /// </summary>
        public object GetOutputValue(string name)
        {
            var parameter = this[name];

            if (parameter.Direction != ParameterDirection.Output &&
                parameter.Direction != ParameterDirection.InputOutput &&
                parameter.Direction != ParameterDirection.ReturnValue)
            {
                throw new InvalidOperationException($"El parámetro {name} no es un parámetro de salida");
            }

            return parameter.Value == DBNull.Value ? null : parameter.Value;
        }

        /// <summary>
        /// Obtiene el valor de un parámetro de salida convertido al tipo especificado
        /// </summary>
        public T GetOutputValue<T>(string name)
        {
            object value = GetOutputValue(name);

            if (value == null)
            {
                return default(T);
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Convierte la colección a un arreglo
        /// </summary>
        public DbParameter[] ToArray()
        {
            return _parameters.ToArray();
        }

        /// <summary>
        /// Devuelve un enumerador para la colección
        /// </summary>
        public IEnumerator<DbParameter> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        /// <summary>
        /// Devuelve un enumerador para la colección
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }
}