using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NexusLink.Dynamic.Emit
{
    /// <summary>
    /// Facilita la implementación de interfaces en tipos dinámicos
    /// </summary>
    public class InterfaceImplementer
    {
        private readonly System.Reflection.Emit.TypeBuilder _typeBuilder;
        private readonly PropertyEmitter _propertyEmitter;
        private readonly MethodEmitter _methodEmitter;

        public InterfaceImplementer(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder;
            _propertyEmitter = new PropertyEmitter(typeBuilder);
            _methodEmitter = new MethodEmitter(typeBuilder);
        }

        /// <summary>
        /// Implementa una interfaz en el tipo dinámico
        /// </summary>
        public void ImplementInterface(Type interfaceType)
        {
            // Verificar que sea una interfaz
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("Type must be an interface", nameof(interfaceType));
            }

            // Añadir la interfaz a la lista de interfaces implementadas
            _typeBuilder.AddInterfaceImplementation(interfaceType);

            // Implementar propiedades
            foreach (PropertyInfo property in interfaceType.GetProperties())
            {
                ImplementProperty(property);
            }

            // Implementar métodos
            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                // Ignorar métodos de propiedades
                if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                {
                    continue;
                }

                ImplementMethod(method);
            }

            // Implementar eventos
            foreach (EventInfo eventInfo in interfaceType.GetEvents())
            {
                ImplementEvent(eventInfo);
            }
        }

        /// <summary>
        /// Implementa una propiedad de interfaz
        /// </summary>
        private void ImplementProperty(PropertyInfo property)
        {
            // Crear la propiedad
            PropertyBuilder propertyBuilder = _propertyEmitter.EmitProperty(
                property.Name,
                property.PropertyType);

            // Implementar método getter si existe
            MethodInfo getMethod = property.GetGetMethod();
            if (getMethod != null)
            {
                _typeBuilder.DefineMethodOverride(
                    propertyBuilder.GetGetMethod(),
                    getMethod);
            }

            // Implementar método setter si existe
            MethodInfo setMethod = property.GetSetMethod();
            if (setMethod != null)
            {
                _typeBuilder.DefineMethodOverride(
                    propertyBuilder.GetSetMethod(),
                    setMethod);
            }
        }

        /// <summary>
        /// Implementa un método de interfaz
        /// </summary>
        private void ImplementMethod(MethodInfo method)
        {
            _methodEmitter.EmitInterfaceMethod(method);
        }

        /// <summary>
        /// Implementa un evento de interfaz
        /// </summary>
        private void ImplementEvent(EventInfo eventInfo)
        {
            // Definir el tipo de delegado del evento
            Type handlerType = eventInfo.EventHandlerType;

            // Crear campo de respaldo
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(
                $"_{eventInfo.Name}",
                handlerType,
                FieldAttributes.Private);

            // Crear evento
            EventBuilder eventBuilder = _typeBuilder.DefineEvent(
                eventInfo.Name,
                EventAttributes.None,
                handlerType);

            // Implementar método add
            MethodInfo addMethod = eventInfo.GetAddMethod();
            if (addMethod != null)
            {
                MethodBuilder addMethodBuilder = _typeBuilder.DefineMethod(
                    addMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    null,
                    new[] { handlerType });

                ILGenerator addIL = addMethodBuilder.GetILGenerator();

                // Implementar Delegate.Combine
                addIL.Emit(OpCodes.Ldarg_0);
                addIL.Emit(OpCodes.Ldarg_0);
                addIL.Emit(OpCodes.Ldfld, fieldBuilder);
                addIL.Emit(OpCodes.Ldarg_1);
                addIL.EmitCall(OpCodes.Call, typeof(Delegate).GetMethod("Combine", new[] { typeof(Delegate), typeof(Delegate) }), null);
                addIL.Emit(OpCodes.Castclass, handlerType);
                addIL.Emit(OpCodes.Stfld, fieldBuilder);
                addIL.Emit(OpCodes.Ret);

                _typeBuilder.DefineMethodOverride(addMethodBuilder, addMethod);
                eventBuilder.SetAddOnMethod(addMethodBuilder);
            }

            // Implementar método remove
            MethodInfo removeMethod = eventInfo.GetRemoveMethod();
            if (removeMethod != null)
            {
                MethodBuilder removeMethodBuilder = _typeBuilder.DefineMethod(
                    removeMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    null,
                    new[] { handlerType });

                ILGenerator removeIL = removeMethodBuilder.GetILGenerator();

                // Implementar Delegate.Remove
                removeIL.Emit(OpCodes.Ldarg_0);
                removeIL.Emit(OpCodes.Ldarg_0);
                removeIL.Emit(OpCodes.Ldfld, fieldBuilder);
                removeIL.Emit(OpCodes.Ldarg_1);
                removeIL.EmitCall(OpCodes.Call, typeof(Delegate).GetMethod("Remove", new[] { typeof(Delegate), typeof(Delegate) }), null);
                removeIL.Emit(OpCodes.Castclass, handlerType);
                removeIL.Emit(OpCodes.Stfld, fieldBuilder);
                removeIL.Emit(OpCodes.Ret);

                _typeBuilder.DefineMethodOverride(removeMethodBuilder, removeMethod);
                eventBuilder.SetRemoveOnMethod(removeMethodBuilder);
            }
        }
    }
}