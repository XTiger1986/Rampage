using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.Log;
using Rampage.Management.Configuration;
using Rampage.Management.Dto;
using Rampage.Management.VO;
using Rampage.Model;
using Rampage.Validation;
using System;

namespace Rampage.Management.Web.Api
{

    [Route("api/passport")]
    [AllowAnonymousAttribute]
    public class PassportApiController : SuperManagementApiController
    {

        [HttpPost("login")]
        public R QueryLogin([ValidAttribute][FromBody] LoginAccountInput input)
        {
            try
            {
                string account = input.Account!;
                string password = input.Password!;
                bool isRemember = input.IsRemember;
                double? rememberDays = isRemember ? ManagementConfig.RememberDays : null;

                R<AdminDto> operationResult = AdminService.Login(account, password, rememberDays);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R<AdminDto>.Failure(ex.Message);
            }
        }

        [HttpPost("logout")]
        public R QueryLogout()
        {
            try
            {
                return AdminService.Logout();
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

    }
}
