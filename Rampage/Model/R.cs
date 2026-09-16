using Rampage.Enums;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Model
{
    /// <summary>
    /// Operation Result
    /// </summary>
    public class R
    {

        /// <summary>
        /// Operation Result Code
        /// </summary>
        public virtual int Code
        {
            get;
            set;
        }

        /// <summary>
        /// 操作提示信息
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual string? Message
        {
            get; 
            set;
        }

        /// <summary>
        /// 数据
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual object? Data
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ValidationResult? ValidationResult
        {
            get;
            set;
        }

        /// <summary>
        /// JsonpId
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual string? JsonpId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSuccess
        {
            get { return Code == (int)ResultCode.Success; }
        }

        /// <summary>
        /// 快速创建成功操作结果（Code=0）
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R Success(object? data = null, string ? message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(ResultCode.Success);
            }
            return Create(ResultCode.Success, message, data);
        }

        /// <summary>
        /// 快速创建失败操作结果（Code=10000）
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R Failure(string? message = null, object? data = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(ResultCode.Failure);
            }
            return Create(ResultCode.Failure, message, data);
        }

        /// <summary>
        /// 快速创建失败操作结果
        /// </summary>
        /// <param name="code">自定义错误码</param>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R Failure(int code, string? message = null, object? data = null)
        {
            return Create(code, message, data);
        }
        /// <summary>
        /// 快速创建失败操作结果
        /// </summary>
        /// <param name="code">自定义错误码</param>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R Failure<E>(E code, string? message = null, object? data = null) where E : struct, Enum
        {
            return Create(code, message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static R Create<E>(E code, string? message = null, object? data = null) where E : struct, Enum
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText(code);
            }
            return Create(Convert.ToInt32(code), message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
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
    /// Operation Result
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class R<M> : R where M : class
    {

        /// <summary>
        /// 数据
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new M? Data
        {
            get;
            set;
        }

        /// <summary>
        /// 快速创建成功操作结果（Code=0）
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R<M> Success(M? data = null, string ? message = null)
        {
            return Create(ResultCode.Success, message, data);
        }

        /// <summary>
        /// 快速创建失败操作结果（Code=10000）
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public static R<M> Failure(string? message = null, M? data = null)
        {
            return Create(ResultCode.Failure, message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static R<M> Create<E>(E code, string? message = null, M? data = null) where E : struct, Enum
        {
            if (string.IsNullOrEmpty(message))
            {
                message = EnumReader.GetText<E>(code);
            }
            return Create(Convert.ToInt32(code), message, data);
        }

        /// <summary>
        /// 创建一个操作结果实例
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
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
