using System;
using System.Reflection.Emit;
using System.Reflection;
using Rampage.Data;
using Rampage.Hierarchy;
using Rampage.Reflection;
using Rampage.Data.Query;

namespace Rampage.Dynamic
{
    /// <summary>
    /// using "Reflection.Emit", build Data Access Object Implemention dynamic code 
    /// </summary>
    public class DynamicDaoCodeBuilder
    {

        public static readonly string ModuleName = "DynamicDataAccessObjects";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static Type? CreateImplementation(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType must be an interface");
            }

            Type idaoType = Globals.TYPE_SUPER_DAO_I;
            var inheritsInterfaceTypes = ReflectionTool.GetInterfaceList(interfaceType);
            Type? inheritsType = inheritsInterfaceTypes.Find(t => t.GetGenericTypeDefinition().Equals(idaoType));
            if (inheritsType == null)
            {
                return null;
            }

            Type modelType = inheritsType.GetGenericArguments()[0];
            TableMapping? modelMapping = ModelSchemaStore.GetModelMapping(modelType);
            if (modelMapping == null)
            {
                return null;
            }

            string proxyTypeName = $"Proxy_{modelType.Name}Dao";
            Type? createdType = DynamicCodeStore.TryGetType(proxyTypeName);
            if (createdType != null)
            {
                return createdType;
            }

            Type iproxyType = typeof(IDynamicProxy);

            ModuleBuilder moduleBuilder = DynamicCodeStore.GetModuleBuilder(ModuleName);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Public, typeof(SuperDao<>).MakeGenericType(modelType));
            typeBuilder.AddInterfaceImplementation(interfaceType);
            typeBuilder.AddInterfaceImplementation(iproxyType);

            foreach (var method in interfaceType.GetMethods())
            {
                GenerateMethodImplementation(typeBuilder, method, modelType, true);
            }

            Type dynamicType = typeBuilder.CreateType();
 
            // store the type
            DynamicCodeStore.SetType(proxyTypeName, dynamicType);

            return dynamicType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="daoType"></param>
        /// <returns></returns>
        public static Type CreateProxy(Type daoType)
        {
            Type idaoType = typeof(ISuperDao<>);
            var interfaceTypes = ReflectionTool.GetInterfaceList(daoType);
            Type interfaceType = interfaceTypes.Find(t => ReflectionTool.IsSubInterface(t, idaoType))!;
            Type iproxyType = typeof(IDynamicProxy);

            var inheritsInterfaceTypes = ReflectionTool.GetInterfaceList(interfaceType);
            Type? inheritsType = inheritsInterfaceTypes.Find(t => t.GetGenericTypeDefinition().Equals(idaoType));
            if (inheritsType == null)
            {
                throw new ArgumentException("Invalid daoType", nameof(daoType));
            }

            Type modelType = inheritsType.GetGenericArguments()[0];
            TableMapping? modelMapping = ModelSchemaStore.GetModelMapping(modelType);
            if (modelMapping == null)
            {
                throw new ArgumentException("Invalid daoType", nameof(daoType));
            }

            string proxyTypeName = $"Proxy_{daoType.Name}";
            Type? createdType = DynamicCodeStore.TryGetType(proxyTypeName);
            if (createdType != null)
            {
                return createdType;
            }

            ModuleBuilder moduleBuilder = DynamicCodeStore.GetModuleBuilder(ModuleName);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Public, daoType);
            typeBuilder.AddInterfaceImplementation(iproxyType);

            foreach (var method in interfaceType.GetMethods())
            {
                GenerateMethodImplementation(typeBuilder, method, modelType, false);
            }

            Type dynamicType = typeBuilder.CreateType();

            // store the type
            DynamicCodeStore.SetType(proxyTypeName, dynamicType);

            return dynamicType;
        }


        private static void GenerateMethodImplementation(TypeBuilder typeBuilder, MethodInfo method, Type modelType, bool isOverrideInterface)
        {
            Type curdExecutorType = Globals.TYPE_CURD_EXECUTOR;
            Type selectCommandType = Globals.TYPE_SELECT_COMMAND;
            Type selectListCommandType = Globals.TYPE_SELECT_LIST_COMMAND;
            Type executeCommandType = Globals.TYPE_EXECUTE_COMMAND;

            var attributes = method.GetCustomAttributes();

            QueryCommand? attributeInstance = null;
            MethodInfo? curdMethod = null;
            Type? curdCommandType = null;
            if (attributes.OfType<SelectCommand>().Any())
            {
                attributeInstance = (SelectCommand)Attribute.GetCustomAttribute(method, selectCommandType)!;
                curdMethod = curdExecutorType.GetMethod("Select")!;
                curdCommandType = selectCommandType;
            }
            else if (attributes.OfType<SelectListCommand>().Any())
            {
                attributeInstance = (SelectListCommand)Attribute.GetCustomAttribute(method, selectCommandType)!;
                curdMethod = curdExecutorType.GetMethod("SelectList")!;
                curdCommandType = selectListCommandType;
            }
            else if (attributes.OfType<ExecuteCommand>().Any())
            {
                attributeInstance = (ExecuteCommand)Attribute.GetCustomAttribute(method, selectCommandType)!;
                curdMethod = curdExecutorType.GetMethod("Execute")!;
                curdCommandType = executeCommandType;
            }

            if (attributeInstance != null && curdMethod != null && curdCommandType != null)
            {
                GenerateCurdMethodImplementation(typeBuilder, method, modelType, attributeInstance, curdMethod);
            }
            else
            {
                if (isOverrideInterface)
                {
                    GenerateDefaultMethodImplementation(typeBuilder, method);
                }
            }

        }

        private static void GenerateCurdMethodImplementation(TypeBuilder typeBuilder, MethodInfo method, Type modelType, QueryCommand? attributeInstance, MethodInfo executorMethod)
        {
            if (attributeInstance == null)
            {
                return;
            }

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        method.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        method.ReturnType,
                        method.GetParameters().Select(p => p.ParameterType).ToArray()
                    );
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();

            // Load "this"
            ilGenerator.Emit(OpCodes.Ldarg_0);

            ilGenerator.Emit(OpCodes.Ldstr, attributeInstance.Sql);

            // Create Dictionary for arguments
            ilGenerator.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes)!);
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup); // Duplicate dictionary
                ilGenerator.Emit(OpCodes.Ldstr, parameters[i].Name!); // Key
                ilGenerator.Emit(OpCodes.Ldarg, i + 1); // Value
                ilGenerator.Emit(OpCodes.Box, parameters[i].ParameterType);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(Dictionary<string, object>).GetMethod("Add")!);
            }

            ilGenerator.Emit(OpCodes.Ldc_I4, attributeInstance.CommandType != null ? (int)attributeInstance.CommandType : 0);
            ilGenerator.Emit(OpCodes.Ldc_I4, attributeInstance.CommandTimeout != null ? attributeInstance.CommandTimeout.Value : -1);

            // Call the CurdExecutor method
            ilGenerator.Emit(OpCodes.Call, executorMethod.MakeGenericMethod(modelType));

            if (method.ReturnType != typeof(void))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, method.ReturnType);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        private static void GenerateDefaultMethodImplementation(TypeBuilder typeBuilder, MethodInfo method)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        method.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        method.ReturnType,
                        method.GetParameters().Select(p => p.ParameterType).ToArray()
                    );
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();

            // Emit IL code to throw NotImplementedException
            ilGenerator.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Throw);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

    }
}
