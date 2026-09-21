using System;

namespace Rampage.Web.Authentication
{
	/// <summary>
	/// JWT 用户信息载体：签发令牌时写入载荷，校验令牌时从载荷还原 / JWT user info carrier: written into the token payload when issuing and restored from it when validating
	/// </summary>
	public class JwtAuthenticationInfo
	{

        /// <summary>
        /// 用户唯一标识（nameid Claim）/ User unique identifier (nameid claim)
        /// </summary>
        public string? NameId
        {
            get;
            set;
        }

        /// <summary>
        /// 用户名（name Claim）/ User name (name claim)
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// 唯一用户名（unique_name Claim）/ Unique user name (unique_name claim)
        /// </summary>
        public string? UniqueName
        {
            get;
            set;
        }

        /// <summary>
        /// 昵称（nickname Claim）/ Nickname (nickname claim)
        /// </summary>
        public string? Nickname
        {
            get;
            set;
        }

        /// <summary>
        /// 性别（gender Claim）/ Gender (gender claim)
        /// </summary>
        public string? Gender
        {
            get;
            set;
        }

        /// <summary>
        /// 邮箱（email Claim）/ Email address (email claim)
        /// </summary>
        public string? Email
        {
            get;
            set;
        }

        /// <summary>
        /// 手机号（phone_number Claim）/ Phone number (phone_number claim)
        /// </summary>
        public string? PhoneNumber
        {
            get;
            set;
        }

        /// <summary>
        /// 随机数（nonce Claim，可用于防止重放）/ Nonce (nonce claim, usable for replay prevention)
        /// </summary>
        public string? Nonce
        {
            get;
            set;
        }


    }
}
