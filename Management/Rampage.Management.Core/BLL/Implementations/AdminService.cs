using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Rampage.AppLog;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Extensions;
using Rampage.Json;
using Rampage.Management.Configuration;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Security;
using Rampage.Utilities;
using Rampage.Web;
using Rampage.Web.Authentication;
using Rampage.Web.Extensions;
using Rampage.Web.Utilities;
using System;
using System.Security.Cryptography;

namespace Rampage.Management.BLL
{

    /// <summary>
    /// 管理员业务逻辑实现：登录认证与令牌下发/刷新、增删改查、密码与头像维护、管理组分配
    /// Administrator business logic: login authentication and token issuing/refreshing, CRUD, password and avatar maintenance, group assignment
    /// </summary>
    public class AdminService : ManagementService<Admin, IAdminDao>, IAdminService
    {

        /// <summary>Web 宿主环境（用于保存头像文件） / The web host environment (used to save avatar files)</summary>
        [Auto]
        protected IWebHostEnvironment WebHostEnvironment = default!;

        /// <summary>管理组业务逻辑（用于填充管理员所属组） / The management group service (used to populate admin groups)</summary>
        [Auto]
        protected IManageGroupService ManageGroupService = default!;

        /// <summary>管理员-管理组关联数据访问 / Admin-group membership data access</summary>
        [Auto]
        protected IAdminInGroupDao AdminInGroupDao = default!;
        /// <summary>管理员登录日志数据访问 / Admin login log data access</summary>
        [Auto]
        protected IAdminLoginLogDao AdminLoginLogDao = default!;
        

        /// <summary>填充单个管理员的关联数据：清空密码、填充所属管理组 / Populate related data for a single admin: clears the password and fills the groups</summary>
        /// <param name="dto">管理员DTO / The admin DTO</param>
        /// <returns>填充后的DTO；入参为 null 时原样返回 / The populated DTO; returned as-is when the input is null</returns>
        public AdminDto? ReadRelatedData(AdminDto? dto)
        {
            if (dto != null)
            {
                dto.Password = null;
                List<AdminInGroup> inGroupList = AdminInGroupDao.SelectList<AdminInGroup>(g => g.AdminId == dto.Id);
                if (inGroupList.Any())
                {
                    dto.GroupIds = inGroupList.Select(x => x.GroupId).ToList();
                    List<ManageGroupDto> groupList = ManageGroupService.GetManageGroupListFromCache(CommonStatus.Enabled);
                    dto.GroupList = groupList.FindAll(g => inGroupList.Any(ig => ig.GroupId == g.Id));
                }
            }
            return dto;
        }

        /// <summary>批量填充管理员列表的关联数据：清空密码、填充所属管理组 / Populate related data for a list of admins: clears passwords and fills the groups</summary>
        /// <param name="adminList">管理员列表 / The admin list</param>
        /// <returns>填充后的列表 / The populated list</returns>
        public List<AdminDto> ReadRelatedData(List<AdminDto> adminList)
        {
            if (adminList.Count > 0)
            {
                List<int> adminIdList = adminList.Select(x => x.Id).ToList();
                List<ManageGroupDto> groupList = ManageGroupService.GetManageGroupListFromCache(CommonStatus.Enabled);
                List<AdminInGroup> inGroupList = AdminInGroupDao.SelectList<AdminInGroup>(g => adminIdList.Contains(g.AdminId));
                foreach (AdminDto admin in adminList)
                {
                    admin.Password = null;
                    List<AdminInGroup> adminInGroupList = inGroupList.FindAll(d => d.AdminId == admin.Id);
                    admin.GroupIds = adminInGroupList.Select(x => x.GroupId).ToList();
                    if (admin.GroupIds != null)
                    {
                        admin.GroupList = groupList.FindAll(g => admin.GroupIds.Contains(g.Id));
                    }
                }
            }
            return adminList;
        }

        /// <summary>将认证令牌下发到客户端：AES 加密后写入 Cookie（会话或持久）并同步写入 Authorization 响应头 / Send the authentication token to the client: AES-encrypted into a cookie (session or persistent) and also written into the Authorization response header</summary>
        /// <param name="token">认证令牌 / The authentication token</param>
        /// <param name="cacheDays">缓存（记住）天数；null 为会话 Cookie / Days to cache (remember) the token; null for a session cookie</param>
        public void SetTokenToClientAgent(AuthenticationToken token, double? cacheDays = null)
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
            // Cookie 键取 SHA1 摘要，避免向客户端暴露内部配置名 / Hash the cookie key with SHA1 to avoid leaking internal config names to the client
            string safeCookieKey = SecurityTool.SHA1(cookieKey);
            DateTime? expireTime = null;
            if (cacheDays != null)
            {
                expireTime = UtcNow.AddDays(cacheDays.Value);
            }
            string json = JsonTool.Stringify(token);
            string encryptJson = SecurityTool.AESEncodeCBC(json);
            if (expireTime != null)
                WebTool.SetCookie(safeCookieKey, encryptJson, expireTime);
            else
                WebTool.SetCookie(safeCookieKey, encryptJson);

