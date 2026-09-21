using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 生成语言 / Target code language
    /// </summary>
    public enum GenerateLanguage
    {
        /// <summary>
        /// C#(默认) / C# (default)
        /// </summary>
        CSharp
    }

    /// <summary>
    /// 模板设置（对应模板目录下的 template.json）/ Template setting (mapped from template.json in a template directory)
    /// </summary>
    public class TemplateSetting
    {
        /// <summary>
        /// 模板名称 / Template name (shown when selecting a template)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 模板目录名（扫描模板根目录时自动填充为子目录名）/ Template directory name (auto-filled with the subdirectory name while scanning)
        /// </summary>
        public string? DirectoryName { get; set; }

        /// <summary>
        /// 生成语言 / Target code language
        /// </summary>
        public GenerateLanguage Language { get; set; }

        /// <summary>
        /// 根命名空间，替换模板中的 @{Namespace} 占位符；未配置时回退为默认值 /
        /// Root namespace replacing the @{Namespace} placeholder; falls back to a default when unset
        /// </summary>
        public string? RootNamespace { get; set; }

        /// <summary>
        /// 模板文件集 / Template file list
        /// </summary>
        public List<TemplateFile>? Files { get; set; }

    }
}
