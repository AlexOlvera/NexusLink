using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.IO;

namespace NexusLink.Dynamic.Emit
{
    /// <summary>
    /// Genera ensamblados dinámicos
    /// </summary>
    public class AssemblyGenerator
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, Type> _generatedTypes;

        /// <summary>
        /// Crea un generador de ensamblados en memoria
        /// </summary>
        public AssemblyGenerator(string assemblyName)
        {
            var name = new AssemblyName(assemblyName);
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                name, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
            _generatedTypes = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Crea un nuevo tipo
        /// </summary>
        public System.Reflection.Emit.TypeBuilder CreateType(string typeName)
        {
            return _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class);
        }

        /// <summary>
        /// Crea un nuevo tipo que hereda de una clase base
        /// </summary>
        public System.Reflection.Emit.TypeBuilder CreateType(string typeName, Type baseType)
        {
            return _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                baseType);
        }

        /// <summary>
        /// Crea un nuevo tipo que implementa interfaces
        /// </summary>
        public System.Reflection.Emit.TypeBuilder CreateType(string typeName, Type[] interfaces)
        {
            return _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                null,
                interfaces);
        }

        /// <summary>
        /// Obtiene un generador de propiedades para un tipo
        /// </summary>
        public PropertyEmitter GetPropertyEmitter(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            return new PropertyEmitter(typeBuilder);
        }

        /// <summary>
        /// Obtiene un generador de métodos para un tipo
        /// </summary>
        public MethodEmitter GetMethodEmitter(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            return new MethodEmitter(typeBuilder);
        }

        /// <summary>
        /// Obtiene un implementador de interfaces para un tipo
        /// </summary>
        public InterfaceImplementer GetInterfaceImplementer(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            return new InterfaceImplementer(typeBuilder);
        }

        /// <summary>
        /// Finaliza la creación de un tipo
        /// </summary>
        public Type CreateTypeComplete(System.Reflection.Emit.TypeBuilder typeBuilder)
        {
            Type type = typeBuilder.CreateType();
            _generatedTypes[type.FullName] = type;
            return type;
        }

        /// <summary>
        /// Obtiene un tipo generado por su nombre
        /// </summary>
        public Type GetType(string typeName)
        {
            if (_generatedTypes.TryGetValue(typeName, out Type type))
            {
                return type;
            }

            return null;
        }
    }
}