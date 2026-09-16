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
    /// Rampage Application Launcher
    /// </summary>
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
        /// 
        /// </summary>
        public static List<Assembly> EssentialAssemblies
        {
            get;
            private set;
        }

        /// <summary>
        /// All Service base types to injection
        /// </summary>
        internal static List<AutoServiceType> ServiceBaseTypeList
        {
            get;
            private set;
        }

        internal static Dictionary<Type, List<Type>> ServiceTypesDict
        {
            get;
            private set;
        }

        /// <summary>
        /// All Service types for injection
        /// </summary>
        internal static List<Type> ServiceTargetTypeList
        {
            get;
            private set;
        }

        private static object _serviceBaseTypeListLock = new object();
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
        /// Get Base Service types to injection from the Assembly
        /// </summary>
        /// <param name="assembly"></param>
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

                // Auto Injections
                foreach (Auto auto in autoList)
                {
                    if (auto.Property != null)
                    {
                        // DatabaseClient's injection handle in InitializeDatabaseClients
                        if (dcType.IsAssignableFrom(auto.Property.PropertyType))
                        {
                            continue;
                        }
                        AddBaseServiceType(auto.Property.PropertyType, auto.Lifetime);
                    }
                    if (auto.Field != null)
                    {
                        // DatabaseClient's injection handle in InitializeDatabaseClients
                        if (dcType.IsAssignableFrom(auto.Field.FieldType))
                        {
                            continue;
                        }
                        AddBaseServiceType(auto.Field.FieldType, auto.Lifetime);
                    }
                }

                // handle IDao Members in ServiceBase Implementations
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
        /// Get Target Service types for injection
        /// </summary>
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
        /// Singleton Application Instance
        /// </summary>
        public static IRampageApplication Application
        {
            get; private set;
        } = default!;
        /// <summary>
        /// Application ServiceProvider
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get; private set;
        } = default!;


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
            ThreadingWorker.Work<Assembly>(EssentialAssemblies, assemblyWorkerName, threadApartmentState == ApartmentState.STA ? 1 : 8);

            string serviceWorkerName = "RegisterServiceWorker";
            ThreadingWorker.Register(serviceWorkerName, (data) =>
            {
                AutoServiceType baseType = (AutoServiceType)data!;
                LoadTargetServiceTypes(baseType);
            });
            ThreadingWorker.Work<AutoServiceType>(ServiceBaseTypeList, serviceWorkerName, threadApartmentState == ApartmentState.STA ? 1 : 8);

        }

        private static bool IsInitialized = false;

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
