using Rampage.Data.Client;
using Rampage.Reflection;
using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Rampage.Dynamic
{
    /// <summary>
    /// using "Reflection.Emit", build extend Database Client class dynamic code 
    /// </summary>
    internal class DynamicDatabaseClientBuilder
    {
        public static readonly string ModuleName = "DynamicDatabaseClients";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="clientType"></param>
        /// <returns></returns>
        public static Type Create(DatabaseClientConfig clientConfig, Type clientType)
        {
            string clientName = clientConfig.Name!;
            string clientTypeName = clientType.Name!;
            string extendClientTypeName = $"{clientTypeName}_{clientName}";

            // check if exists the type
            Type? createdType = DynamicCodeStore.TryGetType(extendClientTypeName);
            if (createdType != null)
            {
                return createdType;
            }

            ModuleBuilder moduleBuilder = DynamicCodeStore.GetModuleBuilder(ModuleName);

            // create a dynmaic type inherits clientType 
            TypeBuilder typeBuilder = moduleBuilder.DefineType(extendClientTypeName, TypeAttributes.Public, clientType);

            // define constructor with empty parameters
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            ILGenerator ctorIL = ctorBuilder.GetILGenerator();

            // call base()
            ConstructorInfo baseCtor = clientType.GetConstructor(Type.EmptyTypes)!;
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, baseCtor);

            // Property: DatabaseClient.Name
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldstr, clientName);
            PropertyInfo nameProperty = ReflectionTool.GetProperty(clientType, "Name")!;
            MethodInfo setNameMethod = nameProperty.GetSetMethod(true)!;
            ctorIL.Emit(OpCodes.Call, setNameMethod);

            // constructor return
            ctorIL.Emit(OpCodes.Ret);

            // created type
            Type dynamicType = typeBuilder.CreateType();

            // store the type
            DynamicCodeStore.SetType(extendClientTypeName, dynamicType);

            return dynamicType;
        }

    }
}
