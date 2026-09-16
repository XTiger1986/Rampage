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
    /// Web程序启动器
    /// </summary>
    public class RampageWebApplication : SuperRampageApplication, IRampageApplication
    {

		/// <summary>
		/// 
		/// </summary>
		public RampageWebApplication()
		{
            ApplicationBuilderCreated = (_, _) => { };
            MvcBuilderCreated = (_, _, _) => { };
            ApplicationBuilderInitialized = (_, _) => { };
            ApplicationBuilt = (_, _) => { };
            ApplicationInitialized = (_, _) => { };
        }

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

        public Action<WebApplicationBuilder, IServiceCollection> ApplicationBuilderCreated
        {
            get;
            protected set;
        }

        public Action<WebApplicationBuilder, IServiceCollection, IMvcBuilder> MvcBuilderCreated
        {
            get;
            protected set;
        }

        public Action<WebApplicationBuilder, IServiceCollection> ApplicationBuilderInitialized
        {
            get;
            protected set;
        }

        public Action<JsonOptions>? JsonConfigureHandler
        {
            get;
            protected set;
        }

        public Action<WebApplication, IServiceProvider> ApplicationBuilt
        {
            get;
            protected set;
        }

        public Action<WebApplication, IServiceProvider> ApplicationInitialized
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        public WebApplicationBuilder ApplicationBuilder
		{
			get;
			private set;
		} = default!;

		/// <summary>
		/// 
		/// </summary>
		public WebApplication Application
		{
			get;
			private set;
		} = default!;

        private bool _enableDefaultRoute = true;
        /// <summary>
        /// 
        /// </summary>
        public bool EnableDefaultRoute
        {
            get { return _enableDefaultRoute; }
            set { _enableDefaultRoute = value; }
        }

        protected bool _enableAuthentication = true;
        /// <summary>
        /// 
        /// </summary>
        public bool EnableAuthentication
        {
            get { return _enableAuthentication; }
            set { _enableAuthentication = value; }
        }

        protected bool _enableAllUnicodeRanges = true;
        /// <summary>
        /// AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All))
        /// </summary>
        public bool EnableAllUnicodeRanges
        {
            get { return _enableAllUnicodeRanges; }
            set { _enableAllUnicodeRanges = value; }
        }

        protected bool _enableAutoTimeZone = false;
        /// <summary>
        /// 
        /// </summary>
        public bool EnableAutoTimeZone
        {
            get { return _enableAutoTimeZone; }
            set { _enableAutoTimeZone = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
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

        public override void Run(string[] args)
        {
            Application.Run();
        }

    }
}
