using Microsoft.Extensions.DependencyInjection;
using Rampage.Configuration;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Hierarchy;
using Rampage.Reflection;
using Rampage.Threading;
using System;
using System.Reflection;

namespace Rampage
{
    /// <summary>
    /// Rampage 应用程序启动器 / Rampage application launcher
    /// </summary>
    /// <remarks>
    /// <see cref="Run{Z}"/> 是框架唯一入口：先扫描程序集收集服务类型，再按固定顺序完成七步初始化，顺序不可调换
    /// （InitializeDatabaseClients 依赖 InitializeGlobalConfiguration 加载的数据库配置）。
    /// Run&lt;Z&gt; is the only entry point of the framework: it scans assemblies to collect service types first, then performs
    /// the seven initialization steps in a fixed order that must not change (InitializeDatabaseClients depends on the
    /// database configuration loaded by InitializeGlobalConfiguration).
    /// </remarks>
    public class RampageBooter
    {

        static RampageBooter()
        {
            EssentialAssemblies = ReflectionTool.GetEssentialAssemblies();
            ServiceTypesDict = new Dictionary<Type, List<Type>>();
            ServiceBaseTypeList = new List<AutoServiceType>();
            ServiceTargetTypeList = new List<Type>();
        }

        /// <summary>
        /// 框架核心程序集列表（Rampage 本体与各业务程序集），配置注册与服务扫描均以此为基础 / List of essential assemblies (Rampage itself plus business assemblies); config registration and service scanning are based on it
        /// </summary>
        public static List<Assembly> EssentialAssemblies
        {
            get;
            private set;
        }

        /// <summary>
        /// 待注入的服务基类型列表（含各自生命周期） / All service base types to be injected, each with its lifetime
        /// </summary>
        internal static List<AutoServiceType> ServiceBaseTypeList
        {
            get;
            private set;
        }

        /// <summary>
        /// 按基类型分组的服务实现类型字典（当前预留未使用） / Reserved dictionary grouping service implementation types by base type (currently unused)
        /// </summary>
        internal static Dictionary<Type, List<Type>> ServiceTypesDict
        {
            get;
            private set;
        }

        /// <summary>
        /// 待注入的服务实现类型列表 / All service implementation types to be injected
        /// </summary>
        internal static List<Type> ServiceTargetTypeList
        {
            get;
            private set;
        }

        private static object _serviceBaseTypeListLock = new object();
        /// <summary>
        /// 追加一个服务基类型（已存在则忽略），线程安全 / Adds a service base type unless already present; thread safe
        /// </summary>
        internal static void AddBaseServiceType(Type type, ServiceLifetime lifetime)
        {
            lock (_serviceBaseTypeListLock)
            {
                if (ServiceBaseTypeList.FindIndex(t => t.Type == type) < 0)
                {
                    ServiceBaseTypeList.Add(new AutoServiceType
                    {
                        Type = type,
                        Lifetime = lifetime
                    });
                }
            }
        }

        private static object _serviceTargetTypeListLock = new object();
        /// <summary>
        /// 批量追加服务实现类型，线程安全 / Appends a batch of service implementation types; thread safe
        /// </summary>
        internal static void AddTargetServiceTypes(List<Type> types)
        {
            lock (_serviceTargetTypeListLock)
            {
                ServiceTargetTypeList.AddRange(types);
            }
        }

        private static readonly Type _boType = typeof(BaseService);
        private static readonly Type _iboType = typeof(IBaseService<,>);
        private static readonly Type _idaoType = typeof(ISuperDao<>);

