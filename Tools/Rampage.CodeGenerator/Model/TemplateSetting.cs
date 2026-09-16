using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 生成语言
    /// </summary>
    public enum GenerateLanguage
    {
        /// <summary>
        /// C#(默认)
        /// </summary>
        CSharp
    }

    /// <summary>
    /// 模板设置
    /// </summary>
    public class TemplateSetting
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 模板目录
        /// </summary>
        public string? DirectoryName { get; set; }

        /// <summary>
        /// 生成语言
        /// </summary>
        public GenerateLanguage Language { get; set; }

        public string? RootNamespace { get; set; }

        /// <summary>
        /// 模板文件集
        /// </summary>
        public List<TemplateFile>? Files { get; set; }

    }
}
