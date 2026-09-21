using Rampage.Data.Client;
using Rampage.Data.Query;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Rampage.Dynamic
{
    /// <summary>
    /// 动态代码存储：集中缓存动态程序集、动态模块与已创建类型，供各 Builder 复用（进程内，不落盘）
    /// Storage for dynamically generated code: caches dynamic assemblies, modules and created types for reuse by the builders (in-process, never persisted)
    /// </summary>
    internal class DynamicCodeStore
    {

        /// <summary>
        /// 动态程序集容器（键为程序集名）/ Dynamic assembly builders keyed by assembly name
        /// </summary>
        public static ConcurrentDictionary<string, AssemblyBuilder> AssemblyBuilderContainer
        {
            get;
        } = new ConcurrentDictionary<string, AssemblyBuilder>();

        /// <summary>
        /// 动态模块容器（键为模块名）/ Dynamic module builders keyed by module name
        /// </summary>
        public static ConcurrentDictionary<string, ModuleBuilder> ModuleBuilderContainer
        {
            get;
        } = new ConcurrentDictionary<string, ModuleBuilder>();

        /// <summary>
        /// 已创建类型缓存（键为类型名，跨模块共享，因此类型名必须全局唯一）
        /// Created types keyed by plain type name (shared across modules, so type names must be globally unique)
        /// </summary>
        public static ConcurrentDictionary<string, Type> CreatedTypes
        {
            get;
        } = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 获取或创建模块对应的动态程序集（程序集名为 Rampage.DynamicCode.模块名，仅内存运行）
        /// Get or create the dynamic assembly for the module (assembly name Rampage.DynamicCode.{moduleName}, run-only in memory)
        /// </summary>
        /// <param name="moduleName">模块名 / The module name</param>
        /// <returns>动态程序集构造器 / The assembly builder</returns>
        public static AssemblyBuilder GetAssemblyBuilder(string moduleName)
        {
            string key = $"Rampage.DynamicCode.{moduleName}";
            return AssemblyBuilderContainer.GetOrAdd(key, k => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(k), AssemblyBuilderAccess.Run));
        }

        /// <summary>
        /// 获取或创建模块对应的动态模块 / Get or create the dynamic module for the module name
        /// </summary>
        /// <param name="moduleName">模块名 / The module name</param>
        /// <returns>动态模块构造器 / The module builder</returns>
        public static ModuleBuilder GetModuleBuilder(string moduleName)
        {
            return ModuleBuilderContainer.GetOrAdd(moduleName, m => GetAssemblyBuilder(m).DefineDynamicModule(m));
        }

        /// <summary>
        /// 查找已创建的动态类型 / Look up a previously created dynamic type
        /// </summary>
        /// <param name="name">类型名 / The type name</param>
        /// <returns>类型；未创建过时为 null / The type, or null when not created yet</returns>
        public static Type? TryGetType(string name)
        {
            if (CreatedTypes.ContainsKey(name))
            {
                return CreatedTypes[name];
            }
            return null;
        }

        /// <summary>
        /// 登记已创建的动态类型（重复登记后者覆盖前者）/ Register a created dynamic type (a later registration overwrites the earlier one)
        /// </summary>
        /// <param name="name">类型名 / The type name</param>
        /// <param name="type">已创建的类型 / The created type</param>
        public static void SetType(string name, Type type)
        {
            CreatedTypes[name] = type;
        }

    }
}
