using Microsoft.Extensions.Configuration;
using Rampage.ExceptionExtensions;
using System;

namespace Rampage.Web.Authentication
{
    /// <summary>
    /// JWT 身份验证配置（对应配置节 JwtAuthenticationConfig 下的一个子节）/ JWT authentication options (bound to a child section of the JwtAuthenticationConfig section)
    /// </summary>
    public class JwtOptions
    {


        /// <summary>
        /// 配置名（子节键），用于构造认证方案名 "{name}.Bearer" / Config name (section key), used to build the authentication scheme name "{name}.Bearer"
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
		/// 签名密钥；可经 <see cref="JwtTokenManager.JwtSecretKeyDecodeHandler"/> 解密后使用（如配置中存放密文）/ Signing secret; can be decrypted via <see cref="JwtTokenManager.JwtSecretKeyDecodeHandler"/> before use (e.g. when stored encrypted)
		/// </summary>
		public string? SecretKey
        {
            get;
            set;
        }

        /// <summary>
        /// 发布者（iss）/ Issuer (iss claim)
        /// </summary>
        public string? Issuer
        {
            get;
            set;
        }

        /// <summary>
        /// 接收者（aud）/ Audience (aud claim)
        /// </summary>
        public string? Audience
        {
            get;
            set;
        }

        /// <summary>
        /// 访问令牌过期秒数 / Access token lifetime in seconds
        /// </summary>
        public double? ExpiredSeconds
		{
            get;
            set;
		}

        /// <summary>
        /// 刷新令牌过期秒数（应大于 <see cref="ExpiredSeconds"/>）/ Refresh token lifetime in seconds (should be greater than <see cref="ExpiredSeconds"/>)
        /// </summary>
        public double? RefreshExpiredSeconds
        {
            get;
            set;
        }



        /// <summary>
        /// 读取指定名称的 JWT 身份验证配置 / Loads the JWT authentication options with the given name
        /// </summary>
        /// <param name="name">认证配置名（JwtAuthenticationConfig 的子节键）/ Authentication config name (child section key of JwtAuthenticationConfig)</param>
        /// <returns>解析后的配置（Name 已回填为 name）/ The parsed options (with Name set to <paramref name="name"/>)</returns>
        /// <exception cref="SuperException">配置节缺失或 SecretKey 为空时抛出，以避免使用可预测的默认密钥而阻止启动 / Thrown when the section is missing or SecretKey is empty, to block startup instead of falling back to a predictable default key</exception>
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
        /// 读取全部 JWT 身份验证配置（遍历 JwtAuthenticationConfig 的每个子节）/ Loads all JWT authentication options (one per child section of JwtAuthenticationConfig)
        /// </summary>
        /// <returns>配置列表；配置节不存在时返回空列表 / The options list; an empty list when the section does not exist</returns>
        /// <exception cref="SuperException">任一子节缺失或 SecretKey 为空时抛出，以避免使用可预测的默认密钥而阻止启动 / Thrown when any child section is missing or its SecretKey is empty, to block startup instead of falling back to a predictable default key</exception>
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
