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
    /// JWT 令牌管理器：签发（访问+刷新成对）、刷新、校验（有效期/用途/签名/撤销）与状态读取 / JWT token manager: issue (access + refresh as a pair), refresh, validate (lifetime/use/signature/revocation) and inspect tokens
    /// </summary>
    public class JwtTokenManager
    {

        /// <summary>
        /// 密钥解码钩子：必须在首次签发/校验 Token 之前设置（配置与校验参数按名称缓存，配置 reload 后自动失效） / Secret decode hook: must be set before the first token is issued or validated (options and validation parameters are cached by name and the caches are cleared on config reload)
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
        /// Claim name that distinguishes access tokens from refresh tokens. Legacy tokens without this claim are treated as access tokens;
        /// refresh tokens must explicitly carry "refresh", so legacy refresh tokens cannot be used to refresh during the transition (users must sign in once more).
        /// </summary>
        public const string TokenUseClaimType = "token_use";

        /// <summary>
        /// 访问令牌的 token_use 取值 / The token_use value for access tokens
        /// </summary>
        public const string TokenUseAccess = "access";

        /// <summary>
        /// 刷新令牌的 token_use 取值 / The token_use value for refresh tokens
        /// </summary>
        public const string TokenUseRefresh = "refresh";

        /// <summary>
        /// 令牌撤销钩子：签名/有效期校验通过后调用，返回 true 表示令牌已被撤销（如密码已修改、用户已登出、安全戳不匹配）。
        /// 参数：认证配置名、令牌载荷、令牌用途（access/refresh）。
        /// Revocation hook invoked after signature/lifetime validation; return true to reject the token.
        /// Parameters: authentication config name, token payload, token use (access/refresh).
        /// </summary>
        public static Func<string, JwtAuthenticationInfo, string, bool>? TokenRevocationHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 读取令牌用途 Claim；缺失时视为访问令牌（兼容旧令牌）。 / Reads the token_use claim; a missing claim is treated as an access token (legacy compatibility).
        /// </summary>
        /// <param name="token">已解析的 JWT / The parsed JWT</param>
        /// <returns>令牌用途（TokenUseAccess 或 TokenUseRefresh）/ The token use (TokenUseAccess or TokenUseRefresh)</returns>
        public static string GetTokenUse(JwtSecurityToken token)
        {
            Claim? claim = token.Claims.FirstOrDefault(c => c.Type == TokenUseClaimType);
            return string.IsNullOrEmpty(claim?.Value) ? TokenUseAccess : claim!.Value;
        }

        /// <summary>
        /// 供 JwtBearer 中间件使用：校验已通过签名验证的 ClaimsPrincipal 是访问令牌且未被撤销。 / Used by the JwtBearer middleware: checks that a signature-validated ClaimsPrincipal is an access token and has not been revoked.
        /// </summary>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="principal">JwtBearer 已验证的主体 / The principal validated by JwtBearer</param>
        /// <param name="token">原始令牌（.NET 8 的 JwtBearer 产出 JsonWebToken，可能为 null）/ The raw token (may be null: .NET 8 JwtBearer yields JsonWebToken instead of JwtSecurityToken)</param>
        /// <returns>可接受（是访问令牌且未撤销）返回 true / True when acceptable (an access token and not revoked)</returns>
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
        /// 从 ClaimsPrincipal 还原 JwtAuthenticationInfo（兼容 JwtBearer 的入站 Claim 映射，如 nameid → ClaimTypes.NameIdentifier）。 / Restores a JwtAuthenticationInfo from a ClaimsPrincipal (tolerates JwtBearer inbound claim mapping such as nameid → ClaimTypes.NameIdentifier).
        /// </summary>
        /// <param name="principal">已认证主体 / The authenticated principal</param>
        /// <returns>还原的用户信息 / The restored user info</returns>
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
        /// 校验访问令牌（要求 token_use=access） / Validates an access token (token_use=access required)
        /// </summary>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="token">令牌字符串 / The token string</param>
        /// <returns>校验结果（含状态与还原的用户信息）/ The validation state (status and restored user info)</returns>
        public static ValidateTokenState ValidateToken(string name, string token)
        {
            return ValidateToken(name, token, TokenUseAccess);
        }

        /// <summary>
        /// 校验令牌签名、有效期、用途（access/refresh）以及撤销状态。 / Validates the token signature, lifetime, use (access/refresh) and revocation status.
        /// </summary>
        /// <remarks>
        /// 校验顺序：先读取载荷判断有效期（不验签名），再校验令牌用途，然后校验签名（校验参数按配置名缓存），
        /// 最后调用撤销钩子（如已注册）。任何一步失败即返回对应状态。
        /// Order: lifetime is judged from the payload first (no signature check), then the token use,
        /// then the signature (validation parameters are cached per config name), and finally the revocation hook if registered. Any failure returns the corresponding status.
        /// </remarks>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="token">令牌字符串 / The token string</param>
        /// <param name="expectedTokenUse">期望的令牌用途：TokenUseAccess 或 TokenUseRefresh；null 表示不校验用途 / Expected token use: TokenUseAccess or TokenUseRefresh; null skips the use check</param>
        /// <returns>校验结果（含状态与还原的用户信息）/ The validation state (status and restored user info)</returns>
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
        /// 签发令牌：一次产出访问令牌与刷新令牌（以 token_use 与各自独立的 jti 区分），并把用户信息写入两者的载荷。 / Issues a token pair: an access token and a refresh token (told apart by token_use and independent jti claims), both carrying the user info in their payloads.
        /// </summary>
        /// <remarks>
        /// exp/nbf 基于 DateTime.Now（服务器本地时间）计算，多机部署需保证各服务器时区一致。
        /// exp/nbf are computed from DateTime.Now (server local time); servers in a cluster must share the same time zone.
        /// </remarks>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="info">要写入载荷的用户信息（空字段不写入对应 Claim）/ The user info to write into the payload (empty fields add no claim)</param>
        /// <returns>令牌对：Token=访问令牌，RefreshToken=刷新令牌 / The token pair: Token = access token, RefreshToken = refresh token</returns>
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
        /// 用刷新令牌换取新的令牌对 / Exchanges a refresh token for a new token pair
        /// </summary>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="refreshToken">刷新令牌 / The refresh token</param>
        /// <returns>新令牌对；校验失败返回 null / The new token pair, or null when validation fails</returns>
        public static AuthenticationToken? RefreshToken(string name, string refreshToken)
        {
            ValidateTokenState? state = null;
            return RefreshToken(name, refreshToken, out state);
        }

        /// <summary>
        /// 用刷新令牌换取新的令牌对，并通过输出参数返回校验结果详情 / Exchanges a refresh token for a new token pair, returning the detailed validation state via an out parameter
        /// </summary>
        /// <param name="name">认证配置名 / Authentication config name</param>
        /// <param name="refreshToken">刷新令牌（必须是 token_use=refresh 的令牌；访问令牌与缺少该 Claim 的旧令牌均不能用于刷新）/ The refresh token (must carry token_use=refresh; access tokens and legacy tokens without the claim are rejected)</param>
        /// <param name="state">校验结果（失败时包含原因状态，成功时为 Valid）/ The validation state (carries the failure reason; Valid on success)</param>
        /// <returns>新令牌对；校验失败返回 null / The new token pair, or null when validation fails</returns>
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
        /// 解析令牌字符串并读取其状态（不校验签名，仅按本地时间判断有效期） / Parses the token string and reads its state (no signature check; lifetime judged against local time)
        /// </summary>
        /// <param name="name">认证配置名（当前实现未使用，保留以保持重载签名一致）/ Authentication config name (currently unused; kept for overload consistency)</param>
        /// <param name="token">令牌字符串 / The token string</param>
        /// <returns>状态对象；非 JWT 格式返回 Invalid / The state; Invalid for a non-JWT string</returns>
        public static ValidateTokenState GetTokenState(string name, string token)
        {
            JwtSecurityToken? jwtToken = null;
            try
            {
                jwtToken = TokenHandler.ReadToken(token) as JwtSecurityToken;
            }
            catch (ArgumentException)
            {
                // 非 JWT 格式的字符串 / not a JWT-formatted string
            }
            return GetTokenState(name, jwtToken);
        }

        /// <summary>
        /// 从已解析的 JWT 中提取状态与载荷（不校验签名）。 / Extracts the state and payload from a parsed JWT (no signature check).
        /// </summary>
        /// <param name="name">认证配置名（不影响结果）/ Authentication config name (does not affect the result)</param>
        /// <param name="jwtToken">已解析的 JWT；null 直接返回 Invalid / The parsed JWT; null returns Invalid</param>
        /// <returns>状态对象（含有效期与还原的用户信息）/ The state (with lifetime and restored user info)</returns>
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
