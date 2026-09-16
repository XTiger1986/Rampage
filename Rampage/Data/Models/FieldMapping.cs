using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Rampage.Data
{
    /// <summary>
    /// 实体属性-数据表字段映射信息
    /// Entity Property - Data Table Field Mapping Information
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class FieldMapping : Attribute
    {

        public FieldMapping()
        {
        }

        public FieldMapping(string columnName)
        {
            this.ColumnName = columnName;
        }


        private bool _autoIncrement = false;
        /// <summary>
        /// Is it a AutoIncrement column (AutoIncrement column do not generate relevant SQL statements in the data insertion update method int this framework)
        /// </summary>
        public bool AutoIncrement
        {
            get { return _autoIncrement; }
            set { _autoIncrement = value; }
        }

        private bool _primaryKey = false;
        /// <summary>
        /// 
        /// </summary>
        public bool PrimaryKey
        {
            get { return _primaryKey; }
            set { _primaryKey = value; }
        }

        private bool _enumString = false;
        /// <summary>
        /// 枚举在数据库中保存为字符串，否则保存为整型
        /// Save enums as strings in database, otherwise save as integers
        /// </summary>
        public bool EnumString
        {
            get { return _enumString; }
            set { _enumString = value; }
        }

        private bool _notColumn = false;
        /// <summary>
        /// 
        /// </summary>
        public bool NotColumn
        {
            get { return _notColumn; }
            set { _notColumn = value; }
        }

        /// <summary>
        /// PostgreSQL 字段是否为 jsonb。启用后 SQL 模板会对写入参数显式添加 ::jsonb，
        /// 避免 Npgsql 将 CLR string 按 text 类型发送而触发 42804。
        /// </summary>
        public bool Jsonb { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? ColumnName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public PropertyInfo? Property
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public Type PropertyType
        {
            get { return Property!.PropertyType; }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public MethodInfo? SetMethod
        {
            get { return Property!.GetSetMethod(); }
        }

        /// <summary>
        /// 属性名
        /// </summary>
        public string PropertyName
        {
            get { return Property!.Name; }
        }

    }
}
