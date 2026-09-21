using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// Rampage 应用程序契约：定义七步初始化流程与作用域管理 / Rampage application contract: the seven-step initialization flow and scope management
    /// </summary>
    public interface IRampageApplication : IDisposable
    {

        /// <summary>
        /// 应用实例唯一标识 / Unique id of the application instance
        /// </summary>
        public string UniqueId
        {
            get;
            set;
        }

        /// <summary>
        /// 应用运行环境名称（如 Development/Staging/Production） / Application running environment name (e.g. Development/Staging/Production)
        /// </summary>
        public string? EnvironmentName
        {
            get;
            set;
        }

        /// <summary>
        /// 获取应用配置完成的根服务提供器 / Gets the application's configured root service provider
        /// </summary>
        /// <returns>应用根级 Provider / The application root provider</returns>
        IServiceProvider GetServiceProvider();

        /// <summary>
        /// 获取应用的作用域容器 / Gets the application's named scope container
        /// </summary>
        /// <returns>作用域字典 / The scope dictionary</returns>
        Dictionary<string, IServiceScope> GetServiceScopes();

        /// <summary>
        /// 获取指定名称的作用域，未指定名称时返回全局作用域 / Gets the scope with the given name, or the global scope when the name is omitted
        /// </summary>
        /// <param name="scopeName">作用域名称，空则使用全局作用域名 / Scope name; falls back to the global scope name when null or empty</param>
        /// <returns>对应的服务作用域 / The matching service scope</returns>
        IServiceScope GetScope(string? scopeName = null);

        /// <summary>
        /// 获取指定作用域的服务提供器 / Gets the service provider of the given scope
        /// </summary>
        /// <param name="scopeName">作用域名称，空则使用全局作用域名 / Scope name; falls back to the global scope name when null or empty</param>
        /// <returns>作用域的 Provider，不存在时为 null / The scope's provider, or null when absent</returns>
        IServiceProvider? GetScopeProvider(string? scopeName = null);

        /// <summary>
        /// 在根服务提供器上创建命名作用域，同名作用域会被替换 / Creates a named scope on the root provider; an existing scope with the same name is replaced
        /// </summary>
        /// <param name="scopeName">作用域名称 / Scope name</param>
        /// <returns>新创建的作用域 / The newly created scope</returns>
        IServiceScope CreateScope(string scopeName);

        /// <summary>
        /// 第一步：初始化应用运行环境 / First step: initialize the application environment
        /// </summary>
        /// <remarks>取值优先级：args[0] → DOTNET_ENVIRONMENT → "Development" / Priority: args[0], then DOTNET_ENVIRONMENT, then "Development"</remarks>
        /// <param name="args">启动参数，args[0] 为环境名 / Boot arguments where args[0] is the environment name</param>
        /// <returns>最终生效的环境名 / The effective environment name</returns>
        string InitializeEnvironment(string[] args);

        /// <summary>
        /// 第二步：初始化日志 / Second step: initialize logging
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        void InitializeLogger(string[] args);

        /// <summary>
        /// 第三步：初始化全局配置 / Third step: initialize global configuration
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        void InitializeGlobalConfiguration(string[] args);

        /// <summary>
        /// 第四步：初始化应用服务容器 / Fourth step: initialize the application service container
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        /// <returns>新创建的 IServiceCollection / The newly created service collection</returns>
        IServiceCollection InitializeServiceContainer(string[] args);

        /// <summary>
        /// 第五步：初始化数据库客户端服务 / Fifth step: initialize database client services
        /// </summary>
        /// <remarks>依赖第三步加载的数据库配置，顺序不可提前 / Depends on the database configuration loaded in the third step; must not run earlier</remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        void InitializeDatabaseClients(string[] args);

        /// <summary>
        /// 第六步：注册全部服务 / Sixth step: register all services
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        void InitializeServices(string[] args);

        /// <summary>
        /// 第七步：构建应用根 ServiceProvider（须在服务注册完成后调用） / Seventh step: build the application root provider (after services are initialized)
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        /// <returns>构建完成的根 Provider / The built root provider</returns>
        IServiceProvider InitializeApplicationProvider(string[] args);


        /// <summary>
        /// 应用启动完成后的业务入口 / Business entry point executed after startup
        /// </summary>
        /// <param name="args">启动参数，args[0] 为环境名 / Boot arguments where args[0] is the environment name</param>
        void Run(string[] args);

    }
}
