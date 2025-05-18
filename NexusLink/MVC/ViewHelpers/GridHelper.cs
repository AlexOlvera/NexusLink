using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusLink.Extensions.ObjectExtensions;

namespace NexusLink.MVC.ViewHelpers
{
    /// <summary>
    /// Helpers para generar grillas de datos en vistas MVC.
    /// Proporciona un enfoque tipado para generar tablas HTML.
    /// </summary>
    public static class GridHelper
    {
        /// <summary>
        /// Crea una grilla a partir de una colección de objetos
        /// </summary>
        /// <typeparam name="T">Tipo de los objetos</typeparam>
        /// <param name="helper">HtmlHelper</param>
        /// <param name="items">Colección de objetos</param>
        /// <returns>Constructor de grilla</returns>
        public static GridBuilder<T> Grid<T>(this IHtmlHelper helper, IEnumerable<T> items)
        {
            return new GridBuilder<T>(helper, items);
        }
    }

    /// <summary>
    /// Constructor de grillas
    /// </summary>
    /// <typeparam name="T">Tipo de los objetos</typeparam>
    public class GridBuilder<T>
    {
        private readonly IHtmlHelper _htmlHelper;
        private readonly IEnumerable<T> _items;
        private readonly List<ColumnDefinition<T>> _columns = new List<ColumnDefinition<T>>();
        private string _tableClass = "table table-striped";
        private string _tableId;
        private bool _responsive = true;
        private bool _showHeader = true;
        private bool _showFooter = false;
        private bool _enablePaging = false;
        private int _pageSize = 10;
        private bool _enableSorting = false;
        private bool _enableFiltering = false;
        private string _emptyText = "No items to display.";
        private string _caption;

        /// <summary>
        /// Crea una nueva instancia de GridBuilder
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="items">Colección de objetos</param>
        public GridBuilder(IHtmlHelper htmlHelper, IEnumerable<T> items)
        {
            _htmlHelper = htmlHelper ?? throw new ArgumentNullException(nameof(htmlHelper));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Agrega una columna a la grilla
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="expression">Expresión para acceder a la propiedad</param>
        /// <param name="headerText">Texto del encabezado</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> AddColumn<TProperty>(Expression<Func<T, TProperty>> expression, string headerText = null)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var propertyLambda = (LambdaExpression)expression;

            if (!(propertyLambda.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("The expression must be a member access expression.", nameof(expression));
            }

            string propertyName = memberExpression.Member.Name;

            _columns.Add(new ColumnDefinition<T>
            {
                Expression = expression,
                PropertyName = propertyName,
                HeaderText = headerText ?? propertyName,
                Format = null,
                HtmlAttributes = null,
                CssClass = null
            });

            return this;
        }

        /// <summary>
        /// Agrega una columna a la grilla con formato personalizado
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="expression">Expresión para acceder a la propiedad</param>
        /// <param name="headerText">Texto del encabezado</param>
        /// <param name="format">Formato a aplicar</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> AddColumn<TProperty>(
            Expression<Func<T, TProperty>> expression,
            string headerText,
            string format)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var propertyLambda = (LambdaExpression)expression;

            if (!(propertyLambda.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("The expression must be a member access expression.", nameof(expression));
            }

            string propertyName = memberExpression.Member.Name;

            _columns.Add(new ColumnDefinition<T>
            {
                Expression = expression,
                PropertyName = propertyName,
                HeaderText = headerText ?? propertyName,
                Format = format,
                HtmlAttributes = null,
                CssClass = null
            });

            return this;
        }

        /// <summary>
        /// Agrega una columna con un template personalizado
        /// </summary>
        /// <param name="headerText">Texto del encabezado</param>
        /// <param name="template">Template para el contenido de la columna</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> AddTemplateColumn(string headerText, Func<T, object> template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            _columns.Add(new ColumnDefinition<T>
            {
                HeaderText = headerText,
                Template = template,
                PropertyName = null,
                Expression = null,
                Format = null,
                HtmlAttributes = null,
                CssClass = null
            });

