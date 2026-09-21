using Microsoft.Extensions.DependencyInjection;
using Rampage.Data;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.Dynamic;
using Rampage.Extensions;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Net;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Reflection;

namespace Rampage
{
    /// <summary>
    /// Rampage 应用程序抽象基类 / Abstract base class of Rampage applications
    /// </summary>
    /// <remarks>
    /// 承载框架全部启动步骤（环境、日志、全局配置、服务容器、数据库客户端、服务注册、根 Provider），
    /// 由 RampageBooter.Run 按固定顺序调用；业务应用继承本类并实现 Run，或直接使用 RampageBasicApplication。
    /// Hosts every framework startup step (environment, logger, global configuration, service container, database clients,
    /// service registration, root provider), invoked in a fixed order by RampageBooter.Run; derive from this class and
    /// implement Run, or use RampageBasicApplication directly.
    /// </remarks>
    public abstract class SuperRampageApplication : IRampageApplication
    {

        /// <summary>
        /// 初始化作用域容器与同步锁 / Initializes the scope container and its lock
        /// </summary>
        public SuperRampageApplication()
        {
            UniqueId = Guid.NewGuid().ToString("N");
            ServiceScopes = new Dictionary<string, IServiceScope>();
            _scopeLock = new object();
        }

        private readonly object _scopeLock;

        /// <summary>
        /// 应用实例唯一标识（构造时生成的 Guid） / Unique id of the application instance (a Guid generated at construction)
        /// </summary>
        public virtual string UniqueId
        {
            get;
            set;
        }

        /// <summary>
        /// 应用运行环境名称（如 Development/Staging/Production） / Application running environment name (e.g. Development/Staging/Production)
        /// </summary>
        public virtual string? EnvironmentName
        {
            get;
            set;
        }

        /// <summary>
        /// 应用服务容器（IServiceCollection） / Application service collection container
        /// </summary>
        public virtual IServiceCollection? ServiceContainer
        {
            get;
            protected set;
        }

        /// <summary>
        /// 应用根级 IServiceProvider（第七步构建后可用） / Application root service provider (available after the seventh step)
        /// </summary>
        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 已命名的作用域容器 / Container of named service scopes
        /// </summary>
        public virtual Dictionary<string, IServiceScope> ServiceScopes
        {
            get; 
            protected set;
        }

        /// <summary>
        /// 获取应用根级 IServiceProvider / Gets the application root service provider
        /// </summary>
        /// <returns>应用根级 Provider / The application root provider</returns>
        public IServiceProvider GetServiceProvider()
        {
            return ServiceProvider!;
        }

        /// <summary>
        /// 获取已命名的作用域容器 / Gets the container of named service scopes
        /// </summary>
        /// <returns>作用域字典 / The scope dictionary</returns>
        public virtual Dictionary<string, IServiceScope> GetServiceScopes()
        {
            return ServiceScopes!;
        }

        /// <summary>
        /// 获取指定名称的作用域，未指定名称时返回全局作用域 / Gets the scope with the given name, or the global scope when the name is omitted
        /// </summary>
        /// <param name="scopeName">作用域名称，空则使用全局作用域名 / Scope name; falls back to the global scope name when null or empty</param>
        /// <returns>对应的服务作用域 / The matching service scope</returns>
        /// <exception cref="KeyNotFoundException">作用域尚未创建时由字典访问抛出 / Thrown by dictionary access when the scope has not been created</exception>
        public virtual IServiceScope GetScope(string? scopeName = null)
        {
            if (string.IsNullOrEmpty(scopeName))
            {
                scopeName = Globals.GLOBAL_SCOPE_NAME;
            }
            lock (_scopeLock)
            {
                return ServiceScopes[scopeName];
            }
        }

        /// <summary>
        /// 获取指定作用域的 IServiceProvider，作用域不存在时返回 null / Gets the provider of the given scope, or null when the scope does not exist
        /// </summary>
        /// <param name="scopeName">作用域名称，空则使用全局作用域名 / Scope name; falls back to the global scope name when null or empty</param>
        /// <returns>作用域的 Provider，不存在时为 null / The scope's provider, or null when absent</returns>
        public virtual IServiceProvider? GetScopeProvider(string? scopeName = null)
        {
            if (string.IsNullOrEmpty(scopeName))
            {
                scopeName = Globals.GLOBAL_SCOPE_NAME;
            }
            lock (_scopeLock)
            {
                IServiceScope? scope = ServiceScopes != null && ServiceScopes.TryGetValue(scopeName, out var s) ? s : null;
                if (scope != null)
                {
                    return scope.ServiceProvider;
                }
                return null;
            }
        }

