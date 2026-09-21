using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Rampage.Reflection
{
    /// <summary>
    /// IL(Emit) 工具：用 DynamicMethod 生成并缓存"无参构造/属性读写/字段读写"三类委托，替代直接反射调用以提升性能
    /// IL emit tool: builds and caches three kinds of delegates via DynamicMethod (parameterless construction, property get/set, field get/set), replacing slow reflection calls
    /// </summary>
    public static class EmitTool
    {

        #region Create Instance

        /// <summary>
        /// 动态创建指定类型的实例（替代 Activator.CreateInstance，性能更好） / Dynamically create an instance of the specified type (replacing Activator.CreateInstance, with better performance)
        /// </summary>
        /// <param name="type">目标类型，须有无参构造 / The target type, must have a parameterless constructor</param>
        /// <returns>新实例 / The new instance</returns>
        /// <exception cref="NullReferenceException">类型缺少无参构造时抛出 / Thrown when the type lacks a parameterless constructor</exception>
        public static object? CreateModel(Type type)
        {
            object? value = ModelCreator.CreateModel(type);
            return value;
        }

        /// <summary>
        /// 动态创建指定泛型类型的实例（替代 Activator.CreateInstance，性能更好） / Dynamically create an instance of the generic type (replacing Activator.CreateInstance, with better performance)
        /// </summary>
        /// <typeparam name="M">目标类型，须有无参构造 / The target type, must have a parameterless constructor</typeparam>
        /// <returns>新实例 / The new instance</returns>
        public static M? CreateModel<M>()
        {
            M? value = ModelCreator<M>.CreateModel();
            return value;
        }

        #region Private Classes

        /// <summary>
        /// 无参构造实例创建器的 IL 生成与缓存 / Builds and caches IL-based parameterless-constructor creators
        /// </summary>
        private class ModelCreator
        {

            #region Properties

            /// <summary>
            /// 构造委托缓存（键：类型 FullName） / Creator delegate cache (key: type full name)
            /// </summary>
            private static readonly ConcurrentDictionary<string, Func<Type, object>> ModelCreators = new ConcurrentDictionary<string, Func<Type, object>>();

            #endregion

            #region Methods

            /// <summary>
            /// 生成执行 newobj 指令的动态方法 / Build the dynamic method that emits the newobj instruction
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <returns>签名为 Func&lt;Type, object&gt; 的动态方法（入参被忽略） / The dynamic method typed Func&lt;Type, object&gt; (its argument is ignored)</returns>
            protected static DynamicMethod CreateDynamicMethod(Type type)
            {
                DynamicMethod dm = new DynamicMethod(string.Empty, type, new Type[] { typeof(Type) }, typeof(ModelCreator));
                ILGenerator il = dm.GetILGenerator();
                LocalBuilder model = il.DeclareLocal(type);

                // T model = new T();
                il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes)!);
                il.Emit(OpCodes.Stloc_S, model);

                // return model;
                il.Emit(OpCodes.Ldloc_S, model);
                il.Emit(OpCodes.Ret);

                return dm;
            }

            /// <summary>
            /// 创建类型的实例构造委托 / Create the instance-creator delegate for the type
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <returns>构造委托 / The creator delegate</returns>
            protected static Func<Type, object> CreateModelCreator(Type type)
            {
                DynamicMethod dm = CreateDynamicMethod(type);
                return (Func<Type, object>)dm.CreateDelegate(typeof(Func<Type, object>));
            }

            /// <summary>
            /// 获取（或生成并缓存）构造委托并创建实例 / Get (or build and cache) the creator delegate, then create an instance
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <returns>新实例 / The new instance</returns>
            internal static object? CreateModel(Type type)
            {
                // 缓存键为类型 FullName / Cache key is the type full name
                string key = type.FullName!;
                var modelCreator = ModelCreators.GetOrAdd(key, _ => CreateModelCreator(type));
                object? val = modelCreator(type);
                return val;
            }

            #endregion
        }

        /// <summary>
        /// 泛型强类型包装的实例创建器 / Strongly-typed generic wrapper of the creator
        /// </summary>
        private class ModelCreator<T> : ModelCreator
        {

            /// <summary>
            /// 创建 T 的新实例 / Create a new instance of T
            /// </summary>
            /// <returns>新实例 / The new instance</returns>
            internal static T? CreateModel()
            {
                Type type = typeof(T);

                object? o = ModelCreator.CreateModel(type);

                if (o != null)
                {
                    T val = (T)o;
                    return val;
                }
                else
                {
                    return default(T);
                }
            }

        }

        #endregion

        #endregion



        #region Property Setter

        /// <summary>
        /// 动态设置实例的属性值（比 PropertyInfo.SetValue 性能更好） / Dynamically set a property value on the instance (better performance than PropertyInfo.SetValue)
        /// </summary>
        /// <param name="model">目标实例 / The target instance</param>
        /// <param name="propertyName">属性名 / The property name</param>
        /// <param name="value">要写入的值 / The value to set</param>
        /// <returns>目标实例（便于链式调用） / The target instance (for chaining)</returns>
        /// <exception cref="ArgumentException">属性不存在或不可写 / The property does not exist or has no setter</exception>
        public static object SetPropertyValue(object model, string propertyName, object? value)
        {
            model = ModelPropertySetter.SetPropertyValue(model, propertyName, value);
            return model;
        }

        /// <summary>
        /// 属性 setter 委托的 IL 生成与缓存 / Builds and caches IL-based property setter delegates
        /// </summary>
        private class ModelPropertySetter
        {

            #region Properties

            /// <summary>
            /// setter 委托缓存（键：类型名__属性名） / Setter delegate cache (key: TypeName__PropertyName)
            /// </summary>
            private static readonly ConcurrentDictionary<string, Func<object, object?, object>> PropertySetters = new ConcurrentDictionary<string, Func<object, object?, object>>();

            #endregion

            #region Methods

            /// <summary>
            /// 生成属性赋值的动态方法：Ldarg0/Ldarg1 + Unbox_Any(或 Castclass) + Callvirt setter，最后返回实例
            /// Build the property-assignment dynamic method: Ldarg0/Ldarg1 + Unbox_Any (or Castclass) + Callvirt setter, then return the instance
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <returns>签名 Func&lt;object, object?, object&gt;：入参(实例, 值)，返回实例 / Typed Func&lt;object, object?, object&gt;: (instance, value) => instance</returns>
            /// <exception cref="ArgumentException">属性不存在或没有 setter / The property does not exist or has no setter</exception>
            protected static DynamicMethod CreateDynamicMethod(Type type, string propertyName)
            {
                var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { typeof(object), typeof(object) }, type.Module);
                var iLGenerator = dynamicMethod.GetILGenerator();
                if (iLGenerator != null)
                {
                    PropertyInfo? property = ReflectionTool.GetProperty(type, propertyName);
                    if (property == null)
                    {
                        throw new ArgumentException($"Property: {propertyName} is not exists in Type: {type.FullName}");
                    }
                    var setMethod = property.SetMethod;
                    if (setMethod == null)
                    {
                        throw new ArgumentException($"Property: {propertyName} is invalid in Type: {type.FullName}");
                    }
                    ParameterInfo[] parameterInfos = setMethod.GetParameters();
                    if (parameterInfos == null || parameterInfos.Length == 0)
                    {
                        throw new ArgumentException($"Property: {propertyName} is invalid in Type: {type.FullName}");
                    }
                    ParameterInfo parameterInfo = parameterInfos[0];

                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Ldarg_1);
                    if (parameterInfo.ParameterType.IsValueType)
                    {
                        iLGenerator.Emit(OpCodes.Unbox_Any, parameterInfo.ParameterType);
                    }
                    else
                    {
                        iLGenerator.Emit(OpCodes.Castclass, parameterInfo.ParameterType);
                    }
                    iLGenerator.Emit(OpCodes.Callvirt, setMethod);

                    // return model;
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Ret);
                }

                return dynamicMethod;
            }

            /// <summary>
            /// 创建属性赋值委托 / Create the property setter delegate
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <returns>setter 委托 / The setter delegate</returns>
            protected static Func<object, object?, object> CreatePropertySetter(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = CreateDynamicMethod(type, propertyName);
                return (Func<object, object?, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object?, object>));
            }

            /// <summary>
            /// 获取（或生成并缓存）setter 并执行赋值 / Get (or build and cache) the setter, then perform the assignment
            /// </summary>
            /// <param name="model">目标实例 / The target instance</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <param name="value">要写入的值 / The value to set</param>
            /// <returns>目标实例 / The target instance</returns>
            internal static object SetPropertyValue(object model, string propertyName, object? value)
            {
                Type type = model.GetType();
                // 缓存键：类型名__属性名 / Cache key: TypeName__PropertyName
                string key = string.Concat(type.FullName, "__", propertyName);
                var propertySetter = PropertySetters.GetOrAdd(key, _ => CreatePropertySetter(type, propertyName));
                model = propertySetter(model, value);
                return model;
            }

            #endregion

        }

        #endregion



        #region Property Getter

        /// <summary>
        /// 动态读取实例的属性值（比 PropertyInfo.GetValue 性能更好） / Dynamically get a property value from the instance (better performance than PropertyInfo.GetValue)
        /// </summary>
        /// <param name="model">目标实例 / The target instance</param>
        /// <param name="propertyName">属性名 / The property name</param>
        /// <returns>属性值（值类型已装箱） / The property value (value types are boxed)</returns>
        /// <exception cref="ArgumentException">属性不存在或不可读 / The property does not exist or has no getter</exception>
        public static object? GetPropertyValue(object model, string propertyName)
        {
            object? value = ModelPropertyGetter.GetPropertyValue(model, propertyName);
            return value;
        }

        /// <summary>
        /// 属性 getter 委托的 IL 生成与缓存 / Builds and caches IL-based property getter delegates
        /// </summary>
        private class ModelPropertyGetter
        {

            #region Properties

            /// <summary>
            /// getter 委托缓存（键：类型名__属性名） / Getter delegate cache (key: TypeName__PropertyName)
            /// </summary>
            private static readonly ConcurrentDictionary<string, Func<object, object?>> PropertyGetters = new ConcurrentDictionary<string, Func<object, object?>>();

            #endregion

            #region Methods

            /// <summary>
            /// 生成读取属性的动态方法：Callvirt getter + 值类型装箱 / Build the property-reading dynamic method: Callvirt getter + boxing for value types
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <returns>签名 Func&lt;object, object?&gt; 的动态方法 / The dynamic method typed Func&lt;object, object?&gt;</returns>
            /// <exception cref="ArgumentException">属性不存在或没有 getter / The property does not exist or has no getter</exception>
            protected static DynamicMethod CreateDynamicMethod(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object) }, type.Module);//定义和表示一个可以编译，执行和丢弃的动态方法 / Define a dynamic method that can be compiled, executed and discarded
                PropertyInfo? property = ReflectionTool.GetProperty(type, propertyName);
                if (property == null)
                {
                    throw new ArgumentException($"Property: {propertyName} is not exists in Type: {type.FullName}");
                }
                var getMethod = property.GetMethod;
                if (getMethod == null)
                {
                    throw new ArgumentException($"Property: {propertyName} is invalid in Type: {type.FullName}");
                }

                var iLGenerator = dynamicMethod.GetILGenerator();//返回方法的Microsoft中间语言（MSIL）生成器，默认的MSIL流大小为64字节 / Returns the MSIL generator of the method (default stream size 64 bytes)
                if (iLGenerator != null)
                {
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Callvirt, getMethod);

                    if (property.PropertyType.IsValueType)
                    {
                        iLGenerator.Emit(OpCodes.Box, property.PropertyType);//值类型需要装箱，因为返回类型是object / Value types must be boxed since the return type is object
                    }
                    iLGenerator.Emit(OpCodes.Ret);
                }

                return dynamicMethod;
            }

            /// <summary>
            /// 创建对象属性读取器的动态委托 / Create the dynamic property-getter delegate
            /// </summary>
            /// <param name="type">对象类型 / The target type</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <returns>getter 委托 / The getter delegate</returns>
            protected static Func<object, object?> CreatePropertyGetter(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = CreateDynamicMethod(type, propertyName);
                return (Func<object, object?>)dynamicMethod.CreateDelegate(typeof(Func<object, object?>));//CreateDelegate()完成动态方法并创建一个可用于执行它的委托。 / CreateDelegate() finalizes the dynamic method and creates an executable delegate.
            }

            /// <summary>
            /// 获取（或生成并缓存）getter 并读取属性值 / Get (or build and cache) the getter, then read the property value
            /// </summary>
            /// <param name="model">目标实例 / The target instance</param>
            /// <param name="propertyName">属性名 / The property name</param>
            /// <returns>属性值 / The property value</returns>
            internal static object? GetPropertyValue(object model, string propertyName)
            {
                Type type = model.GetType();
                // 缓存键：类型名__属性名 / Cache key: TypeName__PropertyName
                string key = string.Concat(type.FullName, "__", propertyName);
                var propertyGetter = PropertyGetters.GetOrAdd(key, _ => CreatePropertyGetter(type, propertyName));
                object? val = propertyGetter(model);
                return val;
            }

            #endregion

        }

        #endregion


        #region Field Accessor

        /// <summary>
        /// 动态设置实例的字段值（比 FieldInfo.SetValue 性能更好） / Dynamically set a field value on the instance (better performance than FieldInfo.SetValue)
        /// </summary>
        /// <param name="model">目标实例 / The target instance</param>
        /// <param name="fieldName">字段名 / The field name</param>
        /// <param name="value">要写入的值 / The value to set</param>
        /// <returns>目标实例（便于链式调用） / The target instance (for chaining)</returns>
        /// <exception cref="ArgumentException">字段不存在 / The field does not exist</exception>
        public static object SetFieldValue(object model, string fieldName, object? value)
        {
            model = ModelFieldAccessor.SetFieldValue(model, fieldName, value);
            return model;
        }

        /// <summary>
        /// 动态读取实例的字段值（比 FieldInfo.GetValue 性能更好） / Dynamically get a field value from the instance (better performance than FieldInfo.GetValue)
        /// </summary>
        /// <param name="model">目标实例 / The target instance</param>
        /// <param name="fieldName">字段名 / The field name</param>
        /// <returns>字段值 / The field value</returns>
        /// <exception cref="ArgumentException">字段不存在 / The field does not exist</exception>
        public static object? GetFieldValue(object model, string fieldName)
        {
            object? v = ModelFieldAccessor.GetFieldValue(model, fieldName);
            return v;
        }

        /// <summary>
        /// 字段访问器（表达式树编译版）的缓存 / Caches field accessors built from compiled expression trees
        /// </summary>
        private class ModelFieldAccessor
        {

            #region Properties

            /// <summary>
            /// 字段访问器缓存（键：类型名__字段名） / Field accessor cache (key: TypeName__FieldName)
            /// </summary>
            private static readonly ConcurrentDictionary<string, FieldAccessor> FieldAccessors = new ConcurrentDictionary<string, FieldAccessor>();

            #endregion

            #region Methods

            /// <summary>
            /// 创建字段访问器 / Create the field accessor
            /// </summary>
            /// <param name="type">目标类型 / The target type</param>
            /// <param name="fieldName">字段名 / The field name</param>
            /// <returns>字段访问器 / The field accessor</returns>
            protected static FieldAccessor CreateFieldAccessor(Type type, string fieldName)
            {
                return new FieldAccessor(type, fieldName);
            }

            /// <summary>
            /// 获取（或创建并缓存）字段访问器并写入字段值 / Get (or build and cache) the field accessor, then set the field value
            /// </summary>
            /// <param name="model">目标实例 / The target instance</param>
            /// <param name="fieldName">字段名 / The field name</param>
            /// <param name="value">要写入的值 / The value to set</param>
            /// <returns>目标实例 / The target instance</returns>
            internal static object SetFieldValue(object model, string fieldName, object? value)
            {
                Type type = model.GetType();
                // 缓存键：类型名__字段名 / Cache key: TypeName__FieldName
                string key = string.Concat(type.FullName, "__", fieldName);
                FieldAccessor fieldAccessor = FieldAccessors.GetOrAdd(key, _ => CreateFieldAccessor(type, fieldName));
                fieldAccessor.Set(model, value);
                return model;
            }

            /// <summary>
            /// 获取（或创建并缓存）字段访问器并读取字段值 / Get (or build and cache) the field accessor, then read the field value
            /// </summary>
            /// <param name="model">目标实例 / The target instance</param>
            /// <param name="fieldName">字段名 / The field name</param>
            /// <returns>字段值 / The field value</returns>
            internal static object? GetFieldValue(object model, string fieldName)
            {
                Type type = model.GetType();
                // 缓存键：类型名__字段名 / Cache key: TypeName__FieldName
                string key = string.Concat(type.FullName, "__", fieldName);
                FieldAccessor fieldAccessor = FieldAccessors.GetOrAdd(key, _ => CreateFieldAccessor(type, fieldName));
                object? v = fieldAccessor.Get(model);
                return v;
            }

            #endregion

        }

        #endregion


    }
}
