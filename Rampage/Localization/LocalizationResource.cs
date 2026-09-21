using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Localization
{
	/// <summary>
	/// 单语言资源：对应模块目录下的一个 {语言}.json 文件（ConfigurationManager 装载，reloadOnChange 热更新）。
	/// / A single-language resource backed by one {language}.json file in the module directory (loaded via ConfigurationManager with reloadOnChange).
	/// </summary>
	public class LocalizationResource
    {
        /// <summary>
        /// 所属模块名。
        /// / The owning module name.
        /// </summary>
        public string ModuleName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 语言名（即文件名，不含扩展名，如 zh-CN）。
        /// / Culture name (the file name without extension, e.g. zh-CN).
        /// </summary>
        public string CultureName
        {
            get; 
            internal set;
        } = default!;

        /// <summary>
        /// 底层配置：JSON 键值读取与热更新由 IConfiguration 提供。
        /// / The underlying IConfiguration providing JSON key lookup and hot reload.
        /// </summary>
        public IConfiguration Resource
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 读取原始键值。
        /// / Read the raw value for the key.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="defaultText">未命中时返回的默认文本，可为 null / text returned on miss, may be null</param>
        /// <returns>键值或 defaultText / the value, or defaultText on miss</returns>
        public string? Get(string key, string? defaultText = null)
        {
            return Resource[key!] ?? defaultText;
        }

        /// <summary>
        /// 读取键值并用 string.Format 格式化；键不存在时按空字符串格式化（返回空串）。
        /// / Read the value and format it via string.Format; a missing key formats an empty string.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="args">格式化参数 / format arguments</param>
        /// <returns>格式化后的文本 / the formatted text</returns>
        public string GetText(string key, params object?[] args)
        {
            string text = Resource[key] ?? string.Empty;
            return string.Format(text, args);
        }


    }
}
