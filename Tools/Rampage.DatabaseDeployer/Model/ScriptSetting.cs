using System;

namespace Rampage.DatabaseDeployer.Model
{
    /// <summary>
    /// 脚本全局设置（对应配置节 ScriptSetting）/ Global script settings (mapped from the ScriptSetting configuration section)
    /// </summary>
    public class ScriptSetting
    {

        /// <summary>
        /// 脚本根目录（相对程序集基目录，如 "SqlScripts"）/ Script root directory (relative to the application base directory, e.g. "SqlScripts")
        /// </summary>
        public string? Root
        {
            get;
            set;
        }

        /// <summary>
        /// 脚本文件名版本号匹配正则（必须包含命名分组 Version，如 "^(?&lt;Version&gt;\d+)[^0-9]+?.+?\.sql"）/
        /// Regex that extracts the version from script file names (must contain the named group "Version")
        /// </summary>
        public string? VersionPattern
        {
            get;
            set;
        }

    }
}
