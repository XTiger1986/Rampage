using Rampage.Localization;
using System;
using System.Net;

namespace Rampage.ExceptionExtensions
{
    public class SuperHttpException : HttpRequestException
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

        public SuperHttpException() : base()
        {
        }

        public SuperHttpException(string key) : base(key)
        {
            LocalizerKey = key;
        }

        public SuperHttpException(string key, HttpStatusCode? statusCode) : base(key, null, statusCode)
        {
            LocalizerKey = key;
        }

        public SuperHttpException(string key, HttpStatusCode? statusCode, params object[] args) : base(key, null, statusCode)
        {
            LocalizerKey = key;
            LocalizerArguments = args;
        }

        public SuperHttpException(HttpStatusCode? statusCode, params object[] args) : base("", null, statusCode)
        {
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
