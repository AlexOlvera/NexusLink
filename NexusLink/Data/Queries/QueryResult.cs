using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NexusLink.Data.Queries
{
    /// <summary>
    /// Representa el resultado de una consulta
    /// </summary>
    /// <typeparam name="T">Tipo de objetos en el resultado</typeparam>
    public class QueryResult<T>
    {
        private readonly List<T> _items;

        /// <summary>
        /// Inicializa una nueva instancia de QueryResult
        /// </summary>
        /// <param name="items">Elementos del resultado</param>
        /// <param name="totalCount">Número total de elementos (para paginación)</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="pageNumber">Número de página</param>
        public QueryResult(IEnumerable<T> items, int? totalCount = null, int? pageSize = null, int? pageNumber = null)
        {
            _items = items?.ToList() ?? new List<T>();
            TotalCount = totalCount ?? _items.Count;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }

        /// <summary>
        /// Número total de elementos
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int? PageSize { get; }

        /// <summary>
        /// Número de página
        /// </summary>
        public int? PageNumber { get; }

        /// <summary>
        /// Número total de páginas
        /// </summary>
        public int? TotalPages => PageSize.HasValue && PageSize.Value > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize.Value) : null;

        /// <summary>
        /// Indica si hay una página anterior
        /// </summary>
        public bool HasPreviousPage => PageNumber.HasValue && PageNumber.Value > 1;

        /// <summary>
        /// Indica si hay una página siguiente
        /// </summary>
        public bool HasNextPage => PageNumber.HasValue && TotalPages.HasValue && PageNumber.Value < TotalPages.Value;

        /// <summary>
        /// Elementos del resultado
        /// </summary>
        public IReadOnlyList<T> Items => _items.AsReadOnly();

        /// <summary>
        /// Número de elementos en el resultado
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Indica si el resultado está vacío
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Obtiene el primer elemento del resultado o default si está vacío
        /// </summary>
        /// <returns>Primer elemento o default</returns>
        public T FirstOrDefault()
        {
            return _items.Count > 0 ? _items[0] : default(T);
        }

        /// <summary>
        /// Obtiene un solo elemento del resultado o default si está vacío o tiene más de un elemento
        /// </summary>
        /// <returns>Único elemento o default</returns>
        public T SingleOrDefault()
        {
            return _items.Count == 1 ? _items[0] : default(T);
        }

        /// <summary>
        /// Convierte el resultado a una lista
        /// </summary>
        /// <returns>Lista de elementos</returns>
        public List<T> ToList()
        {
            return new List<T>(_items);
        }

        /// <summary>
        /// Convierte el resultado a un array
        /// </summary>
        /// <returns>Array de elementos</returns>
        public T[] ToArray()
        {
            return _items.ToArray();
        }

        /// <summary>
        /// Convierte el resultado a un DataTable
        /// </summary>
        /// <returns>DataTable con los elementos</returns>
        public DataTable ToDataTable()
        {
            var dataTable = new DataTable();
            var type = typeof(T);

            // Si es un tipo primitivo o string, crear una sola columna
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                dataTable.Columns.Add("Value", type);

                foreach (T item in _items)
                {
                    DataRow row = dataTable.NewRow();
                    row["Value"] = item;
                    dataTable.Rows.Add(row);
                }
            }
            else
            {
                // Para tipos complejos, crear una columna por cada propiedad pública
                var properties = type.GetProperties().Where(p => p.CanRead);

                foreach (var property in properties)
                {
                    dataTable.Columns.Add(property.Name, property.PropertyType);
                }

                foreach (T item in _items)
                {
                    DataRow row = dataTable.NewRow();

                    foreach (var property in properties)
                    {
                        object value = property.GetValue(item);
                        row[property.Name] = value ?? DBNull.Value;
                    }

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }
    }
}