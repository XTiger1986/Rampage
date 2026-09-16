using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Rampage.ExceptionExtensions;
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rampage.Web.Authentication
{
    /// <summary>
    /// 
    /// </summary>
    public class JwtTokenManager
    {

        /// <summary>
        /// 密钥解码钩子：必须在首次签发/校验 Token 之前设置（配置与校验参数按名称缓存，配置 reload 后自动失效）
        /// </summary>
        public static Func<string, string>? JwtSecretKeyDecodeHandler
        {
            get;
            set;
        }

        private static readonly ConcurrentDictionary<string, JwtOptions> JwtOptionsCache = new ConcurrentDictionary<string, JwtOptions>();
        private static readonly ConcurrentDictionary<string, TokenValidationParameters> ValidationParametersCache = new ConcurrentDictionary<string, TokenValidationParameters>();
        private static readonly JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();

        /// <summary>
        /// 区分访问令牌与刷新令牌的私有 Claim 名称。没有该 Claim 的旧令牌按访问令牌处理（兼容过渡）；
        /// 刷新令牌必须显式携带 refresh，旧的刷新令牌在过渡期内无法再用于刷新（用户需重新登录一次）。
        /// </summary>
        public const string TokenUseClaimType = "token_use";
        public const string TokenUseAccess = "access";
        public const string TokenUseRefresh = "refresh";

        /// <summary>
        /// 令牌撤销钩子：签名/有效期校验通过后调用，返回 true 表示令牌已被撤销（如密码已修改、用户已登出、安全戳不匹配）。
        /// 参数：认证配置名、令牌载荷、令牌用途（access/refresh）。
        /// Revocation hook invoked after signature/lifetime validation. Return true to reject the token.
        /// </summary>
        public static Func<string, JwtAuthenticationInfo, string, bool>? TokenRevocationHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 读取令牌用途 Claim；缺失时视为访问令牌（兼容旧令牌）。
        /// </summary>
        public static string GetTokenUse(JwtSecurityToken token)
        {
            Claim? claim = token.Claims.FirstOrDefault(c => c.Type == TokenUseClaimType);
            return string.IsNullOrEmpty(claim?.Value) ? TokenUseAccess : claim!.Value;
        }

        /// <summary>
        /// 供 JwtBearer 中间件使用：校验已通过签名验证的 ClaimsPrincipal 是访问令牌且未被撤销。
        /// </summary>
        public static bool IsAcceptableAccessToken(string name, ClaimsPrincipal principal, JwtSecurityToken? token)
        {
            string tokenUse = principal.FindFirst(TokenUseClaimType)?.Value ?? TokenUseAccess;
            if (!string.Equals(tokenUse, TokenUseAccess, StringComparison.Ordinal))
            {
                return false;
            }
            if (TokenRevocationHandler != null)
            {
                // .NET 8 的 JwtBearer 默认产出 JsonWebToken 而非 JwtSecurityToken，因此统一从 ClaimsPrincipal 还原载荷
                JwtAuthenticationInfo info = token != null ? GetTokenState(name, token).Info : GetAuthenticationInfo(principal);
                if (TokenRevocationHandler(name, info, tokenUse))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 从 ClaimsPrincipal 还原 JwtAuthenticationInfo（兼容 JwtBearer 的入站 Claim 映射，如 nameid → ClaimTypes.NameIdentifier）。
        /// </summary>
        public static JwtAuthenticationInfo GetAuthenticationInfo(ClaimsPrincipal principal)
        {
            static string? First(ClaimsPrincipal p, params string[] types)
            {
                foreach (string t in types)
                {
                    string? v = p.FindFirst(t)?.Value;
                    if (!string.IsNullOrEmpty(v))
                    {
                        return v;
                    }
                }
                return null;
            }

            return new JwtAuthenticationInfo
            {
                NameId = First(principal, JwtRegisteredClaimNames.NameId, ClaimTypes.NameIdentifier),
                Name = First(principal, JwtRegisteredClaimNames.Name, ClaimTypes.Name),
                UniqueName = First(principal, JwtRegisteredClaimNames.UniqueName),
                Nickname = First(principal, JwtRegisteredClaimNames.Nickname),
                Email = First(principal, JwtRegisteredClaimNames.Email, ClaimTypes.Email),
                PhoneNumber = First(principal, JwtRegisteredClaimNames.PhoneNumber, ClaimTypes.MobilePhone),
                Gender = First(principal, JwtRegisteredClaimNames.Gender, ClaimTypes.Gender),
                Nonce = First(principal, JwtRegisteredClaimNames.Nonce),
            };
        }

        static JwtTokenManager()
        {
            GlobalConfiguration.RegisterReloadCallback(_ =>
            {
                JwtOptionsCache.Clear();
                ValidationParametersCache.Clear();
            });
        }

        private static JwtOptions GetCachedAuthenticationConfig(string name)
        {
            return JwtOptionsCache.GetOrAdd(name, JwtOptions.GetAuthenticationConfig);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static ValidateTokenState ValidateToken(string name, string token)
        {
            return ValidateToken(name, token, TokenUseAccess);
        }

        /// <summary>
        /// 校验令牌签名、有效期、用途（access/refresh）以及撤销状态。
        /// </summary>
        /// <param name="name">认证配置名</param>
        /// <param name="token">令牌字符串</param>
        /// <param name="expectedTokenUse">期望的令牌用途：TokenUseAccess 或 TokenUseRefresh；null 表示不校验用途</param>
        public static ValidateTokenState ValidateToken(string name, string token, string? expectedTokenUse)
        {
            ValidateTokenState state = GetTokenState(name, token);
            if (state.Status != ValidateTokenStatus.Valid)
            {
                return state;
            }

            string actualTokenUse = state.RawToken is JwtSecurityToken rawJwt ? GetTokenUse(rawJwt) : TokenUseAccess;
            if (expectedTokenUse != null && !string.Equals(actualTokenUse, expectedTokenUse, StringComparison.Ordinal))
            {
                // 用刷新令牌当访问令牌用（或反之）一律拒绝
                state.Status = ValidateTokenStatus.Invalid;
                return state;
            }

            JwtOptions jwtOptions;
            try
            {
                jwtOptions = GetCachedAuthenticationConfig(name);
            }
            catch (SuperException)
            {
                state.Status = ValidateTokenStatus.Invalid;
                return state;
            }

            TokenValidationParameters validationParameters = ValidationParametersCache.GetOrAdd(name, n =>
            {
                JwtOptions optionsForParams = GetCachedAuthenticationConfig(n);
                string jwtSecret = optionsForParams.SecretKey!;
                if (JwtSecretKeyDecodeHandler != null)
                {
                    jwtSecret = JwtSecretKeyDecodeHandler(jwtSecret);
                }
                return new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = optionsForParams.Issuer,
                    ValidAudience = optionsForParams.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

            SecurityToken? validatedToken = null;
            ClaimsPrincipal claims;
            try
            {
                claims = TokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            }
            catch (SecurityTokenException)
            {
                // 签名不匹配 / 颁发者不符等：不要把库异常抛给调用方，统一按无效处理
                state.Status = ValidateTokenStatus.Invalid;
                return state;
            }

            if (validatedToken == null || !claims.Claims.Any())
            {
                state.Status = ValidateTokenStatus.Invalid;
                return state;
            }

            if (TokenRevocationHandler != null && TokenRevocationHandler(name, state.Info, actualTokenUse))
            {
                state.Status = ValidateTokenStatus.Revoked;
            }

            return state;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static AuthenticationToken BuildToken(string name, JwtAuthenticationInfo info)
        {
            JwtOptions jwtOptions = GetCachedAuthenticationConfig(name);

            //Payload, store user info
            List<Claim> claims = new List<Claim>();
            if (!string.IsNullOrEmpty(info.NameId))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.NameId, info.NameId));
            }
            if (!string.IsNullOrEmpty(info.Name))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Name, info.Name));
            }
            if (!string.IsNullOrEmpty(info.UniqueName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, info.UniqueName));
            }
            if (!string.IsNullOrEmpty(info.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, info.Email));
            }
            if (!string.IsNullOrEmpty(info.PhoneNumber))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.PhoneNumber, info.PhoneNumber));
            }
            if (!string.IsNullOrEmpty(info.Nickname))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Nickname, info.Nickname));
            }
            if (!string.IsNullOrEmpty(info.Gender))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Gender, info.Gender));
            }
            if (!string.IsNullOrEmpty(info.Nonce))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, info.Nonce));
            }

            // 访问令牌与刷新令牌使用不同的 token_use，并各自携带唯一 jti，避免两者可互换使用
            List<Claim> accessClaims = new List<Claim>(claims)
            {
                new Claim(TokenUseClaimType, TokenUseAccess),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };
            List<Claim> refreshClaims = new List<Claim>(claims)
            {
                new Claim(TokenUseClaimType, TokenUseRefresh),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            string jwtSecret;
            string originalJwtSecret = jwtOptions.SecretKey!;
            if (JwtSecretKeyDecodeHandler != null)
            {
                jwtSecret = JwtSecretKeyDecodeHandler(originalJwtSecret);
            }
            else
            {
                jwtSecret = originalJwtSecret;
            }

            var signingAlogorithm = SecurityAlgorithms.HmacSha256;
            /*
             * 取出私钥并以utf8编码字节输出
             * Extract the private key and output it as UTF-8 encoded bytes
             * */
            var secretByte = Encoding.UTF8.GetBytes(jwtSecret);
            /*
             * 使用非对称算法对私钥进行加密
             * Encrypt the private key using an asymmetric algorithm
             * */
            var signingKey = new SymmetricSecurityKey(secretByte);
            /*
             * 使用HmacSha256来验证加密后的私钥生成数字签名
             * Use HmacSha256 to verify the encrypted private key and generate a digital signature
             * */
            var signingCredentials = new SigningCredentials(signingKey, signingAlogorithm);
            DateTime? expireTime = jwtOptions.ExpiredSeconds != null ? DateTime.Now.AddSeconds(jwtOptions.ExpiredSeconds.Value) : null;
            /*
             * 生成Token
             * Generate Token
             * */
            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: accessClaims,
                notBefore: DateTime.Now,
                expires: expireTime,
                signingCredentials
            );

            DateTime? refreshExpireTime = jwtOptions.RefreshExpiredSeconds != null ? DateTime.Now.AddSeconds(jwtOptions.RefreshExpiredSeconds.Value) : null;
            var refreshToken = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: refreshClaims,
                notBefore: DateTime.Now,
                expires: refreshExpireTime,
                signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            /*
             * 生成Token字符串
             * Generate Token String
             * */
            var tokenStr = tokenHandler.WriteToken(token);
            var refreshTokenStr = tokenHandler.WriteToken(refreshToken);

            AuthenticationToken tokenInfo = new AuthenticationToken();
            tokenInfo.Token = tokenStr;
            tokenInfo.RefreshToken = refreshTokenStr;

            return tokenInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public static AuthenticationToken? RefreshToken(string name, string refreshToken)
        {
            ValidateTokenState? state = null;
            return RefreshToken(name, refreshToken, out state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public static AuthenticationToken? RefreshToken(string name, string refreshToken, out ValidateTokenState? state)
        {
            // 只接受 token_use=refresh 的令牌；访问令牌（或缺少该 Claim 的旧令牌）不能用于刷新
            state = ValidateToken(name, refreshToken, TokenUseRefresh);
            if (state.Status != ValidateTokenStatus.Valid)
            {
                return null;
            }

            AuthenticationToken token = BuildToken(name, state.Info);
            return token;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static ValidateTokenState GetTokenState(string name, string token)
        {
            JwtSecurityToken? jwtToken = null;
            try
            {
                jwtToken = TokenHandler.ReadToken(token) as JwtSecurityToken;
            }
            catch (ArgumentException)
            {
                // 非 JWT 格式的字符串
            }
            return GetTokenState(name, jwtToken);
        }

        /// <summary>
        /// 从已解析的 JWT 中提取状态与载荷（不校验签名）。
        /// </summary>
        public static ValidateTokenState GetTokenState(string name, JwtSecurityToken? jwtToken)
        {
            ValidateTokenState state = new ValidateTokenState();

            if (jwtToken != null)
            {
                DateTime localValidFrom = jwtToken.ValidFrom.ToLocalTime();
                DateTime localValidTo = jwtToken.ValidTo.ToLocalTime();
                state.ValidFrom = localValidFrom;
                state.ValidTo = localValidTo;

                if (DateTime.Now < localValidFrom)
                {
                    state.Status = ValidateTokenStatus.Invalid;
                }
                else if (DateTime.Now >= localValidTo)
                {
                    state.Status = ValidateTokenStatus.Expired;
                }
                else
                {
                    state.Status = ValidateTokenStatus.Valid;
                }

                var claims = jwtToken.Claims;
                Claim? nameIdClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.NameId));
                if (nameIdClaim != null)
                {
                    state.Info.NameId = nameIdClaim.Value;
                }
                Claim? nameClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Name));
                if (nameClaim != null)
                {
                    state.Info.Name = nameClaim.Value;
                }
                Claim? nonceClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Nonce));
                if (nonceClaim != null)
                {
                    state.Info.Nonce = nonceClaim.Value;
                }
                Claim? uniqueNameClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.UniqueName));
                if (uniqueNameClaim != null)
                {
                    state.Info.UniqueName = uniqueNameClaim.Value;
                }
                Claim? nicknameClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Nickname));
                if (nicknameClaim != null)
                {
                    state.Info.Nickname = nicknameClaim.Value;
                }
                Claim? emailClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Email));
                if (emailClaim != null)
                {
                    state.Info.Email = emailClaim.Value;
                }
                Claim? phoneNumberClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.PhoneNumber));
                if (phoneNumberClaim != null)
                {
                    state.Info.PhoneNumber = phoneNumberClaim.Value;
                }
                Claim? genderClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Gender));
                if (genderClaim != null)
                {
                    state.Info.Gender = genderClaim.Value;
                }

                state.RawToken = jwtToken;
            }
            else
            {
                state.Status = ValidateTokenStatus.Invalid;
            }

            return state;
        }


    }
}
