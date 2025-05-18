using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusLink.Extensions.DataExtensions;

namespace NexusLink.MVC.ViewHelpers
{
    /// <summary>
    /// Helpers para generar controles HTML para DataTable.
    /// Facilita la visualización de datos tabulares en vistas MVC.
    /// </summary>
    public static class DataTableHelper
    {
        /// <summary>
        /// Genera una tabla HTML a partir de un DataTable
        /// </summary>
        /// <param name="helper">HtmlHelper</param>
        /// <param name="dataTable">DataTable a renderizar</param>
        /// <param name="tableClass">Clase CSS para la tabla</param>
        /// <param name="tableId">ID para la tabla</param>
        /// <param name="includeRowNumbers">Si es true, incluye una columna con números de fila</param>
        /// <returns>Contenido HTML</returns>
        public static IHtmlContent DataTableToHtml(
            this IHtmlHelper helper,
            DataTable dataTable,
            string tableClass = "table table-striped",
            string tableId = null,
            bool includeRowNumbers = false)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            var sb = new StringBuilder();

            // Abrir tabla
            sb.AppendLine($"<table class=\"{tableClass}\" {(tableId != null ? $"id=\"{tableId}\"" : "")}>");

            // Encabezado
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");

            // Columna de números de fila
            if (includeRowNumbers)
            {
                sb.AppendLine("<th>#</th>");
            }

            // Columnas
            foreach (DataColumn column in dataTable.Columns)
            {
                sb.AppendLine($"<th>{column.ColumnName}</th>");
            }

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            // Cuerpo
            sb.AppendLine("<tbody>");

            for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                sb.AppendLine("<tr>");

                // Número de fila
                if (includeRowNumbers)
                {
                    sb.AppendLine($"<td>{rowIndex + 1}</td>");
                }

