using Microsoft.AspNetCore.Html;
using Rampage.Localization;
using Rampage.Management.Dto;
using System;

namespace Rampage.Management.Web
{
    /// <summary>
    /// 管理系统展示层逻辑工具 / Presentation-layer logic utilities for the management system
    /// </summary>
    public class ManagementLogicUtility
    {

        /// <summary>
        /// 渲染树形菜单为 HTML（公开入口，从第 0 层开始递归）
        /// Renders a menu tree into HTML (public entry that recurses from depth 0)
        /// </summary>
        /// <param name="treeMenuList">树形菜单数据 / The tree-shaped menu data</param>
        /// <param name="builder">单节点 HTML 生成委托：(节点, 默认选中Id, 当前层级, 同级序号, 语言模块, 时区) => HTML / The per-node HTML builder: (node, default selected id, depth, sibling index, language module, time zone) => HTML</param>
        /// <param name="lang">语言模块 / The language module</param>
        /// <param name="ctz">客户端时区 / The client time zone</param>
        /// <param name="defaultId">默认选中的菜单Id / The default selected menu id</param>
        /// <param name="maxDepth">最大渲染深度（与节点自身 Depth 比较），null 表示不限 / The maximum rendering depth (compared against the node's own Depth); null for unlimited</param>
        /// <param name="excludeId">排除的菜单Id，该节点连同其子树一并跳过 / The menu id to exclude; the node and its whole subtree are skipped</param>
        /// <returns>拼接后的 HTML 内容 / The concatenated HTML content</returns>
        public static IHtmlContent RenderMenuTreeHtml(List<ManageMenuDto> treeMenuList, Func<ManageMenuDto, int?, int, int, LocalizationModule, TimeZoneInfo, string> builder, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId = null, int? maxDepth = null, int? excludeId = null)
        {
            return RenderMenuTreeHtml(treeMenuList, builder, lang, ctz, defaultId, maxDepth, 0, excludeId);
        }

        /// <summary>
        /// 递归渲染树形菜单为 HTML / Recursively renders a menu tree into HTML
        /// </summary>
        /// <param name="treeMenuList">树形菜单数据 / The tree-shaped menu data</param>
        /// <param name="depth">当前递归层级 / The current recursion depth</param>
        /// <param name="maxDepth">最大渲染深度，null 表示不限 / The maximum rendering depth; null for unlimited</param>
        /// <param name="builder">单节点 HTML 生成委托 / The per-node HTML builder</param>
        /// <param name="lang">语言模块 / The language module</param>
        /// <param name="ctz">客户端时区 / The client time zone</param>
        /// <param name="defaultId">默认选中的菜单Id / The default selected menu id</param>
        /// <param name="excludeId">排除的菜单Id / The menu id to exclude</param>
        /// <returns>拼接后的 HTML 内容 / The concatenated HTML content</returns>
        private static IHtmlContent RenderMenuTreeHtml(List<ManageMenuDto> treeMenuList, Func<ManageMenuDto, int?, int, int, LocalizationModule, TimeZoneInfo, string> builder, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId, int? maxDepth, int depth, int? excludeId = null)
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < treeMenuList.Count; i++)
            {
                ManageMenuDto item = treeMenuList[i];
                // continue 跳过该节点的同时也跳过其整棵子树 / continue skips the node together with its whole subtree
                if (excludeId != null && item.Id == excludeId.Value)
                {
                    continue;
                }

                // 按"节点自身 Depth"而非当前递归层级判断，且 break 前提是同层列表已按深度有序 / Judged against the node's own Depth (not the recursion level); break assumes siblings are already depth-ordered
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
