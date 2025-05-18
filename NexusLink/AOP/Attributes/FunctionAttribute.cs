using System;
using System.Reflection;

namespace NexusLink.AOP.Attributes
{
    /// <summary>
    /// Atributo para declarar métodos que ejecutan funciones SQL
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : InterceptAttribute
    {
        /// <summary>
        /// Nombre de la función SQL
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Esquema de la función (valor predeterminado: dbo)
        /// </summary>
        public string Schema { get; set; } = "dbo";

        /// <summary>
        /// Tipo de función (Escalar o Tabla)
        /// </summary>
        public FunctionType FunctionType { get; set; } = FunctionType.Scalar;

        /// <summary>
        /// Constructor con nombre de función
        /// </summary>
        public FunctionAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        /// <summary>
        /// Ejecuta la función configurada
        /// </summary>
        public override object Execute(MethodInfo targetMethod, object[] args, object targetInstance)
        {
            // Determinar la consulta SQL según el tipo de función
            string sql;
            if (FunctionType == FunctionType.Scalar)
            {
                // Función escalar
                sql = $"SELECT {Schema}.{FunctionName}(";
            }
            else
            {
                // Función de tabla
                sql = $"SELECT * FROM {Schema}.{FunctionName}(";
            }

            // Agregar parámetros a la consulta
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    sql += $"@p{i},";
                }

                // Eliminar la última coma
                sql = sql.Substring(0, sql.Length - 1);
            }

            sql += ")";

            // Crear una conexión a la base de datos
            using (var connection = new System.Data.SqlClient.SqlConnection(GetConnectionString()))
            {
                connection.Open();

                using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                {
                    // Agregar parámetros al comando
                    for (int i = 0; i < args.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);
                    }

                    // Ejecutar según el tipo de función y el tipo de retorno del método
                    if (FunctionType == FunctionType.Scalar)
                    {
                        var result = command.ExecuteScalar();

                        if (result == DBNull.Value)
                            return null;

                        if (targetMethod.ReturnType != typeof(void) && result != null)
                        {
                            // Convertir el resultado al tipo esperado
                            return Convert.ChangeType(result, targetMethod.ReturnType);
                        }

                        return result;
                    }
                    else
                    {
                        // Para funciones de tabla, crear un DataTable o IEnumerable según el tipo de retorno
                        var dataTable = new System.Data.DataTable();
                        using (var adapter = new System.Data.SqlClient.SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        // Si el tipo de retorno es DataTable, devolverlo directamente
                        if (targetMethod.ReturnType == typeof(System.Data.DataTable))
                        {
                            return dataTable;
                        }

                        // Si es IEnumerable<T>, convertir DataTable a lista
                        if (targetMethod.ReturnType.IsGenericType &&
                            targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                        {
                            var elementType = targetMethod.ReturnType.GetGenericArguments()[0];

                            // Usar DataTableExtensions para convertir a lista
                            var toListMethod = typeof(System.Data.DataTableExtensions)
                                .GetMethod("ToList")
                                .MakeGenericMethod(elementType);

                            return toListMethod.Invoke(null, new object[] { dataTable });
                        }

                        return dataTable;
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene la cadena de conexión basada en la configuración
        /// </summary>
        private string GetConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        }
    }

    /// <summary>
    /// Tipo de función SQL
    /// </summary>
    public enum FunctionType
    {
        /// <summary>
        /// Función escalar que devuelve un valor único
        /// </summary>
        Scalar,

        /// <summary>
        /// Función que devuelve una tabla
        /// </summary>
        Table
    }
}