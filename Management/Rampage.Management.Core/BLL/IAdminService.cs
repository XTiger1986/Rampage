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
    /// <summary>
    /// 管理员业务逻辑
    /// Admin business logic
    /// </summary>
    public interface IAdminService : ISuperService<Admin, IAdminDao>
    {

        /// <summary>
        /// 获取创始人
        /// Get the founder admin
        /// </summary>
        /// <returns></returns>
        AdminDto? GetCreator();

        /// <summary>
        /// 根据id获取管理员
        /// Get the administrator by ID
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        AdminDto? GetAdminById(int adminId);

        /// <summary>
        /// 从缓存根据id获取管理员
        /// Get the administrator by ID from cache
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        AdminDto? GetAdminByIdFromCache(int adminId);

        /// <summary>
        /// 根据用户名获取管理员
        /// Get the administrator by username
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        AdminDto? GetAdminByUsername(string username);

        /// <summary>
        /// 根据手机号获取管理员
        /// Get the administrator by mobile number
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        AdminDto? GetAdminByMobile(string mobile);

        /// <summary>
        /// 根据邮箱地址获取管理员
        /// Get the administrator by email address
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        AdminDto? GetAdminByEmail(string email);

        /// <summary>
        /// 获取所有管理员列表
        /// Get all administrator list
        /// </summary>
        /// <returns></returns>
        List<AdminDto> GetAllAdminList();

        /// <summary>
        /// 根据id集获取管理员列表
        /// Get administrator list by ID list
        /// </summary>
        /// <param name="adminIds"></param>
        /// <returns></returns>
        List<AdminDto> GetAdminList(IList<int> adminIds);

        /// <summary>
        /// 获取分页管理员列表数据
        /// Get paginated administrator list data
        /// </summary>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        List<AdminDto> GetAdminPageList(PagerInfo pagerInfo);

        /// <summary>
        /// 根据筛选器获取分页管理员列表数据
        /// Get the paginated administrator list data by the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        List<AdminDto> GetAdminPageListByFilter(AdminFilter filter, PagerInfo pagerInfo);

        /// <summary>
        /// 获取当前登录的管理员
        /// Get the currently logged-in administrator
        /// </summary>
        /// <returns></returns>
        R<AdminDto> GetLoginAdmin();


        /// <summary>
        /// 管理员密码加密
        /// Administrator password encryption
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
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
        /// <param name="rememberDays"></param>
        /// <returns>
        /// 10001:用户名或密码错误;10002:用户不存在;10003:用户被禁用;
        /// 10001: Incorrect username or password; 10002: User does not exist; 10003: User is disabled;
        /// </returns>
        R<AdminDto> Login(string account, string password, double? rememberDays);

        /// <summary>
        /// Administrator Log out
        /// </summary>
        R Logout();

        /// <summary>
        /// 添加一个管理员
        /// Add an administrator
        /// </summary>
        /// <param name="admin"></param>
        /// <param name="groupIds">
        /// 分配至管理组
        /// Assigned to the management groups
        /// </param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        R AddAdmin(AdminDto admin, IEnumerable<int>? groupIds);

        /// <summary>
        /// 更新一个管理员
        /// </summary>
        /// <param name="admin"></param>
        /// <returns>
        /// 10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;
        /// 10001: The username already exists; 10002: The email already exists; 10003: The phone number already exists;
        /// </returns>
        R UpdateAdmin(AdminDto admin);

        /// <summary>
        /// 更新一个管理员
        /// </summary>
        /// <param name="admin"></param>
        /// <param name="groupIds">
        /// 分配至管理组
        /// Assigned to the management groups
        /// </param>
        /// <returns>10001:用户名已经存在;10002:邮箱已经存在;10003:手机号已经存在;</returns>
        R UpdateAdmin(AdminDto admin, IEnumerable<int>? groupIds);

        /// <summary>
        /// 更新管理员头像
        /// Update administrator avatar
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="avatarBuffer"></param>
        /// <returns></returns>
        R UpdateAdminAvatar(int adminId, byte[] avatarBuffer);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        bool CheckAdminPassword(int adminId, string password);

        /// <summary>
        /// 修改管理员密码
        /// Update administrator password
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        R UpdateAdminPassword(int adminId, string newPassword);

        /// <summary>
        /// 分配管理员至管理组
        /// Assign administrator to management groups
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="manageGroupIds"></param>
        /// <returns></returns>
        R UpdateAdminInGroupListForAdmin(int adminId, IEnumerable<int> manageGroupIds);

        /// <summary>
        /// 修改管理员状态
        /// Update administrator status
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        R ChangeAdminStatus(int adminId, CommonStatus status);

        /// <summary>
        /// 删除管理员
        /// delete the administrator
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        R DeleteAdminById(int adminId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminId"></param>
        void RemoveAdminByIdCache(int adminId);



    }
}
