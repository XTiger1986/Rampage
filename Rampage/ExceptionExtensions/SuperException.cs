using Rampage.Localization;
using System;

namespace Rampage.ExceptionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    public class SuperException : Exception
    {
        public virtual string ResourceModule
        {
            get; set;
        } = "SuperException";

        /// <summary>
        /// Customize localized key
        /// </summary>
        public virtual string? LocalizerKey
        {
            get; set;
        }

        public virtual object[]? LocalizerArguments
        {
            get; set;
        }

        public SuperException() : base()
        {
        }

        public SuperException(string key) : base(key)
        {
            LocalizerKey = key;
        }

        public SuperException(string key, params object[] args) : base(key)
        {
            LocalizerKey = key;
            LocalizerArguments = args;
        }

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
