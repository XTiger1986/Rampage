using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Rampage.Reflection
{
    /// <summary>
    /// 
    /// </summary>
    public static class EmitTool
    {

        #region Create Instance

        /// <summary>
        /// Dynamically create instances of specified types (replacing Activator.CreatInstance, with better performance)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object? CreateModel(Type type)
        {
            object? value = ModelCreator.CreateModel(type);
            return value;
        }

        /// <summary>
        /// Dynamically create instances of specified types (replacing Activator.CreatInstance, with better performance)
        /// </summary>
        /// <returns></returns>
        public static M? CreateModel<M>()
        {
            M? value = ModelCreator<M>.CreateModel();
            return value;
        }

        #region Private Classes

        /// <summary>
        /// 
        /// </summary>
        private class ModelCreator
        {

            #region Properties

            private static readonly ConcurrentDictionary<string, Func<Type, object>> ModelCreators = new ConcurrentDictionary<string, Func<Type, object>>();

            #endregion

            #region Methods

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
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
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            protected static Func<Type, object> CreateModelCreator(Type type)
            {
                DynamicMethod dm = CreateDynamicMethod(type);
                return (Func<Type, object>)dm.CreateDelegate(typeof(Func<Type, object>));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            internal static object? CreateModel(Type type)
            {
                string key = type.FullName!;
                var modelCreator = ModelCreators.GetOrAdd(key, _ => CreateModelCreator(type));
                object? val = modelCreator(type);
                return val;
            }

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        private class ModelCreator<T> : ModelCreator
        {

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
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
        /// Dynamically set instance object property value (with better performance)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object SetPropertyValue(object model, string propertyName, object? value)
        {
            model = ModelPropertySetter.SetPropertyValue(model, propertyName, value);
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        private class ModelPropertySetter
        {

            #region Properties

            private static readonly ConcurrentDictionary<string, Func<object, object?, object>> PropertySetters = new ConcurrentDictionary<string, Func<object, object?, object>>();

            #endregion

            #region Methods

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
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
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            protected static Func<object, object?, object> CreatePropertySetter(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = CreateDynamicMethod(type, propertyName);
                return (Func<object, object?, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object?, object>));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="model"></param>
            /// <param name="propertyName"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            internal static object SetPropertyValue(object model, string propertyName, object? value)
            {
                Type type = model.GetType();
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
        /// Dynamically obtain the attribute value of object (with better performance)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object? GetPropertyValue(object model, string propertyName)
        {
            object? value = ModelPropertyGetter.GetPropertyValue(model, propertyName);
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        private class ModelPropertyGetter
        {

            #region Properties

            private static readonly ConcurrentDictionary<string, Func<object, object?>> PropertyGetters = new ConcurrentDictionary<string, Func<object, object?>>();

            #endregion

            #region Methods

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            protected static DynamicMethod CreateDynamicMethod(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object) }, type.Module);//定义和表示一个可以编译，执行和丢弃的动态方法
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

                var iLGenerator = dynamicMethod.GetILGenerator();//返回方法的Microsoft中间语言（MSIL）生成器，默认的MSIL流大小为64字节
                if (iLGenerator != null)
                {
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Callvirt, getMethod);

                    if (property.PropertyType.IsValueType)
                    {
                        iLGenerator.Emit(OpCodes.Box, property.PropertyType);//值类型需要装箱，因为返回类型是object
                    }
                    iLGenerator.Emit(OpCodes.Ret);
                }

                return dynamicMethod;
            }

            /// <summary>
            /// 创建对象属性赋值器的动态委托
            /// </summary>
            /// <param name="type">对象类型</param>
            /// <param name="propertyName">属性名</param>
            /// <returns></returns>
            protected static Func<object, object?> CreatePropertyGetter(Type type, string propertyName)
            {
                DynamicMethod dynamicMethod = CreateDynamicMethod(type, propertyName);
                return (Func<object, object?>)dynamicMethod.CreateDelegate(typeof(Func<object, object?>));//CreateDelegate()完成动态方法并创建一个可用于执行它的委托。
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="model"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            internal static object? GetPropertyValue(object model, string propertyName)
            {
                Type type = model.GetType();
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
        /// Dynamically set the field value of object (with better performance)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object SetFieldValue(object model, string fieldName, object? value)
        {
            model = ModelFieldAccessor.SetFieldValue(model, fieldName, value);
            return model;
        }

        /// <summary>
        /// Dynamically obtain field value of object (with better performance)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object? GetFieldValue(object model, string fieldName)
        {
            object? v = ModelFieldAccessor.GetFieldValue(model, fieldName);
            return v;
        }

        /// <summary>
        /// 
        /// </summary>
        private class ModelFieldAccessor
        {

            #region Properties

            private static readonly ConcurrentDictionary<string, FieldAccessor> FieldAccessors = new ConcurrentDictionary<string, FieldAccessor>();

            #endregion

            #region Methods

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            protected static FieldAccessor CreateFieldAccessor(Type type, string fieldName)
            {
                return new FieldAccessor(type, fieldName);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="model"></param>
            /// <param name="fieldName"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            internal static object SetFieldValue(object model, string fieldName, object? value)
            {
                Type type = model.GetType();
                string key = string.Concat(type.FullName, "__", fieldName);
                FieldAccessor fieldAccessor = FieldAccessors.GetOrAdd(key, _ => CreateFieldAccessor(type, fieldName));
                fieldAccessor.Set(model, value);
                return model;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="model"></param>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            internal static object? GetFieldValue(object model, string fieldName)
            {
                Type type = model.GetType();
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
