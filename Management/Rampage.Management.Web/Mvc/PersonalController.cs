using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rampage.AppLog;
using Rampage.Management.Dto;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 个人中心控制器：当前管理员自身的资料编辑、修改密码与头像设置，仅需登录（不纳入权限资源管理）
    /// Personal center controller: the current admin's own profile editing, password change and avatar settings; login only (excluded from permission resources)
    /// </summary>
    [PermissionLoggedInFilter]
    public class PersonalController : SuperManagementController
    {

        /// <summary>
        /// 编辑个人资料页 / The edit-profile page
        /// </summary>
        /// <returns>资料编辑视图（模型为当前管理员）/ The profile edit view (modelled on the current admin)</returns>
        [PermissionAction(Ignore = true)]
        public IActionResult EditProfile()
        {
            AdminDto? admin = AdminService.GetAdminById(CurrentAdmin!.Id);
            return VW(admin);
        }

        /// <summary>
        /// 修改密码页 / The change-password page
        /// </summary>
        /// <returns>修改密码视图 / The change-password view</returns>
        [PermissionAction(Ignore = true)]
        public IActionResult ChangePassword()
        {
            return VW();
        }

        /// <summary>
        /// 头像设置页 / The avatar settings page
        /// </summary>
        /// <returns>头像设置视图 / The avatar settings view</returns>
        [PermissionAction(Ignore = true)]
        public IActionResult AvatarSetting()
        {
            return VW();
        }

        /// <summary>
        /// 上传头像（POST）/ Uploads the avatar (POST)
        /// </summary>
        /// <param name="avatarFile">头像文件 / The avatar file</param>
        /// <returns>携带操作结果的视图 / The view carrying the operation result</returns>
        [RecordOperationFilter]
        [PermissionAction(Ignore = true)]
        [HttpPost]
        public IActionResult UploadAvatar(IFormFile avatarFile)
        {
            R operationResult;
            try
            {
                operationResult = DoUploadAvatar(avatarFile);
                return VW(operationResult);
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                operationResult = R.Failure(ex.Message);
                return View(operationResult);
            }
        }

        /// <summary>
        /// 执行头像上传：读取文件字节流并更新当前管理员的头像
        /// Performs the avatar upload: reads the file bytes and updates the current admin's avatar
        /// </summary>
        /// <param name="avatarFile">头像文件 / The avatar file</param>
        /// <returns>操作结果 / The operation result</returns>
        private R DoUploadAvatar(IFormFile avatarFile)
        {
            int adminId = CurrentAdmin!.Id;
            byte[] buffer = avatarFile.GetBytes();

            R operationResult = AdminService.UpdateAdminAvatar(adminId, buffer);
            return operationResult;
        }

    }
}