            return this;
        }

        /// <summary>
        /// Establece la clase CSS para la tabla
        /// </summary>
        /// <param name="tableClass">Clase CSS</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> TableClass(string tableClass)
        {
            _tableClass = tableClass;
            return this;
        }

        /// <summary>
        /// Establece el ID para la tabla
        /// </summary>
        /// <param name="tableId">ID de la tabla</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> TableId(string tableId)
        {
            _tableId = tableId;
            return this;
        }

        /// <summary>
        /// Habilita o deshabilita la respuesta adaptativa
        /// </summary>
        /// <param name="responsive">True para habilitar, false para deshabilitar</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> Responsive(bool responsive)
        {
            _responsive = responsive;
            return this;
        }

        /// <summary>
        /// Habilita o deshabilita el encabezado
        /// </summary>
        /// <param name="showHeader">True para mostrar, false para ocultar</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> ShowHeader(bool showHeader)
        {
            _showHeader = showHeader;
            return this;
        }

        /// <summary>
        /// Habilita o deshabilita el pie de tabla
        /// </summary>
        /// <param name="showFooter">True para mostrar, false para ocultar</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> ShowFooter(bool showFooter)
        {
            _showFooter = showFooter;
            return this;
        }

        /// <summary>
        /// Habilita la paginación
        /// </summary>
        /// <param name="pageSize">Tamaño de página</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> EnablePaging(int pageSize = 10)
        {
            _enablePaging = true;
            _pageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Habilita la ordenación
        /// </summary>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> EnableSorting()
        {
            _enableSorting = true;
            return this;
        }

        /// <summary>
        /// Habilita el filtrado
        /// </summary>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> EnableFiltering()
        {
            _enableFiltering = true;
            return this;
        }

        /// <summary>
        /// Establece el texto a mostrar cuando no hay elementos
        /// </summary>
        /// <param name="emptyText">Texto a mostrar</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> EmptyText(string emptyText)
        {
            _emptyText = emptyText;
            return this;
        }

        /// <summary>
        /// Establece el título de la tabla
        /// </summary>
        /// <param name="caption">Título de la tabla</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> Caption(string caption)
        {
            _caption = caption;
            return this;
        }

        /// <summary>
        /// Agrega clase CSS a una columna
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="expression">Expresión para acceder a la propiedad</param>
        /// <param name="cssClass">Clase CSS</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> AddColumnClass<TProperty>(Expression<Func<T, TProperty>> expression, string cssClass)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var propertyLambda = (LambdaExpression)expression;

            if (!(propertyLambda.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("The expression must be a member access expression.", nameof(expression));
            }

            string propertyName = memberExpression.Member.Name;

            var column = _columns.FirstOrDefault(c => c.PropertyName == propertyName);
            if (column != null)
            {
                column.CssClass = cssClass;
            }

            return this;
        }

        /// <summary>
        /// Agrega atributos HTML a una columna
        /// </summary>
        /// <typeparam name="TProperty">Tipo de la propiedad</typeparam>
        /// <param name="expression">Expresión para acceder a la propiedad</param>
        /// <param name="htmlAttributes">Atributos HTML</param>
        /// <returns>Constructor de grilla</returns>
        public GridBuilder<T> AddColumnAttributes<TProperty>(
            Expression<Func<T, TProperty>> expression,
            object htmlAttributes)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var propertyLambda = (LambdaExpression)expression;

            if (!(propertyLambda.Body is MemberExpression memberExpression))
            {
                throw new ArgumentException("The expression must be a member access expression.", nameof(expression));
            }

            string propertyName = memberExpression.Member.Name;

            var column = _columns.FirstOrDefault(c => c.PropertyName == propertyName);
            if (column != null)
            {
                column.HtmlAttributes = htmlAttributes;
            }

            return this;
        }

        /// <summary>
        /// Genera el HTML para la grilla
        /// </summary>
        /// <returns>Contenido HTML</returns>
        public IHtmlContent Render()
        {
            if (_columns.Count == 0)
            {
                throw new InvalidOperationException("Cannot render a grid without columns. Use AddColumn method to add columns.");
            }

            var sb = new StringBuilder();

            // Abrir tabla
            sb.AppendLine($"<table class=\"{_tableClass}\" {(_tableId != null ? $"id=\"{_tableId}\"" : "")}>");

            // Agregar título si existe
            if (!string.IsNullOrEmpty(_caption))
            {
                sb.AppendLine($"<caption>{_caption}</caption>");
            }

            // Encabezado
            if (_showHeader)
            {
                sb.AppendLine("<thead>");
                sb.AppendLine("<tr>");

                foreach (var column in _columns)
                {
                    string attributes = RenderHtmlAttributes(column.HtmlAttributes);
                    string cssClass = column.CssClass != null ? $" class=\"{column.CssClass}\"" : "";

                    sb.AppendLine($"<th{cssClass}{attributes}>{column.HeaderText}</th>");
                }

                sb.AppendLine("</tr>");

                // Fila de filtros
                if (_enableFiltering)
                {
                    sb.AppendLine("<tr class=\"filters\">");

                    foreach (var column in _columns)
                    {
                        sb.AppendLine("<th>");
                        if (column.PropertyName != null)
                        {
                            sb.AppendLine($"<input type=\"text\" class=\"form-control\" placeholder=\"Filter {column.HeaderText}\" data-column=\"{column.PropertyName}\">");
                        }
                        sb.AppendLine("</th>");
                    }

                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</thead>");
            }

            // Pie de tabla
            if (_showFooter)
            {
                sb.AppendLine("<tfoot>");
                sb.AppendLine("<tr>");

                foreach (var column in _columns)
                {
                    string attributes = RenderHtmlAttributes(column.HtmlAttributes);
                    string cssClass = column.CssClass != null ? $" class=\"{column.CssClass}\"" : "";

                    sb.AppendLine($"<th{cssClass}{attributes}>{column.HeaderText}</th>");
                }

                sb.AppendLine("</tr>");
                sb.AppendLine("</tfoot>");
            }

            // Cuerpo
            sb.AppendLine("<tbody>");

            if (!_items.Any())
            {
                // No hay elementos
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td colspan=\"{_columns.Count}\" class=\"text-center\">{_emptyText}</td>");
                sb.AppendLine("</tr>");
            }
            else
            {
                // Renderizar filas
                foreach (var item in _items)
                {
                    sb.AppendLine("<tr>");

                    foreach (var column in _columns)
                    {
                        string attributes = RenderHtmlAttributes(column.HtmlAttributes);
                        string cssClass = column.CssClass != null ? $" class=\"{column.CssClass}\"" : "";

                        sb.AppendLine($"<td{cssClass}{attributes}>");

                        if (column.Template != null)
                        {
                            // Usar template personalizado
                            object content = column.Template(item);
                            sb.AppendLine(content?.ToString() ?? "");
                        }
                        else if (column.Expression != null)
                        {
                            // Obtener valor a través de la expresión
                            object value = column.Expression.Compile().DynamicInvoke(item);

                            // Aplicar formato si existe
                            string formattedValue = FormatValue(value, column.Format);
                            sb.AppendLine(formattedValue);
                        }

                        sb.AppendLine("</td>");
                    }

                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            // Paginación
            if (_enablePaging)
            {
                int totalItems = _items.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / _pageSize);

                if (totalPages > 1)
                {
                    sb.AppendLine("<nav>");
                    sb.AppendLine("<ul class=\"pagination\">");

                    for (int i = 1; i <= totalPages; i++)
                    {
                        sb.AppendLine($"<li class=\"page-item\"><a class=\"page-link\" href=\"#\" data-page=\"{i}\">{i}</a></li>");
                    }

                    sb.AppendLine("</ul>");
                    sb.AppendLine("</nav>");
                }
            }

            // Script para interactividad
            if (_enablePaging || _enableSorting || _enableFiltering)
            {
                sb.AppendLine("<script>");
                sb.AppendLine("$(document).ready(function() {");

                if (_enableSorting)
                {
                    sb.AppendLine($"  $('#{_tableId} thead th').each(function() {{");
                    sb.AppendLine("    $(this).click(function() {");
                    sb.AppendLine("      sortTable($(this).index());");
                    sb.AppendLine("    });");
                    sb.AppendLine("  });");

                    sb.AppendLine("  function sortTable(column) {");
                    sb.AppendLine($"    var table = $('#{_tableId}');");
                    sb.AppendLine("    var tbody = table.find('tbody');");
                    sb.AppendLine("    var rows = tbody.find('tr').toArray();");
                    sb.AppendLine("    var ascending = table.data('sort-order') != 'asc' || table.data('sort-column') != column;");
                    sb.AppendLine("    table.data('sort-order', ascending ? 'asc' : 'desc');");
                    sb.AppendLine("    table.data('sort-column', column);");
                    sb.AppendLine("    rows.sort(function(a, b) {");
                    sb.AppendLine("      var aValue = $(a).find('td').eq(column).text();");
                    sb.AppendLine("      var bValue = $(b).find('td').eq(column).text();");
                    sb.AppendLine("      if (!isNaN(aValue) && !isNaN(bValue)) {");
                    sb.AppendLine("        return ascending ? aValue - bValue : bValue - aValue;");
                    sb.AppendLine("      }");
                    sb.AppendLine("      return ascending ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);");
                    sb.AppendLine("    });");
                    sb.AppendLine("    $.each(rows, function(index, row) {");
                    sb.AppendLine("      tbody.append(row);");
                    sb.AppendLine("    });");
                    sb.AppendLine("  }");
                }

                if (_enableFiltering)
                {
                    sb.AppendLine($"  $('#{_tableId} tr.filters input').keyup(function() {{");
                    sb.AppendLine("    var column = $(this).data('column');");
                    sb.AppendLine("    var value = $(this).val().toLowerCase();");
                    sb.AppendLine($"    $('#{_tableId} tbody tr').filter(function() {{");
                    sb.AppendLine("      var cell = $(this).find('td[data-column=\"' + column + '\"]');");
                    sb.AppendLine("      return cell.text().toLowerCase().indexOf(value) === -1;");
                    sb.AppendLine("    }).hide();");
                    sb.AppendLine($"    $('#{_tableId} tbody tr').filter(function() {{");
                    sb.AppendLine("      var cell = $(this).find('td[data-column=\"' + column + '\"]');");
                    sb.AppendLine("      return cell.text().toLowerCase().indexOf(value) > -1;");
                    sb.AppendLine("    }).show();");
                    sb.AppendLine("  });");
                }

                if (_enablePaging)
                {
                    sb.AppendLine("  // Initialize paging");
                    sb.AppendLine($"  var pageSize = {_pageSize};");
                    sb.AppendLine($"  var rows = $('#{_tableId} tbody tr');");
                    sb.AppendLine("  var rowsCount = rows.length;");
                    sb.AppendLine("  var pageCount = Math.ceil(rowsCount / pageSize);");
                    sb.AppendLine("  var currentPage = 1;");
                    sb.AppendLine("");
                    sb.AppendLine("  function showPage(page) {");
                    sb.AppendLine("    var start = (page - 1) * pageSize;");
                    sb.AppendLine("    var end = start + pageSize;");
                    sb.AppendLine("    rows.hide();");
                    sb.AppendLine("    rows.slice(start, end).show();");
                    sb.AppendLine("    $('.pagination li').removeClass('active');");
                    sb.AppendLine("    $('.pagination li a[data-page=\"' + page + '\"]').parent().addClass('active');");
                    sb.AppendLine("    currentPage = page;");
                    sb.AppendLine("  }");
                    sb.AppendLine("");
                    sb.AppendLine("  $('.pagination li a').click(function(e) {");
                    sb.AppendLine("    e.preventDefault();");
                    sb.AppendLine("    var page = $(this).data('page');");
                    sb.AppendLine("    showPage(page);");
                    sb.AppendLine("  });");
                    sb.AppendLine("");
                    sb.AppendLine("  // Show first page on init");
                    sb.AppendLine("  showPage(1);");
                }

                sb.AppendLine("});");
                sb.AppendLine("</script>");
            }

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Formatea un valor
        /// </summary>
        /// <param name="value">Valor a formatear</param>
        /// <param name="format">Formato a aplicar</param>
        /// <returns>Valor formateado</returns>
        private string FormatValue(object value, string format)
        {
            if (value == null)
                return string.Empty;

            if (string.IsNullOrEmpty(format))
                return value.ToString();

            if (value is IFormattable formattable)
                return formattable.ToString(format, null);

            return value.ToString();
        }

        /// <summary>
        /// Renderiza atributos HTML
        /// </summary>
        /// <param name="htmlAttributes">Atributos HTML</param>
        /// <returns>Cadena de atributos HTML</returns>
        private string RenderHtmlAttributes(object htmlAttributes)
        {
            if (htmlAttributes == null)
                return string.Empty;

            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var sb = new StringBuilder();

            foreach (var attribute in attributes)
            {
                sb.Append($" {attribute.Key}=\"{attribute.Value}\"");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Definición de columna para la grilla
    /// </summary>
    /// <typeparam name="T">Tipo de los objetos</typeparam>
    public class ColumnDefinition<T>
    {
        public string PropertyName { get; set; }
        public string HeaderText { get; set; }
        public LambdaExpression Expression { get; set; }
        public Func<T, object> Template { get; set; }
        public string Format { get; set; }
        public object HtmlAttributes { get; set; }
        public string CssClass { get; set; }
    }
}