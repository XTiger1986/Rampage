using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 代码生成全局设置（对应配置节 GenerateSetting）/ Global generation settings (mapped from the GenerateSetting section)
    /// </summary>
    public class GenerateSetting
    {

        /// <summary>
        /// 模板根目录（相对程序集基目录，如 "Templates"）/ Template root directory (relative to the application base directory, e.g. "Templates")
        /// </summary>
        public string? TemplateRoot
        {
            get;
            set;
        }

        /// <summary>
        /// 代码输出根目录（相对程序集基目录，如 "Outputs"，输出时再拼接模板目录名）/
        /// Output root directory (relative to the application base directory, e.g. "Outputs"; the template directory name is appended)
        /// </summary>
        public string? OutputRoot
        {
            get;
            set;
        }

    }
}
