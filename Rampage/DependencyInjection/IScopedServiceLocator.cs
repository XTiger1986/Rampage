using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public interface IScopedServiceLocator
    {
        /// <summary>
        /// 
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出错误
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Get<T>() where T : notnull;

        /// <summary>
        /// 获取指定名称的服务并显式转为指定类型，不存在则抛出错误
        /// </summary>
        /// <param name="serviceName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Get<T>(string serviceName) where T : notnull;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        List<T> GetList<T>() where T : notnull;

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出错误
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        object Get(Type type);

        /// <summary>
        /// 获取指定名称的服务，不存在则抛出错误
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        object Get(string serviceName);

        /// <summary>
        /// 获取指定名称的服务并显式转为指定类型，不存在则抛出错误
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        object Get(string serviceName, Type type);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        List<object?> GetList(Type type);

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T? TryGet<T>() where T : notnull;

        /// <summary>
        /// 尝试获取指定名称的服务并显式转为指定类型，不存在返回null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T? TryGet<T>(string serviceName);

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        object? TryGet(Type type);

        /// <summary>
        /// 尝试获取指定名称的服务，不存在返回null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        object? TryGet(string serviceName);

        /// <summary>
        /// 尝试获取指定名称的服务并显式转为指定类型，不存在返回null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        object? TryGet(string serviceName, Type type);


    }
}
