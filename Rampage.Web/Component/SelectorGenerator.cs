using Rampage.Utilities;
using System;
using System.Text;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 选择器 HTML 生成器，输出选项（option）片段 / Selector HTML generator that emits option fragments.
    /// </summary>
    public class SelectorGenerator
    {

        /// <summary>
        /// 构建单个 option 标签的 HTML / Builds the HTML of a single option tag.
        /// </summary>
        /// <param name="text">显示文本 / The display text.</param>
        /// <param name="value">选项值 / The option value.</param>
        /// <param name="title">title 属性内容 / The title attribute.</param>
        /// <param name="data">data-wrap 属性内容 / The data-wrap attribute.</param>
        /// <param name="isSelected">是否选中 / Whether the option is selected.</param>
        /// <param name="depth">层级深度，大于 0 时在文本前加层级缩进符 / The depth; when greater than 0 a padding prefix is prepended to the text.</param>
        /// <returns>option 标签 HTML / The option tag HTML.</returns>
        public static string BuildOptionHtml(string text, string value, string title, string data, bool isSelected, int depth)
        {
            var sb = new StringBuilder();

            string depthPadding = depth > 0 ? "|" + StringTool.LoopString("-", depth * 4) : "";

            sb.Append(@$"<option value=""{value}"" title=""{title}"" data-wrap=""{data}""");
            if (isSelected)
            {
                sb.Append(@$" selected=""selected""");
            }
            sb.Append(@$">{depthPadding}{text}</option>");

            return sb.ToString();
        }

    }
}
