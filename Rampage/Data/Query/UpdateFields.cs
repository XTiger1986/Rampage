using Rampage.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 数据表更新字段集合（字段名 -> 新值）
    /// Update field collection for a data table (field name -> new value)
    /// </summary>
    public class UpdateFields
    {

        /// <summary>
        /// 初始化一个空的更新字段集合
        /// Initialize an empty update field collection
        /// </summary>
        public UpdateFields()
        {
            Fields = new Dictionary<string, object?>();
        }

        /// <summary>
        /// 创建一个空的更新字段集合
        /// Create an empty update field collection
        /// </summary>
        /// <returns>新的更新字段实例 / A new instance</returns>
        public static UpdateFields New
        {
            get
            {
                return new UpdateFields();
            }
        }

        /// <summary>
        /// 创建包含单个更新字段的更新字段集合
        /// Create an update field collection with a single field
        /// </summary>
        /// <param name="field">字段名 / Field name</param>
        /// <param name="value">新值 / New value</param>
        /// <returns>新的更新字段实例 / A new instance</returns>
        public static UpdateFields Init(string field, object? value)
        {
            UpdateFields fields = new UpdateFields();
            fields = fields.Set(field, value);
            return fields;
        }

        /// <summary>
        /// 更新字段字典（字段名 -> 新值），保留插入顺序
        /// Update dictionary (field name -> new value); insertion order preserved
        /// </summary>
        public Dictionary<string, object?> Fields
        {
            get; protected set;
        }

        /// <summary>
        /// 添加一个更新字段（同名字段覆盖原值）
        /// Add an update field (an existing field name overwrites its value)
        /// </summary>
        /// <param name="field">字段名 / Field name</param>
        /// <param name="value">新值 / New value</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual UpdateFields Set(string field, object? value)
        {
            if (!Fields.ContainsKey(field))
                Fields.Add(field, value);
            else
                Fields[field] = value;
            return this;
        }

    }

    /// <summary>
    /// 强类型更新字段集合（字段选择器经模型映射解析为实际列名）
    /// Strongly-typed update field collection (selectors resolved to real column names via model mapping)
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class UpdateFields<M> : UpdateFields
        where M : class
    {

        /// <summary>
        /// 模型与数据表的映射信息（构造时按 <typeparamref name="M"/> 自动获取）
        /// Model-to-table mapping (resolved from <typeparamref name="M"/> at construction)
        /// </summary>
        public TableMapping Mapping
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始化一个空的强类型更新字段集合
        /// Initialize an empty strongly-typed update field collection
        /// </summary>
        public UpdateFields() : base()
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// 创建一个空的强类型更新字段集合
        /// Create an empty strongly-typed update field collection
        /// </summary>
        /// <returns>新的更新字段实例 / A new instance</returns>
        public new static UpdateFields<M> New
        {
            get
            {
                return new UpdateFields<M>();
            }
        }

        /// <summary>
        /// 创建包含单个更新字段的强类型更新字段集合
        /// Create a strongly-typed update field collection with a single field
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <param name="value">新值 / New value</param>
        /// <returns>新的更新字段实例 / A new instance</returns>
        public static UpdateFields<M> Build<TKey>(Expression<Func<M, TKey>> keySelector, TKey? value)
        {
            UpdateFields<M> fields = new UpdateFields<M>();
            fields = fields.Set(keySelector, value);
            return fields;
        }

        /// <summary>
        /// 以字段选择器添加一个更新字段（解析为实际列名，同名列覆盖原值）
        /// Add an update field via key selector (resolved to the mapped column; an existing column overwrites its value)
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <param name="value">新值 / New value</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual UpdateFields<M> Set<TKey>(Expression<Func<M, TKey>> keySelector, TKey? value)
        {
            string field = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
            if (!Fields.ContainsKey(field))
                Fields.Add(field, value);
            else
                Fields[field] = value;
            return this;
        }

    }

}
