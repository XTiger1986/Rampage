using System;

namespace Rampage.Localization
{
	/// <summary>
	/// Localized language resource setting
	/// </summary>
	public class LocalizationConfig
    {
		/// <summary>
		/// language resource root directory path
		/// </summary>
		public string RootDirectory
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public string DefaultModuleName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public string DefaultLanguageName
        {
            get;
            set;
        } = default!;

    }
}
