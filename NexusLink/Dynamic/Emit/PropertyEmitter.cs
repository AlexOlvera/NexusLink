using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NexusLink.Dynamic.Emit
{
    /// <summary>
    /// Facilita la emisión de propiedades en tipos dinámicos
    /// </summary>
    public class PropertyEmitter
    {
        private readonly System.Reflection.Emit.TypeBuilder _typeBuilder;

        public PropertyEmitter(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder;
        }

        /// <summary>
        /// Emite una propiedad de sólo lectura
        /// </summary>
        public PropertyBuilder EmitReadOnlyProperty(string name, Type type)
        {
            // Crear campo de respaldo
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(
                $"_{name}",
                type,
                FieldAttributes.Private);

            // Crear propiedad
            PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(
                name,
                PropertyAttributes.None,
                type,
                Type.EmptyTypes);

            // Crear método getter
            MethodBuilder getMethodBuilder = _typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                type,
                Type.EmptyTypes);

            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            // Asignar getter a la propiedad
            propertyBuilder.SetGetMethod(getMethodBuilder);

            return propertyBuilder;
        }

        /// <summary>
        /// Emite una propiedad de lectura/escritura
        /// </summary>
        public PropertyBuilder EmitProperty(string name, Type type)
        {
            // Crear campo de respaldo
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(
                $"_{name}",
                type,
                FieldAttributes.Private);

            // Crear propiedad
            PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(
                name,
                PropertyAttributes.HasDefault,
                type,
                Type.EmptyTypes);

            // Crear método getter
            MethodBuilder getMethodBuilder = _typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                type,
                Type.EmptyTypes);

            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            // Crear método setter
            MethodBuilder setMethodBuilder = _typeBuilder.DefineMethod(
                $"set_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                null,
                new[] { type });

            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);

            // Asignar métodos a la propiedad
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            return propertyBuilder;
        }

        /// <summary>
        /// Emite una propiedad que notifica cambios
        /// </summary>
        public PropertyBuilder EmitNotifyingProperty(string name, Type type)
        {
            // Asegurar que el tipo implemente INotifyPropertyChanged
            Type[] interfaces = _typeBuilder.GetInterfaces();
            bool implementsInterface = false;
            foreach (Type interfaceType in interfaces)
            {
                if (interfaceType == typeof(System.ComponentModel.INotifyPropertyChanged))
                {
                    implementsInterface = true;
                    break;
                }
            }

            if (!implementsInterface)
            {
                throw new InvalidOperationException("Type must implement INotifyPropertyChanged to emit notifying properties");
            }

            // Crear campo de respaldo
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(
                $"_{name}",
                type,
                FieldAttributes.Private);

            // Crear propiedad
            PropertyBuilder propertyBuilder = _typeBuilder.DefineProperty(
                name,
                PropertyAttributes.HasDefault,
                type,
                Type.EmptyTypes);

            // Crear método getter
            MethodBuilder getMethodBuilder = _typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                type,
                Type.EmptyTypes);

            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            // Crear método setter
            MethodBuilder setMethodBuilder = _typeBuilder.DefineMethod(
                $"set_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                null,
                new[] { type });

            // Obtener método OnPropertyChanged
            MethodInfo raisePropertyChanged = _typeBuilder.GetMethod("OnPropertyChanged") ??
                                             _typeBuilder.GetMethod("RaisePropertyChanged");

            if (raisePropertyChanged == null)
            {
                throw new InvalidOperationException("Type must have OnPropertyChanged or RaisePropertyChanged method");
            }

            // Generar código para el setter con notificación
            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            Label exitLabel = setIL.DefineLabel();

            // Comparar valor actual con nuevo valor
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldfld, fieldBuilder);
            setIL.Emit(OpCodes.Ldarg_1);

            if (type.IsValueType)
            {
                MethodInfo equals = type.GetMethod("Equals", new[] { type });
                setIL.EmitCall(OpCodes.Call, equals, null);
            }
            else
            {
                MethodInfo equals = typeof(object).GetMethod("Equals", new[] { typeof(object) });
                setIL.EmitCall(OpCodes.Callvirt, equals, null);
            }

            setIL.Emit(OpCodes.Brtrue, exitLabel);

            // Asignar nuevo valor
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);

            // Notificar cambio
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldstr, name);
            setIL.EmitCall(OpCodes.Call, raisePropertyChanged, null);

            // Salir
            setIL.MarkLabel(exitLabel);
            setIL.Emit(OpCodes.Ret);

            // Asignar métodos a la propiedad
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            return propertyBuilder;
        }
    }
}