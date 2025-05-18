using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NexusLink.AOP.Interception;

namespace NexusLink.Dynamic.DynamicProxy
{
    /// <summary>
    /// Constructor de proxies para interfaces.
    /// Permite generar implementaciones dinámicas de interfaces con interceptores.
    /// </summary>
    public class InterfaceProxyBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;

        /// <summary>
        /// Crea una nueva instancia de InterfaceProxyBuilder
        /// </summary>
        /// <param name="moduleBuilder">ModuleBuilder para generar tipos</param>
        public InterfaceProxyBuilder(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder ?? throw new ArgumentNullException(nameof(moduleBuilder));
        }

        /// <summary>
        /// Genera un tipo que implementa una interfaz con intercepción
        /// </summary>
        /// <param name="interfaceType">Tipo de interfaz a implementar</param>
        /// <param name="additionalInterfaces">Interfaces adicionales a implementar</param>
        /// <returns>Tipo generado</returns>
        public Type BuildProxyType(Type interfaceType, params Type[] additionalInterfaces)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            if (!interfaceType.IsInterface)
                throw new ArgumentException($"{interfaceType.Name} is not an interface", nameof(interfaceType));

            // Validar interfaces adicionales
            foreach (var additionalInterface in additionalInterfaces)
            {
                if (!additionalInterface.IsInterface)
                    throw new ArgumentException($"{additionalInterface.Name} is not an interface", nameof(additionalInterfaces));
            }

            // Crear el nombre del tipo proxy
            string proxyName = $"{interfaceType.Name}Proxy_{Guid.NewGuid():N}";

            // Crear un arreglo con todas las interfaces
            var allInterfaces = new List<Type> { interfaceType };
            allInterfaces.AddRange(additionalInterfaces);

