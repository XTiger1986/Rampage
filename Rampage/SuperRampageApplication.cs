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
    /// Rampage Application Base Class
    /// </summary>
    public abstract class SuperRampageApplication : IRampageApplication
    {

        public SuperRampageApplication()
        {
            UniqueId = Guid.NewGuid().ToString("N");
            ServiceScopes = new Dictionary<string, IServiceScope>();
            _scopeLock = new object();
        }

        private readonly object _scopeLock;

        public virtual string UniqueId
        {
            get;
            set;
        }

        /// <summary>
        /// Application Running Environment Name
        /// </summary>
        public virtual string? EnvironmentName
        {
            get;
            set;
        }

        /// <summary>
        /// Application Services Container
        /// </summary>
        public virtual IServiceCollection? ServiceContainer
        {
            get;
            protected set;
        }

        /// <summary>
        /// Application Level ServiceProvider
        /// </summary>
        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// Application ServiceScope Container
        /// </summary>
        public virtual Dictionary<string, IServiceScope> ServiceScopes
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Get Application Level ServiceProvider
        /// </summary>
        public IServiceProvider GetServiceProvider()
        {
            return ServiceProvider!;
        }

        /// <summary>
        /// Get Application ServiceScope Container
        /// </summary>
        public virtual Dictionary<string, IServiceScope> GetServiceScopes()
        {
            return ServiceScopes!;
        }

        /// <summary>
        /// get service scope in root
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
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
        /// Get the ServiceScope's ServiceProvider
        /// </summary>
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
        /// create new ServiceScope
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        public virtual IServiceScope CreateScope(string scopeName)
        {
            IServiceScope scope = ServiceProvider!.CreateScope();
            lock (_scopeLock)
            {
                if (ServiceScopes.TryGetValue(scopeName, out var existing))
                {
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
        /// 
        /// </summary>
        public virtual S GetService<S>()
        {
            return ServiceProvider!.GetService<S>()!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="scope"></param>
        /// <returns></returns>
        public virtual S GetScopeService<S>(IServiceScope scope)
        {
            return scope.ServiceProvider.GetService<S>()!;
        }

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

        public virtual void InitializeGlobalConfiguration(string[] args)
        {
            // register callbacks of GlobalConfiguration changes 
            GlobalConfiguration.RegisterReloadCallback((_) =>
            {
                LocalizationManager.ReloadConfig();
                DatabaseFactory.ReloadConfig();
                SuperClientConfigManager.ReloadConfigs();
            });
            GlobalConfiguration.Initialize(EnvironmentName!);
        }

        public virtual IServiceCollection InitializeServiceContainer(string[] args)
        {
            ServiceContainer = new ServiceCollection();
            return ServiceContainer;
        }

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

        public virtual IServiceProvider InitializeApplicationProvider(string[] args)
        {
            ServiceProvider = ServiceContainer!.BuildServiceProvider();
            SingletonServiceLocator.SetProvider(ServiceProvider);
            return ServiceProvider;
        }


        /// <summary>
        /// Register and inject specified types of services
        /// </summary>
        /// <param name="baseType">services base type</param>
        /// <param name="lifetime"></param>
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

        protected virtual void RegisterService<Base, T>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            RegisterService(typeof(Base), typeof(T), lifetime);
        }

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

            // Fail fast：立即 [Auto] 互依赖会在首请求栈溢出；启动时直接报环
            AutoInjectCycleDetector.ValidateOrThrow(RampageBooter.ServiceTargetTypeList);
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract void Run(string[] args);

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
