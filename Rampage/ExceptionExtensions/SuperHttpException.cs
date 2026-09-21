using Rampage.Localization;
using System;
using System.Net;

namespace Rampage.ExceptionExtensions
{
    /// <summary>
    /// 支持本地化的 HTTP 请求异常 / Localizable HTTP request exception
    /// </summary>
    /// <remarks>
    /// Message 按 ResourceModule（默认 "SuperException"）本地化模块解析 LocalizerKey（可带格式化参数）；
    /// 模块不存在时直接返回 LocalizerKey 本身；未设置 LocalizerKey 时返回基类 Message。
    /// Message resolves LocalizerKey (with optional format arguments) through the localization module named by
    /// ResourceModule (default "SuperException"); when the module is missing the key itself is returned, and without
    /// a LocalizerKey the base Message is used.
    /// </remarks>
    public class SuperHttpException : HttpRequestException
    {

        /// <summary>
        /// 本地化资源模块名，默认 SuperException / Localization resource module name, SuperException by default
        /// </summary>
        public virtual string ResourceModule
        {
            get; set;
        } = "SuperException";

        /// <summary>
        /// 自定义本地化键 / Custom localization key
        /// </summary>
        public virtual string? LocalizerKey
        {
            get; set;
        }

        /// <summary>
        /// 本地化文本的格式化参数 / Format arguments for the localized text
        /// </summary>
        public virtual object[]? LocalizerArguments
        {
            get; set;
        }

        /// <summary>
        /// 创建空异常 / Creates an empty exception
        /// </summary>
        public SuperHttpException() : base()
        {
        }

        /// <summary>
        /// 以本地化键创建异常（键同时作为基类消息） / Creates the exception with a localization key that also serves as the base message
        /// </summary>
        /// <param name="key">本地化键 / Localization key</param>
        public SuperHttpException(string key) : base(key)
        {
            LocalizerKey = key;
        }

        /// <summary>
        /// 以本地化键与 HTTP 状态码创建异常 / Creates the exception with a localization key and an HTTP status code
        /// </summary>
        /// <param name="key">本地化键 / Localization key</param>
        /// <param name="statusCode">HTTP 状态码 / HTTP status code</param>
        public SuperHttpException(string key, HttpStatusCode? statusCode) : base(key, null, statusCode)
        {
            LocalizerKey = key;
        }

        /// <summary>
        /// 以本地化键、HTTP 状态码与格式化参数创建异常 / Creates the exception with a localization key, an HTTP status code and format arguments
        /// </summary>
        /// <param name="key">本地化键 / Localization key</param>
        /// <param name="statusCode">HTTP 状态码 / HTTP status code</param>
        /// <param name="args">格式化参数 / Format arguments</param>
        public SuperHttpException(string key, HttpStatusCode? statusCode, params object[] args) : base(key, null, statusCode)
        {
            LocalizerKey = key;
            LocalizerArguments = args;
        }

        /// <summary>
        /// 仅以 HTTP 状态码与格式化参数创建异常（不设置本地化键，基类消息为空串） / Creates the exception with only a status code and format arguments (no localization key; the base message is an empty string)
        /// </summary>
        /// <param name="statusCode">HTTP 状态码 / HTTP status code</param>
        /// <param name="args">格式化参数 / Format arguments</param>
        public SuperHttpException(HttpStatusCode? statusCode, params object[] args) : base("", null, statusCode)
        {
            LocalizerArguments = args;
        }

        /// <summary>
        /// 获取本地化后的异常消息 / Gets the localized exception message
        /// </summary>
        public override string Message
        {
            get
            {
                if (string.IsNullOrEmpty(LocalizerKey))
                {
                    return base.Message;
                }
                string message;
                LocalizationModule? localizationModule = LocalizationManager.GetModule(ResourceModule);
                if (localizationModule != null)
                {
                    if (LocalizerArguments != null && LocalizerArguments.Any())
                    {
                        message = localizationModule.GetText(LocalizerKey!, LocalizerArguments);
                    }
                    else
                    {
                        message = localizationModule.Get(LocalizerKey!) ?? string.Empty;
                    }
                }
                else
                {
                    message = LocalizerKey!;
                }
                return message;
            }
        }

    }
}