        /// <summary>
        /// 创建新的命名作用域；同名作用域已存在时会先释放旧作用域再替换 / Creates a new named scope; an existing scope with the same name is disposed and replaced
        /// </summary>
        /// <param name="scopeName">作用域名称 / Scope name</param>
        /// <returns>新创建的作用域 / The newly created scope</returns>
        public virtual IServiceScope CreateScope(string scopeName)
        {
            IServiceScope scope = ServiceProvider!.CreateScope();
            lock (_scopeLock)
            {
                if (ServiceScopes.TryGetValue(scopeName, out var existing))
                {
                    // 替换前先释放旧作用域，避免泄漏 / dispose the old scope before replacing to avoid leaks
                    existing.Dispose();
                    ServiceScopes[scopeName] = scope;
                }
                else
                {
                    ServiceScopes.Add(scopeName, scope);
                }
            }
            return scope;
        }

        /// <summary>
        /// 从应用根 Provider 解析服务 / Resolves a service from the application root provider
        /// </summary>
        /// <typeparam name="S">服务类型 / Service type</typeparam>
        /// <returns>服务实例 / The service instance</returns>
        public virtual S GetService<S>()
        {
            return ServiceProvider!.GetService<S>()!;
        }

        /// <summary>
        /// 从指定作用域解析服务 / Resolves a service from the given scope
        /// </summary>
        /// <typeparam name="S">服务类型 / Service type</typeparam>
        /// <param name="scope">目标作用域 / Target scope</param>
        /// <returns>服务实例 / The service instance</returns>
        public virtual S GetScopeService<S>(IServiceScope scope)
        {
            return scope.ServiceProvider.GetService<S>()!;
        }

        /// <summary>
        /// 第一步：初始化运行环境名 / First step: initialize the running environment name
        /// </summary>
        /// <remarks>
        /// 取值优先级：args[0] → 环境变量 DOTNET_ENVIRONMENT → 默认 "Development"。
        /// Priority: args[0], then the DOTNET_ENVIRONMENT variable, then the default "Development".
        /// </remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        /// <returns>最终生效的环境名 / The effective environment name</returns>
        public virtual string InitializeEnvironment(string[] args)
        {
            if (args.Length > 0)
            {
                EnvironmentName = args[0];
            }
            if (string.IsNullOrEmpty(EnvironmentName))
            {
                EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            }
            if (string.IsNullOrEmpty(EnvironmentName))
            {
                EnvironmentName = "Development";
            }
            return EnvironmentName;
        }

        /// <summary>
        /// 第二步：初始化日志（log4net） / Second step: initialize logging (log4net)
        /// </summary>
        /// <remarks>
        /// 优先加载 config\log4net.{环境名}.config，不存在时回退到 config\log4net.config。
        /// Loads config\log4net.{EnvironmentName}.config first and falls back to config\log4net.config when it does not exist.
        /// </remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        public virtual void InitializeLogger(string[] args)
        {
            string? log4netConfigPath = null;
            if (!string.IsNullOrEmpty(EnvironmentName))
            {
                log4netConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config\\log4net.{EnvironmentName}.config");
            }
            if (string.IsNullOrEmpty(log4netConfigPath) || false == File.Exists(log4netConfigPath))
            {
                log4netConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config\\log4net.config");
            }
            LoggerFactory.Configure(log4netConfigPath);
        }

