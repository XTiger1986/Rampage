using System;

namespace Rampage.Web.Theme
{
    /// <summary>
    /// 皮肤（主题）元数据 / Skin (theme) metadata.
    /// </summary>
    /// <remarks>
    /// 宿主在 SuperController.ThemeSetter 委托中按请求返回本类实例（框架本身不做 Cookie 或持久化）；
    /// <see cref="Code"/> 用于将视图路径映射为 /Themes/{Code}/{Controller}/{View}.cshtml。
    /// </remarks>
    public class WebSkin
    {

        /// <summary>
        /// 皮肤名称 / The skin display name.
        /// </summary>
        public string? Name
        {
            get; set;
        }

        /// <summary>
        /// 皮肤代码（目录名），决定视图路径 /Themes/{Code}/{Controller}/{View}.cshtml / The skin code (folder name) used to map view paths to /Themes/{Code}/{Controller}/{View}.cshtml.
        /// </summary>
        public string? Code
        {
            get; set;
        }

        /// <summary>
        /// 作者 / The skin author.
        /// </summary>
        public string? Author
        {
            get; set;
        }

        /// <summary>
        /// 版权信息 / The copyright information.
        /// </summary>
        public string? Copyright
        {
            get; set;
        }

        /// <summary>
        /// 版本号 / The skin version.
        /// </summary>
        public string? Version
        {
            get; set;
        }

        /// <summary>
        /// 发布时间 / The publish time.
        /// </summary>
        public DateTime PublishTime
        {
            get; set;
        }

        /// <summary>
        /// 更新时间 / The update time.
        /// </summary>
        public DateTime UpdateTime
        {
            get; set;
        }

    }
}
