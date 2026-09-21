using Rampage.Data.Client;
using Rampage.Reflection;
using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Rampage.Dynamic
{
    /// <summary>
    /// 使用 Reflection.Emit 为数据库客户端类型动态生成绑定名称的扩展子类 / Build named dynamic subclasses of database client types using Reflection.Emit
    /// </summary>
    internal class DynamicDatabaseClientBuilder
    {
        /// <summary>
        /// 动态数据库客户端所在的模块名 / The module name hosting the generated database clients
        /// </summary>
        public static readonly string ModuleName = "DynamicDatabaseClients";

        /// <summary>
        /// 为客户端类型生成绑定配置名称的动态子类：生成的无参构造会调用基类构造并把配置名写入 Name 属性
        /// Build a dynamic subclass bound to the config name: its parameterless constructor calls the base constructor then assigns the Name property
        /// </summary>
        /// <param name="clientConfig">客户端配置（使用其 Name）/ The client config (its Name is used)</param>
        /// <param name="clientType">客户端基类类型，须有无参构造 / The client base type, must have a parameterless constructor</param>
        /// <returns>生成的子类类型 / The generated subclass type</returns>
        /// <remarks>同名子类已存在时直接返回缓存 / A cached subclass of the same name is returned directly</remarks>
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