        /// <summary>
        /// 第三步：初始化全局配置 / Third step: initialize global configuration
        /// </summary>
        /// <remarks>
        /// 先注册配置热重载回调链（本地化、数据库工厂、客户端配置），再执行 GlobalConfiguration.Initialize；
        /// 此后 app.json 变更会依次触发上述组件 ReloadConfig。
        /// Registers the hot-reload callback chain (localization, database factory, client configs) before calling
        /// GlobalConfiguration.Initialize; afterwards any app.json change triggers ReloadConfig on those components in order.
        /// </remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        public virtual void InitializeGlobalConfiguration(string[] args)
        {
            // 注册全局配置热重载回调链 / register the hot-reload callback chain of GlobalConfiguration
            GlobalConfiguration.RegisterReloadCallback((_) =>
            {
                LocalizationManager.ReloadConfig();
                DatabaseFactory.ReloadConfig();
                SuperClientConfigManager.ReloadConfigs();
            });
            GlobalConfiguration.Initialize(EnvironmentName!);
        }

        /// <summary>
        /// 第四步：创建应用服务容器 / Fourth step: create the application service container
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        /// <returns>新创建的 IServiceCollection / The newly created service collection</returns>
        public virtual IServiceCollection InitializeServiceContainer(string[] args)
        {
            ServiceContainer = new ServiceCollection();
            return ServiceContainer;
        }

