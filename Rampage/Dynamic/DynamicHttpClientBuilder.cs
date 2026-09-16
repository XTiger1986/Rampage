using System;
using System.Reflection.Emit;
using System.Reflection;
using Rampage.Net;

namespace Rampage.Dynamic
{
    /// <summary>
    /// using "Reflection.Emit", build Custom Http Client Implemention dynamic code 
    /// </summary>
    public class DynamicHttpClientBuilder
    {

        public static readonly string ModuleName = "DynamicHttpClients";

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
                // 接口方法签名 Task<T>：调用 CallAsync<T>，返回的 Task<T> 直接作为方法返回值
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
