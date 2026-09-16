using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rampage.Log;
using Rampage.Management.Dto;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    [PermissionLoggedInFilter]
    public class PersonalController : SuperManagementController
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Ignore = true)]
        public IActionResult EditProfile()
        {
            AdminDto? admin = AdminService.GetAdminById(CurrentAdmin!.Id);
            return VW(admin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Ignore = true)]
        public IActionResult ChangePassword()
        {
            return VW();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Ignore = true)]
        public IActionResult AvatarSetting()
        {
            return VW();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="avatarFile"></param>
        /// <returns></returns>
        private R DoUploadAvatar(IFormFile avatarFile)
        {
            int adminId = CurrentAdmin!.Id;
            byte[] buffer = avatarFile.GetBytes();

            R operationResult = AdminService.UpdateAdminAvatar(adminId, buffer);
            return operationResult;
        }

    }
}
