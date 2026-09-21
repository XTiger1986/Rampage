using Rampage.ExceptionExtensions;
using System;
using Globals = Rampage.Globals;
using System.Text.Json.Serialization;

namespace Rampage.Data
{
    /// <summary>
    /// 实体-数据表映射信息
    /// Entity-Table mapping information
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableMapping : Attribute
    {

        /// <summary>
        /// 初始化一个空的映射特性
        /// Initialize an empty mapping attribute
        /// </summary>
        public TableMapping() { }

        /// <summary>
        /// 初始化映射特性并指定数据表名
        /// Initialize the mapping attribute with a table name
        /// </summary>
        /// <param name="tableName">数据表名 / table name</param>
        public TableMapping(string tableName)
        {
            this.TableName = tableName;
        }

        /// <summary>
        /// 数据表名
        /// table name
        /// </summary>
        public string? TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 实体类型
        /// Entity type
        /// </summary>
        [JsonIgnore]
        public Type? ModelType
        {
            get;
            internal set;
        }

        /// <summary>
        /// 主键字段名称（本框架不支持联合主键）
        /// primary key (notice: Using this framework for development does not allow the use of federated primary keys)
        /// </summary>
        public string? PrimaryKey
        {
            get;
            internal set;
        }

        /// <summary>
        /// 以实体属性名作为键名的映射信息字典
        /// A dictionary of mapping information using entity property names as key names
        /// </summary>
        public Dictionary<string, FieldMapping>? PropertyMappingDict
        {
            get;
            internal set;
        }

        /// <summary>
        /// 以数据表字段名作为键名的映射信息字典
        /// Mapping information dictionary using data table field names as key names
        /// </summary>
        public Dictionary<string, FieldMapping>? ColumnMappingDict
        {
            get;
            internal set;
        }

        private List<FieldMapping>? _fieldMappingList;
        /// <summary>
        /// 所有映射信息列表
        /// all mapping information list
        /// </summary>
        public List<FieldMapping>? FieldMappingList
        {
            get
            {
                if (_fieldMappingList == null && PropertyMappingDict != null)
                {
                    _fieldMappingList = PropertyMappingDict.Values.ToList();
                }
                return _fieldMappingList ?? new List<FieldMapping>();
            }
        }

        private List<FieldMapping>? _columnMappingList;
        /// <summary>
        /// 与数据表字段对应的映射信息列表
        /// List of mapping information corresponding to table fields
        /// </summary>
        public List<FieldMapping>? ColumnMappingList
        {
            get
            {
                if (_columnMappingList == null && FieldMappingList != null)
                {
                    _columnMappingList = FieldMappingList.FindAll(p => p.NotColumn == false);
                }
                return _columnMappingList ?? new List<FieldMapping>();
            }
        }

        /// <summary>
        /// 按实体属性名获取映射信息
        /// Get the field mapping by entity property name
        /// </summary>
        /// <param name="propertyName">实体属性名 / entity property name</param>
        /// <returns>映射信息；未找到时返回 null / the mapping, or null when not found</returns>
        public FieldMapping? GetPropertyMapping(string propertyName)
        {
            if (PropertyMappingDict != null && PropertyMappingDict.ContainsKey(propertyName))
            {
                return PropertyMappingDict[propertyName];
            }
            return null;
        }

        /// <summary>
        /// 按数据表字段名获取映射信息
        /// Get the field mapping by table column name
        /// </summary>
        /// <param name="columnName">数据表字段名 / table column name</param>
        /// <returns>映射信息；未找到时返回 null / the mapping, or null when not found</returns>
        public FieldMapping? GetColumnMapping(string columnName)
        {
            if (ColumnMappingDict != null && ColumnMappingDict.ContainsKey(columnName))
            {
                return ColumnMappingDict[columnName];
            }
            return null;
        }

        /// <summary>
        /// 将实体属性名或数据表字段名解析为映射的数据表列名，无法解析时抛出异常
        /// Resolve a property or column name to a mapped table column; throws when unknown
        /// </summary>
        /// <remarks>
        /// 先按属性名精确匹配（GetPropertyMapping），未命中时再按字段名忽略大小写匹配。
        /// 命中的映射若标记了 <see cref="FieldMapping.NotColumn"/>（非表列属性），同样视为无效并抛出异常。
        /// </remarks>
        /// <param name="fieldOrProperty">实体属性名或数据表字段名 / property name or column name</param>
        /// <returns>映射的数据表列名 / the mapped column name</returns>
        /// <exception cref="SuperException">名称未映射到任何有效表列时抛出 / thrown when the name maps to no valid column</exception>
        public string ResolveColumnName(string fieldOrProperty)
        {
            FieldMapping? fieldMapping = GetPropertyMapping(fieldOrProperty);
            if (fieldMapping == null && ColumnMappingDict != null)
            {
                foreach (var kv in ColumnMappingDict)
                {
                    if (string.Equals(kv.Key, fieldOrProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        fieldMapping = kv.Value;
                        break;
                    }
                }
            }
            if (fieldMapping == null || fieldMapping.NotColumn)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, fieldOrProperty);
            }
            return fieldMapping.ColumnName!;
        }

    }
}
