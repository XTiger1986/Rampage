using Rampage.Data.Client;
using Rampage.Data.Query;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Rampage.Dynamic
{
    /// <summary>
    ///
    /// </summary>
    internal class DynamicCodeStore
    {

        public static ConcurrentDictionary<string, AssemblyBuilder> AssemblyBuilderContainer
        {
            get;
        } = new ConcurrentDictionary<string, AssemblyBuilder>();

        public static ConcurrentDictionary<string, ModuleBuilder> ModuleBuilderContainer
        {
            get;
        } = new ConcurrentDictionary<string, ModuleBuilder>();

        /// <summary>
        /// key: {ModuleName}_{TypeName}
        /// </summary>
        public static ConcurrentDictionary<string, Type> CreatedTypes
        {
            get;
        } = new ConcurrentDictionary<string, Type>();

        public static AssemblyBuilder GetAssemblyBuilder(string moduleName)
        {
            string key = $"Rampage.DynamicCode.{moduleName}";
            return AssemblyBuilderContainer.GetOrAdd(key, k => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(k), AssemblyBuilderAccess.Run));
        }

        public static ModuleBuilder GetModuleBuilder(string moduleName)
        {
            return ModuleBuilderContainer.GetOrAdd(moduleName, m => GetAssemblyBuilder(m).DefineDynamicModule(m));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">{ModuleName}_{TypeName}</param>
        /// <returns></returns>
        public static Type? TryGetType(string name)
        {
            if (CreatedTypes.ContainsKey(name))
            {
                return CreatedTypes[name];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">{ModuleName}_{TypeName}</param>
        /// <param name="type"></param>
        public static void SetType(string name, Type type)
        {
            CreatedTypes[name] = type;
        }

    }
}
