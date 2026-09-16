using Rampage.Enums;
using System;

namespace Rampage.Model
{

    /// <summary>
    /// Common Operation Result Code
    /// 0: Success
    /// </summary>
    public enum ResultCode : int
    {
        /// <summary>
        /// Successful
        /// 成功
        /// </summary>
        [EnumFieldAttribute("Successful")]
        Success = 0,
        /// <summary>
        /// Failed
        /// 失败
        /// </summary>
        [EnumFieldAttribute("Failed")]
        Failure = 1000,

        /// <summary>
        /// Not logged in
        /// 未登录
        /// </summary>
        [EnumFieldAttribute("Not logged in")]
        NotLoggedIn = 4000,
        /// <summary>
        /// Unauthorized
        /// 未授权
        /// </summary>
        [EnumFieldAttribute("Unauthorized")]
        Unauthorized = 4001,
        /// <summary>
        /// Incorrect Password
        /// 密码不正确
        /// </summary>
        [EnumFieldAttribute("Incorrect Password")]
        IncorrectPassword = 4002,
        /// <summary>
        /// Incorrect Password for many times
        /// 多次密码不正确
        /// </summary>
        [EnumFieldAttribute("Incorrect Password")]
        ManyIncorrectPassword = 4003,
        /// <summary>
        /// Account does not exist
        /// 账号不存在
        /// </summary>
        [EnumFieldAttribute("Account does not exist")]
        InvalidAccount = 4004,
        /// <summary>
        /// Account disabled
        /// 账号被禁用
        /// </summary>
        [EnumFieldAttribute("Account disabled")]
        DisabledAccount = 4005,
        /// <summary>
        /// Token is expired
        /// 凭据过期
        /// </summary>
        [EnumFieldAttribute("Token is expired")]
        TokenExpired = 4006,
        /// <summary>
        /// 凭据无效
        /// Invalid token
        /// </summary>
        [EnumFieldAttribute("Invalid token")]
        InvalidToken = 4007,
        /// <summary>
        /// 凭据需要刷新
        /// Token need refresh
        /// </summary>
        [EnumFieldAttribute("Token need refresh")]
        RefreshToken = 4008,

        /// <summary>
        /// Illegal Request
        /// 非法请求
        /// </summary>
        [EnumFieldAttribute("Illegal Request")]
        IllegalRequest = 5000,
        /// <summary>
        /// Illegal Arguments
        /// 参数非法
        /// </summary>
        [EnumFieldAttribute("Illegal Arguments")]
		IllegalArguments = 5001,
        /// <summary>
        /// Operation too Frequent
        /// 操作太频繁
        /// </summary>
        [EnumFieldAttribute("Operation too Frequent")]
        OperationTooFrequent = 5002,
        /// <summary>
        /// Only allow make changes once a day
        /// 一天只能修改一次
        /// </summary>
        [EnumFieldAttribute("Only allow make changes once a day")]
        OneDayOneChangeTime = 5003,
        /// <summary>
        /// fail to verify data
        /// 数据验证失败
        /// </summary>
        [EnumFieldAttribute("Fail to Verify Data")]
        DataVerificationFailed = 5100,
        /// <summary>
        /// data not exists
        /// 数据不存在
        /// </summary>
        [EnumFieldAttribute("Data Not Exists")]
        DataNotExists = 5101,
        /// <summary>
        /// data exists
        /// 数据已经存在
        /// </summary>
        [EnumFieldAttribute("Data Exists")]
        DataExists = 5102,
        /// <summary>
        /// exists associated data
        /// 存在关联数据
        /// </summary>
        [EnumFieldAttribute("Exists Associated Data")]
        ExistsAssociatedData = 5103,
        /// <summary>
        /// Incorrect Verification Code
        /// 验证码错误
        /// </summary>
        [EnumFieldAttribute("Incorrect Verification Code")]
        IncorrectVerificationCode = 5201,
        /// <summary>
        /// Verification Code Expired
        /// 验证码已经过期或失效
        /// </summary>
        [EnumFieldAttribute("Verification Code Expired")]
        VerificationCodeExpired = 5202,
        

    }
}
