using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace NexusLink.Dynamic.Emit
{
    /// <summary>
    /// Facilita la emisión de métodos en tipos dinámicos
    /// </summary>
    public class MethodEmitter
    {
        private readonly System.Reflection.Emit.TypeBuilder _typeBuilder;

        public MethodEmitter(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder;
        }

        /// <summary>
        /// Emite un método simple sin parámetros
        /// </summary>
        public MethodBuilder EmitMethod(string name, Type returnType)
        {
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                returnType,
                Type.EmptyTypes);

            return methodBuilder;
        }

        /// <summary>
        /// Emite un método con parámetros
        /// </summary>
        public MethodBuilder EmitMethod(string name, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                returnType,
                parameterTypes);

            return methodBuilder;
        }

        /// <summary>
        /// Emite un método que devuelve un valor constante
        /// </summary>
        public MethodBuilder EmitConstantMethod(string name, object constantValue)
        {
            Type returnType = constantValue?.GetType() ?? typeof(object);

            MethodBuilder methodBuilder = EmitMethod(name, returnType);
            ILGenerator il = methodBuilder.GetILGenerator();

            // Cargar valor constante
            if (constantValue == null)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else if (returnType == typeof(string))
            {
                il.Emit(OpCodes.Ldstr, (string)constantValue);
            }
            else if (returnType == typeof(int))
            {
                il.Emit(OpCodes.Ldc_I4, (int)constantValue);
            }
            else if (returnType == typeof(long))
            {
                il.Emit(OpCodes.Ldc_I8, (long)constantValue);
            }
            else if (returnType == typeof(float))
            {
                il.Emit(OpCodes.Ldc_R4, (float)constantValue);
            }
            else if (returnType == typeof(double))
            {
                il.Emit(OpCodes.Ldc_R8, (double)constantValue);
            }
            else if (returnType == typeof(bool))
            {
                il.Emit((bool)constantValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }
            else
            {
                throw new NotSupportedException($"Constant of type {returnType} is not supported");
            }

            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        /// <summary>
        /// Emite un método delegador que llama a otro método
        /// </summary>
        public MethodBuilder EmitDelegatingMethod(string name, MethodInfo targetMethod)
        {
            // Crear método con la misma firma
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                targetMethod.ReturnType,
                targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());

            ILGenerator il = methodBuilder.GetILGenerator();

            // Cargar instancia si el método no es estático
            if (!targetMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            // Cargar parámetros
            for (int i = 0; i < targetMethod.GetParameters().Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
            }

            // Llamar al método objetivo
            il.EmitCall(
                targetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                targetMethod,
                null);

            // Retornar
            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        /// <summary>
        /// Emite un método de implementación de interfaz
        /// </summary>
        public MethodBuilder EmitInterfaceMethod(MethodInfo interfaceMethod)
        {
            // Crear método con la misma firma
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                interfaceMethod.ReturnType,
                interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray());

            ILGenerator il = methodBuilder.GetILGenerator();

            // Implementación por defecto: devolver valor por defecto
            if (interfaceMethod.ReturnType != typeof(void))
            {
                if (interfaceMethod.ReturnType.IsValueType)
                {
                    // Para tipos de valor, inicializar con 0
                    if (interfaceMethod.ReturnType == typeof(int) ||
                        interfaceMethod.ReturnType == typeof(byte) ||
                        interfaceMethod.ReturnType == typeof(short))
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                    }
                    else if (interfaceMethod.ReturnType == typeof(long))
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_I8);
                    }
                    else if (interfaceMethod.ReturnType == typeof(float))
                    {
                        il.Emit(OpCodes.Ldc_R4, 0f);
                    }
                    else if (interfaceMethod.ReturnType == typeof(double))
                    {
                        il.Emit(OpCodes.Ldc_R8, 0.0);
                    }
                    else if (interfaceMethod.ReturnType == typeof(bool))
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                    }
                    else
                    {
                        // Para otros tipos de valor, usar initobj
                        LocalBuilder local = il.DeclareLocal(interfaceMethod.ReturnType);
                        il.Emit(OpCodes.Ldloca_S, local);
                        il.Emit(OpCodes.Initobj, interfaceMethod.ReturnType);
                        il.Emit(OpCodes.Ldloc, local);
                    }
                }
                else
                {
                    // Para tipos de referencia, devolver null
                    il.Emit(OpCodes.Ldnull);
                }
            }

            il.Emit(OpCodes.Ret);

            // Establecer implementación de interfaz
            _typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);

            return methodBuilder;
        }
    }
}