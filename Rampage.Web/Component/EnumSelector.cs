using Rampage.Enums;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 枚举选择器，基于枚举类型生成下拉/单选/多选等表单项数据 / Enum-based selector that builds option data (dropdown/radio/multi-select, etc.) from an enum type.
    /// </summary>
    /// <typeparam name="T">枚举类型 / The enum type.</typeparam>
    public class EnumSelector<T> where T : struct
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
        /// 是否多选（如果使用 check、radio 组件，此设置无效）/ Whether multiple selection is allowed (ineffective for check/radio controls).
        /// </summary>
        public bool Multiple
        {
            get;
            set;
        }

        /// <summary>
        /// 是否使用字符值（项的值以字符串输出）/ Whether item values are emitted as strings.
        /// </summary>
        public bool UseStringValue
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
        public Nullable<T> SelectedValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值，如果是多选选择器，有多个项默认选中，可以用此参数传递 / Selected values; pass multiple values here when a multi-select selector has several preselected items.
        /// </summary>
        public T[]? SelectedValues
        {
            get;
            set;
        }

        /// <summary>
        /// 不使用的项的值 / Values of the options to exclude from the selector.
        /// </summary>
        public T[]? RemoveValues
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// 项集合，由 <see cref="Init"/> 基于枚举生成 / The option collection, built from the enum by <see cref="Init"/>.
        /// </summary>
        public List<EnumSelectorItem>? Items
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
        /// 选中项（单选，由 <see cref="Init"/> 解析）/ The selected item (single selection), resolved by <see cref="Init"/>.
        /// </summary>
        public EnumSelectorItem? SelectedItem
        {
            get;
            private set;
        }

        /// <summary>
        /// 选中项集合（多选，由 <see cref="Init"/> 解析）/ The selected items (multiple selection), resolved by <see cref="Init"/>.
        /// </summary>
        public List<EnumSelectorItem>? SelectedItems
        {
            get;
            private set;
        }

        /// <summary>
        /// 判断指定值的项是否为选中项 / Determines whether the option with the given value is selected.
        /// </summary>
        /// <param name="itemValue">项的值 / The option value.</param>
        /// <returns>选中返回 true / true if selected.</returns>
        public bool IsSelected(int itemValue)
        {
            if (SelectedItem != null)
            {
                return SelectedItem.Value == itemValue;
            }
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                foreach (EnumSelectorItem item in SelectedItems)
                {
                    if (item.Value == itemValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 判断指定文本的项是否为选中项（忽略大小写，与项的文本比较而非值）/ Determines whether an option is selected by comparing its text (case-insensitive) with the given value.
        /// </summary>
        /// <param name="itemValue">项的文本 / The option text.</param>
        /// <returns>选中返回 true / true if selected.</returns>
        public bool IsSelected(string itemValue)
        {
            if (SelectedItem != null)
            {
                return string.Compare(SelectedItem.Text, itemValue, true) == 0;
            }
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                foreach (EnumSelectorItem item in SelectedItems)
                {
                    if (string.Compare(item.Text, itemValue, true) == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 初始化枚举选择器 / Initializes a new instance of the <see cref="EnumSelector{T}"/> class.
        /// </summary>
        public EnumSelector()
        {

        }

        /// <summary>
        /// 根据枚举与当前属性值初始化项集合及选中状态 / Builds the option collection and resolves the selection state from the enum and current property values.
        /// </summary>
        /// <remarks>
        /// 通过 EnumReader 读取枚举各项文本生成 <see cref="Items"/>（<see cref="RemoveValues"/> 中的值会被排除）；
        /// 再依据 <see cref="SelectedValue"/>/<see cref="SelectedValues"/> 填充 <see cref="SelectedItem"/>/<see cref="SelectedItems"/>，
        /// 并依据 <see cref="DefaultText"/> 设置 <see cref="UseDefault"/>；应在各属性赋值完成后调用。
        /// </remarks>
        public void Init()
        {
            if (RemoveValues == null || RemoveValues.Length == 0)
            {
                var enumAttrList = EnumReader.GetInfoList<T>();
                this.Items = enumAttrList.Select(a =>
                {
                    return new EnumSelectorItem { Text = EnumReader.GetText<T>(a), Value = a.IntValue };
                }).ToList();
            }
            else
            {
                List<EnumSelectorItem> items = new List<EnumSelectorItem>();
                List<EnumInfo> orgItems = EnumReader.GetInfoList<T>();
                foreach (EnumInfo ea in orgItems)
                {
                    bool isRemove = false;
                    foreach (T removeValue in RemoveValues)
                    {
                        if (Convert.ToInt32(removeValue) == ea.IntValue)
                        {
                            isRemove = true;
                            break;
                        }
                    }
                    if (false == isRemove)
                    {
                        items.Add(new EnumSelectorItem { Text = EnumReader.GetText<T>(ea), Value = ea.IntValue });
                    }
                }

                this.Items = items;
            }

            if (this.SelectedValue != null)
            {
                this.SelectedItem = this.Items.Find(delegate (EnumSelectorItem ea)
                {
                    return Convert.ToInt32(this.SelectedValue.Value) == ea.Value;
                });
            }
            if (this.SelectedValues != null)
            {
                this.SelectedItems = this.Items.FindAll(delegate (EnumSelectorItem ea)
                {
                    return this.SelectedValues.Any(s => Convert.ToInt32(s) == ea.Value);
                });
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

    /// <summary>
    /// 枚举选择器项 / An option item of an enum selector.
    /// </summary>
    public class EnumSelectorItem
    {
        /// <summary>
        /// 文本 / The display text.
        /// </summary>
        public string? Text
        {
            get;
            set;
        }

        /// <summary>
        /// 值 / The option value.
        /// </summary>
        public int Value
        {
            get;
            set;
        }
    }

}
