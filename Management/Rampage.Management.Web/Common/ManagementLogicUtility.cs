using Microsoft.AspNetCore.Html;
using Rampage.Localization;
using Rampage.Management.Dto;
using System;

namespace Rampage.Management.Web
{
    public class ManagementLogicUtility
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="treeMenuList"></param>
        /// <param name="maxDepth"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHtmlContent RenderMenuTreeHtml(List<ManageMenuDto> treeMenuList, Func<ManageMenuDto, int?, int, int, LocalizationModule, TimeZoneInfo, string> builder, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId = null, int? maxDepth = null, int? excludeId = null)
        {
            return RenderMenuTreeHtml(treeMenuList, builder, lang, ctz, defaultId, maxDepth, 0, excludeId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="treeMenuList"></param>
        /// <param name="depth"></param>
        /// <param name="maxDepth"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static IHtmlContent RenderMenuTreeHtml(List<ManageMenuDto> treeMenuList, Func<ManageMenuDto, int?, int, int, LocalizationModule, TimeZoneInfo, string> builder, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId, int? maxDepth, int depth, int? excludeId = null)
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < treeMenuList.Count; i++)
            {
                ManageMenuDto item = treeMenuList[i];
                if (excludeId != null && item.Id == excludeId.Value)
                {
                    continue;
                }

                if (maxDepth != null && item.Depth > maxDepth)
                {
                    break;
                }

                string itemHtml = builder(item, defaultId, depth, i, lang, ctz);
                sb.Append(itemHtml);

                if (item.MenuList != null && item.MenuList.Count > 0)
                {
                    sb.Append(RenderMenuTreeHtml(item.MenuList, builder, lang, ctz, defaultId, maxDepth, depth + 1));
                }
            }

            return new HtmlString(sb.ToString());
        }


    }
}
