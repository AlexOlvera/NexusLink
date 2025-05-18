using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using NexusLink.AOP.Interception;

namespace NexusLink.Dynamic.DynamicProxy
{
    /// <summary>
    /// Generador de proxies dinámicos para la interposición de aspectos AOP.
    /// Crea subclases o implementaciones de interfaces en tiempo de ejecución.
    /// </summary>
    public class ProxyGenerator
    {
        private static readonly ModuleBuilder _moduleBuilder;
        private readonly ProxyCache _proxyCache;

        static ProxyGenerator()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("NexusLink.DynamicProxy.Generated"),
                AssemblyBuilderAccess.Run);

            _moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
        }

        /// <summary>
        /// Crea una nueva instancia de ProxyGenerator
        /// </summary>
        public ProxyGenerator()
        {
            _proxyCache = new ProxyCache();
        }

        /// <summary>
        /// Crea un proxy para una clase
        /// </summary>
        /// <typeparam name="T">Tipo de la clase</typeparam>
        /// <param name="interceptors">Interceptores a aplicar</param>
        /// <param name="constructorArgs">Argumentos para el constructor</param>
        /// <returns>Instancia del proxy</returns>
        public T CreateClassProxy<T>(IEnumerable<IInterceptor> interceptors, params object[] constructorArgs) where T : class
        {
            Type targetType = typeof(T);
            ValidateTarget(targetType);

            // Buscar en caché primero
            Type proxyType = _proxyCache.GetProxyType(targetType)
                ?? CreateClassProxyType(targetType);

            // Crear instancia
            object instance = Activator.CreateInstance(proxyType, constructorArgs);

            // Inicializar interceptores
            object interceptorField = proxyType.GetField("__interceptors",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);

            if (interceptorField is IList<IInterceptor> interceptorList)
            {
                foreach (var interceptor in interceptors)
                {
                    interceptorList.Add(interceptor);
                }
            }

            return (T)instance;
        }

        /// <summary>
        /// Crea un proxy para una interfaz
        /// </summary>
        /// <typeparam name="T">Tipo de la interfaz</typeparam>
        /// <param name="interceptors">Interceptores a aplicar</param>
        /// <returns>Instancia del proxy</returns>
        public T CreateInterfaceProxy<T>(IEnumerable<IInterceptor> interceptors) where T : class
        {
            Type targetType = typeof(T);

            if (!targetType.IsInterface)
            {
                throw new ArgumentException($"Type {targetType.Name} is not an interface", nameof(T));
            }

            // Buscar en caché primero
            Type proxyType = _proxyCache.GetProxyType(targetType)
                ?? CreateInterfaceProxyType(targetType);

            // Crear instancia
            object instance = Activator.CreateInstance(proxyType);

            // Inicializar interceptores
            object interceptorField = proxyType.GetField("__interceptors",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);

            if (interceptorField is IList<IInterceptor> interceptorList)
            {
                foreach (var interceptor in interceptors)
                {
                    interceptorList.Add(interceptor);
                }
            }

            return (T)instance;
        }

        private Type CreateClassProxyType(Type targetType)
        {
            string proxyName = $"{targetType.Name}_Proxy_{Guid.NewGuid().ToString("N")}";
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                proxyName,
                TypeAttributes.Public | TypeAttributes.Class,
                targetType);

            // Agregar campo para los interceptores
            FieldBuilder interceptorsField = typeBuilder.DefineField(
                "__interceptors",
                typeof(List<IInterceptor>),
                FieldAttributes.Private);

            // Implementar constructores
            ImplementConstructors(typeBuilder, targetType, interceptorsField);

            // Sobreescribir métodos virtuales
            OverrideVirtualMethods(typeBuilder, targetType, interceptorsField);

            // Crear el tipo y cachear
            Type proxyType = typeBuilder.CreateType();
            _proxyCache.CacheProxyType(targetType, proxyType);

            return proxyType;
        }

        private Type CreateInterfaceProxyType(Type interfaceType)
        {
            string proxyName = $"{interfaceType.Name}_Proxy_{Guid.NewGuid().ToString("N")}";
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                proxyName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(object),
                new[] { interfaceType });

            // Agregar campo para los interceptores
            FieldBuilder interceptorsField = typeBuilder.DefineField(
                "__interceptors",
                typeof(List<IInterceptor>),
                FieldAttributes.Private);

            // Implementar constructor predeterminado
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            ILGenerator ctorIL = ctorBuilder.GetILGenerator();

            // Llamar al constructor de object
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Inicializar el campo de interceptores
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Newobj, typeof(List<IInterceptor>).GetConstructor(Type.EmptyTypes));
            ctorIL.Emit(OpCodes.Stfld, interceptorsField);
            ctorIL.Emit(OpCodes.Ret);

            // Implementar métodos de la interfaz
            ImplementInterfaceMethods(typeBuilder, interfaceType, interceptorsField);

            // Crear el tipo y cachear
            Type proxyType = typeBuilder.CreateType();
            _proxyCache.CacheProxyType(interfaceType, proxyType);

            return proxyType;
        }

        private void ImplementConstructors(TypeBuilder typeBuilder, Type targetType, FieldBuilder interceptorsField)
        {
            foreach (ConstructorInfo ctor in targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                // Obtener parámetros del constructor
                ParameterInfo[] parameters = ctor.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Definir constructor en el proxy
                ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    parameterTypes);

                // Agregar atributos de parámetro
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = ctorBuilder.DefineParameter(
                        i + 1, parameters[i].Attributes, parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                ILGenerator ctorIL = ctorBuilder.GetILGenerator();

                // Llamar al constructor base
                ctorIL.Emit(OpCodes.Ldarg_0);

                for (int i = 0; i < parameters.Length; i++)
                {
                    ctorIL.Emit(OpCodes.Ldarg, i + 1);
                }

                ctorIL.Emit(OpCodes.Call, ctor);

                // Inicializar el campo de interceptores
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Newobj, typeof(List<IInterceptor>).GetConstructor(Type.EmptyTypes));
                ctorIL.Emit(OpCodes.Stfld, interceptorsField);
                ctorIL.Emit(OpCodes.Ret);
            }
        }

        private void OverrideVirtualMethods(TypeBuilder typeBuilder, Type targetType, FieldBuilder interceptorsField)
        {
            const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (MethodInfo method in targetType.GetMethods(methodFlags)
                .Where(m => m.IsVirtual && !m.IsFinal && !m.IsPrivate))
            {
                // Obtener información de parámetros
                ParameterInfo[] parameters = method.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                // Definir el método sobreescrito
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                    method.ReturnType,
                    parameterTypes);

                // Agregar atributos de parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = methodBuilder.DefineParameter(
                        i + 1, parameters[i].Attributes, parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                ILGenerator methodIL = methodBuilder.GetILGenerator();

                // Generar código para la intercepción
                GenerateInterceptionCode(methodIL, method, interceptorsField, parameterTypes);

                // Marcar como implementación de un método específico
                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }
        }

        private void ImplementInterfaceMethods(TypeBuilder typeBuilder, Type interfaceType, FieldBuilder interceptorsField)
        {
            // Implementar métodos de la interfaz especificada
            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                ParameterInfo[] parameters = method.GetParameters();
                Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                    method.ReturnType,
                    parameterTypes);

                // Agregar atributos de parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterBuilder paramBuilder = methodBuilder.DefineParameter(
                        i + 1, parameters[i].Attributes, parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        paramBuilder.SetConstant(parameters[i].DefaultValue);
                    }
                }

                ILGenerator methodIL = methodBuilder.GetILGenerator();

                // Generar código para la intercepción
                GenerateInterceptionCode(methodIL, method, interceptorsField, parameterTypes);

                // Marcar como implementación de un método específico
                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            // Implementar métodos de interfaces heredadas
            foreach (Type parentInterface in interfaceType.GetInterfaces())
            {
                ImplementInterfaceMethods(typeBuilder, parentInterface, interceptorsField);
            }
        }

        private void GenerateInterceptionCode(ILGenerator il, MethodInfo method, FieldBuilder interceptorsField, Type[] parameterTypes)
        {
            // Declarar variables locales
            LocalBuilder invocationLocal = il.DeclareLocal(typeof(MethodInvocation));
            LocalBuilder resultLocal = method.ReturnType != typeof(void) ? il.DeclareLocal(method.ReturnType) : null;

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
            il.Emit(OpCodes.Ldarg_0);                                  // this
            il.Emit(OpCodes.Ldtoken, method.DeclaringType);            // RuntimeTypeHandle
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));  // convert to Type
            il.Emit(OpCodes.Ldstr, method.Name);                       // method name

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

            // Argumentos
            // El arreglo de argumentos debe estar en la pila

            // Invocar constructor de MethodInvocation
            ConstructorInfo invocationCtor = typeof(MethodInvocation).GetConstructor(
                new[] { typeof(object), typeof(Type), typeof(string), typeof(Type[]), typeof(object[]) });

            il.Emit(OpCodes.Newobj, invocationCtor);
            il.Emit(OpCodes.Stloc, invocationLocal);

            // Obtener interceptores
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, interceptorsField);

            // Para cada interceptor, invocar Intercept
            Label noInterceptorsLabel = il.DefineLabel();

            // Comprobar si hay interceptores
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Callvirt, typeof(ICollection<IInterceptor>).GetProperty("Count").GetGetMethod());
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Beq, noInterceptorsLabel);

            // Obtener enumerador
            il.Emit(OpCodes.Callvirt, typeof(IEnumerable<IInterceptor>).GetMethod("GetEnumerator"));
            LocalBuilder enumeratorLocal = il.DeclareLocal(typeof(IEnumerator<IInterceptor>));
            il.Emit(OpCodes.Stloc, enumeratorLocal);

            // Loop de enumeración
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();

            // Try/finally para el enumerador
            il.BeginExceptionBlock();

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

            // Si no hay interceptores, ejecutar el método base
            il.MarkLabel(noInterceptorsLabel);

            // Obtener el valor de retorno de la invocación
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

                il.Emit(OpCodes.Stloc, resultLocal);

                // Retornar valor
                il.Emit(OpCodes.Ldloc, resultLocal);
            }

            il.Emit(OpCodes.Ret);
        }

        private void ValidateTarget(Type targetType)
        {
            if (targetType.IsSealed)
            {
                throw new ArgumentException($"Cannot create proxy for sealed type {targetType.Name}", nameof(targetType));
            }

            if (targetType.IsInterface)
            {
                throw new ArgumentException($"Cannot create class proxy for interface {targetType.Name}. Use CreateInterfaceProxy instead.", nameof(targetType));
            }

            if (!HasAccessibleConstructor(targetType))
            {
                throw new ArgumentException($"Type {targetType.Name} does not have an accessible constructor", nameof(targetType));
            }
        }

        private bool HasAccessibleConstructor(Type type)
        {
            return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0;
        }
    }
}