using System;

namespace Rampage.Web.Theme
{
    /// <summary>
    /// 网页主题管理器（当前为空的占位实现）/ Web page theme manager (currently an empty placeholder).
    /// </summary>
    /// <remarks>
    /// 本类暂无任何实现；框架的主题切换不经过此类的实例，也没有 Cookie 或其他持久化机制——
    /// 主题由宿主通过 SuperController.ThemeSetter 委托按请求返回 <see cref="WebSkin"/>，
    /// 视图路径映射为 /Themes/{Code}/{Controller}/{View}.cshtml。
    /// </remarks>
    public class ThemeManager
    {

    }
}