        /// <summary>
        /// 扫描程序集中所有 [Auto] 注入成员，收集待注册的服务基类型 / Scans [Auto] injection members in the assembly and collects service base types to register
        /// </summary>
        /// <remarks>
        /// DatabaseClient 类型的成员在此跳过，统一在 InitializeDatabaseClients 中注册；
        /// 非抽象的 BaseService 实现还会依据其基类第二个泛型参数（ISuperDao 派生接口）追加 DAO 基类型注册。
        /// Members typed as DatabaseClient are skipped here and registered in InitializeDatabaseClients instead;
        /// non-abstract BaseService implementations also register the DAO interface taken from the second generic argument of their base type.
        /// </remarks>
        /// <param name="assembly">待扫描的程序集 / Assembly to scan</param>
        public static void LoadBaseServiceTypes(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            Type dcType = typeof(DatabaseClient);

            foreach (Type type in types)
            {
                AutoInjectionSchema? autoSchema = AutoInjectionSchemaStore.GetSchema(type);
                if (autoSchema == null)
                {
                    continue;
                }

                List<Auto>? autoList = autoSchema.AutoList;
                if (autoList == null || autoList.Count == 0)
                {
                    continue;
                }

                // 自动注入成员 / Auto injection members
                foreach (Auto auto in autoList)
                {
                    if (auto.Property != null)
                    {
                        // DatabaseClient 类型在 InitializeDatabaseClients 中统一注册 / DatabaseClient injections are handled in InitializeDatabaseClients
                        if (dcType.IsAssignableFrom(auto.Property.PropertyType))
                        {
                            continue;
                        }
                        AddBaseServiceType(auto.Property.PropertyType, auto.Lifetime);
                    }
                    if (auto.Field != null)
                    {
                        // DatabaseClient 类型在 InitializeDatabaseClients 中统一注册 / DatabaseClient injections are handled in InitializeDatabaseClients
                        if (dcType.IsAssignableFrom(auto.Field.FieldType))
                        {
                            continue;
                        }
                        AddBaseServiceType(auto.Field.FieldType, auto.Lifetime);
                    }
                }

                // 处理 ServiceBase 实现中的 IDao 成员 / handle IDao members in ServiceBase implementations
                if (!type.IsAbstract && type.IsAssignableTo(_boType))
                {
                    var superServiceType = type.BaseType!;
                    var genArgs = superServiceType.GetGenericArguments();
                    if (genArgs.Any() && genArgs.Length == 2)
                    {
                        var daoType = genArgs[1];
                        if (ReflectionTool.IsSubInterface(daoType, _idaoType))
                        {
                            AddBaseServiceType(daoType, ServiceLifetime.Scoped);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在全部核心程序集中查找指定基类型的非抽象实现并加入待注册列表 / Finds non-abstract implementations of the given base type across all essential assemblies and adds them for registration
        /// </summary>
        /// <param name="autoBaseType">服务基类型及其生命周期 / Service base type with its lifetime</param>
        public static void LoadTargetServiceTypes(AutoServiceType autoBaseType)
        {
            Type baseType = autoBaseType.Type;
            bool isIBo = ReflectionTool.IsSubInterface(baseType, _iboType);

            foreach (Assembly assembly in EssentialAssemblies)
            {
                Type[] types = assembly.GetTypes();
                var tempTargetTypes = types.Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract).ToList();
                if (tempTargetTypes.Any())
                {
                    AddTargetServiceTypes(tempTargetTypes);
                }
            }
        }

        /// <summary>
        /// 全局单例应用实例 / Singleton application instance
        /// </summary>
        public static IRampageApplication Application
        {
            get; private set;
        } = default!;
        /// <summary>
        /// 应用根级 IServiceProvider / Application root service provider
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get; private set;
        } = default!;


        /// <summary>
        /// 并行扫描核心程序集：注册配置类型并收集服务基类型/实现类型 / Scans essential assemblies in parallel to register configs and collect service base/target types
        /// </summary>
        /// <param name="environmentName">环境名称 / Environment name</param>
        private static void LoadServices(string environmentName)
        {
            ApartmentState threadApartmentState = Thread.CurrentThread.GetApartmentState();
            string assemblyWorkerName = "LoopAssemblyWorker";
            ThreadingWorker.Register(assemblyWorkerName, (data) =>
            {
                Assembly assembly = (Assembly)data!;
                SuperConfigManager.RegisterConfigs(assembly, environmentName);
                LoadBaseServiceTypes(assembly);
            });
            // STA 主线程下退化为单线程执行，避免 COM 单元跨线程问题 / degrade to a single worker under STA to avoid cross-apartment COM issues
            ThreadingWorker.Work<Assembly>(EssentialAssemblies, assemblyWorkerName, threadApartmentState == ApartmentState.STA ? 1 : 8);

            string serviceWorkerName = "RegisterServiceWorker";
            ThreadingWorker.Register(serviceWorkerName, (data) =>
            {
                AutoServiceType baseType = (AutoServiceType)data!;
                LoadTargetServiceTypes(baseType);
            });
            // 同上：STA 下退化为单线程 / same as above: single worker under STA
            ThreadingWorker.Work<AutoServiceType>(ServiceBaseTypeList, serviceWorkerName, threadApartmentState == ApartmentState.STA ? 1 : 8);

        }

        private static bool IsInitialized = false;

        /// <summary>
        /// 启动 Rampage 应用程序（框架唯一入口），初始化并运行后返回应用实例 / Boots the Rampage application (the only framework entry point) and returns the application instance after initialization and Run
        /// </summary>
        /// <remarks>
        /// 通过 EmitTool.CreateModel 动态构造 Z 的实例；随后按固定顺序初始化且不可调换：
        /// InitializeEnvironment → LoadServices（程序集扫描）→ InitializeLogger → InitializeGlobalConfiguration →
        /// InitializeServiceContainer → InitializeDatabaseClients → InitializeServices → InitializeApplicationProvider → Run。
        /// 其中 InitializeDatabaseClients 依赖第三步加载的数据库配置，必须在其后执行。
        /// The instance of Z is built dynamically by EmitTool.CreateModel; initialization then runs in a fixed order that must not change:
        /// InitializeEnvironment, LoadServices (assembly scanning), InitializeLogger, InitializeGlobalConfiguration,
        /// InitializeServiceContainer, InitializeDatabaseClients, InitializeServices, InitializeApplicationProvider, Run.
        /// InitializeDatabaseClients depends on the database configuration loaded in the third step and must run after it.
        /// </remarks>
        /// <typeparam name="Z">应用类型，须实现 IRampageApplication / Application type implementing IRampageApplication</typeparam>
        /// <param name="args">启动参数，args[0] 为环境名 / Boot arguments where args[0] is the environment name</param>
        /// <returns>初始化完成并已执行 Run 的应用实例 / The initialized application instance after Run has executed</returns>
        /// <exception cref="InvalidOperationException">应用已被初始化时再次调用 / Thrown when the application has already been initialized</exception>
        public static Z Run<Z>(string[] args) where Z : IRampageApplication
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("RampageApplication alread Initialized!");
            }

            Application = EmitTool.CreateModel<Z>()!;

            string environmentName = Application.InitializeEnvironment(args);
            LoadServices(environmentName);

            Application.InitializeLogger(args);
            Application.InitializeGlobalConfiguration(args);
            Application.InitializeServiceContainer(args);         
            Application.InitializeDatabaseClients(args);
            Application.InitializeServices(args);

            ServiceProvider = Application.InitializeApplicationProvider(args);
            // 仅在未设置时写入，避免覆盖业务自定义的定位器 / set only when absent so a custom locator is not overwritten
            if (SingletonServiceLocator.ServiceProvider == null)
            {
                SingletonServiceLocator.SetProvider(ServiceProvider);
            }

            IsInitialized = true;

            Application.Run(args);

            return (Z)Application;
        }

    }
}
