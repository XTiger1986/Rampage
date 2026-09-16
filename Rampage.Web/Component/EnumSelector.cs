using Rampage.Enums;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 
    /// </summary>
    public class EnumSelector<T> where T : struct
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
        /// 是否多选（如果是使用check、radio组件，此设置无效）
        /// </summary>
        public bool Multiple
        {
            get;
            set;
        }

        /// <summary>
        /// 是否使用字符值
        /// </summary>
        public bool UseStringValue
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
        public Nullable<T> SelectedValue
        {
            get;
            set;
        }

        /// <summary>
        /// 选中项的值，如果是多选选择器，有多个项默认选中，可以用此参数传递
        /// </summary>
        public T[]? SelectedValues
        {
            get;
            set;
        }

        /// <summary>
        /// 不使用的项的值
        /// </summary>
        public T[]? RemoveValues
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// 项集合
        /// </summary>
        public List<EnumSelectorItem>? Items
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
        /// 选中项
        /// </summary>
        public EnumSelectorItem? SelectedItem
        {
            get;
            private set;
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public List<EnumSelectorItem>? SelectedItems
        {
            get;
            private set;
        }

        /// <summary>
        /// 判断该项是否为选中项
        /// </summary>
        /// <param name="itemValue"></param>
        /// <returns></returns>
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
        /// 判断该项是否为选中项
        /// </summary>
        /// <param name="itemValue"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        public EnumSelector()
        {

        }

        /// <summary>
        /// 
        /// </summary>
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
    /// 枚举选择器项
    /// </summary>
    public class EnumSelectorItem
    {
        /// <summary>
        /// 文本
        /// </summary>
        public string? Text
        {
            get;
            set;
        }

        /// <summary>
        /// 值
        /// </summary>
        public int Value
        {
            get;
            set;
        }
    }

}
