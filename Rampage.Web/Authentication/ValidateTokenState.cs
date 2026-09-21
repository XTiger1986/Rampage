using Microsoft.IdentityModel.Tokens;
using System;

namespace Rampage.Web.Authentication
{

    /// <summary>
    /// 令牌校验结果状态 / Token validation result status
    /// </summary>
    public enum ValidateTokenStatus : int
    {
        /// <summary>
        /// 无效（格式错误、签名不通过、令牌用途不符等）/ Invalid (malformed, bad signature, wrong token use, etc.)
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// 有效 / Valid
        /// </summary>
        Valid = 1,

        /// <summary>
        /// 已过期 / Expired
        /// </summary>
        Expired = 2,

        /// <summary>
        /// 签名与有效期均正确，但已被服务端撤销（密码修改、登出、安全戳不匹配等） / Signature and lifetime are valid but the token was revoked server-side (password changed, signed out, security stamp mismatch, etc.)
        /// </summary>
        Revoked = 3
    }

    /// <summary>
    /// 令牌校验结果：状态、有效期、原始令牌与还原的用户信息 / Token validation result: status, lifetime, raw token and the restored user info
    /// </summary>
    public class ValidateTokenState
    {

        /// <summary>
        /// 初始化并创建空的 <see cref="Info"/> / Initializes the instance with an empty <see cref="Info"/>
        /// </summary>
        public ValidateTokenState()
        {
            Info = new JwtAuthenticationInfo();
        }


        /// <summary>
        /// 校验结果状态 / Validation result status
        /// </summary>
        public ValidateTokenStatus Status
        {
            get; set;
        }

        /// <summary>
        /// 已解析的原始令牌（非 JWT 格式时为 null）/ The parsed raw token (null when not a valid JWT format)
        /// </summary>
        public SecurityToken? RawToken
        {
            get; set;
        }

        /// <summary>
        /// 生效时间（本地时间）/ Valid from (local time)
        /// </summary>
        public DateTime ValidFrom
        {
            get;
            set;
        }

        /// <summary>
        /// 失效时间（本地时间）/ Valid to (local time)
        /// </summary>
        public DateTime ValidTo
        {
            get;
            set;
        }

        /// <summary>
        /// 从令牌载荷还原的用户信息 / User info restored from the token payload
        /// </summary>
        public JwtAuthenticationInfo Info
        {
            get; set;
        }


    }
}
