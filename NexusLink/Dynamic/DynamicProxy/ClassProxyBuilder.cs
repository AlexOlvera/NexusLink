using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NexusLink.AOP.Interception;

namespace NexusLink.Dynamic.DynamicProxy
{
    /// <summary>
    /// Constructor de proxies para clases.
    /// Permite generar subclases dinámicas con interceptores.
    /// </summary>
    public class ClassProxyBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;

        /// <summary>
        /// Crea una nueva instancia de ClassProxyBuilder
        /// </summary>
        /// <param name="moduleBuilder">ModuleBuilder para generar tipos</param>
        public ClassProxyBuilder(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder ?? throw new ArgumentNullException(nameof(moduleBuilder));
        }

        /// <summary>
        /// Genera un tipo que extiende una clase base con intercepción
        /// </summary>
        /// <param name="baseType">Tipo base a extender</param>
        /// <param name="additionalInterfaces">Interfaces adicionales a implementar</param>
        /// <returns>Tipo generado</returns>
        public Type BuildProxyType(Type baseType, params Type[] additionalInterfaces)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            // Validar tipo base
            if (baseType.IsSealed)
                throw new ArgumentException($"Cannot create proxy for sealed type {baseType.Name}", nameof(baseType));

            if (baseType.IsInterface)
                throw new ArgumentException($"Use InterfaceProxyBuilder for interface types", nameof(baseType));

            // Validar interfaces adicionales
            foreach (var additionalInterface in additionalInterfaces)
            {
                if (!additionalInterface.IsInterface)
                    throw new ArgumentException($"{additionalInterface.Name} is not an interface", nameof(additionalInterfaces));
            }

            // Crear el nombre del tipo proxy
            string proxyName = $"{baseType.Name}Proxy_{Guid.NewGuid():N}";