                // Celdas
                foreach (DataColumn column in dataTable.Columns)
                {
                    var cellValue = dataTable.Rows[rowIndex][column];
                    string formattedValue = FormatCellValue(cellValue, column.DataType);
                    sb.AppendLine($"<td>{formattedValue}</td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Genera una tabla HTML interactiva (DataTables.js) a partir de un DataTable
        /// </summary>
        /// <param name="helper">HtmlHelper</param>
        /// <param name="dataTable">DataTable a renderizar</param>
        /// <param name="tableId">ID para la tabla</param>
        /// <param name="tableClass">Clase CSS para la tabla</param>
        /// <param name="options">Opciones de configuración para DataTables</param>
        /// <returns>Contenido HTML</returns>
        public static IHtmlContent DataTableToInteractiveHtml(
            this IHtmlHelper helper,
            DataTable dataTable,
            string tableId,
            string tableClass = "table table-striped",
            DataTablesOptions options = null)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("Table ID is required for interactive tables", nameof(tableId));

            options = options ?? new DataTablesOptions();

            var sb = new StringBuilder();

            // Generar tabla HTML
            sb.AppendLine(DataTableToHtml(helper, dataTable, tableClass, tableId).GetString());

            // Script para inicializar DataTables
            sb.AppendLine("<script>");
            sb.AppendLine("$(document).ready(function() {");
            sb.AppendLine($"  $('#{tableId}').DataTable({{");

            // Opciones
            sb.AppendLine($"    paging: {options.Paging.ToString().ToLower()},");
            sb.AppendLine($"    searching: {options.Searching.ToString().ToLower()},");
            sb.AppendLine($"    ordering: {options.Ordering.ToString().ToLower()},");
            sb.AppendLine($"    info: {options.Info.ToString().ToLower()},");
            sb.AppendLine($"    responsive: {options.Responsive.ToString().ToLower()},");

            // Longitud de página
            sb.AppendLine($"    pageLength: {options.PageLength},");

            // Opciones de longitud de página
            if (options.LengthMenu != null && options.LengthMenu.Length > 0)
            {
                sb.AppendLine($"    lengthMenu: [{string.Join(", ", options.LengthMenu)}],");
            }

            // Orden inicial
            if (options.Order != null && options.Order.Length >= 2)
            {
                sb.AppendLine($"    order: [[{options.Order[0]}, '{options.Order[1].ToLower()}']],");
            }

            // Definición de columnas
            if (options.ColumnDefs != null && options.ColumnDefs.Count > 0)
            {
                sb.AppendLine("    columnDefs: [");

                for (int i = 0; i < options.ColumnDefs.Count; i++)
                {
                    var columnDef = options.ColumnDefs[i];
                    sb.Append($"      {{ targets: {columnDef.Target}, ");

                    if (columnDef.Visible.HasValue)
                    {
                        sb.Append($"visible: {columnDef.Visible.Value.ToString().ToLower()}, ");
                    }

                    if (columnDef.Searchable.HasValue)
                    {
                        sb.Append($"searchable: {columnDef.Searchable.Value.ToString().ToLower()}, ");
                    }

                    if (columnDef.Orderable.HasValue)
                    {
                        sb.Append($"orderable: {columnDef.Orderable.Value.ToString().ToLower()}, ");
                    }

                    if (!string.IsNullOrEmpty(columnDef.ClassName))
                    {
                        sb.Append($"className: '{columnDef.ClassName}', ");
                    }

                    if (!string.IsNullOrEmpty(columnDef.Width))
                    {
                        sb.Append($"width: '{columnDef.Width}', ");
                    }

                    // Eliminar la coma y el espacio finales
                    sb.Length -= 2;

                    sb.AppendLine(i < options.ColumnDefs.Count - 1 ? " }," : " }");
                }

                sb.AppendLine("    ],");
            }

            // Idioma
            if (!string.IsNullOrEmpty(options.Language))
            {
                sb.AppendLine($"    language: {{");
                sb.AppendLine($"      url: '{options.Language}'");
                sb.AppendLine($"    }},");
            }

            // Opciones personalizadas
            if (!string.IsNullOrEmpty(options.CustomOptions))
            {
                sb.AppendLine($"    {options.CustomOptions},");
            }

            // Eliminar la coma final y cerrar el objeto
            sb.Length -= 3;
            sb.AppendLine();
            sb.AppendLine("  });");
            sb.AppendLine("});");
            sb.AppendLine("</script>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Genera un gráfico a partir de un DataTable
        /// </summary>
        /// <param name="helper">HtmlHelper</param>
        /// <param name="dataTable">DataTable con los datos</param>
        /// <param name="chartId">ID para el canvas del gráfico</param>
        /// <param name="chartType">Tipo de gráfico</param>
        /// <param name="options">Opciones de configuración del gráfico</param>
        /// <returns>Contenido HTML</returns>
        public static IHtmlContent DataTableToChart(
            this IHtmlHelper helper,
            DataTable dataTable,
            string chartId,
            ChartType chartType = ChartType.Bar,
            ChartOptions options = null)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            if (string.IsNullOrEmpty(chartId))
                throw new ArgumentException("Chart ID is required", nameof(chartId));

            options = options ?? new ChartOptions();

            var sb = new StringBuilder();

            // Canvas para el gráfico
            sb.AppendLine($"<canvas id=\"{chartId}\" width=\"{options.Width}\" height=\"{options.Height}\"></canvas>");

            // Script para inicializar Chart.js
            sb.AppendLine("<script>");
            sb.AppendLine("$(document).ready(function() {");
            sb.AppendLine($"  var ctx = document.getElementById('{chartId}').getContext('2d');");
            sb.AppendLine($"  var chart = new Chart(ctx, {{");
            sb.AppendLine($"    type: '{chartType.ToString().ToLower()}',");
            sb.AppendLine($"    data: {{");

            // Etiquetas (primera columna)
            sb.AppendLine($"      labels: [");

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var label = dataTable.Rows[i][options.LabelColumn ?? 0];
                sb.AppendLine($"        '{label}'{(i < dataTable.Rows.Count - 1 ? "," : "")}");
            }

            sb.AppendLine($"      ],");

            // Datos (demás columnas)
            sb.AppendLine($"      datasets: [");

            for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
            {
                // Omitir la columna de etiquetas
                if (colIndex == (options.LabelColumn ?? 0))
                    continue;

                sb.AppendLine($"        {{");
                sb.AppendLine($"          label: '{dataTable.Columns[colIndex].ColumnName}',");

                // Datos de la columna
                sb.AppendLine($"          data: [");

                for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
                {
                    var value = dataTable.Rows[rowIndex][colIndex];
                    sb.AppendLine($"            {value}{(rowIndex < dataTable.Rows.Count - 1 ? "," : "")}");
                }

                sb.AppendLine($"          ],");

                // Color
                if (options.Colors != null && options.Colors.Count > colIndex - 1)
                {
                    string color = options.Colors[colIndex - 1];
                    sb.AppendLine($"          backgroundColor: '{color}',");
                    sb.AppendLine($"          borderColor: '{color}',");
                }
                else
                {
                    // Color aleatorio
                    string color = $"rgb({new Random().Next(0, 255)}, {new Random().Next(0, 255)}, {new Random().Next(0, 255)})";
                    sb.AppendLine($"          backgroundColor: '{color}',");
                    sb.AppendLine($"          borderColor: '{color}',");
                }

                sb.AppendLine($"          borderWidth: 1");

                // Cerrar dataset
                bool isLastColumn = colIndex == dataTable.Columns.Count - 1 ||
                                    (colIndex == dataTable.Columns.Count - 2 && (options.LabelColumn ?? 0) == dataTable.Columns.Count - 1);

                sb.AppendLine(isLastColumn ? $"        }}" : $"        }},");
            }

            sb.AppendLine($"      ]");
            sb.AppendLine($"    }},");

            // Opciones
            sb.AppendLine($"    options: {{");
            sb.AppendLine($"      responsive: true,");
            sb.AppendLine($"      title: {{");
            sb.AppendLine($"        display: {(!string.IsNullOrEmpty(options.Title)).ToString().ToLower()},");
            sb.AppendLine($"        text: '{options.Title ?? ""}',");
            sb.AppendLine($"        fontSize: {options.FontSize}");
            sb.AppendLine($"      }},");
            sb.AppendLine($"      legend: {{");
            sb.AppendLine($"        display: {options.ShowLegend.ToString().ToLower()},");
            sb.AppendLine($"        position: '{options.LegendPosition.ToString().ToLower()}'");
            sb.AppendLine($"      }},");

            // Opciones personalizadas
            if (!string.IsNullOrEmpty(options.CustomOptions))
            {
                sb.AppendLine($"      {options.CustomOptions}");
            }
            else
            {
                sb.AppendLine($"      scales: {{");
                sb.AppendLine($"        yAxes: [{{");
                sb.AppendLine($"          ticks: {{");
                sb.AppendLine($"            beginAtZero: true");
                sb.AppendLine($"          }}");
                sb.AppendLine($"        }}]");
                sb.AppendLine($"      }}");
            }

            sb.AppendLine($"    }}");
            sb.AppendLine($"  }});");
            sb.AppendLine("});");
            sb.AppendLine("</script>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Genera una tabla HTML a partir de una colección de objetos
        /// </summary>
        /// <typeparam name="T">Tipo de los objetos</typeparam>
        /// <param name="helper">HtmlHelper</param>
        /// <param name="items">Colección de objetos</param>
        /// <param name="properties">Propiedades a incluir (null para todas)</param>
        /// <param name="tableClass">Clase CSS para la tabla</param>
        /// <param name="tableId">ID para la tabla</param>
        /// <returns>Contenido HTML</returns>
        public static IHtmlContent ObjectListToHtml<T>(
            this IHtmlHelper helper,
            IEnumerable<T> items,
            IEnumerable<string> properties = null,
            string tableClass = "table table-striped",
            string tableId = null)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            // Convertir a DataTable y usar DataTableToHtml
            DataTable dataTable = items.ToDataTable(properties);
            return DataTableToHtml(helper, dataTable, tableClass, tableId);
        }

        /// <summary>
        /// Formatea el valor de una celda
        /// </summary>
        /// <param name="value">Valor de la celda</param>
        /// <param name="dataType">Tipo de dato</param>
        /// <returns>Valor formateado</returns>
        private static string FormatCellValue(object value, Type dataType)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            if (dataType == typeof(DateTime))
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
            {
                return string.Format("{0:0.00}", value);
            }

            if (dataType == typeof(bool))
            {
                return (bool)value ? "Yes" : "No";
            }

            return value.ToString();
        }
    }

