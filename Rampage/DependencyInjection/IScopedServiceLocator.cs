using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 作用域服务定位器：在当前作用域内按类型/服务名解析服务 / Scoped service locator: resolves services by type or service name within the current scope.
    /// </summary>
    public interface IScopedServiceLocator
    {
        /// <summary>
        /// 当前作用域的服务提供者 / The service provider of the current scope.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        T Get<T>() where T : notnull;

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        T Get<T>(string serviceName) where T : notnull;

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>实例列表 / The instance list.</returns>
        List<T> GetList<T>() where T : notnull;

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        object Get(Type type);

        /// <summary>
        /// 获取指定名称的服务，不存在则抛出异常 / Gets the named service; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例 / The service instance.</returns>
        object Get(string serviceName);

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        object Get(string serviceName, Type type);

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>实例列表 / The instance list.</returns>
        List<object?> GetList(Type type);

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        T? TryGet<T>() where T : notnull;

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        T? TryGet<T>(string serviceName);

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        object? TryGet(Type type);

        /// <summary>
        /// 尝试获取指定名称的服务，不存在返回 null / Tries to get the named service; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        object? TryGet(string serviceName);

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        object? TryGet(string serviceName, Type type);


    }
}
