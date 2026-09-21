using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 布尔选择器，为 check/radio 形式的布尔表单项提供数据 / Boolean selector that provides data for boolean form items such as check and radio.
    /// </summary>
    public class BooleanSelector
    {

        #region Parameters

        /// <summary>
        /// 表单项 id，如果是 check、radio，则此值为每一项的前缀 / The form item id; for check/radio controls this value acts as the prefix of every item.
        /// </summary>
        public string? FormItemId
        {
            get;
            set;
        }

        /// <summary>
        /// 表单项名称 / The form item name.
        /// </summary>
        public string? FormItemName
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义样式名（class）/ Custom CSS class name.
        /// </summary>
        public string? Css
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义样式（内联 style）/ Custom inline style.
        /// </summary>
        public string? Style
        {
            get;
            set;
        }

        /// <summary>
        /// 默认项文本 / The text of the default option.
        /// </summary>
        public string? DefaultText
        {
            get;
            set;
        }

        /// <summary>
        /// 默认项的值 / The value of the default option.
        /// </summary>
        public object? DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值（true/false）/ The value of the selected item (true/false).
        /// </summary>
        public bool SelectedValue
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// 初始化布尔选择器 / Initializes a new instance of the <see cref="BooleanSelector"/> class.
        /// </summary>
        public BooleanSelector()
        {

        }

        /// <summary>
        /// 根据当前属性值初始化选择器状态 / Initializes the selector state from the current property values.
        /// </summary>
        /// <remarks>仅依据 <see cref="DefaultText"/> 是否为 null 设置 <see cref="UseDefault"/>；应在各属性赋值完成后调用。</remarks>
        public void Init()
        {
            if (this.DefaultText != null)
            {
                this.UseDefault = true;
            }
            else
            {
                this.UseDefault = false;
            }
        }

        /// <summary>
        /// 是否使用默认项，由 <see cref="Init"/> 依据 <see cref="DefaultText"/> 设置 / Whether the default option is used; set by <see cref="Init"/> based on <see cref="DefaultText"/>.
        /// </summary>
        public bool UseDefault
        {
            get;
            private set;
        }

    }
}