            if (!HttpContext.Response.Headers.ContainsKey(WebGlobals.AuthorizationHeaderKey))
            {
                HttpContext.Response.Headers.Append(WebGlobals.AuthorizationHeaderKey, new Microsoft.Extensions.Primitives.StringValues(encryptJson));
            }
            else
            {
                HttpContext.Response.Headers[WebGlobals.AuthorizationHeaderKey] = encryptJson;
            }
        }

        /// <summary>从客户端读取认证令牌，依次尝试 Cookie、查询串、Authorization 请求头、表单 / Read the authentication token from the client, trying the cookie, query string, Authorization request header, then the form in order</summary>
        /// <returns>解密并反序列化后的令牌；不存在或无效时为 null，无效 Cookie 会被清除 / The decrypted and deserialized token; null if missing or invalid, with invalid cookies removed</returns>
        public AuthenticationToken? GetClientAgentToken()
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
            string safeCookieKey = SecurityTool.SHA1(cookieKey);
            string? encryptTokenStr = WebTool.GetCookie(safeCookieKey);
            bool fromCookie = !string.IsNullOrEmpty(encryptTokenStr);
            if (string.IsNullOrEmpty(encryptTokenStr))
            {
                if (HttpContext.Request.Query.ContainsKey(cookieKey))
                {
                    encryptTokenStr = HttpContext.Request.Query[cookieKey];
                }
                else if (HttpContext.Request.Headers.ContainsKey(WebGlobals.AuthorizationHeaderKey))
                {
                    encryptTokenStr = HttpContext.Request.Headers[WebGlobals.AuthorizationHeaderKey];
                }
                else
                {
                    encryptTokenStr = HttpContext.Request.GetForm(cookieKey);
                }
            }
            if (string.IsNullOrEmpty(encryptTokenStr))
            {
                return null;
            }
            try
            {
                // 客户端输入须包含 16 字节 IV 和完整密文块，避免短密文触发工具层业务异常。
                // Client input must contain a 16-byte IV and complete ciphertext blocks.
                byte[] data = Convert.FromBase64String(encryptTokenStr);
                if (data.Length >= 32 && data.Length % 16 == 0)
                {
                    string tokenStr = SecurityTool.AESDecodeCBC(encryptTokenStr);
                    AuthenticationToken? token = JsonTool.Parse<AuthenticationToken>(tokenStr);
                    if (token != null && !string.IsNullOrWhiteSpace(token.Token))
                    {
                        return token;
                    }
                }
            }
            catch (FormatException)
            {
                // 非 Base64 输入按无效令牌处理 / Treat non-Base64 input as an invalid token.
            }
            catch (CryptographicException)
            {
                // 旧密钥签发或损坏的密文按未登录处理 / Treat stale-key or damaged ciphertext as unauthenticated.
            }

            if (fromCookie)
            {
                ClearClientAgentToken();
            }

