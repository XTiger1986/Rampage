using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 
    /// </summary>
    public class BooleanSelector
    {

        #region Parameters

        /// <summary>
        /// 表单项id，如果是check、radio，则此值为每一项的前缀
        /// </summary>
        public string? FormItemId
        {
            get;
            set;
        }

        /// <summary>
        /// 表单项名称
        /// </summary>
        public string? FormItemName
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义样式名
        /// </summary>
        public string? Css
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义样式
        /// </summary>
        public string? Style
        {
            get;
            set;
        }

        /// <summary>
        /// 默认项文本
        /// </summary>
        public string? DefaultText
        {
            get;
            set;
        }

        /// <summary>
        /// 默认项的值
        /// </summary>
        public object? DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值
        /// </summary>
        public bool SelectedValue
        {
            get;
            set;
        }

        #endregion

        public BooleanSelector()
        {

        }

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        public bool UseDefault
        {
            get;
            private set;
        }

    }
}
