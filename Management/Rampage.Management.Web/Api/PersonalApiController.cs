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
    /// 
    /// </summary>
    [Route("api/personal")]
    public class PersonalApiController : SuperManagementApiController
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