            // Definir el tipo proxy
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                proxyName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(object),
                allInterfaces.ToArray());

            // Definir campos
            FieldBuilder interceptorsField = typeBuilder.DefineField(
                "_interceptors",
                typeof(IList<IInterceptor>),
                FieldAttributes.Private);

            FieldBuilder targetField = typeBuilder.DefineField(
                "_target",
                typeof(object),
                FieldAttributes.Private);

            // Definir constructor
            ImplementConstructor(typeBuilder, interceptorsField, targetField);

            // Implementar propiedades
            ImplementInterceptorsProperty(typeBuilder, interceptorsField);
            ImplementTargetProperty(typeBuilder, targetField);

            // Implementar métodos de las interfaces
            foreach (var interfaceToImplement in allInterfaces)
            {
                ImplementInterfaceMethods(typeBuilder, interfaceToImplement, interceptorsField, targetField);
            }

            // Crear el tipo
            return typeBuilder.CreateType();
        }

        /// <summary>
        /// Implementa el constructor del proxy
        /// </summary>
        private void ImplementConstructor(TypeBuilder typeBuilder, FieldBuilder interceptorsField, FieldBuilder targetField)
        {
            // Constructor sin parámetros
            var defaultCtor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var defaultIL = defaultCtor.GetILGenerator();

            // Llamar al constructor base
            defaultIL.Emit(OpCodes.Ldarg_0);
            defaultIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Inicializar lista de interceptores
            defaultIL.Emit(OpCodes.Ldarg_0);
            defaultIL.Emit(OpCodes.Newobj, typeof(List<IInterceptor>).GetConstructor(Type.EmptyTypes));
            defaultIL.Emit(OpCodes.Stfld, interceptorsField);

            // Inicializar target como null
            defaultIL.Emit(OpCodes.Ldarg_0);
            defaultIL.Emit(OpCodes.Ldnull);
            defaultIL.Emit(OpCodes.Stfld, targetField);

            defaultIL.Emit(OpCodes.Ret);

            // Constructor con target
            var targetCtor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(object) });

            var targetCtorIL = targetCtor.GetILGenerator();

            // Llamar al constructor base
            targetCtorIL.Emit(OpCodes.Ldarg_0);
            targetCtorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Inicializar lista de interceptores
            targetCtorIL.Emit(OpCodes.Ldarg_0);
            targetCtorIL.Emit(OpCodes.Newobj, typeof(List<IInterceptor>).GetConstructor(Type.EmptyTypes));
            targetCtorIL.Emit(OpCodes.Stfld, interceptorsField);

            // Establecer target
            targetCtorIL.Emit(OpCodes.Ldarg_0);
            targetCtorIL.Emit(OpCodes.Ldarg_1);
            targetCtorIL.Emit(OpCodes.Stfld, targetField);

            targetCtorIL.Emit(OpCodes.Ret);
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
        /// Implementa la propiedad Target
        /// </summary>
        private void ImplementTargetProperty(TypeBuilder typeBuilder, FieldBuilder targetField)
        {
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                "Target",
                PropertyAttributes.None,
                typeof(object),
                null);

            // Método get
            MethodBuilder getMethod = typeBuilder.DefineMethod(
                "get_Target",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(object),
                Type.EmptyTypes);

            ILGenerator getIL = getMethod.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, targetField);
            getIL.Emit(OpCodes.Ret);

            // Método set
            MethodBuilder setMethod = typeBuilder.DefineMethod(
                "set_Target",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                null,
                new[] { typeof(object) });

            ILGenerator setIL = setMethod.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, targetField);
            setIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethod);
            propertyBuilder.SetSetMethod(setMethod);
        }

        /// <summary>
        /// Implementa los métodos de una interfaz
        /// </summary>
        private void ImplementInterfaceMethods(
            TypeBuilder typeBuilder,
            Type interfaceType,
            FieldBuilder interceptorsField,
            FieldBuilder targetField)
        {
            foreach (var methodInfo in interfaceType.GetMethods())
            {
                ImplementInterfaceMethod(typeBuilder, methodInfo, interceptorsField, targetField);
            }

            foreach (var propertyInfo in interfaceType.GetProperties())
            {
                ImplementInterfaceProperty(typeBuilder, propertyInfo, interceptorsField, targetField);
            }

            foreach (var eventInfo in interfaceType.GetEvents())
            {
                ImplementInterfaceEvent(typeBuilder, eventInfo, interceptorsField, targetField);
            }
        }

        /// <summary>
        /// Implementa un método de interfaz
        /// </summary>
        private void ImplementInterfaceMethod(
            TypeBuilder typeBuilder,
            MethodInfo methodInfo,
            FieldBuilder interceptorsField,
            FieldBuilder targetField)
        {
            // Obtener tipos de parámetros
            ParameterInfo[] parameters = methodInfo.GetParameters();
            Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            // Definir método
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                methodInfo.ReturnType,
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

            // Generar código de intercepción
            ILGenerator il = methodBuilder.GetILGenerator();
            GenerateInterceptionCode(il, methodInfo, interceptorsField, targetField, parameterTypes);

            // Marcar como implementación
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        /// <summary>
        /// Implementa una propiedad de interfaz
        /// </summary>
        private void ImplementInterfaceProperty(
            TypeBuilder typeBuilder,
            PropertyInfo propertyInfo,
            FieldBuilder interceptorsField,
            FieldBuilder targetField)
        {
            // Definir propiedad
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                propertyInfo.Name,
                PropertyAttributes.None,
                propertyInfo.PropertyType,
                null);

            // Implementar método get si existe
            MethodInfo getMethod = propertyInfo.GetMethod;
            if (getMethod != null)
            {
                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                    getMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    getMethod.ReturnType,
                    Type.EmptyTypes);

                ILGenerator getIL = getMethodBuilder.GetILGenerator();
                GenerateInterceptionCode(getIL, getMethod, interceptorsField, targetField, Type.EmptyTypes);

                propertyBuilder.SetGetMethod(getMethodBuilder);
                typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);
            }

            // Implementar método set si existe
            MethodInfo setMethod = propertyInfo.SetMethod;
            if (setMethod != null)
            {
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                    setMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    setMethod.ReturnType,
                    new[] { propertyInfo.PropertyType });

                ILGenerator setIL = setMethodBuilder.GetILGenerator();
                GenerateInterceptionCode(setIL, setMethod, interceptorsField, targetField, new[] { propertyInfo.PropertyType });

                propertyBuilder.SetSetMethod(setMethodBuilder);
                typeBuilder.DefineMethodOverride(setMethodBuilder, setMethod);
            }
        }

        /// <summary>
        /// Implementa un evento de interfaz
        /// </summary>
        private void ImplementInterfaceEvent(
            TypeBuilder typeBuilder,
            EventInfo eventInfo,
            FieldBuilder interceptorsField,
            FieldBuilder targetField)
        {
            // Definir evento
            EventBuilder eventBuilder = typeBuilder.DefineEvent(
                eventInfo.Name,
                EventAttributes.None,
                eventInfo.EventHandlerType);

            // Implementar método add
            MethodInfo addMethod = eventInfo.AddMethod;
            if (addMethod != null)
            {
                MethodBuilder addMethodBuilder = typeBuilder.DefineMethod(
                    addMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    addMethod.ReturnType,
                    new[] { eventInfo.EventHandlerType });

                ILGenerator addIL = addMethodBuilder.GetILGenerator();
                GenerateInterceptionCode(addIL, addMethod, interceptorsField, targetField, new[] { eventInfo.EventHandlerType });

                eventBuilder.SetAddOnMethod(addMethodBuilder);
                typeBuilder.DefineMethodOverride(addMethodBuilder, addMethod);
            }

            // Implementar método remove
            MethodInfo removeMethod = eventInfo.RemoveMethod;
            if (removeMethod != null)
            {
                MethodBuilder removeMethodBuilder = typeBuilder.DefineMethod(
                    removeMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    removeMethod.ReturnType,
                    new[] { eventInfo.EventHandlerType });

                ILGenerator removeIL = removeMethodBuilder.GetILGenerator();
                GenerateInterceptionCode(removeIL, removeMethod, interceptorsField, targetField, new[] { eventInfo.EventHandlerType });

                eventBuilder.SetRemoveOnMethod(removeMethodBuilder);
                typeBuilder.DefineMethodOverride(removeMethodBuilder, removeMethod);
            }
        }

        /// <summary>
        /// Genera código de intercepción para un método
        /// </summary>
        private void GenerateInterceptionCode(
            ILGenerator il,
            MethodInfo method,
            FieldBuilder interceptorsField,
            FieldBuilder targetField,
            Type[] parameterTypes)
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
            il.Emit(OpCodes.Ldfld, targetField);           // target
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

            // Saltar la sección NoInterceptors
            Label returnLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, returnLabel);

            // Caso: no hay interceptores o target es null
            il.MarkLabel(noInterceptorsLabel);

            // Comprobar si el target es null
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, targetField);
            Label targetNullLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, targetNullLabel);

            // Target no es null, llamar al método original
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, targetField);

            // Comprobar que el target implementa la interfaz
            il.Emit(OpCodes.Isinst, method.DeclaringType);
            il.Emit(OpCodes.Dup);

            // Si el target no implementa la interfaz, generar excepción
            Label targetImplementsInterfaceLabel = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, targetImplementsInterfaceLabel);

            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldstr, $"Target object does not implement interface {method.DeclaringType.Name}");
            il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
            il.Emit(OpCodes.Throw);

            il.MarkLabel(targetImplementsInterfaceLabel);

            // Llamar al método en el target
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
            }

            il.Emit(OpCodes.Callvirt, method);

            // Si el método tiene valor de retorno, guardarlo
            if (method.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Stloc, resultLocal);
            }

            il.Emit(OpCodes.Br, returnLabel);

            // Target es null, lanzar excepción o retornar valor predeterminado
            il.MarkLabel(targetNullLabel);

            if (method.ReturnType == typeof(void))
            {
                // No hacer nada para void
            }
            else if (method.ReturnType.IsValueType)
            {
                // Para tipos de valor, retornar valor predeterminado
                il.Emit(OpCodes.Ldloca_S, resultLocal);
                il.Emit(OpCodes.Initobj, method.ReturnType);
                il.Emit(OpCodes.Ldloc, resultLocal);
            }
            else
            {
                // Para tipos de referencia, retornar null
                il.Emit(OpCodes.Ldnull);
            }

            // Sección de retorno
            il.MarkLabel(returnLabel);

            // Obtener el valor de retorno de la invocación si no es void
            if (method.ReturnType != typeof(void))
            {
                // Si estamos aquí desde la intercepción, obtener el valor de retorno de la invocación
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
    }
}