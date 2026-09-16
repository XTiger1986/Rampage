using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Localization
{
	/// <summary>
	/// Localized language resources
	/// </summary>
	public class LocalizationResource
    {
        /// <summary>
        /// 
        /// </summary>
        public string ModuleName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public string CultureName
        {
            get; 
            internal set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Resource
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public string? Get(string key, string? defaultText = null)
        {
            return Resource[key!] ?? defaultText;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetText(string key, params object?[] args)
        {
            string text = Resource[key] ?? string.Empty;
            return string.Format(text, args);
        }


    }
}
