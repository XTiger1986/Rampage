using Microsoft.Extensions.Configuration;
using Rampage.ExceptionExtensions;
using System;

namespace Rampage.Web.Authentication
{
    /// <summary>
    /// 
    /// </summary>
    public class JwtOptions
    {


        /// <summary>
        /// 
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
		/// 密钥
		/// </summary>
		public string? SecretKey
        {
            get;
            set;
        }

        /// <summary>
        /// 发布者
        /// </summary>
        public string? Issuer
        {
            get;
            set;
        }

        /// <summary>
        /// 接收者
        /// </summary>
        public string? Audience
        {
            get;
            set;
        }

        /// <summary>
        /// 过期秒数
        /// </summary>
        public double? ExpiredSeconds
		{
            get;
            set;
		}

        /// <summary>
        /// 过期秒数
        /// </summary>
        public double? RefreshExpiredSeconds
        {
            get;
            set;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public static JwtOptions GetAuthenticationConfig(string name)
        {
            var jwtSection = GlobalConfiguration.GetSection("JwtAuthenticationConfig");
            var section = jwtSection?.GetSection(name);
            if (section == null)
            {
                throw new SuperException($"JWT身份验证配置 JwtAuthenticationConfig:{name} 缺失，为避免使用可预测默认密钥已阻止启动");
            }
            JwtOptions? options = section.Get<JwtOptions>();
            if (options == null || string.IsNullOrWhiteSpace(options.SecretKey))
            {
                throw new SuperException($"JWT身份验证配置 JwtAuthenticationConfig:{name} 缺失或 SecretKey 为空，为避免使用可预测默认密钥已阻止启动");
            }
            options.Name = name;
            return options;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<JwtOptions> GetAuthenticationConfigList()
		{
            List<JwtOptions> list = new List<JwtOptions>();

            var section = GlobalConfiguration.GetSection("JwtAuthenticationConfig");
            if (section != null)
            {
                var configSections = section.GetChildren();
                foreach(var configSection in configSections)
				{
                    string name = configSection.Key;
                    JwtOptions? options = configSection.Get<JwtOptions>();
                    if (options == null || string.IsNullOrWhiteSpace(options.SecretKey))
					{
                        throw new SuperException($"JWT身份验证配置 JwtAuthenticationConfig:{name} 缺失或 SecretKey 为空，为避免使用可预测默认密钥已阻止启动");
					}
                    options.Name = name;
                    list.Add(options);
                }
            }

            return list;
        }


    }
}
