using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Extensions;
using Rampage.Json;
using Rampage.Log;
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

namespace Rampage.Management.BLL
{

    /// <summary>
    /// 
    /// </summary>
    public class AdminService : ManagementService<Admin, IAdminDao>, IAdminService
    {

        [Auto]
        protected IWebHostEnvironment WebHostEnvironment = default!;

        [Auto]
        protected IManageGroupService ManageGroupService = default!;

        [Auto]
        protected IAdminInGroupDao AdminInGroupDao = default!;
        [Auto]
        protected IAdminLoginLogDao AdminLoginLogDao = default!;
        

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

        public void SetTokenToClientAgent(AuthenticationToken token, double? cacheDays = null)
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
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

        public AuthenticationToken? GetClientAgentToken()
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
            string safeCookieKey = SecurityTool.SHA1(cookieKey);
            string? encryptTokenStr = WebTool.GetCookie(safeCookieKey);
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
            string tokenStr = SecurityTool.AESDecodeCBC(encryptTokenStr);
            if (!string.IsNullOrEmpty(tokenStr))
            {
                return JsonTool.Parse<AuthenticationToken>(tokenStr);
            }

            return null;
        }

        public void ClearClientAgentToken()
        {
            string cookieKey = $"{WebGlobals.AuthorizationHeaderKey}.{ManagementVars.AuthenticationConfigName}";
            string safeCookieKey = SecurityTool.SHA1(cookieKey);

            WebTool.SetCookie(safeCookieKey, null);
            WebTool.RemoveCookie(safeCookieKey);
        }

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

        public List<AdminDto> GetAllAdminList()
        {
            List<AdminDto> list = SelectList<AdminDto>(new LambdaWrapper<Admin>());
            ReadRelatedData(list);
            return list;
        }

        public List<AdminDto> GetAdminList(IList<int> adminIds)
        {
            List<AdminDto> list = SelectList<AdminDto>(a => adminIds.Contains(a.Id));
            ReadRelatedData(list);
            return list;
        }

        public List<AdminDto> GetAdminPageList(PagerInfo pagerInfo)
        {
            List<AdminDto> list = SelectPage<AdminDto>(pagerInfo, new LambdaWrapper<Admin>());
            ReadRelatedData(list);
            return list;
        }

        public List<AdminDto> GetAdminPageListByFilter(AdminFilter filter, PagerInfo pagerInfo)
        {
            List<AdminDto> list = SelectPage<AdminDto>(pagerInfo, filter);
            ReadRelatedData(list);
            return list;
        }

        public AdminDto? GetAdminByIdFromCache(int adminId)
        {
            return HandleWebCache<AdminDto?>(() =>
            {
                return GetAdminById(adminId);
            }, AdminDto.CACHE_KEY_ADMIN_BYID_TPL, MathTool.GenerateInt(3600, 5400), adminId);
        }

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


        public string EncryptAdminPassword(string password)
        {
            string encryptPassword = SecurityTool.PBKDF2Hash(password);
            return encryptPassword;
        }

        /// <summary>
        /// 登录失败计数器：优先使用 DI 中注册的 ILoginAttemptTracker（集群可注册分布式实现），否则使用进程内默认实例
        /// </summary>
        protected virtual ILoginAttemptTracker LoginAttemptTracker
        {
            get { return ServiceLocator?.TryGet<ILoginAttemptTracker>() ?? MemoryLoginAttemptTracker.Default; }
        }

        private const string LoginProtectionScope = "management.admin";
        private const string CACHE_KEY_ADMIN_STAMP_TPL = "Rampage.Management.AdminSecurityStamp_{0}";

        /// <summary>
        /// 由密码哈希派生的安全戳（不可逆、不泄露哈希本身），写入 JWT 的 Nonce；密码修改后旧令牌失效
        /// </summary>
        public static string ComputeSecurityStamp(int adminId, string? passwordHash)
        {
            string material = string.Concat(adminId, "|", passwordHash ?? string.Empty, "|", ManagementConfig.AdminPasswordEncryptKey);
            return SecurityTool.SHA256(material, System.Text.Encoding.UTF8).Substring(0, 32);
        }

        /// <summary>
        /// 当前数据库中的安全戳（短缓存，密码更新时通过 RemoveAdminByIdCache 失效）
        /// </summary>
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
        /// </summary>
        public bool IsSecurityStampValid(int adminId, string? nonce)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                return false;
            }
            string? current = GetCurrentSecurityStamp(adminId);
            return current != null && string.Equals(current, nonce, StringComparison.Ordinal);
        }

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

        public void RemoveAdminByIdCache(int adminId)
        {
            MemoryCache.Remove(string.Format(AdminDto.CACHE_KEY_ADMIN_BYID_TPL, adminId));
            MemoryCache.Remove(string.Format(CACHE_KEY_ADMIN_STAMP_TPL, adminId));
        }

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

        public R ChangeAdminStatus(int adminId, CommonStatus status)
        {
            bool result = AdminDao.UpdateById(adminId, c => c.Status, status);
            if (result)
            {
                RemoveAdminByIdCache(adminId);
            }

            return result ? R.Success() : R.Failure();
        }

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

        public bool CheckAdminPassword(int adminId, string password)
        {
            Admin? entity = SelectById(adminId);
            if (entity == null)
            {
                return false;
            }
            return SecurityTool.PBKDF2Verify(password, entity.Password);
        }

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