    /// <summary>
    /// Opciones para DataTables
    /// </summary>
    public class DataTablesOptions
    {
        public bool Paging { get; set; } = true;
        public bool Searching { get; set; } = true;
        public bool Ordering { get; set; } = true;
        public bool Info { get; set; } = true;
        public bool Responsive { get; set; } = true;
        public int PageLength { get; set; } = 10;
        public int[] LengthMenu { get; set; } = new[] { 10, 25, 50, 100 };
        public string[] Order { get; set; } = new[] { "0", "asc" };
        public List<ColumnDef> ColumnDefs { get; set; } = new List<ColumnDef>();
        public string Language { get; set; }
        public string CustomOptions { get; set; }
    }

    /// <summary>
    /// Definición de columna para DataTables
    /// </summary>
    public class ColumnDef
    {
        public string Target { get; set; }
        public bool? Visible { get; set; }
        public bool? Searchable { get; set; }
        public bool? Orderable { get; set; }
        public string ClassName { get; set; }
        public string Width { get; set; }
    }

    /// <summary>
    /// Opciones para gráficos
    /// </summary>
    public class ChartOptions
    {
        public string Title { get; set; }
        public int FontSize { get; set; } = 16;
        public bool ShowLegend { get; set; } = true;
        public LegendPosition LegendPosition { get; set; } = LegendPosition.Top;
        public int? LabelColumn { get; set; } = 0;
        public List<string> Colors { get; set; } = new List<string>();
        public string CustomOptions { get; set; }
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 300;
    }

    /// <summary>
    /// Tipo de gráfico
    /// </summary>
    public enum ChartType
    {
        Bar,
        Line,
        Pie,
        Doughnut,
        Radar,
        PolarArea,
        Bubble,
        Scatter
    }

    /// <summary>
    /// Posición de la leyenda
    /// </summary>
    public enum LegendPosition
    {
        Top,
        Right,
        Bottom,
        Left
    }

    /// <summary>
    /// Extensiones para HtmlString
    /// </summary>
    internal static class HtmlStringExtensions
    {
        /// <summary>
        /// Obtiene la cadena de un HtmlString
        /// </summary>
        /// <param name="htmlContent">Contenido HTML</param>
        /// <returns>Cadena HTML</returns>
        public static string GetString(this IHtmlContent htmlContent)
        {
            using (var writer = new System.IO.StringWriter())
            {
                htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }
    }
}