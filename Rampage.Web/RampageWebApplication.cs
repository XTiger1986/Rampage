using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Rampage.DependencyInjection;
using Rampage.Log;
using Rampage.Web.Authentication;
using Rampage.Web.DependencyInjection;
using Rampage.Web.Extensions;
using Rampage.Web.Mvc;
using Rampage.Web.TimeZone;
using System;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace Rampage.Web
{
    /// <summary>
    /// Web 程序启动器：封装 WebApplicationBuilder 创建、服务注册、中间件挂载与启动的完整生命周期 / Web application launcher: encapsulates the full lifecycle of WebApplicationBuilder creation, service registration, middleware setup and startup
    /// </summary>
    /// <remarks>
    /// 中间件固定挂载顺序（见 <see cref="InitializeApplicationProvider"/>）：
    /// ForwardedHeaders → CommonMiddleware → 静态文件 → Routing → CORS（仅当容器有 ICorsService 且 Cors:Enabled=true）→
    /// Authentication → Authorization → /healthz 与 /api/healthz → MapControllers → WebLogger 初始化 →（可选）默认路由 → ApplicationInitialized 回调。
    /// CorrelationIdMiddleware 不在此自动挂载，需要链路 ID 时须显式调用 UseRampageCorrelationId()。
    /// Fixed middleware order (see <see cref="InitializeApplicationProvider"/>):
    /// ForwardedHeaders → CommonMiddleware → static files → Routing → CORS (only when ICorsService is registered and Cors:Enabled=true) →
    /// Authentication → Authorization → /healthz and /api/healthz → MapControllers → WebLogger init → (optional) default route → ApplicationInitialized callback.
    /// CorrelationIdMiddleware is NOT wired up here; call UseRampageCorrelationId() explicitly when a correlation id is needed.
    /// </remarks>
    public class RampageWebApplication : SuperRampageApplication, IRampageApplication
    {

		/// <summary>
		/// 构造函数：把各生命周期回调初始化为空委托，宿主无须判空即可调用 / Constructor: initializes every lifecycle callback to an empty delegate so hosts can invoke them without null checks
		/// </summary>
		public RampageWebApplication()
		{
            ApplicationBuilderCreated = (_, _) => { };
            MvcBuilderCreated = (_, _, _) => { };
            ApplicationBuilderInitialized = (_, _) => { };
            ApplicationBuilt = (_, _) => { };
            ApplicationInitialized = (_, _) => { };
        }

        /// <summary>
        /// 未指定 scope 名且存在 HTTP 上下文时返回当前请求的 RequestServices，否则回退基类实现 / Returns the current request's RequestServices when no scope name is given and an HTTP context exists; otherwise falls back to the base implementation
        /// </summary>
        /// <param name="scopeName">作用域名 / The scope name</param>
        /// <returns>请求级或全局服务提供者 / The request-scoped or global service provider</returns>
        public override IServiceProvider? GetScopeProvider(string? scopeName = null)
        {
            if (string.IsNullOrEmpty(scopeName) && RequestContext.ContextAccessor != null)
            {
                HttpContext? httpContext = RequestContext.ContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    return httpContext.RequestServices;
                }
            }
            return base.GetScopeProvider(scopeName);
        }

        /// <summary>
        /// WebApplicationBuilder 创建后、任何框架服务注册之前触发的回调 / Callback invoked right after the WebApplicationBuilder is created, before any framework service registration
        /// </summary>
        public Action<WebApplicationBuilder, IServiceCollection> ApplicationBuilderCreated
        {
            get;
            protected set;
        }

        /// <summary>
        /// AddControllersWithViews（开发环境含 RazorRuntimeCompilation）之后、AddJsonOptions 之前触发的回调 / Callback invoked after AddControllersWithViews (with RazorRuntimeCompilation in Development) and before AddJsonOptions
        /// </summary>
        public Action<WebApplicationBuilder, IServiceCollection, IMvcBuilder> MvcBuilderCreated
        {
            get;
            protected set;
        }

        /// <summary>
        /// 服务容器初始化完成（含 JWT 认证与全局过滤器注册）之后触发的回调 / Callback invoked after the service container is fully initialized (including JWT authentication and global filter registration)
        /// </summary>
        public Action<WebApplicationBuilder, IServiceCollection> ApplicationBuilderInitialized
        {
            get;
            protected set;
        }

        /// <summary>
        /// 自定义 JSON 序列化配置的回调，在框架自身的 JsonOptions 设置之后执行 / Callback for custom JSON serialization settings, executed after the framework's own JsonOptions setup
        /// </summary>
        public Action<JsonOptions>? JsonConfigureHandler
        {
            get;
            protected set;
        }

        /// <summary>
        /// Application.Build() 完成后、中间件挂载之前触发的回调 / Callback invoked after Application.Build() and before any middleware is wired up
        /// </summary>
        public Action<WebApplication, IServiceProvider> ApplicationBuilt
        {
            get;
            protected set;
        }

        /// <summary>
        /// 全部初始化（中间件、路由、默认路由）完成后触发的回调，宿主可在此追加终结点 / Callback invoked after all initialization (middleware, routes, default route); hosts can append extra endpoints here
        /// </summary>
        public Action<WebApplication, IServiceProvider> ApplicationInitialized
        {
            get;
            protected set;
        }

        /// <summary>
        /// 底层 WebApplicationBuilder（InitializeServiceContainer 之后可用）/ The underlying WebApplicationBuilder (available after InitializeServiceContainer)
        /// </summary>
        public WebApplicationBuilder ApplicationBuilder
		{
			get;
			private set;
		} = default!;

		/// <summary>
		/// 底层 WebApplication（InitializeApplicationProvider 中 Build 之后可用）/ The underlying WebApplication (available after Build inside InitializeApplicationProvider)
		/// </summary>
		public WebApplication Application
		{
			get;
			private set;
		} = default!;

        private bool _enableDefaultRoute = true;
        /// <summary>
        /// 是否映射默认路由 "{controller=Home}/{action=Index}"，默认 true / Whether to map the default route "{controller=Home}/{action=Index}"; true by default
        /// </summary>
        public bool EnableDefaultRoute
        {
            get { return _enableDefaultRoute; }
            set { _enableDefaultRoute = value; }
        }

        /// <summary>
        /// <see cref="EnableAuthentication"/> 的后备字段 / Backing field of <see cref="EnableAuthentication"/>
        /// </summary>
        protected bool _enableAuthentication = true;
        /// <summary>
        /// 是否注册 JWT Bearer 认证服务（默认方案 + 遍历 JwtAuthenticationConfig 的每个 "{name}.Bearer" 方案），默认 true / Whether to register JWT bearer authentication (the default scheme plus one "{name}.Bearer" scheme per JwtAuthenticationConfig section); true by default
        /// </summary>
        public bool EnableAuthentication
        {
            get { return _enableAuthentication; }
            set { _enableAuthentication = value; }
        }

        /// <summary>
        /// <see cref="EnableAllUnicodeRanges"/> 的后备字段 / Backing field of <see cref="EnableAllUnicodeRanges"/>
        /// </summary>
        protected bool _enableAllUnicodeRanges = true;
        /// <summary>
        /// 是否注册全 Unicode 范围的 HtmlEncoder（AddSingleton(HtmlEncoder.Create(UnicodeRanges.All))），默认 true；
        /// false 时使用 ASP.NET Core 默认编码器，非 ASCII 字符（如中文）会被输出为数字字符实体。
        /// Whether to register an all-Unicode HtmlEncoder (AddSingleton(HtmlEncoder.Create(UnicodeRanges.All))); true by default.
        /// When false the default ASP.NET Core encoder is used and non-ASCII characters (e.g. Chinese) are emitted as numeric entities.
        /// </summary>
        public bool EnableAllUnicodeRanges
        {
            get { return _enableAllUnicodeRanges; }
            set { _enableAllUnicodeRanges = value; }
        }

        /// <summary>
        /// <see cref="EnableAutoTimeZone"/> 的后备字段 / Backing field of <see cref="EnableAutoTimeZone"/>
        /// </summary>
        protected bool _enableAutoTimeZone = false;
        /// <summary>
        /// 是否启用 JSON 序列化的自动时区转换，默认 false；true 时挂载 JsonTypeInfoModifiers，按客户端时区转换 DateTime 输出。
        /// Whether to enable automatic time zone conversion in JSON serialization; false by default. When true a JsonTypeInfoModifiers resolver is attached that converts DateTime values to the client time zone.
        /// </summary>
        public bool EnableAutoTimeZone
        {
            get { return _enableAutoTimeZone; }
            set { _enableAutoTimeZone = value; }
        }

        /// <summary>
        /// 注册全局 Action 过滤器：先注册 ValidationActionFilter，再扫描所有关键程序集中实现 IAutoActionFilter 的非抽象类型逐个注册 / Registers global action filters: ValidationActionFilter first, then every non-abstract IAutoActionFilter implementation found in the essential assemblies
        /// </summary>
        private void InitializeActionFilters()
        {
            IEnumerable<Assembly> assemblies = RampageBooter.EssentialAssemblies;

            List<Type> list = new List<Type>();
            Type baseType = typeof(IAutoActionFilter);

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (list.Contains(type)) { continue; }

                    if (baseType.IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        list.Add(type);
                        continue;
                    }
                }
            }


            ApplicationBuilder.Services.AddControllers(opt =>
            {
                opt.Filters.Add<ValidationActionFilter>();
                if (list.Count > 0)
                {
                    foreach (Type type in list)
                    {
                        opt.Filters.Add(type);
                    }
                }
            });
        }

        /// <summary>
        /// 注册 JWT 多方案认证：默认 Bearer 方案之外，遍历配置节 JwtAuthenticationConfig 的每个子节注册 "{name}.Bearer"。
        /// ClockSkew 固定为 Zero；OnTokenValidated 中校验 token_use=access 并执行撤销钩子；
        /// 配置缺失或 SecretKey 为空时抛出 SuperException 阻止启动。
        /// Registers multi-scheme JWT authentication: besides the default bearer scheme, one "{name}.Bearer" scheme per child
        /// section of JwtAuthenticationConfig. ClockSkew is fixed to Zero; OnTokenValidated enforces token_use=access and the
        /// revocation hook; a missing section or empty SecretKey throws SuperException to block startup.
        /// </summary>
		protected virtual void InitializeAuthenticationServices()
		{
            if (EnableAuthentication)
            {
                AuthenticationBuilder authenticationBuilder = ApplicationBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
                List<JwtOptions> jwtOptionsList = JwtOptions.GetAuthenticationConfigList();

                foreach (var jwtOptions in jwtOptionsList)
                {
                    authenticationBuilder.AddJwtBearer($"{jwtOptions.Name!}.Bearer", o =>
                    {
                        string jwtSecret;
                        string originalJwtSecret = jwtOptions.SecretKey!;
                        if (JwtTokenManager.JwtSecretKeyDecodeHandler != null)
                        {
                            jwtSecret = JwtTokenManager.JwtSecretKeyDecodeHandler(originalJwtSecret);
                        }
                        else
                        {
                            jwtSecret = originalJwtSecret;
                        }
                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true, // Whether to verify the signature, unverified drawings can tamper with data and are not secure
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),// Decryption key
                            ValidateIssuer = true, // Whether to verify the issuer is to verify whether the Iss in the payload corresponds to the ValidIssuer parameter
                            ValidIssuer = jwtOptions.Issuer, // issuer,
                            ValidateAudience = true, // Whether to verify the subscriber is to verify whether the Aud in the payload corresponds to the ValidAudience parameter
                            ValidAudience = jwtOptions.Audience, // Subscriber
                            ClockSkew = TimeSpan.Zero // 与 JwtTokenManager.ValidateToken 的过期判定保持一致
                        };
                        string configName = jwtOptions.Name!;
                        o.Events = new JwtBearerEvents
                        {
                            // 刷新令牌不能当访问令牌用；宿主注册了 TokenRevocationHandler 时同步做撤销校验
                            OnTokenValidated = ctx =>
                            {
                                System.IdentityModel.Tokens.Jwt.JwtSecurityToken? jwt = ctx.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                                if (ctx.Principal == null || !JwtTokenManager.IsAcceptableAccessToken(configName, ctx.Principal, jwt))
                                {
                                    ctx.Fail("Token is not an acceptable access token (wrong token_use or revoked).");
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }
        }

        /// <summary>
        /// 初始化服务容器：创建 WebApplicationBuilder → ApplicationBuilderCreated 回调 → HttpContextAccessor →
        /// （可选）全 Unicode HtmlEncoder → WebServiceLocator → AddControllersWithViews（Dev 加 RazorRuntimeCompilation）→
        /// MvcBuilderCreated 回调 → AddJsonOptions（属性名保序；EnableAutoTimeZone 时挂时区 resolver）→
        /// JWT 认证 → 全局过滤器 → ApplicationBuilderInitialized 回调。
        /// Initializes the service container: creates the WebApplicationBuilder → ApplicationBuilderCreated callback → HttpContextAccessor →
        /// (optional) all-Unicode HtmlEncoder → WebServiceLocator → AddControllersWithViews (RazorRuntimeCompilation in Dev) →
        /// MvcBuilderCreated callback → AddJsonOptions (property-name order preserved; a time zone resolver when EnableAutoTimeZone) →
        /// JWT authentication → global filters → ApplicationBuilderInitialized callback.
        /// </summary>
        /// <param name="args">命令行参数 / Command line arguments</param>
        /// <returns>服务集合 / The service collection</returns>
        public override IServiceCollection InitializeServiceContainer(string[] args)
        {
            ApplicationBuilder = WebApplication.CreateBuilder(args);
            ServiceContainer = ApplicationBuilder.Services;
            if (ApplicationBuilderCreated != null)
            {
                ApplicationBuilderCreated(ApplicationBuilder, ServiceContainer);
            }

            ServiceContainer.AddHttpContextAccessor();
            if (EnableAllUnicodeRanges)
            {
                ServiceContainer.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All));
            }
            ServiceContainer.AddScoped<IScopedServiceLocator, WebServiceLocator>();
            IMvcBuilder mvcBuilder = ServiceContainer.AddControllersWithViews();
            if (ApplicationBuilder.Environment.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }
            if (MvcBuilderCreated != null)
            {
                MvcBuilderCreated(ApplicationBuilder, ServiceContainer, mvcBuilder);
            }
            mvcBuilder.AddJsonOptions(options =>
            {
                // Preserve the original case of class fields
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                if (EnableAutoTimeZone)
                {
                    options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers = { JsonTypeInfoModifiers.ModifyJsonTypeInfo }
                    };
                }
                if (JsonConfigureHandler != null)
                {
                    JsonConfigureHandler(options);
                }
            });

            InitializeAuthenticationServices();
            InitializeActionFilters();

            if (ApplicationBuilderInitialized != null)
            {
                ApplicationBuilderInitialized(ApplicationBuilder, ServiceContainer);
            }

            return ServiceContainer;
        }

        /// <summary>
        /// 构建 WebApplication 并按固定顺序挂载中间件与路由（详见类备注），最后执行 WebLogger 初始化与 ApplicationInitialized 回调 / Builds the WebApplication and wires up middleware and routes in the fixed order described in the class remarks, then initializes the web logger and invokes the ApplicationInitialized callback
        /// </summary>
        /// <param name="args">命令行参数 / Command line arguments</param>
        /// <returns>根服务提供者 / The root service provider</returns>
        public override IServiceProvider InitializeApplicationProvider(string[] args)
        {
            Application = ApplicationBuilder.Build();
            ServiceProvider = Application.Services;
            SingletonServiceLocator.SetProvider(ServiceProvider);

            if (ApplicationBuilt != null)
            {
                ApplicationBuilt(Application, ServiceProvider);
            }

            // 转发头必须在其它中间件之前处理，否则 RemoteIpAddress / Scheme 仍是代理层的值
            Rampage.Web.Http.ForwardedHeadersConfig.Apply(Application);

            Application.UseMiddleware<CommonMiddleware>();

            Application.UseStaticFiles();
            Application.UseRouting();
            // 仅当宿主注册了 CORS 且配置 Cors:Enabled=true 时挂载；同域后台可关闭
            if (Application.Services.GetService<ICorsService>() != null
                && Rampage.Web.Cors.CorsOptions.Load().Enabled)
            {
                Application.UseCors();
            }
            Application.UseAuthentication();
            Application.UseAuthorization();
            Application.MapGet("/healthz", () => Results.Text("ok"));
            Application.MapGet("/api/healthz", () => Results.Text("ok"));
            Application.MapControllers();

            WebLoggerInitializer.Initialize();

            if (EnableDefaultRoute)
            {
                Application.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}"
                );
            }
            if (ApplicationInitialized != null)
            {
                ApplicationInitialized(Application, ServiceProvider);
            }

            return ServiceProvider;
        }

        /// <summary>
        /// 运行 Web 应用（阻塞直至应用停止）/ Runs the web application (blocks until it shuts down)
        /// </summary>
        /// <param name="args">命令行参数 / Command line arguments</param>
        public override void Run(string[] args)
        {
            Application.Run();
        }

    }
}
