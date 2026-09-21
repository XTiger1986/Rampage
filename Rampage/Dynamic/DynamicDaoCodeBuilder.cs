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
    /// 使用 Reflection.Emit 为 DAO 接口动态生成实现代码 / Build Data Access Object implementations at runtime using Reflection.Emit
    /// </summary>
    /// <remarks>
    /// 生成的代理类型会同时实现 IDynamicProxy 标记接口，并缓存于 DynamicCodeStore，同名类型只生成一次。
    /// Generated proxies also implement the IDynamicProxy marker interface and are cached in DynamicCodeStore; each type name is generated only once.
    /// </remarks>
    public class DynamicDaoCodeBuilder
    {

        /// <summary>
        /// 动态 DAO 代理所在的模块名 / The module name hosting the generated DAO proxies
        /// </summary>
        public static readonly string ModuleName = "DynamicDataAccessObjects";

        /// <summary>
        /// 为继承 ISuperDao&lt;T&gt; 的 DAO 接口生成动态实现类（基类为 SuperDao&lt;T&gt;，同时实现 IDynamicProxy）
        /// Build a dynamic implementation for a DAO interface inheriting ISuperDao&lt;T&gt; (base class SuperDao&lt;T&gt;, also implements IDynamicProxy)
        /// </summary>
        /// <param name="interfaceType">DAO 接口类型 / The DAO interface type</param>
        /// <returns>生成的代理类型；接口未继承 ISuperDao&lt;&gt; 或模型未配置表映射时返回 null / The generated proxy type, or null when the interface does not inherit ISuperDao&lt;&gt; or the model has no table mapping</returns>
        /// <exception cref="ArgumentException">interfaceType 不是接口 / interfaceType is not an interface</exception>
        /// <remarks>
        /// 接口方法按其标注的查询命令特性（SelectCommand / SelectListCommand / ExecuteCommand）生成转发到 CurdExecutor 的实现；
        /// Interface methods generate implementations forwarding to CurdExecutor according to their query command attribute (SelectCommand / SelectListCommand / ExecuteCommand);
        /// 未标注特性的方法生成抛出 NotImplementedException 的默认实现；同名代理类型已存在时直接返回缓存。
        /// Methods without such attributes get a default implementation throwing NotImplementedException; a cached proxy of the same name is returned directly.
        /// </remarks>
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
        /// 为已有的 DAO 类生成动态代理子类（继承该类并实现 IDynamicProxy）
        /// Build a dynamic proxy subclass of an existing DAO class (inherits it and implements IDynamicProxy)
        /// </summary>
        /// <param name="daoType">DAO 类型，须实现继承 ISuperDao&lt;&gt; 的接口 / The DAO type, must implement an interface inheriting ISuperDao&lt;&gt;</param>
        /// <returns>生成的代理子类 / The generated proxy subclass</returns>
        /// <exception cref="ArgumentException">daoType 无效（未实现 ISuperDao 接口或模型无表映射）/ daoType is invalid (no ISuperDao interface or no table mapping for the model)</exception>
        /// <remarks>代理子类仅重写其 DAO 接口上标注了查询命令特性的方法；同名代理已存在时直接返回缓存 / Only interface methods carrying query command attributes are overridden; a cached proxy of the same name is returned directly</remarks>
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


        /// <summary>
        /// 按方法特性选择生成策略：有查询命令特性时生成 CURD 调用，否则（仅接口实现模式）生成默认实现
        /// Pick the generation strategy by the method's attributes: CURD call for command attributes, otherwise a default implementation (interface-implementation mode only)
        /// </summary>
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
                attributeInstance = (SelectListCommand)Attribute.GetCustomAttribute(method, selectListCommandType)!;
                curdMethod = curdExecutorType.GetMethod("SelectList")!;
                curdCommandType = selectListCommandType;
            }
            else if (attributes.OfType<ExecuteCommand>().Any())
            {
                attributeInstance = (ExecuteCommand)Attribute.GetCustomAttribute(method, executeCommandType)!;
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

        /// <summary>
        /// 生成转发到 CurdExecutor 的方法体：压入 this、SQL、参数字典（方法参数逐个装箱加入）、命令类型与超时后调用执行器
        /// Emit the method body forwarding to CurdExecutor: pushes this, the SQL, an argument dictionary (parameters boxed in), command type and timeout, then calls the executor
        /// </summary>
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

        /// <summary>
        /// 生成抛出 NotImplementedException 的默认实现 / Emit a default implementation that throws NotImplementedException
        /// </summary>
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
