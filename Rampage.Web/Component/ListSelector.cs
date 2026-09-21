using Rampage.Utilities;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 列表选择器，基于任意数据列表与指定值/文本字段生成表单项数据 / List-based selector that builds option data from an arbitrary data list with configurable value/text fields.
    /// </summary>
    /// <typeparam name="M">数据模型类型 / The data model type.</typeparam>
    /// <typeparam name="V">值字段类型 / The value field type.</typeparam>
    /// <typeparam name="T">文本字段类型 / The text field type.</typeparam>
    public class ListSelector<M, V, T>
        where M : class
        where V : IComparable<V>
        where T : IComparable<T>
    {
        #region Parameters

        /// <summary>
        /// 列表数据 / The source data list.
        /// </summary>
        public IList<M>? Data
        {
            get;
            set;
        }

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
        /// 是否多选（如果使用 check、radio 组件，此设置无效）/ Whether multiple selection is allowed (ineffective for check/radio controls).
        /// </summary>
        public bool Multiple
        {
            get;
            set;
        }

        /// <summary>
        /// 值字段名 / The name of the value field.
        /// </summary>
        public string? ValueField
        {
            get;
            set;
        }

        /// <summary>
        /// 文本字段名 / The name of the text field.
        /// </summary>
        public string? TextField
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
        /// 选中项的值 / The value of the selected item.
        /// </summary>
        public object? SelectedValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值，如果是多选选择器，有多个项默认选中，可以用此参数传递 / Selected values; pass multiple values here when a multi-select selector has several preselected items.
        /// </summary>
        public IList<V>? SelectedValues
        {
            get;
            set;
        }

        /// <summary>
        /// 不使用的项的值 / Values of the options to exclude from the selector.
        /// </summary>
        public V[]? RemoveValues
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// 项集合（键为值、值为文本），由 <see cref="Init"/> 生成 / The option collection (key = value, value = text), built by <see cref="Init"/>.
        /// </summary>
        public Dictionary<V, T>? Items
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否使用默认项，由 <see cref="Init"/> 依据 <see cref="DefaultText"/> 设置 / Whether the default option is used; set by <see cref="Init"/> based on <see cref="DefaultText"/>.
        /// </summary>
        public bool UseDefault
        {
            get;
            private set;
        }

        /// <summary>
        /// 判断指定值的项是否为选中项 / Determines whether the option with the given value is selected.
        /// </summary>
        /// <param name="itemValue">项的值 / The option value.</param>
        /// <returns>选中返回 true / true if selected.</returns>
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
        /// 初始化列表选择器 / Initializes a new instance of the <see cref="ListSelector{M,V,T}"/> class.
        /// </summary>
        public ListSelector()
        {
        }

        /// <summary>
        /// 依据数据列表与值/文本字段初始化项集合 / Builds the option collection from the data list and the value/text fields.
        /// </summary>
        /// <remarks>
        /// 若 <see cref="Data"/>、<see cref="ValueField"/> 或 <see cref="TextField"/> 未设置则直接返回（<see cref="Items"/> 保持 null）；
        /// 否则按值/文本字段提取键值对生成 <see cref="Items"/>（重复值只保留首个；设置了 <see cref="RemoveValues"/> 时其中的值会被排除），
        /// 并依据 <see cref="DefaultText"/> 设置 <see cref="UseDefault"/>；应在各属性赋值完成后调用。
        /// </remarks>
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
                    if (false == isRemove && !this.Items.ContainsKey(value))
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
