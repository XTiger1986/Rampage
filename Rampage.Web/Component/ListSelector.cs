using Rampage.Utilities;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 
    /// </summary>
    public class ListSelector<M, V, T>
        where M : class
        where V : IComparable<V>
        where T : IComparable<T>
    {
        #region Parameters

        /// <summary>
        /// 列表数据
        /// </summary>
        public IList<M>? Data
        {
            get;
            set;
        }

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
        /// 是否多选（如果是使用check、radio组件，此设置无效）
        /// </summary>
        public bool Multiple
        {
            get;
            set;
        }

        /// <summary>
        /// 值字段名
        /// </summary>
        public string? ValueField
        {
            get;
            set;
        }

        /// <summary>
        /// 文本字段名
        /// </summary>
        public string? TextField
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
        public object? SelectedValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值，如果是多选选择器，有多个项默认选中，可以用此参数传递
        /// </summary>
        public IList<V>? SelectedValues
        {
            get;
            set;
        }

        /// <summary>
        /// 不使用的项的值
        /// </summary>
        public V[]? RemoveValues
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// 项集合
        /// </summary>
        public Dictionary<V, T>? Items
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否使用默认项
        /// </summary>
        public bool UseDefault
        {
            get;
            private set;
        }

        /// <summary>
        /// 判断该项是否为选中项
        /// </summary>
        /// <param name="itemValue"></param>
        /// <returns></returns>
        public bool IsSelected(V itemValue)
        {
            if (SelectedValue != null)
            {
                return SelectedValue.Equals(itemValue);
            }
            if (SelectedValues != null && SelectedValues.Count > 0)
            {
                foreach (V selectedValue in SelectedValues)
                {
                    if (selectedValue.Equals(itemValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public ListSelector()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            if (this.Data == null || string.IsNullOrEmpty(this.ValueField) || string.IsNullOrEmpty(this.TextField))
            {
                return;
            }
            List<V> valueList = GenericTool.GetValueList<M, V>(this.Data, this.ValueField);
            List<T> textList = GenericTool.GetValueList<M, T>(this.Data, this.TextField);
            this.Items = new Dictionary<V, T>();

            if (RemoveValues == null || RemoveValues.Length == 0)
            {
                for (int i = 0; i < valueList.Count; i++)
                {
                    V value = valueList[i];
                    T text = textList[i];
                    if (!this.Items.ContainsKey(value))
                    {
                        this.Items.Add(value, text);
                    }
                }
            }
            else
            {
                for (int i = 0; i < valueList.Count; i++)
                {
                    V value = valueList[i];
                    T text = textList[i];
                    bool isRemove = false;
                    foreach (V removeValue in RemoveValues)
                    {
                        if (removeValue.Equals(value))
                        {
                            isRemove = true;
                            break;
                        }
                    }
                    if (false == isRemove)
                    {
                        this.Items.Add(value, text);
                    }
                }
            }

            if (this.DefaultText != null)
            {
                this.UseDefault = true;
            }
            else
            {
                this.UseDefault = false;
            }
        }

    }
}
