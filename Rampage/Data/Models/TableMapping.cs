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

        public TableMapping() { }

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
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public FieldMapping? GetPropertyMapping(string propertyName)
        {
            if (PropertyMappingDict != null && PropertyMappingDict.ContainsKey(propertyName))
            {
                return PropertyMappingDict[propertyName];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public FieldMapping? GetColumnMapping(string columnName)
        {
            if (ColumnMappingDict != null && ColumnMappingDict.ContainsKey(columnName))
            {
                return ColumnMappingDict[columnName];
            }
            return null;
        }

        /// <summary>
        /// Resolve a property or column name to a mapped table column; throws when unknown.
        /// </summary>
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
