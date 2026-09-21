using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rampage.Log;
using Rampage.Management.Dto;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Management.VO;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Utilities;
using Rampage.Validation;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Management.Web.Api
{
    /// <summary>
    /// 个人中心 API 控制器（路由 api/personal）：维护当前管理员自身的资料、密码与头像，不纳入权限资源（仅需登录）
    /// Personal center API controller (route api/personal): maintains the current admin's own profile, password and avatar; excluded from permission resources (login only)
    /// </summary>
    [Route("api/personal")]
    public class PersonalApiController : SuperManagementApiController
    {

        /// <summary>
        /// 保存当前管理员的个人资料 / Saves the current admin's profile
        /// </summary>
        /// <param name="input">资料输入 / The profile input</param>
        /// <returns>操作结果 / The operation result</returns>
        /// <remarks>目标固定为当前登录管理员；输入属性覆盖拷贝到管理员数据后清空 Password，避免经由本接口改动密码。</remarks>
        [RecordOperationFilter]
        [PermissionAction(Ignore = true)]
        [HttpPost("profile/save")]
        public R QuerySaveAdminProfile([ValidAttribute] AdminPersonalInput input)
        {
            try
            {
                AdminDto admin = AdminService.GetAdminById(CurrentAdmin!.Id)!;
                ReflectionTool.CopyProperties(input, admin, true);
                admin.Password = string.Empty;
                R operationResult = AdminService.UpdateAdmin(admin);

                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 修改当前管理员的登录密码 / Changes the current admin's password
        /// </summary>
        /// <param name="input">改密输入（新密码 Password 与原密码 VerifyPassword）/ The change-password input (new Password and original VerifyPassword)</param>
        /// <returns>原密码校验失败返回 IncorrectPassword，成功返回操作结果 / Returns IncorrectPassword when the original password mismatches; otherwise the operation result</returns>
        [RecordOperationFilter]
        [PermissionAction(Ignore = true)]
        [HttpPost("password/change")]
        public R QueryChangePassword([ValidAttribute] ChangePasswordInput input)
        {
            try
            {
                int adminId = CurrentAdmin!.Id;
                string newPassword = input.Password!;
                string verifyPassword = input.VerifyPassword!;
                if (!AdminService.CheckAdminPassword(adminId, verifyPassword))
                {
                    return R.Failure(ResultCode.IncorrectPassword);
                }

                R operationResult = AdminService.UpdateAdminPassword(adminId, newPassword);

                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 上传当前管理员的头像 / Uploads the current admin's avatar
        /// </summary>
        /// <param name="avatarFile">头像文件 / The avatar file</param>
        /// <returns>操作结果 / The operation result</returns>
        [RecordOperationFilter]
        [PermissionAction(Ignore = true)]
        [HttpPost("avatar/upload")]
        public R QueryUploadAvatar(IFormFile avatarFile)
        {
            try
            {
                int adminId = CurrentAdmin!.Id;
                byte[] buffer = avatarFile.GetBytes();

                R operationResult = AdminService.UpdateAdminAvatar(adminId, buffer);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

    }
}
