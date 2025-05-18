using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace NexusLink.Dynamic.Emit
{
    /// <summary>
    /// Construye tipos dinámicos en tiempo de ejecución
    /// </summary>
    public static class TypeBuilder
    {
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly AssemblyBuilder _assemblyBuilder;
        private static readonly ModuleBuilder _moduleBuilder;

        static TypeBuilder()
        {
            var assemblyName = new AssemblyName("NexusLink.DynamicTypes");
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
        }

        /// <summary>
        /// Crea un tipo dinámico a partir de una tabla de datos
        /// </summary>
        public static Type CreateTypeFromDataTable(DataTable dataTable, string typeName)
        {
            // Verificar caché
            string cacheKey = $"{typeName}_{ComputeSchemaHash(dataTable)}";
            if (_typeCache.TryGetValue(cacheKey, out Type cachedType))
            {
                return cachedType;
            }

            // Crear un nuevo tipo
            var typeBuilder = _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

            // Añadir propiedades para cada columna
            foreach (DataColumn column in dataTable.Columns)
            {
                Type propertyType = column.DataType;

                // Permitir valores nulos para tipos que no son de referencia
                if (propertyType.IsValueType && !column.AllowDBNull)
                {
                    propertyType = typeof(Nullable<>).MakeGenericType(propertyType);
                }

                CreateProperty(typeBuilder, column.ColumnName, propertyType);
            }

            // Crear el tipo y almacenarlo en caché
            Type newType = typeBuilder.CreateType();
            _typeCache[cacheKey] = newType;

            return newType;
        }

        /// <summary>
        /// Crea un tipo dinámico a partir de un diccionario de propiedades
        /// </summary>
        public static Type CreateTypeFromProperties(
            Dictionary<string, Type> properties,
            string typeName)
        {
            // Verificar caché
            string cacheKey = $"{typeName}_{ComputePropertiesHash(properties)}";
            if (_typeCache.TryGetValue(cacheKey, out Type cachedType))
            {
                return cachedType;
            }

            // Crear un nuevo tipo
            var typeBuilder = _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

            // Añadir propiedades
            foreach (var property in properties)
            {
                CreateProperty(typeBuilder, property.Key, property.Value);
            }

            // Crear el tipo y almacenarlo en caché
            Type newType = typeBuilder.CreateType();
            _typeCache[cacheKey] = newType;

            return newType;
        }

        /// <summary>
        /// Crea un tipo que implementa una interfaz
        /// </summary>
        public static Type CreateTypeFromInterface(Type interfaceType, string typeName)
        {
            // Verificar caché
            string cacheKey = $"{typeName}_{interfaceType.FullName}";
            if (_typeCache.TryGetValue(cacheKey, out Type cachedType))
            {
                return cachedType;
            }

            // Crear un nuevo tipo
            var typeBuilder = _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                parent: null,
                interfaces: new[] { interfaceType });

            // Implementar propiedades de la interfaz
            foreach (PropertyInfo property in interfaceType.GetProperties())
            {
                CreateProperty(typeBuilder, property.Name, property.PropertyType);
            }

            // Implementar métodos de la interfaz
            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                // Omitir métodos de propiedades
                if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                {
                    continue;
                }

                // Crear implementación del método
                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType,
                    method.GetParameters().Select(p => p.ParameterType).ToArray());

                // Generar cuerpo del método (implementación por defecto)
                ILGenerator il = methodBuilder.GetILGenerator();

                // Devolver valor por defecto para el tipo de retorno
                if (method.ReturnType != typeof(void))
                {
                    if (method.ReturnType.IsValueType && !method.ReturnType.IsGenericType)
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                        if (method.ReturnType != typeof(int))
                        {
                            il.Emit(OpCodes.Conv_R8);
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                }

                il.Emit(OpCodes.Ret);

                // Implementar la interfaz
                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            // Crear el tipo y almacenarlo en caché
            Type newType = typeBuilder.CreateType();
            _typeCache[cacheKey] = newType;

            return newType;
        }

        /// <summary>
        /// Crea una propiedad en un tipo dinámico
        /// </summary>
        private static void CreateProperty(System.Reflection.Emit.TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            // Crear un campo privado
            FieldBuilder fieldBuilder = typeBuilder.DefineField(
                $"_{propertyName}",
                propertyType,
                FieldAttributes.Private);

            // Crear la propiedad
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.HasDefault,
                propertyType,
                null);

            // Crear el método getter
            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                $"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes);

            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            // Crear el método setter
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                $"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new[] { propertyType });

            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);

            // Asignar métodos a la propiedad
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        /// <summary>
        /// Calcula un hash del esquema de la tabla para identificación en caché
        /// </summary>
        private static string ComputeSchemaHash(DataTable dataTable)
        {
            var hash = new HashCode();
            foreach (DataColumn column in dataTable.Columns)
            {
                hash.Add(column.ColumnName);
                hash.Add(column.DataType.FullName);
                hash.Add(column.AllowDBNull);
            }
            return hash.ToHashCode().ToString();
        }

        /// <summary>
        /// Calcula un hash de las propiedades para identificación en caché
        /// </summary>
        private static string ComputePropertiesHash(Dictionary<string, Type> properties)
        {
            var hash = new HashCode();
            foreach (var property in properties)
            {
                hash.Add(property.Key);
                hash.Add(property.Value.FullName);
            }
            return hash.ToHashCode().ToString();
        }
    }
}