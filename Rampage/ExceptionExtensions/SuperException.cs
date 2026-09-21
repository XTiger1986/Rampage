using Rampage.Localization;
using System;

namespace Rampage.ExceptionExtensions
{
    /// <summary>
    /// 支持本地化的业务异常 / Localizable business exception
    /// </summary>
    /// <remarks>
    /// Message 按 ResourceModule（默认 "SuperException"）本地化模块解析 LocalizerKey（可带格式化参数）；
    /// 模块不存在时直接返回 LocalizerKey 本身；未设置 LocalizerKey 时返回基类 Message。
    /// Message resolves LocalizerKey (with optional format arguments) through the localization module named by
    /// ResourceModule (default "SuperException"); when the module is missing the key itself is returned, and without
    /// a LocalizerKey the base Message is used.
    /// </remarks>
    public class SuperException : Exception
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
        public SuperException() : base()
        {
        }

        /// <summary>
        /// 以本地化键创建异常（键同时作为基类消息） / Creates the exception with a localization key that also serves as the base message
        /// </summary>
        /// <param name="key">本地化键 / Localization key</param>
        public SuperException(string key) : base(key)
        {
            LocalizerKey = key;
        }

        /// <summary>
        /// 以本地化键与格式化参数创建异常 / Creates the exception with a localization key and format arguments
        /// </summary>
        /// <param name="key">本地化键 / Localization key</param>
        /// <param name="args">格式化参数 / Format arguments</param>
        public SuperException(string key, params object[] args) : base(key)
        {
            LocalizerKey = key;
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
