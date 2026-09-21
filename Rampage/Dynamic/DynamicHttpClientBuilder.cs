using System;
using System.Reflection.Emit;
using System.Reflection;
using Rampage.Net;

namespace Rampage.Dynamic
{
    /// <summary>
    /// 使用 Reflection.Emit 为声明式 Http 客户端接口动态生成实现代码 / Build declarative HTTP client implementations at runtime using Reflection.Emit
    /// </summary>
    public class DynamicHttpClientBuilder
    {

        /// <summary>
        /// 动态 Http 客户端所在的模块名 / The module name hosting the generated HTTP clients
        /// </summary>
        public static readonly string ModuleName = "DynamicHttpClients";

        /// <summary>
        /// 为声明式 Http 客户端接口生成动态实现类（基类为 SuperHttpClient）
        /// Build a dynamic implementation for a declarative HTTP client interface (base class SuperHttpClient)
        /// </summary>
        /// <param name="interfaceType">客户端接口类型，其方法必须返回 Task&lt;T&gt; / The client interface; its methods must return Task&lt;T&gt;</param>
        /// <returns>生成的实现类型 / The generated implementation type</returns>
        /// <exception cref="ArgumentException">interfaceType 不是接口 / interfaceType is not an interface</exception>
        /// <remarks>同名实现已存在时直接返回缓存 / A cached implementation of the same name is returned directly</remarks>
        public static Type? CreateImplementation(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType must be an interface");
            }

            string implTypeName = interfaceType.Name + "Implementation";

            Type? implType = DynamicCodeStore.TryGetType(implTypeName);
            if (implType != null)
            {
                return implType;
            }

            ModuleBuilder moduleBuilder = DynamicCodeStore.GetModuleBuilder(ModuleName);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(implTypeName, TypeAttributes.Public | TypeAttributes.Class, typeof(SuperHttpClient), new[] { interfaceType });

            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                GenerateMethodImplementation(typeBuilder, interfaceType, method);
            }

            var finalType = typeBuilder.CreateType();
            DynamicCodeStore.SetType(implTypeName, finalType);
            return finalType;
        }

        /// <summary>
        /// 生成接口方法体：把方法参数装箱打包为 Dictionary&lt;string, object?&gt;，再调用 SuperHttpClient.CallAsync&lt;T&gt;(接口类型, 方法名, 参数字典)
        /// Emit the method body: packs parameters into a Dictionary&lt;string, object?&gt; (boxing value types), then calls SuperHttpClient.CallAsync&lt;T&gt;(interfaceType, methodName, arguments)
        /// </summary>
        private static void GenerateMethodImplementation(TypeBuilder typeBuilder, Type interfaceType, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type returnType = method.ReturnType;

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual, returnType, GetParameterTypes(parameters));

            ILGenerator il = methodBuilder.GetILGenerator();

            LocalBuilder argumentsDict = il.DeclareLocal(typeof(Dictionary<string, object?>));
            il.Emit(OpCodes.Newobj, typeof(Dictionary<string, object?>).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Stloc, argumentsDict);

            for (int i = 0; i < parameters.Length; i++)
            {
                string paramName = parameters[i].Name!;
                il.Emit(OpCodes.Ldloc, argumentsDict);
                il.Emit(OpCodes.Ldstr, paramName);
                switch (i)
                {
                    case 0: il.Emit(OpCodes.Ldarg_1); break;
                    case 1: il.Emit(OpCodes.Ldarg_2); break;
                    case 2: il.Emit(OpCodes.Ldarg_3); break;
                    default: il.Emit(OpCodes.Ldarg_S, (byte)(i + 1)); break;
                }
                if (parameters[i].ParameterType.IsValueType)
                {
                    il.Emit(OpCodes.Box, parameters[i].ParameterType);
                }
                il.Emit(OpCodes.Call, typeof(Dictionary<string, object?>).GetMethod("Add", new Type[] { typeof(string), typeof(object) })!);
            }

            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldtoken, interfaceType); // Type
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Ldloc, argumentsDict);

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // 接口方法签名 Task<T>：调用 CallAsync<T>，返回的 Task<T> 直接作为方法返回值 / Task<T> signature: call CallAsync<T> whose Task<T> becomes the return value directly
                Type taskArgumentType = returnType.GetGenericArguments()[0];
                il.Emit(OpCodes.Call, typeof(SuperHttpClient).GetMethod("CallAsync")!.MakeGenericMethod(taskArgumentType)!);
            }
            else
            {
                throw new NotSupportedException(
                    $"声明式Http客户端接口 {interfaceType.FullName}.{method.Name} 的返回类型必须为 Task<T>（同步调用已移除，请改为异步签名）");
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 提取参数类型数组 / Extract the parameter type array
        /// </summary>
        private static Type[] GetParameterTypes(ParameterInfo[] parameters)
        {
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            }
            return paramTypes;
        }
    }
}
