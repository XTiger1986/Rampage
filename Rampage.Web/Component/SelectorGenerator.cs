using Rampage.Utilities;
using System;
using System.Text;

namespace Rampage.Web.Component
{
    public class SelectorGenerator
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="title"></param>
        /// <param name="data"></param>
        /// <param name="isSelected"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
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
