using Rampage.Enums;
using System;

namespace Rampage.Model
{

    /// <summary>
    /// 通用操作结果码（0=成功，4xxx=登录授权类，5xxx=请求与数据类） / Common operation result codes (0 success, 4xxx authentication, 5xxx request/data errors)
    /// </summary>
    public enum ResultCode : int
    {
        /// <summary>
        /// 成功 / Successful
        /// </summary>
        [EnumFieldAttribute("Successful")]
        Success = 0,
        /// <summary>
        /// 失败 / Failed
        /// </summary>
        [EnumFieldAttribute("Failed")]
        Failure = 1000,

        /// <summary>
        /// 未登录 / Not logged in
        /// </summary>
        [EnumFieldAttribute("Not logged in")]
        NotLoggedIn = 4000,
        /// <summary>
        /// 未授权 / Unauthorized
        /// </summary>
        [EnumFieldAttribute("Unauthorized")]
        Unauthorized = 4001,
        /// <summary>
        /// 密码不正确 / Incorrect password
        /// </summary>
        [EnumFieldAttribute("Incorrect Password")]
        IncorrectPassword = 4002,
        /// <summary>
        /// 多次密码不正确 / Incorrect password entered too many times
        /// </summary>
        [EnumFieldAttribute("Incorrect Password")]
        ManyIncorrectPassword = 4003,
        /// <summary>
        /// 账号不存在 / Account does not exist
        /// </summary>
        [EnumFieldAttribute("Account does not exist")]
        InvalidAccount = 4004,
        /// <summary>
        /// 账号被禁用 / Account disabled
        /// </summary>
        [EnumFieldAttribute("Account disabled")]
        DisabledAccount = 4005,
        /// <summary>
        /// 凭据过期 / Token expired
        /// </summary>
        [EnumFieldAttribute("Token is expired")]
        TokenExpired = 4006,
        /// <summary>
        /// 凭据无效 / Invalid token
        /// </summary>
        [EnumFieldAttribute("Invalid token")]
        InvalidToken = 4007,
        /// <summary>
        /// 凭据需要刷新 / Token needs refresh
        /// </summary>
        [EnumFieldAttribute("Token need refresh")]
        RefreshToken = 4008,

        /// <summary>
        /// 非法请求 / Illegal request
        /// </summary>
        [EnumFieldAttribute("Illegal Request")]
        IllegalRequest = 5000,
        /// <summary>
        /// 参数非法 / Illegal arguments
        /// </summary>
        [EnumFieldAttribute("Illegal Arguments")]
		IllegalArguments = 5001,
        /// <summary>
        /// 操作太频繁 / Operation too frequent
        /// </summary>
        [EnumFieldAttribute("Operation too Frequent")]
        OperationTooFrequent = 5002,
        /// <summary>
        /// 一天只能修改一次 / Only one change allowed per day
        /// </summary>
        [EnumFieldAttribute("Only allow make changes once a day")]
        OneDayOneChangeTime = 5003,
        /// <summary>
        /// 数据验证失败 / Data verification failed
        /// </summary>
        [EnumFieldAttribute("Fail to Verify Data")]
        DataVerificationFailed = 5100,
        /// <summary>
        /// 数据不存在 / Data does not exist
        /// </summary>
        [EnumFieldAttribute("Data Not Exists")]
        DataNotExists = 5101,
        /// <summary>
        /// 数据已经存在 / Data already exists
        /// </summary>
        [EnumFieldAttribute("Data Exists")]
        DataExists = 5102,
        /// <summary>
        /// 存在关联数据 / Associated data exists
        /// </summary>
        [EnumFieldAttribute("Exists Associated Data")]
        ExistsAssociatedData = 5103,
        /// <summary>
        /// 验证码错误 / Incorrect verification code
        /// </summary>
        [EnumFieldAttribute("Incorrect Verification Code")]
        IncorrectVerificationCode = 5201,
        /// <summary>
        /// 验证码已经过期或失效 / Verification code expired
        /// </summary>
        [EnumFieldAttribute("Verification Code Expired")]
        VerificationCodeExpired = 5202,
        

    }
}