            // Definir el tipo proxy
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                proxyName,
                TypeAttributes.Public | TypeAttributes.Class,
                baseType,
                additionalInterfaces);

            // Definir campo para los interceptores
            FieldBuilder interceptorsField = typeBuilder.DefineField(
                "_interceptors",
                typeof(IList<IInterceptor>),
                FieldAttributes.Private);

            // Implementar constructores
            ImplementConstructors(typeBuilder, baseType, interceptorsField);

            // Implementar propiedad Interceptors
            ImplementInterceptorsProperty(typeBuilder, interceptorsField);

            // Sobreescribir métodos virtuales
            OverrideVirtualMethods(typeBuilder, baseType, interceptorsField);

            // Implementar interfaces adicionales
            foreach (var additionalInterface in additionalInterfaces)
            {
                ImplementInterface(typeBuilder, additionalInterface, interceptorsField);
            }

            // Crear el tipo
            return typeBuilder.CreateType();
        }

        /// <summary>
        /// Implementa los constructores del tipo base
        /// </summary>
        private void ImplementConstructors(TypeBuilder typeBuilder, Type baseType, FieldBuilder interceptorsField)
        {
            foreach (var baseCtor in baseType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                // Obtener parámetros del constructor base
                ParameterInfo[] parameters = baseCtor.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Definir constructor en el proxy
                ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    parameterTypes);

                // Copiar atributos de parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = ctorBuilder.DefineParameter(
                        i + 1,
                        parameters[i].Attributes,
                        parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                ILGenerator il = ctorBuilder.GetILGenerator();

                // Llamar al constructor base
                il.Emit(OpCodes.Ldarg_0);

                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                }

                il.Emit(OpCodes.Call, baseCtor);

                // Inicializar el campo interceptors
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Newobj, typeof(List<IInterceptor>).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stfld, interceptorsField);

                il.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// Implementa la propiedad Interceptors
        /// </summary>
        private void ImplementInterceptorsProperty(TypeBuilder typeBuilder, FieldBuilder interceptorsField)
        {
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                "Interceptors",
                PropertyAttributes.None,
                typeof(IList<IInterceptor>),
                null);

            // Método get
            MethodBuilder getMethod = typeBuilder.DefineMethod(
                "get_Interceptors",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(IList<IInterceptor>),
                Type.EmptyTypes);

            ILGenerator getIL = getMethod.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, interceptorsField);
            getIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethod);
        }

        /// <summary>
        /// Sobreescribe los métodos virtuales del tipo base
        /// </summary>
        private void OverrideVirtualMethods(TypeBuilder typeBuilder, Type baseType, FieldBuilder interceptorsField)
        {
            const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.Instance;

            // Obtener todos los métodos virtuales
            var virtualMethods = new List<MethodInfo>();
            Type currentType = baseType;

            while (currentType != typeof(object))
            {
                virtualMethods.AddRange(currentType.GetMethods(methodFlags | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsVirtual && !m.IsFinal && !m.IsPrivate));

                currentType = currentType.BaseType;
            }

            // Sobreescribir cada método virtual
            foreach (var method in virtualMethods)
            {
                // Ignorar métodos de propiedades y eventos
                if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_") ||
                    method.Name.StartsWith("add_") || method.Name.StartsWith("remove_")))
                {
                    continue;
                }

                // Obtener información de parámetros
                ParameterInfo[] parameters = method.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Definir el método sobreescrito
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    method.Attributes & ~MethodAttributes.NewSlot,
                    method.ReturnType,
                    parameterTypes);

                // Copiar atributos de parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = methodBuilder.DefineParameter(
                        i + 1,
                        parameters[i].Attributes,
                        parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                // Generar código IL
                ILGenerator il = methodBuilder.GetILGenerator();
                GenerateInterceptionCode(il, method, interceptorsField, parameterTypes, true);

                // Marcar como sobreescritura
                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }
        }

        /// <summary>
        /// Implementa una interfaz
        /// </summary>
        private void ImplementInterface(TypeBuilder typeBuilder, Type interfaceType, FieldBuilder interceptorsField)
        {
            // Implementar métodos de la interfaz
            foreach (var method in interfaceType.GetMethods())
            {
                // Verificar si ya está implementado por la clase base
                bool alreadyImplemented = false;

                try
                {
                    var baseMethod = typeBuilder.BaseType.GetInterfaceMap(interfaceType).TargetMethods
                        .FirstOrDefault(m => m.Name == method.Name && SignaturesMatch(m, method));

                    if (baseMethod != null)
                    {
                        alreadyImplemented = true;
                    }
                }
                catch (ArgumentException)
                {
                    // La clase base no implementa la interfaz
                }

                if (alreadyImplemented)
                {
                    continue;
                }

                // Obtener información de parámetros
                ParameterInfo[] parameters = method.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Definir el método
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    method.ReturnType,
                    parameterTypes);

                // Copiar atributos de parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = methodBuilder.DefineParameter(
                        i + 1,
                        parameters[i].Attributes,
                        parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                // Generar código IL
                ILGenerator il = methodBuilder.GetILGenerator();
                GenerateInterceptionCode(il, method, interceptorsField, parameterTypes, false);

                // Marcar como implementación de la interfaz
                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            // Implementar propiedades de la interfaz
            foreach (var property in interfaceType.GetProperties())
            {
                // Verificar si ya está implementada por la clase base
                bool alreadyImplemented = false;

                try
                {
                    var baseProperty = typeBuilder.BaseType.GetProperty(property.Name);
                    if (baseProperty != null && baseProperty.PropertyType == property.PropertyType)
                    {
                        alreadyImplemented = true;
                    }
                }
                catch
                {
                    // La propiedad no existe en la clase base
                }

                if (alreadyImplemented)
                {
                    continue;
                }

                // Definir la propiedad
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.None,
                    property.PropertyType,
                    null);

                // Implementar método get si existe
                if (property.GetMethod != null)
                {
                    MethodInfo getMethod = property.GetMethod;

                    MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                        getMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                        getMethod.ReturnType,
                        Type.EmptyTypes);

                    ILGenerator getIL = getMethodBuilder.GetILGenerator();
                    GenerateInterceptionCode(getIL, getMethod, interceptorsField, Type.EmptyTypes, false);

                    propertyBuilder.SetGetMethod(getMethodBuilder);
                    typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);
                }

                // Implementar método set si existe
                if (property.SetMethod != null)
                {
                    MethodInfo setMethod = property.SetMethod;

                    MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                        setMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                        setMethod.ReturnType,
                        new[] { property.PropertyType });

                    ILGenerator setIL = setMethodBuilder.GetILGenerator();
                    GenerateInterceptionCode(setIL, setMethod, interceptorsField, new[] { property.PropertyType }, false);

                    propertyBuilder.SetSetMethod(setMethodBuilder);
                    typeBuilder.DefineMethodOverride(setMethodBuilder, setMethod);
                }
            }

            // Procesar interfaces heredadas
            foreach (var parentInterface in interfaceType.GetInterfaces())
            {
                ImplementInterface(typeBuilder, parentInterface, interceptorsField);
            }
        }

        /// <summary>
        /// Genera código de intercepción para un método
        /// </summary>
        private void GenerateInterceptionCode(
            ILGenerator il,
            MethodInfo method,
            FieldBuilder interceptorsField,
            Type[] parameterTypes,
            bool callBase)
        {
            // Declarar variables locales
            LocalBuilder invocationLocal = il.DeclareLocal(typeof(MethodInvocation));
            LocalBuilder interceptorsLocal = il.DeclareLocal(typeof(IList<IInterceptor>));
            LocalBuilder resultLocal = method.ReturnType != typeof(void) ? il.DeclareLocal(method.ReturnType) : null;

            // Cargar interceptores y comprobar si hay alguno
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, interceptorsField);
            il.Emit(OpCodes.Stloc, interceptorsLocal);

            // Crear arreglo de argumentos
            il.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));

            // Cargar los argumentos en el arreglo
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);

                // Convertir tipo de valor a objeto si es necesario
                if (parameterTypes[i].IsValueType)
                {
                    il.Emit(OpCodes.Box, parameterTypes[i]);
                }

                il.Emit(OpCodes.Stelem_Ref);
            }

            // Crear instancia de MethodInvocation
            il.Emit(OpCodes.Ldarg_0);                      // this
            il.Emit(OpCodes.Ldtoken, method.DeclaringType); // RuntimeTypeHandle
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));  // convert to Type
            il.Emit(OpCodes.Ldstr, method.Name);           // method name

            // Obtener arreglo de tipos de parámetros
            il.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(Type));

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldtoken, parameterTypes[i]);
                il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                il.Emit(OpCodes.Stelem_Ref);
            }

            // El arreglo de argumentos debe estar en la pila

            // Invocar constructor de MethodInvocation
            ConstructorInfo invocationCtor = typeof(MethodInvocation).GetConstructor(
                new[] { typeof(object), typeof(Type), typeof(string), typeof(Type[]), typeof(object[]) });

            il.Emit(OpCodes.Newobj, invocationCtor);
            il.Emit(OpCodes.Stloc, invocationLocal);

            // Procesar a través de interceptores
            Label noInterceptorsLabel = il.DefineLabel();

            // Comprobar si hay interceptores
            il.Emit(OpCodes.Ldloc, interceptorsLocal);
            il.Emit(OpCodes.Callvirt, typeof(ICollection<IInterceptor>).GetProperty("Count").GetGetMethod());
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Beq, noInterceptorsLabel);

            // Obtener enumerador
            il.Emit(OpCodes.Ldloc, interceptorsLocal);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerable<IInterceptor>).GetMethod("GetEnumerator"));
            LocalBuilder enumeratorLocal = il.DeclareLocal(typeof(IEnumerator<IInterceptor>));
            il.Emit(OpCodes.Stloc, enumeratorLocal);

            // Try/finally para el enumerador
            il.BeginExceptionBlock();

            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();

            il.MarkLabel(loopStart);

            // Comprobar MoveNext
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext"));
            il.Emit(OpCodes.Brfalse, loopEnd);

            // Obtener interceptor actual
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerator<IInterceptor>).GetProperty("Current").GetGetMethod());

            // Invocar Intercept
            il.Emit(OpCodes.Ldloc, invocationLocal);
            il.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod("Intercept"));

            // Siguiente iteración
            il.Emit(OpCodes.Br, loopStart);

            il.MarkLabel(loopEnd);

            // Finally block para liberar enumerador
            il.BeginFinallyBlock();

            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose"));

            il.EndExceptionBlock();

            // Saltar la sección de llamada a base
            Label returnLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, returnLabel);

            // Caso: no hay interceptores
            il.MarkLabel(noInterceptorsLabel);

            if (callBase)
            {
                // Llamar al método base
                il.Emit(OpCodes.Ldarg_0);

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                }

                il.Emit(OpCodes.Call, method);

                // Guardar resultado si no es void
                if (method.ReturnType != typeof(void))
                {
                    il.Emit(OpCodes.Stloc, resultLocal);

                    // Actualizar ReturnValue en la invocación
                    il.Emit(OpCodes.Ldloc, invocationLocal);
                    il.Emit(OpCodes.Ldloc, resultLocal);

                    if (method.ReturnType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, method.ReturnType);
                    }

                    il.Emit(OpCodes.Callvirt, typeof(MethodInvocation).GetProperty("ReturnValue").GetSetMethod());
                }
            }
            else
            {
                // No llamar a base, lanzar NotImplementedException
                il.Emit(OpCodes.Ldstr, "Interface method not implemented");
                il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(string) }));
                il.Emit(OpCodes.Throw);
            }

            // Sección de retorno
            il.MarkLabel(returnLabel);

            // Obtener el valor de retorno de la invocación si no es void
            if (method.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Ldloc, invocationLocal);
                il.Emit(OpCodes.Callvirt, typeof(MethodInvocation).GetProperty("ReturnValue").GetGetMethod());

                // Convertir de object al tipo de retorno
                if (method.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, method.ReturnType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, method.ReturnType);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Comprueba si las firmas de dos métodos coinciden
        /// </summary>
        private bool SignaturesMatch(MethodInfo method1, MethodInfo method2)
        {
            if (method1.Name != method2.Name)
                return false;

            if (method1.ReturnType != method2.ReturnType)
                return false;

            ParameterInfo[] params1 = method1.GetParameters();
            ParameterInfo[] params2 = method2.GetParameters();

            if (params1.Length != params2.Length)
                return false;

            for (int i = 0; i < params1.Length; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                    return false;
            }

            return true;
        }
    }
}