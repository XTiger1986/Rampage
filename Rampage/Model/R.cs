using Rampage.Enums;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Model
{
    /// <summary>
    /// 操作结果（Code=0 表示成功，非 0 为对应错误码） / Operation result (Code 0 means success, non-zero is the error code)
    /// </summary>
    public class R
    {

        /// <summary>
        /// 操作结果码（见 <see cref="ResultCode"/>） / Operation result code (see <see cref="ResultCode"/>)
        /// </summary>
        public virtual int Code
        {
            get;
            set;
        }

        /// <summary>
        /// 操作提示信息 / Operation message
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual string? Message
        {
            get; 
            set;
        }

        /// <summary>
        /// 返回数据 / Payload data
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual object? Data
        {
            get;
            set;
        }

        /// <summary>
        /// 数据验证结果（无验证失败时为 null） / Validation result, null when there is nothing to report
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ValidationResult? ValidationResult
        {
            get;
            set;
        }

        /// <summary>
        /// Jsonp 回调标识 / Jsonp callback id
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual string? JsonpId
        {
            get;
            set;
        }

        /// <summary>
        /// 是否成功（Code == ResultCode.Success，即 0） / Whether the operation succeeded (Code equals ResultCode.Success, i.e. 0)
        /// </summary>
        public bool IsSuccess
        {
            get { return Code == (int)ResultCode.Success; }
        }

        /// <summary>
        /// 快速创建成功结果（Code=0），未传 message 时使用 ResultCode.Success 的本地化文本 / Creates a success result (Code=0); the localized text of ResultCode.Success is used when message is omitted
        /// </summary>
        /// <param name="data">返回数据 / Payload data</param>
        /// <param name="message">提示信息 / Message</param>
        /// <returns>成功结果实例 / The success result</returns>
        public static R Success(object? data = null, string ? message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(ResultCode.Success);
            }
            return Create(ResultCode.Success, message, data);
        }

        /// <summary>
        /// 快速创建失败结果（Code=1000，即 ResultCode.Failure） / Creates a failure result (Code=1000, i.e. ResultCode.Failure)
        /// </summary>
        /// <param name="message">提示信息，为空时使用 ResultCode.Failure 的本地化文本 / Message; the localized text of ResultCode.Failure is used when empty</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>失败结果实例 / The failure result</returns>
        public static R Failure(string? message = null, object? data = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(ResultCode.Failure);
            }
            return Create(ResultCode.Failure, message, data);
        }

        /// <summary>
        /// 以自定义数字错误码快速创建失败结果 / Creates a failure result with a custom numeric error code
        /// </summary>
        /// <param name="code">自定义错误码 / Custom error code</param>
        /// <param name="message">提示信息，为空时输出空串 / Message; empty string when omitted</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>失败结果实例 / The failure result</returns>
        public static R Failure(int code, string? message = null, object? data = null)
        {
            return Create(code, message, data);
        }
        /// <summary>
        /// 以枚举错误码快速创建失败结果 / Creates a failure result with an enum error code
        /// </summary>
        /// <typeparam name="E">错误码枚举类型 / Error code enum type</typeparam>
        /// <param name="code">枚举错误码 / Enum error code</param>
        /// <param name="message">提示信息，为空时使用枚举的本地化文本 / Message; the enum's localized text is used when empty</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>失败结果实例 / The failure result</returns>
        public static R Failure<E>(E code, string? message = null, object? data = null) where E : struct, Enum
        {
            return Create(code, message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例，未传 message 时使用枚举的本地化文本 / Creates a result instance; the enum's localized text is used when message is omitted
        /// </summary>
        /// <typeparam name="E">结果码枚举类型 / Result code enum type</typeparam>
        /// <param name="code">结果码 / Result code</param>
        /// <param name="message">提示信息 / Message</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>操作结果实例 / The created result</returns>
        public static R Create<E>(E code, string? message = null, object? data = null) where E : struct, Enum
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(code);
            }
            return Create(Convert.ToInt32(code), message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例，message 为 null 时输出空串 / Creates a result instance; message becomes an empty string when null
        /// </summary>
        /// <param name="code">结果码 / Result code</param>
        /// <param name="message">提示信息 / Message</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>操作结果实例 / The created result</returns>
        public static R Create(int code, string? message = null, object? data = null)
        {
            if (message == null)
            {
                message = string.Empty;
            }
            R opResult = new R();
            opResult.Code = code;
            opResult.Message = message;
            opResult.Data = data;

            return opResult;
        }


    }

    /// <summary>
    /// 强类型操作结果（Code=0 表示成功，非 0 为对应错误码） / Strongly typed operation result (Code 0 means success, non-zero is the error code)
    /// </summary>
    /// <typeparam name="M">负载数据类型（引用类型） / Payload data type (reference type)</typeparam>
    public class R<M> : R where M : class
    {

        /// <summary>
        /// 强类型返回数据，隐藏基类同名的 object 版本 / Strongly typed payload data, hiding the base class object version
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new M? Data
        {
            get;
            set;
        }

        /// <summary>
        /// 快速创建成功结果（Code=0） / Creates a success result (Code=0)
        /// </summary>
        /// <param name="data">返回数据 / Payload data</param>
        /// <param name="message">提示信息，为空时使用 ResultCode.Success 的本地化文本 / Message; the localized text of ResultCode.Success is used when empty</param>
        /// <returns>成功结果实例 / The success result</returns>
        public static R<M> Success(M? data = null, string ? message = null)
        {
            return Create(ResultCode.Success, message, data);
        }

        /// <summary>
        /// 快速创建失败结果（Code=1000，即 ResultCode.Failure） / Creates a failure result (Code=1000, i.e. ResultCode.Failure)
        /// </summary>
        /// <param name="message">提示信息，为空时使用 ResultCode.Failure 的本地化文本 / Message; the localized text of ResultCode.Failure is used when empty</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>失败结果实例 / The failure result</returns>
        public static R<M> Failure(string? message = null, M? data = null)
        {
            return Create(ResultCode.Failure, message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例，未传 message 时使用枚举的本地化文本 / Creates a result instance; the enum's localized text is used when message is omitted
        /// </summary>
        /// <typeparam name="E">结果码枚举类型 / Result code enum type</typeparam>
        /// <param name="code">结果码 / Result code</param>
        /// <param name="message">提示信息 / Message</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>操作结果实例 / The created result</returns>
        public static R<M> Create<E>(E code, string? message = null, M? data = null) where E : struct, Enum
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText<E>(code);
            }
            return Create(Convert.ToInt32(code), message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例，message 为 null 时输出空串 / Creates a result instance; message becomes an empty string when null
        /// </summary>
        /// <param name="code">结果码 / Result code</param>
        /// <param name="message">提示信息 / Message</param>
        /// <param name="data">返回数据 / Payload data</param>
        /// <returns>操作结果实例 / The created result</returns>
        public static R<M> Create(int code, string? message = null, M? data = null)
        {
            if (message == null)
            {
                message = string.Empty;
            }
            R<M> opResult = new R<M>();
            opResult.Code = code;
            opResult.Message = message;
            opResult.Data = data;

            return opResult;
        }

    }

}
