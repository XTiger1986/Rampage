using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 模板文件设置（对应 template.json 中 Files 数组的一项）/
    /// Template file setting (one entry of the "Files" array in template.json)
    /// </summary>
    public class TemplateFile
    {
        /// <summary>
        /// 模板文件名 / Template file name (located in the template directory)
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 输出文件名格式 / Output file name pattern
        /// </summary>
        /// <remarks>
        /// 支持表级占位符并可包含相对子目录，如 "Model\@{EntityName}.cs"、"Service\I@{EntityName}Service.cs"。
        /// Supports table-level placeholders and relative subdirectories, e.g. "Model\@{EntityName}.cs".
        /// </remarks>
        public string? OutputFile { get; set; }

    }
}
