using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRampageApplication : IDisposable
    {

        public string UniqueId
        {
            get;
            set;
        }

        /// <summary>
        /// Application Running Environment Name
        /// </summary>
        public string? EnvironmentName
        {
            get;
            set;
        }

        /// <summary>
        /// Get The application's configured service provider.
        /// </summary>
        IServiceProvider GetServiceProvider();

        /// <summary>
        /// Get The application's children service scopes container.
        /// </summary>
        Dictionary<string, IServiceScope> GetServiceScopes();

        /// <summary>
        /// get service scope in root
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        IServiceScope GetScope(string? scopeName = null);

        /// <summary>
        /// get scope service provider
        /// </summary>
        /// <param name="scopeName"></param>
        IServiceProvider? GetScopeProvider(string? scopeName = null);

        /// <summary>
        /// create service scope in root service provider
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        IServiceScope CreateScope(string scopeName);

        /// <summary>
        /// First Step: Initialize Application Environment
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        string InitializeEnvironment(string[] args);

        /// <summary>
        /// Second Step: Initialize Logger
        /// </summary>
        /// <param name="args"></param>
        void InitializeLogger(string[] args);

        /// <summary>
        /// Third Step: Initialize Global Configuration
        /// </summary>
        /// <param name="args"></param>
        void InitializeGlobalConfiguration(string[] args);

        /// <summary>
        /// Fourth Step: Initialize Application Service Container
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        IServiceCollection InitializeServiceContainer(string[] args);

        /// <summary>
        /// Fifth Step: Initialize DatabaseClient Services
        /// </summary>
        /// <param name="args"></param>
        void InitializeDatabaseClients(string[] args);

        /// <summary>
        /// Sixth Step: Initialize Services
        /// </summary>
        /// <param name="args"></param>
        void InitializeServices(string[] args);

        /// <summary>
        /// Seventh Step: Initialize Application ServiceProvider （After Services Initialized）
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        IServiceProvider InitializeApplicationProvider(string[] args);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">0:EnvironmentName;</param>
        void Run(string[] args);

    }
}