            return null;
        }

        /// <summary>清除客户端令牌（置空并移除 Cookie） / Clear the client token (empties and removes the cookie)</summary>
        public void ClearClientAgentToken()
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
            string safeCookieKey = SecurityTool.SHA1(cookieKey);

            WebTool.SetCookie(safeCookieKey, null);
            WebTool.RemoveCookie(safeCookieKey);
        }

        /// <summary>获取创始人（启用的超级管理员中 ID 最小者） / Get the founder (the enabled master admin with the smallest ID)</summary>
        /// <returns>创始人DTO（含所属组）；不存在时为 null / The founder DTO (with groups); null if none exists</returns>
        public AdminDto? GetCreator()
        {
            LambdaWrapper<Admin> wrapper = new LambdaWrapper<Admin>();
            wrapper.And(a => a.IsMaster == true && a.Status == CommonStatus.Enabled);
            wrapper.OrderBy(a => a.Id);
            wrapper.Limit = 1;
            Admin? admin = AdminDao.SelectOne(wrapper);
            if (admin != null)
            {
                AdminDto dto = ReflectionTool.Copy<Admin, AdminDto>(admin);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>根据ID获取管理员（含所属组，密码已清空） / Get the administrator by ID (with groups, password cleared)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        public AdminDto? GetAdminById(int adminId)
        {
            Admin? admin = SelectById(adminId);
            if (admin != null)
            {
                AdminDto dto = ReflectionTool.Copy<Admin, AdminDto>(admin);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>根据用户名获取管理员（含所属组，密码已清空） / Get the administrator by username (with groups, password cleared)</summary>
        /// <param name="username">用户名 / Username</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        public AdminDto? GetAdminByUsername(string username)
        {
            Admin? admin = SelectOne(a => a.Username!.Equals(username));
            if (admin != null)
            {
                AdminDto dto = ReflectionTool.Copy<Admin, AdminDto>(admin);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>根据手机号获取管理员（含所属组，密码已清空） / Get the administrator by mobile number (with groups, password cleared)</summary>
        /// <param name="mobile">手机号 / Mobile number</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        public AdminDto? GetAdminByMobile(string mobile)
        {
            Admin? admin = SelectOne(a => a.Mobile!.Equals(mobile));
            if (admin != null)
            {
                AdminDto dto = ReflectionTool.Copy<Admin, AdminDto>(admin);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>根据邮箱地址获取管理员（含所属组，密码已清空） / Get the administrator by email address (with groups, password cleared)</summary>
        /// <param name="email">邮箱地址 / Email address</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        public AdminDto? GetAdminByEmail(string email)
        {
            Admin? admin = SelectOne(a => a.Email!.Equals(email));
            if (admin != null)
            {
                AdminDto dto = ReflectionTool.Copy<Admin, AdminDto>(admin);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>获取所有管理员列表（含所属组，密码已清空） / Get the list of all administrators (with groups, passwords cleared)</summary>
        /// <returns>全部管理员列表 / The list of all administrators</returns>
        public List<AdminDto> GetAllAdminList()
        {
            List<AdminDto> list = SelectList<AdminDto>(new LambdaWrapper<Admin>());
            ReadRelatedData(list);
            return list;
        }

        /// <summary>根据ID集合获取管理员列表（含所属组，密码已清空） / Get the administrator list by ID collection (with groups, passwords cleared)</summary>
        /// <param name="adminIds">管理员ID集合 / Administrator ID collection</param>
        /// <returns>指定ID集合的管理员列表 / The administrators with the specified IDs</returns>
        public List<AdminDto> GetAdminList(IList<int> adminIds)
        {
            List<AdminDto> list = SelectList<AdminDto>(a => adminIds.Contains(a.Id));
            ReadRelatedData(list);
            return list;
        }

        /// <summary>获取分页的管理员列表（含所属组，密码已清空） / Get the paginated administrator list (with groups, passwords cleared)</summary>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <returns>分页的管理员列表 / The paginated administrator list</returns>
        public List<AdminDto> GetAdminPageList(PagerInfo pagerInfo)
        {
            List<AdminDto> list = SelectPage<AdminDto>(pagerInfo, new LambdaWrapper<Admin>());
            ReadRelatedData(list);
            return list;
        }

        /// <summary>根据筛选器获取分页的管理员列表（含所属组，密码已清空） / Get the paginated administrator list by filter (with groups, passwords cleared)</summary>
        /// <param name="filter">筛选器 / The filter</param>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <returns>按筛选器过滤的分页管理员列表 / The paginated administrator list filtered by the filter</returns>
        public List<AdminDto> GetAdminPageListByFilter(AdminFilter filter, PagerInfo pagerInfo)
        {
            List<AdminDto> list = SelectPage<AdminDto>(pagerInfo, filter);
            ReadRelatedData(list);
            return list;
        }

        /// <summary>从缓存根据ID获取管理员（缓存时长在 3600~5400 秒间随机，避免集中过期） / Get the administrator by ID from cache (cache duration randomized between 3600 and 5400 seconds to avoid synchronized expiry)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的管理员DTO；不存在时为 null / The cached admin DTO; null if not found</returns>
        public AdminDto? GetAdminByIdFromCache(int adminId)
        {
            return HandleWebCache<AdminDto?>(() =>
            {
                return GetAdminById(adminId);
            }, AdminDto.CACHE_KEY_ADMIN_BYID_TPL, MathTool.GenerateInt(3600, 5400), adminId);
        }

        /// <summary>获取当前登录的管理员 / Get the currently logged-in administrator</summary>
        /// <remarks>
        /// 流程：请求上下文缓存命中直接返回→读取客户端令牌→校验 JWT；过期时用刷新令牌换新并立即回写客户端；无刷新令牌返回 InvalidToken。
        /// 随后按 NameId 取管理员缓存，并校验令牌 Nonce 与密码派生的安全戳（密码修改后旧令牌全部失效），校验失败返回 InvalidToken。
        /// Flow: context cache hit returns directly -> read the client token -> validate the JWT; when expired, refresh it with the refresh token and write the new one back to the client immediately; without a refresh token InvalidToken is returned.
        /// Then the admin cache is loaded by NameId and the token Nonce is checked against the password-derived security stamp (all old tokens become invalid after a password change); a failed check returns InvalidToken.
        /// </remarks>
        /// <returns>成功时 Data 为当前管理员；未登录或令牌无效/过期时返回相应失败码 / On success Data holds the current admin; otherwise the corresponding failure code when not logged in or the token is invalid/expired</returns>
        public R<AdminDto> GetLoginAdmin()
        {
            AdminDto? loginAdmin = null;
            string contextCacheKey = "CurrentAdmin";
            if (ContextCache.TryGetValue<AdminDto>(contextCacheKey, out loginAdmin))
            {
                return R<AdminDto>.Success(loginAdmin);
            }

            AuthenticationToken? tokenInfo = GetClientAgentToken();
            if (tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Token))
            {
                JwtAuthenticationInfo? authenticationInfo = null;
                string token = tokenInfo.Token;

                ValidateTokenState tokenState = JwtTokenManager.ValidateToken(ManagementVars.AuthenticationConfigName, token);
                if (tokenState.Status == ValidateTokenStatus.Expired)
				{
                    if (!string.IsNullOrEmpty(tokenInfo.RefreshToken))
					{
                        ValidateTokenState? refreshTokenState = null;
                        AuthenticationToken? refreshTokenInfo = JwtTokenManager.RefreshToken(ManagementVars.AuthenticationConfigName, tokenInfo.RefreshToken, out refreshTokenState);
                        
                        if (refreshTokenInfo == null || refreshTokenState == null)
                        {
                            return R<AdminDto>.Create(ResultCode.TokenExpired);
                        }
                        tokenInfo = refreshTokenInfo;
                        authenticationInfo = refreshTokenState.Info;
                        // 刷新成功立即回写客户端，避免每个请求都重复走刷新流程
                        SetTokenToClientAgent(refreshTokenInfo);
                    }
                    else
                    {
                        return R<AdminDto>.Create(ResultCode.InvalidToken);
                    }
                }

                if (authenticationInfo == null)
                {
                    authenticationInfo = tokenState.Info;
                }
                if (authenticationInfo != null)
				{
                    int adminId = StringTool.TryParse<int>(authenticationInfo.NameId!);
                    loginAdmin = GetAdminByIdFromCache(adminId);
                    // Nonce 是由密码哈希派生的安全戳：密码一旦修改，旧令牌（含刷新令牌）立即失效
                    if (loginAdmin != null && !IsSecurityStampValid(adminId, authenticationInfo.Nonce))
					{
                        loginAdmin = null;
                        return R<AdminDto>.Create(ResultCode.InvalidToken);
                    }
                }
				else
				{
                    return R<AdminDto>.Create(ResultCode.TokenExpired);

                }
            }
            else
            {
                return R<AdminDto>.Create(ResultCode.InvalidToken);
            }

            if (loginAdmin != null)
            {
                ContextCache.Set(contextCacheKey, loginAdmin);
            }
            return R<AdminDto>.Success(loginAdmin);
        }


        /// <summary>使用 PBKDF2 加密管理员密码 / Encrypt an administrator password with PBKDF2</summary>
        /// <param name="password">明文密码 / The plaintext password</param>
        /// <returns>加密后的密码哈希 / The encrypted password hash</returns>
        public string EncryptAdminPassword(string password)
        {
            string encryptPassword = SecurityTool.PBKDF2Hash(password);
            return encryptPassword;
        }

        /// <summary>
        /// 登录失败计数器：优先使用 DI 中注册的 ILoginAttemptTracker（集群可注册分布式实现），否则使用进程内默认实例
        /// Login failure tracker: prefers the ILoginAttemptTracker registered in DI (clusters may register a distributed implementation), otherwise the in-process default
        /// </summary>
        protected virtual ILoginAttemptTracker LoginAttemptTracker
        {
            get { return ServiceLocator?.TryGet<ILoginAttemptTracker>() ?? MemoryLoginAttemptTracker.Default; }
        }

        private const string LoginProtectionScope = "management.admin";
        private const string CACHE_KEY_ADMIN_STAMP_TPL = "Rampage.Management.AdminSecurityStamp_{0}";

        /// <summary>
        /// 由密码哈希派生的安全戳（不可逆、不泄露哈希本身），写入 JWT 的 Nonce；密码修改后旧令牌失效
        /// A security stamp derived from the password hash (irreversible, does not leak the hash itself), written into the JWT Nonce; old tokens become invalid once the password changes
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="passwordHash">密码哈希 / The password hash</param>
        /// <returns>32 位安全戳 / The 32-character security stamp</returns>
        public static string ComputeSecurityStamp(int adminId, string? passwordHash)
        {
            string material = string.Concat(adminId, "|", passwordHash ?? string.Empty, "|", ManagementConfig.AdminPasswordEncryptKey);
            return SecurityTool.SHA256(material, System.Text.Encoding.UTF8).Substring(0, 32);
        }

        /// <summary>
        /// 当前数据库中的安全戳（短缓存，密码更新时通过 RemoveAdminByIdCache 失效）
        /// The current security stamp from the database (short-lived cache, invalidated by RemoveAdminByIdCache when the password changes)
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>当前安全戳；管理员不存在时为 null / The current stamp; null if the admin does not exist</returns>
        protected string? GetCurrentSecurityStamp(int adminId)
        {
            return HandleWebCache<string?>(() =>
            {
                Admin? entity = SelectById(adminId);
                return entity == null ? null : ComputeSecurityStamp(adminId, entity.Password);
            }, CACHE_KEY_ADMIN_STAMP_TPL, 300, adminId);
        }

        /// <summary>
        /// 校验令牌中的 Nonce 与当前安全戳一致。缺失 Nonce 的旧令牌一律视为无效（要求重新登录）。
        /// Checks that the token Nonce matches the current security stamp. Legacy tokens without a Nonce are always treated as invalid (re-login required).
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="nonce">令牌中的 Nonce / The Nonce from the token</param>
        /// <returns>一致返回 true / true when they match</returns>
        public bool IsSecurityStampValid(int adminId, string? nonce)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                return false;
            }
            string? current = GetCurrentSecurityStamp(adminId);
            return current != null && string.Equals(current, nonce, StringComparison.Ordinal);
        }

        /// <summary>
        /// 管理员登录
        /// Administrator login
        /// </summary>
        /// <remarks>
        /// 先经过暴力破解防护（ILoginAttemptTracker，按账号+IP 计数），被封锁时直接返回 ManyIncorrectPassword。
        /// 账号按邮箱/手机号/用户名自动识别；账号不存在也计入失败，避免通过响应差异枚举账号。成功后写入登录日志并签发 JWT（Nonce 为密码安全戳）。
        /// Brute-force protection (ILoginAttemptTracker, counting by account + IP) runs first and returns ManyIncorrectPassword while blocked.
        /// The account is auto-detected as email / mobile / username; a missing account also counts as a failure to prevent account enumeration via response differences. On success a login log is written and a JWT is issued (Nonce is the password security stamp).
        /// </remarks>
        /// <param name="account">用户名/手机/邮箱 / Username / mobile number / email address</param>
        /// <param name="password">密码（未加密） / Password (unencrypted)</param>
        /// <param name="rememberDays">记住登录天数；null 表示会话级 Cookie / Days to remember the login; null for a session cookie</param>
        /// <returns>
        /// 成功时 Data 为登录管理员；10001:用户名或密码错误;10002:用户不存在;10003:用户被禁用;
        /// On success Data holds the logged-in admin; 10001: Incorrect username or password; 10002: User does not exist; 10003: User is disabled;
        /// </returns>
        public R<AdminDto> Login(string account, string password, double? rememberDays)
        {
            string? loginIP = WebTool.GetClientIP();
            ILoginAttemptTracker tracker = LoginAttemptTracker;
            if (tracker.IsBlocked(LoginProtectionScope, account, loginIP, out TimeSpan retryAfter))
            {
                RampageLogger.Warn($"Admin login blocked by brute-force protection: account='{account}', ip='{loginIP}', retryAfter={retryAfter.TotalSeconds:0}s");
                return R<AdminDto>.Create(ResultCode.ManyIncorrectPassword);
            }

            AdminDto? loginAdmin;
            if (StringTool.IsEmail(account))
            {
                loginAdmin = GetAdminByEmail(account);
            }
            else if (StringTool.IsCommonMobile(account))
            {
                loginAdmin = GetAdminByMobile(account);
            }
            else
            {
                loginAdmin = GetAdminByUsername(account);
            }

            if (loginAdmin != null)
            {
                if (loginAdmin.Status == CommonStatus.Disabled)
                {
                    //用户被禁用
                    loginAdmin = null;
                    tracker.RecordFailure(LoginProtectionScope, account, loginIP);
                    return R<AdminDto>.Create(ResultCode.DisabledAccount);
                }
                Admin entity = SelectById(loginAdmin.Id)!;
                if (!SecurityTool.PBKDF2Verify(password, entity.Password))
                {
                    //用户名或密码错误
                    loginAdmin = null;
                    tracker.RecordFailure(LoginProtectionScope, account, loginIP);
                    return R<AdminDto>.Create(ResultCode.IncorrectPassword);
                }
                else
                {
                    tracker.Reset(LoginProtectionScope, account);

                    string? userAgent = HttpContext.Request.Headers["User-Agent"];
                    AdminLoginLog loginLog = new AdminLoginLog
                    {
                        AdminId = loginAdmin.Id,
                        LoginIp = loginIP,
                        LoginTime = UtcNow,
                        UserAgent = userAgent,
                    };
                    AddAdminLoginLog(loginLog);

                    var token = JwtTokenManager.BuildToken(ManagementVars.AuthenticationConfigName, new JwtAuthenticationInfo
					{
                        NameId = loginAdmin.Id.ToString(),
                        Name = loginAdmin.Username,
                        // 安全戳而不是密码哈希：JWT 载荷仅 Base64 编码，任何持有令牌的人都能读到
                        Nonce = ComputeSecurityStamp(loginAdmin.Id, entity.Password)
					});
                    SetTokenToClientAgent(token, rememberDays);

                    RemoveAdminByIdCache(loginAdmin.Id);
                    return R<AdminDto>.Success(loginAdmin);
                }
            }
            else
            {
                // 账号不存在同样计入失败，避免通过响应差异枚举账号
                tracker.RecordFailure(LoginProtectionScope, account, loginIP);
                return R<AdminDto>.Create(ResultCode.InvalidAccount);
            }
        }

        /// <summary>管理员注销（清除客户端令牌） / Administrator log out (clears the client token)</summary>
        /// <returns>成功或失败 / Success or failure</returns>
        public R Logout()
        {
            try
            {
                ClearClientAgentToken();
                return R.Success();
            }
            catch (Exception)
            {
                return R.Failure();
            }
        }

        /// <summary>移除指定管理员的缓存（实体缓存与安全戳缓存） / Remove the specified admin's caches (entity cache and security-stamp cache)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        public void RemoveAdminByIdCache(int adminId)
        {
            MemoryCache.Remove(string.Format(AdminDto.CACHE_KEY_ADMIN_BYID_TPL, adminId));
            MemoryCache.Remove(string.Format(CACHE_KEY_ADMIN_STAMP_TPL, adminId));
        }

        /// <summary>
        /// 添加一个管理员
        /// Add an administrator
        /// </summary>
        /// <remarks>密码加密后入库；用户名/邮箱/手机号唯一性校验失败分别返回 10001/10002/10003；成功后按需分配管理组并失效缓存。
        /// The password is encrypted before insertion; username/email/mobile uniqueness failures return 10001/10002/10003 respectively; on success groups are assigned as needed and caches invalidated.</remarks>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <param name="groupIds">分配至管理组 / Assigned to the management groups</param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        public R AddAdmin(AdminDto admin, IEnumerable<int>? groupIds)
        {
            if (admin.CreateTime <= DateTime.MinValue)
            {
                admin.CreateTime = UtcNow;
            }
            admin.Password = EncryptAdminPassword(admin.Password!);

            Admin? usernameAdmin = SelectOne(a => a.Username!.Equals(admin.Username));
            if (usernameAdmin != null)
            {
                return R.Failure(AdminSaveResult.ExistsUsername);
            }

            if (!string.IsNullOrEmpty(admin.Email))
            {
                Admin? emailAdmin = SelectOne(a => a.Email!.Equals(admin.Email));
                if (emailAdmin != null)
                {
                    return R.Failure(AdminSaveResult.ExistsEmail);
                }
            }
            if (!string.IsNullOrEmpty(admin.Mobile))
            {
                Admin? mobileAdmin = SelectOne(a => a.Mobile!.Equals(admin.Mobile));
                if (mobileAdmin != null)
                {
                    return R.Failure(AdminSaveResult.ExistsMobile);
                }
            }

            admin.Email = admin.Email ?? string.Empty;
            admin.Mobile = admin.Mobile ?? string.Empty;
            admin.Avatar = admin.Avatar ?? string.Empty;
            admin.TrueName = admin.TrueName ?? string.Empty;
            admin.LoginIp = admin.LoginIp ?? string.Empty;

            Admin entity = ReflectionTool.Copy<AdminDto, Admin>(admin);

            bool sucess = Insert(entity);
            admin.Id = entity.Id;

            R result = R.Success(admin);
            if (groupIds != null && groupIds.Any())
            {
                UpdateAdminInGroupListForAdmin(admin.Id, groupIds);
            }
            RemoveAdminByIdCache(admin.Id);
            return result;
        }

        /// <summary>
        /// 更新一个管理员
        /// Update an administrator
        /// </summary>
        /// <remarks>密码留空则保留原密码；ID 为 1 的创始人强制为启用状态且 IsMaster=true；用户名/邮箱/手机号与其他管理员冲突时返回 10001/10002/10003。
        /// An empty password keeps the original; the founder with ID 1 is forced to enabled with IsMaster=true; conflicts with other admins return 10001/10002/10003.</remarks>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        public R UpdateAdmin(AdminDto admin)
        {
            admin.UpdateTime = UtcNow;
            Admin? entity = SelectById(admin.Id);
            if (entity == null)
            {
                return R.Failure(ResultCode.DataNotExists);
            }
            if (string.IsNullOrEmpty(admin.Password))
            {
                admin.Password = entity.Password;
            }
            else
            {
                admin.Password = EncryptAdminPassword(admin.Password!);
            }

            long usernameCount = Count(a => a.Username!.Equals(admin.Username) && a.Id != admin.Id);
            if (usernameCount > 0)
                return R.Failure(AdminSaveResult.ExistsUsername);

            if (!string.IsNullOrEmpty(admin.Email))
            {
                long emailCount = Count(a => a.Email!.Equals(admin.Email) && a.Id != admin.Id);
                if (emailCount > 0)
                    return R.Failure(AdminSaveResult.ExistsEmail);
            }
            if (!string.IsNullOrEmpty(admin.Mobile))
            {
                long mobileCount = Count(a => a.Mobile!.Equals(admin.Mobile) && a.Id != admin.Id);
                if (mobileCount > 0)
                    return R.Failure(AdminSaveResult.ExistsMobile);
            }

            if (admin.Id == 1)
            {
                admin.Status = CommonStatus.Enabled;
                admin.IsMaster = true;
            }

            admin.Email = admin.Email ?? string.Empty;
            admin.Mobile = admin.Mobile ?? string.Empty;
            admin.Avatar = admin.Avatar ?? string.Empty;
            admin.TrueName = admin.TrueName ?? string.Empty;
            admin.LoginIp = admin.LoginIp ?? string.Empty;
            ReflectionTool.CopyProperties(admin, entity, true);
            bool success = UpdateById(entity);

            R result = R.Success(success);
            RemoveAdminByIdCache(admin.Id);
            return result;
        }

        /// <summary>
        /// 更新一个管理员并重新分配管理组
        /// Update an administrator and reassign its management groups
        /// </summary>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <param name="groupIds">分配至管理组 / Assigned to the management groups</param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        public R UpdateAdmin(AdminDto admin, IEnumerable<int>? groupIds)
        {
            R result = UpdateAdmin(admin);
            if (result.IsSuccess)
            {
                if (groupIds != null && groupIds.Any())
                {
                    UpdateAdminInGroupListForAdmin(admin.Id, groupIds);
                }
                RemoveAdminByIdCache(admin.Id);
            }
            return result;
        }

        /// <summary>
        /// 分配管理员至管理组
        /// Assign an administrator to management groups
        /// </summary>
        /// <remarks>事务内差量更新：已存在且在目标集合中的置为启用，缺失的新增插入，不在目标集合中的置为禁用（软删除，不物理删除记录）。
        /// Delta update inside a transaction: existing rows in the target set are enabled, missing ones inserted, rows outside the target set disabled (soft delete, no physical deletion).</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="manageGroupIds">管理组ID集合 / Management group IDs</param>
        /// <returns>分配成功或失败（集合为空或管理员不存在返回失败） / Assignment success or failure (failure when the set is empty or the admin is missing)</returns>
        public R UpdateAdminInGroupListForAdmin(int adminId, IEnumerable<int> manageGroupIds)
        {
            if (manageGroupIds == null || false == manageGroupIds.Any())
            {
                return R.Failure();
            }

            Admin? admin = SelectOne(a => a.Id == adminId);
            if (admin == null)
            {
                return R.Failure();
            }

            List<AdminInGroup> disableList = AdminInGroupDao.SelectList(aig => aig.AdminId == adminId && !manageGroupIds.Contains(aig.GroupId));

            BeginTransaction();
            List<AdminInGroup> toUpdateList = new List<AdminInGroup>();
            List<AdminInGroup> toAddList = new List<AdminInGroup>();

            try
            {
                List<AdminInGroup> existsList = AdminInGroupDao.SelectList(aig => aig.AdminId == adminId && manageGroupIds.Contains(aig.GroupId));
                foreach (int manageGroupId in manageGroupIds)
                {
                    AdminInGroup? existsItem = existsList.Find(aig => aig.AdminId == adminId && aig.GroupId == manageGroupId);
                    if (existsItem != null)
                    {
                        existsItem.Status = CommonStatus.Enabled;
                        existsItem.UpdateTime = UtcNow;
                        toUpdateList.Add(existsItem);
                    }
                    else
                    {
                        AdminInGroup newItem = new AdminInGroup
                        {
                            AdminId = adminId,
                            GroupId = manageGroupId,
                            Status = CommonStatus.Enabled,
                            CreateTime = UtcNow,
                            UpdateTime = UtcNow
                        };
                        toAddList.Add(newItem);
                    }
                }
                if (toUpdateList.Count > 0)
                {
                    AdminInGroupDao.UpdateBatch(toUpdateList);
                }
                if (toAddList.Count > 0)
                {
                    AdminInGroupDao.InsertBatch(toAddList);
                }
                if (disableList.Count > 0)
                {
                    disableList.ForEach(d =>
                    {
                        d.Status = CommonStatus.Disabled;
                        d.UpdateTime = UtcNow;
                    });
                    AdminInGroupDao.UpdateBatch(disableList);
                }
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                RampageLogger.Error(ex);
                return R.Failure();
            }

            return R.Success();
        }

        /// <summary>修改管理员状态 / Change the administrator status</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        public R ChangeAdminStatus(int adminId, CommonStatus status)
        {
            bool result = AdminDao.UpdateById(adminId, c => c.Status, status);
            if (result)
            {
                RemoveAdminByIdCache(adminId);
            }

            return result ? R.Success() : R.Failure();
        }

        /// <summary>
        /// 更新管理员头像
        /// Update the administrator avatar
        /// </summary>
        /// <remarks>头像以 PNG 存至 wwwroot/AdminFiles/avatars/{管理员ID}_{时间戳}.png，并删除旧头像文件；成功后失效管理员缓存。
        /// The avatar is saved as PNG to wwwroot/AdminFiles/avatars/{adminId}_{timestamp}.png and the old file is deleted; the admin cache is invalidated on success.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="avatarBuffer">头像图片字节 / The avatar image bytes</param>
        /// <returns>成功时 Data 为新的头像地址 / On success Data contains the new avatar URL</returns>
        public R UpdateAdminAvatar(int adminId, byte[] avatarBuffer)
        {
            AdminDto? admin = GetAdminById(adminId);
            if (admin == null)
            {
                return R.Failure();
            }
            
            string avatar = $"AdminFiles/avatars/{adminId}_{UtcNow.ToString("yyyyMMddHHmmss")}.png";
            string savePath = Path.Combine(WebHostEnvironment.WebRootPath, avatar);
            string saveDirPath = Path.GetDirectoryName(savePath)!;
            if (!Directory.Exists(saveDirPath))
            {
                Directory.CreateDirectory(saveDirPath);
            }
            FileTool.WriteFile(savePath, avatarBuffer);

            if (!string.IsNullOrEmpty(admin.Avatar))
            {
                string oldSavePath = Path.Combine(WebHostEnvironment.WebRootPath, admin.Avatar);
                if (File.Exists(oldSavePath))
                {
                    File.Delete(oldSavePath);
                }
            }

            admin.Password = string.Empty;
            admin.Avatar = avatar;
            admin.UpdateTime = UtcNow;
            R result = UpdateAdmin(admin);
            if (result.IsSuccess)
            {
                result.Data = admin.AvatarUrl;
                RemoveAdminByIdCache(adminId);
            }
            return result;
        }

        /// <summary>校验管理员密码是否正确 / Check whether the administrator's password is correct</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="password">待校验的明文密码 / The plaintext password to verify</param>
        /// <returns>密码正确返回 true；管理员不存在返回 false / true if the password matches; false if the admin does not exist</returns>
        public bool CheckAdminPassword(int adminId, string password)
        {
            Admin? entity = SelectById(adminId);
            if (entity == null)
            {
                return false;
            }
            return SecurityTool.PBKDF2Verify(password, entity.Password);
        }

        /// <summary>
        /// 修改管理员密码
        /// Update the administrator password
        /// </summary>
        /// <remarks>密码修改后安全戳变化，该管理员所有旧令牌（含刷新令牌）立即失效，需重新登录。
        /// After a password change the security stamp changes, so all of the admin's old tokens (including refresh tokens) become invalid immediately and re-login is required.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="newPassword">新密码（明文，内部加密后保存） / The new password (plaintext, encrypted before saving)</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        public R UpdateAdminPassword(int adminId, string newPassword)
        {
            AdminDto? admin = GetAdminById(adminId);
            if (admin == null)
            {
                return R.Failure();
            }
            string encryptNewPassword = EncryptAdminPassword(newPassword);
            admin.Password = encryptNewPassword;
            admin.UpdateTime = UtcNow;
            R result = UpdateAdmin(admin);
            if (result.IsSuccess)
            {
                RemoveAdminByIdCache(adminId);
            }
            return result;
        }

        /// <summary>
        /// 删除管理员
        /// Delete the administrator
        /// </summary>
        /// <remarks>事务内同时删除该管理员的组关联记录 / The admin's group memberships are deleted in the same transaction</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>成功时 Data 为管理员ID；失败时返回失败结果 / On success Data contains the admin ID; otherwise a failure result</returns>
        public R DeleteAdminById(int adminId)
        {
            R result;

            BeginTransaction();
            try
            {
                AdminInGroupDao.Delete(aig => aig.AdminId == adminId);
                Delete(a => a.Id == adminId);
                CommitTransaction();
                result = R.Success(adminId);
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                RampageLogger.Error(ex);
                result = R.Failure();
            }

            return result;
        }



        /// <summary>
        /// 添加管理员登录日志
        /// Add an administrator login log
        /// </summary>
        /// <remarks>同时更新管理员表的最后登录时间/IP；管理员不存在直接返回 0；异常仅记录日志不抛出。
        /// Also updates the admin's last login time/IP; returns 0 when the admin does not exist; exceptions are logged, not thrown.</remarks>
        /// <param name="log">登录日志数据 / The login log data</param>
        /// <returns>成功时返回日志ID，否则为 0 / The log ID on success, otherwise 0</returns>
        public long AddAdminLoginLog(AdminLoginLog log)
        {
            long result = 0;

            Admin? admin = SelectOne(a => a.Id == log.AdminId);
            if (admin == null)
            {
                return result;
            }

            try
            {
                admin.LoginTime = UtcNow;
                admin.LoginIp = log.LoginIp;
                admin.UpdateTime = UtcNow;
                UpdateById(admin);
                AdminLoginLogDao.Insert(log);
                result = log.Id;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
            return result;
        }


    }
}
