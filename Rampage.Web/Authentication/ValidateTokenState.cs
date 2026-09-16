using Microsoft.IdentityModel.Tokens;
using System;

namespace Rampage.Web.Authentication
{

    public enum ValidateTokenStatus : int
    {
        Invalid = 0,

        Valid = 1,

        Expired = 2,

        /// <summary>
        /// 签名与有效期均正确，但已被服务端撤销（密码修改、登出、安全戳不匹配等）
        /// </summary>
        Revoked = 3
    }

    public class ValidateTokenState
    {

        public ValidateTokenState()
        {
            Info = new JwtAuthenticationInfo();
        }


        public ValidateTokenStatus Status
        {
            get; set;
        }

        public SecurityToken? RawToken
        {
            get; set;
        }

        /// <summary>
        /// Valid Time From (Local Time)
        /// </summary>
        public DateTime ValidFrom
        {
            get;
            set;
        }

        /// <summary>
        /// Valid Time To (Local Time)
        /// </summary>
        public DateTime ValidTo
        {
            get;
            set;
        }

        public JwtAuthenticationInfo Info
        {
            get; set;
        }


    }
}
