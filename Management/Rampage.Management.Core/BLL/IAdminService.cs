using Rampage.Data;
using Rampage.Enums;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Model;
using Rampage.Web;
using System;

namespace Rampage.Management.BLL
{
    /// <summary>管理员业务逻辑 / Administrator business logic</summary>
    public interface IAdminService : ISuperService<Admin, IAdminDao>
    {

        /// <summary>
        /// 获取创始人
        /// Get the founder admin
        /// </summary>
        /// <returns>创始人管理员（启用的超管中 ID 最小者）；不存在时为 null / The founder admin (the enabled master with the smallest ID); null if none exists</returns>
        AdminDto? GetCreator();

        /// <summary>
        /// 根据id获取管理员
        /// Get the administrator by ID
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        AdminDto? GetAdminById(int adminId);

        /// <summary>
        /// 从缓存根据id获取管理员
        /// Get the administrator by ID from cache
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的管理员DTO；不存在时为 null / The cached admin DTO; null if not found</returns>
        AdminDto? GetAdminByIdFromCache(int adminId);

        /// <summary>
        /// 根据用户名获取管理员
        /// Get the administrator by username
        /// </summary>
        /// <param name="username">用户名 / Username</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        AdminDto? GetAdminByUsername(string username);

        /// <summary>
        /// 根据手机号获取管理员
        /// Get the administrator by mobile number
        /// </summary>
        /// <param name="mobile">手机号 / Mobile number</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        AdminDto? GetAdminByMobile(string mobile);

        /// <summary>
        /// 根据邮箱地址获取管理员
        /// Get the administrator by email address
        /// </summary>
        /// <param name="email">邮箱地址 / Email address</param>
        /// <returns>管理员DTO；不存在时为 null / The admin DTO; null if not found</returns>
        AdminDto? GetAdminByEmail(string email);

        /// <summary>
        /// 获取所有管理员列表
        /// Get all administrator list
        /// </summary>
        /// <returns>全部管理员列表 / The list of all administrators</returns>
        List<AdminDto> GetAllAdminList();

        /// <summary>
        /// 根据id集获取管理员列表
        /// Get administrator list by ID list
        /// </summary>
        /// <param name="adminIds">管理员ID集合 / Administrator ID collection</param>
        /// <returns>指定ID集合的管理员列表 / The administrators with the specified IDs</returns>
        List<AdminDto> GetAdminList(IList<int> adminIds);

        /// <summary>
        /// 获取分页管理员列表数据
        /// Get paginated administrator list data
        /// </summary>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <returns>分页的管理员列表 / The paginated administrator list</returns>
        List<AdminDto> GetAdminPageList(PagerInfo pagerInfo);

        /// <summary>
        /// 根据筛选器获取分页管理员列表数据
        /// Get the paginated administrator list data by the filter
        /// </summary>
        /// <param name="filter">筛选器 / The filter</param>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <returns>按筛选器过滤的分页管理员列表 / The paginated administrator list filtered by the filter</returns>
        List<AdminDto> GetAdminPageListByFilter(AdminFilter filter, PagerInfo pagerInfo);

        /// <summary>
        /// 获取当前登录的管理员
        /// Get the currently logged-in administrator
        /// </summary>
        /// <returns>当前登录的管理员；未登录或令牌无效/过期时返回相应失败码 / The currently logged-in admin; a failure code when not logged in or the token is invalid/expired</returns>
        R<AdminDto> GetLoginAdmin();


        /// <summary>
        /// 管理员密码加密
        /// Administrator password encryption
        /// </summary>
        /// <param name="password">明文密码 / The plaintext password</param>
        /// <returns>加密后的密码哈希 / The encrypted password hash</returns>
        string EncryptAdminPassword(string password);

        /// <summary>
        /// 管理员登录
        /// Administrator Login
        /// </summary>
        /// <param name="account">
        /// 用户名/手机/邮箱
        /// Username/Mobile number/Email address
        /// </param>
        /// <param name="password">
        /// 密码（未加密）
        /// Password (unencrypted)
        /// </param>
        /// <param name="rememberDays">记住登录天数；null 表示会话级 Cookie / Days to remember the login; null for a session cookie</param>
        /// <returns>
        /// 10001:用户名或密码错误;10002:用户不存在;10003:用户被禁用;
        /// 10001: Incorrect username or password; 10002: User does not exist; 10003: User is disabled;
        /// </returns>
        R<AdminDto> Login(string account, string password, double? rememberDays);

        /// <summary>管理员注销（清除客户端令牌） / Administrator log out (clears the client token)</summary>
        /// <returns>成功或失败 / Success or failure</returns>
        R Logout();

        /// <summary>
        /// 添加一个管理员
        /// Add an administrator
        /// </summary>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <param name="groupIds">
        /// 分配至管理组
        /// Assigned to the management groups
        /// </param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        R AddAdmin(AdminDto admin, IEnumerable<int>? groupIds);

        /// <summary>更新一个管理员 / Update an administrator</summary>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        R UpdateAdmin(AdminDto admin);

        /// <summary>更新一个管理员并重新分配管理组 / Update an administrator and reassign its management groups</summary>
        /// <param name="admin">管理员数据 / The administrator data</param>
        /// <param name="groupIds">
        /// 分配至管理组
        /// Assigned to the management groups
        /// </param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        R UpdateAdmin(AdminDto admin, IEnumerable<int>? groupIds);

        /// <summary>
        /// 更新管理员头像
        /// Update administrator avatar
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="avatarBuffer">头像图片字节 / The avatar image bytes</param>
        /// <returns>成功时 Data 为新的头像地址 / On success Data contains the new avatar URL</returns>
        R UpdateAdminAvatar(int adminId, byte[] avatarBuffer);

        /// <summary>校验管理员密码是否正确 / Check whether the administrator's password is correct</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="password">待校验的明文密码 / The plaintext password to verify</param>
        /// <returns>密码正确返回 true；管理员不存在返回 false / true if the password matches; false if the admin does not exist</returns>
        bool CheckAdminPassword(int adminId, string password);

        /// <summary>
        /// 修改管理员密码
        /// Update administrator password
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="newPassword">新密码（明文，内部加密后保存） / The new password (plaintext, encrypted before saving)</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        R UpdateAdminPassword(int adminId, string newPassword);

        /// <summary>
        /// 分配管理员至管理组
        /// Assign administrator to management groups
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="manageGroupIds">管理组ID集合 / Management group IDs</param>
        /// <returns>分配成功或失败 / Assignment success or failure</returns>
        R UpdateAdminInGroupListForAdmin(int adminId, IEnumerable<int> manageGroupIds);

        /// <summary>
        /// 修改管理员状态
        /// Update administrator status
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        R ChangeAdminStatus(int adminId, CommonStatus status);

        /// <summary>
        /// 删除管理员
        /// delete the administrator
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>删除成功或失败 / Deletion success or failure</returns>
        R DeleteAdminById(int adminId);

        /// <summary>移除指定管理员的缓存（实体缓存与安全戳缓存） / Remove the specified admin's caches (entity cache and security-stamp cache)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        void RemoveAdminByIdCache(int adminId);



    }
}