        /// <summary>
        /// 第五步：初始化数据库客户端 / Fifth step: initialize database clients
        /// </summary>
        /// <remarks>
        /// 依赖第三步加载的数据库配置。为每个客户端动态生成实现类型、注册 Scoped 服务，并建立实际连接读取表结构；
        /// 仅当映射模型的所有列都能在数据库表列中找到（忽略大小写）时，该模型才被收录进 MappingModelTypes。
        /// Depends on the database configuration loaded in the third step. Each client gets a dynamically generated implementation
        /// type and a Scoped registration, and a real connection is opened to read table schemas; a mapped model is added to
        /// MappingModelTypes only when all of its columns exist in the table columns (case-insensitive).
        /// </remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        public virtual void InitializeDatabaseClients(string[] args)
        {
            if (DatabaseFactory.Config != null && DatabaseFactory.Config.Clients != null && DatabaseFactory.Config.Clients.Any())
            {
                if (DatabaseFactory.Config.ImplClientTypes == null)
                {
                    DatabaseFactory.Config.ImplClientTypes = new Dictionary<string, Type>();
                }
                if (DatabaseFactory.Config.MappingModelTypes == null)
                {
                    DatabaseFactory.Config.MappingModelTypes = new Dictionary<string, Dictionary<string, Type>>();
                }

                Type databaseClientType = typeof(DatabaseClient);
                foreach (var clientConfigItem in DatabaseFactory.Config.Clients)
                {
                    string name = clientConfigItem.Key;
                    DatabaseClientConfig clientConfig = clientConfigItem.Value;
                    Type baseClientType = DatabaseFactory.GetClientType(name);
                    Type clientType = DynamicDatabaseClientBuilder.Create(clientConfig, baseClientType);
                    if (!DatabaseFactory.Config.ImplClientTypes.ContainsKey(name))
                    {
                        DatabaseFactory.Config.ImplClientTypes.Add(name, clientType);
                    }
                    if (!DatabaseFactory.Config.MappingModelTypes.ContainsKey(name))
                    {
                        DatabaseFactory.Config.MappingModelTypes.Add(name, new Dictionary<string, Type>());
                    }

                    string serviceName = string.Concat(DatabaseClient.SERVICE_NAME_PREFIX, name);
                    AutoInjectionSchemaStore.AddServiceName(serviceName, clientType);
                    ServiceContainer!.AddScoped(databaseClientType, clientType);

                    using (DatabaseClient db = DatabaseFactory.CreateClient(name))
                    {
                        var tableInfoList = db.GetTableInfos();
                        if (tableInfoList != null && tableInfoList.Any())
                        {
                            foreach (var tableInfo in tableInfoList)
                            {
                                string tableName = tableInfo.TableName ?? string.Empty;
                                TableMapping? modelMapping = ModelSchemaStore.GetModelMapping(tableName);
                                if (modelMapping == null || modelMapping.ModelType == null)
                                {
                                    continue;
                                }
                                var columnMappingList = modelMapping.ColumnMappingList;
                                if (columnMappingList == null || !columnMappingList.Any())
                                {
                                    continue;
                                }

                                // 检查模型映射列是否与数据库表列全部匹配（忽略大小写） / check that every mapped column exists in the table columns (case-insensitive)
                                bool matchColumns = true;
                                var dbColumnInfoList = tableInfo.Columns;
                                if (dbColumnInfoList != null && dbColumnInfoList.Any())
                                {
                                    foreach (var columnMapping in columnMappingList)
                                    {
                                        if (!dbColumnInfoList.Any(c => StringTool.EqualsIgnoreCase(c.ColumnName, columnMapping.ColumnName)))
                                        {
                                            matchColumns = false;
                                            break;
                                        }
                                    }
                                }

                                if (matchColumns)
                                {
                                    DatabaseFactory.Config.MappingModelTypes[name].Add(modelMapping.ModelType.FullName!, modelMapping.ModelType);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 第七步：构建应用根 IServiceProvider 并注册到服务定位器 / Seventh step: build the application root provider and register it with the service locator
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        /// <returns>构建完成的根 Provider / The built root provider</returns>
        public virtual IServiceProvider InitializeApplicationProvider(string[] args)
        {
            ServiceProvider = ServiceContainer!.BuildServiceProvider();
            SingletonServiceLocator.SetProvider(ServiceProvider);
            return ServiceProvider;
        }


        /// <summary>
        /// 注册指定基类型找到的全部服务实现 / Registers every implementation of the given base type found in the scanned list
        /// </summary>
        /// <param name="baseType">服务基类型 / Service base type</param>
        /// <param name="lifetime">生命周期，默认 Scoped / Service lifetime, Scoped by default</param>
        /// <returns>找到并注册了至少一个实现时返回 true / true when at least one implementation was found and registered</returns>
        protected virtual bool RegisterServices(Type baseType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var targetTypeList = RampageBooter.ServiceTargetTypeList.Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract);
            if (!targetTypeList.Any())
            {
                return false;
            }

            foreach (Type type in targetTypeList)
            {
                RegisterService(baseType, type, lifetime);
            }

            return true;
        }

        /// <summary>
        /// 注册单个服务实现（生命周期/实现类型/基类型三元组已存在时跳过） / Registers a single service implementation, skipping when the lifetime/implementation/base triple is already registered
        /// </summary>
        /// <remarks>
        /// 服务名取 [Service] 特性的 Name，缺省为 "命名空间.类型名"，并登记到 AutoInjectionSchemaStore 供按名注入；
        /// 当实现类型与基类型相同时按自身类型注册。
        /// The service name comes from the [Service] attribute Name, defaulting to "Namespace.TypeName", and is recorded in
        /// AutoInjectionSchemaStore for name-based injection; when the implementation type equals the base type it is registered as itself.
        /// </remarks>
        /// <param name="baseType">服务基类型 / Service base type</param>
        /// <param name="type">实现类型 / Implementation type</param>
        /// <param name="lifetime">生命周期，默认 Scoped / Service lifetime, Scoped by default</param>
        protected virtual void RegisterService(Type baseType, Type type, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            string? serviceName = null;
            if (type.IsDefined(typeof(ServiceAttribute)))
            {
                ServiceAttribute serviceAttribute = type.GetCustomAttribute<ServiceAttribute>(false)!;
                serviceName = serviceAttribute.Name;
            }
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = $"{type.Namespace}.{type.Name}";
            }

            if (!type.FullName.EqualsIgnoreCase(baseType.FullName))
            {
                if (!ServiceContainer!.Any(s => s.Lifetime == lifetime && s.ImplementationType == type && s.ServiceType == baseType))
                {
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        AutoInjectionSchemaStore.AddServiceName(serviceName, type);
                    }

                    switch (lifetime)
                    {
                        case ServiceLifetime.Scoped:
                        default:
                            ServiceContainer!.AddScoped(baseType, type);
                            break;
                        case ServiceLifetime.Transient:
                            ServiceContainer!.AddTransient(baseType, type);
                            break;
                        case ServiceLifetime.Singleton:
                            ServiceContainer!.AddSingleton(baseType, type);
                            break;
                    }
                }
            }
            else
            {
                if (!ServiceContainer!.Any(s => s.Lifetime == lifetime && s.ImplementationType == type && s.ServiceType == type))
                {
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        AutoInjectionSchemaStore.AddServiceName(serviceName, type);
                    }

                    switch (lifetime)
                    {
                        case ServiceLifetime.Scoped:
                        default:
                            ServiceContainer!.AddScoped(type);
                            break;
                        case ServiceLifetime.Transient:
                            ServiceContainer!.AddTransient(type);
                            break;
                        case ServiceLifetime.Singleton:
                            ServiceContainer!.AddSingleton(type);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 以泛型参数注册单个服务实现 / Registers a single service implementation via generic parameters
        /// </summary>
        /// <typeparam name="Base">服务基类型 / Service base type</typeparam>
        /// <typeparam name="T">实现类型 / Implementation type</typeparam>
        /// <param name="lifetime">生命周期，默认 Scoped / Service lifetime, Scoped by default</param>
        protected virtual void RegisterService<Base, T>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            RegisterService(typeof(Base), typeof(T), lifetime);
        }

        /// <summary>
        /// 第六步：注册全部服务 / Sixth step: register all services
        /// </summary>
        /// <remarks>
        /// 先按扫描到的基类型批量注册；基类型无实现且为 ISuperDao 派生接口或 HTTP 客户端接口（或标注 SuperClientAttribute）时，
        /// 动态生成实现类型再注册；最后执行 [Auto] 注入循环依赖检测，发现环立即抛出异常。
        /// Registers services in batches by the scanned base types; base types without implementations that derive from ISuperDao
        /// or represent HTTP client interfaces (or carry SuperClientAttribute) get dynamically generated implementations;
        /// finally [Auto] injection cycles are detected and reported immediately.
        /// </remarks>
        /// <param name="args">启动参数 / Boot arguments</param>
        public virtual void InitializeServices(string[] args)
        {
            Type idaoType = Globals.TYPE_SUPER_DAO_I;
            Type isuperClientType = Globals.TYPE_SUPER_HTTP_CLIENT_I_TYPE;
            Type superClientAttrType = Globals.TYPE_SUPER_CLIENT_ATTR_TYPE;

            foreach (var baseType in RampageBooter.ServiceBaseTypeList)
            {
                bool isRegister = RegisterServices(baseType.Type, baseType.Lifetime);

                if (!isRegister)
                {
                    if (ReflectionTool.IsSubInterface(baseType.Type, idaoType))
                    {
                        Type? dynamicDaoType = DynamicDaoCodeBuilder.CreateImplementation(baseType.Type);
                        if (dynamicDaoType == null)
                        {
                            throw new NotImplementedException($"Unable to generate dynamic implemention codes of \"{baseType.Type.FullName}\"");
                        }
                        RampageBooter.AddTargetServiceTypes(new List<Type> { dynamicDaoType });
                        RegisterServices(baseType.Type, baseType.Lifetime);
                    }

                    if (ReflectionTool.IsSubInterface(baseType.Type, isuperClientType) || baseType.Type.IsDefined(superClientAttrType))
                    {
                        Type? dynamicSuperClientType = DynamicHttpClientBuilder.CreateImplementation(baseType.Type);
                        if (dynamicSuperClientType == null)
                        {
                            throw new NotImplementedException($"Unable to generate dynamic implemention codes of \"{baseType.Type.FullName}\"");
                        }
                        RampageBooter.AddTargetServiceTypes(new List<Type> { dynamicSuperClientType });
                        RegisterServices(baseType.Type, baseType.Lifetime);
                    }
                }
            }

            // 快速失败：[Auto] 循环依赖会在首个请求时栈溢出，启动阶段直接报环 / fail fast: [Auto] cycles would overflow the stack on the first request, so report them at startup
            AutoInjectCycleDetector.ValidateOrThrow(RampageBooter.ServiceTargetTypeList);
        }

        /// <summary>
        /// 应用启动完成后的业务入口，由派生类实现 / Business entry point executed after startup, implemented by derived classes
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        public abstract void Run(string[] args);

        /// <summary>
        /// 释放全部作用域并清空根 Provider / Disposes every scope and clears the root provider
        /// </summary>
        public virtual void Dispose()
        {
            lock (_scopeLock)
            {
                foreach (var scope in ServiceScopes)
                {
                    scope.Value.Dispose();
                }
                ServiceScopes.Clear();
            }
            if (ServiceProvider != null)
            {
                ServiceProvider = null;
            }
        }


    }
}
