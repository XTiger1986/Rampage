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

        /// <summary>
        /// 初始化一个空的映射特性
        /// Initialize an empty mapping attribute
        /// </summary>
        public FieldMapping()
        {
        }

        /// <summary>
        /// 初始化映射特性并指定数据表字段名
        /// Initialize the mapping attribute with a column name
        /// </summary>
        /// <param name="columnName">数据表字段名 / column name</param>
        public FieldMapping(string columnName)
        {
            this.ColumnName = columnName;
        }


        private bool _autoIncrement = false;
        /// <summary>
        /// 是否为自增列。Insert 时框架生成的语句会跳过该列，由数据库生成值并在插入后回填到实体属性；
        /// Update / Delete 时该列不受此标记影响，仍可作为普通更新列或条件字段参与 SQL 生成
        /// Whether the column is auto-increment. On Insert the generated statement omits the column,
        /// letting the database generate it (the value is written back to the entity after insert);
        /// on Update / Delete the flag has no effect and the column still participates as a normal
        /// update column or condition field
        /// </summary>
        public bool AutoIncrement
        {
            get { return _autoIncrement; }
            set { _autoIncrement = value; }
        }

        private bool _primaryKey = false;
        /// <summary>
        /// 是否为主键字段（本框架不支持联合主键）
        /// Whether the field is the primary key (composite primary keys are not supported)
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
        /// 该属性不与任何数据表字段映射：不参与 SQL 生成，也不会写入 ColumnMappingDict
        /// The property maps to no table column: excluded from SQL generation and from ColumnMappingDict
        /// </summary>
        public bool NotColumn
        {
            get { return _notColumn; }
            set { _notColumn = value; }
        }

        /// <summary>
        /// PostgreSQL 字段是否为 jsonb。启用后 SQL 模板会对写入参数显式添加 ::jsonb，
        /// 避免 Npgsql 将 CLR string 按 text 类型发送而触发 42804 错误。
        /// Whether the PostgreSQL column is jsonb. When enabled, the SQL template explicitly appends
        /// ::jsonb to the write parameter, preventing Npgsql from sending the CLR string as text
        /// and raising error 42804.
        /// </summary>
        public bool Jsonb { get; set; }

        /// <summary>
        /// 所属数据表名
        /// The owning table name
        /// </summary>
        public string? TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 映射的数据表字段名（未显式指定时取属性名）
        /// The mapped table column name (defaults to the property name)
        /// </summary>
        public string? ColumnName
        {
            get;
            set;
        }

        /// <summary>
        /// 映射的实体属性反射信息
        /// Reflection info of the mapped entity property
        /// </summary>
        [JsonIgnore]
        public PropertyInfo? Property
        {
            get;
            set;
        }

        /// <summary>
        /// 属性类型（等同 Property.PropertyType）
        /// The property type (same as Property.PropertyType)
        /// </summary>
        [JsonIgnore]
        public Type PropertyType
        {
            get { return Property!.PropertyType; }
        }

        /// <summary>
        /// 属性的 set 访问器；属性只读时为 null
        /// The property setter; null when the property is read-only
        /// </summary>
        [JsonIgnore]
        public MethodInfo? SetMethod
        {
            get { return Property!.GetSetMethod(); }
        }

        /// <summary>
        /// 属性名（等同 Property.Name）
        /// The property name (same as Property.Name)
        /// </summary>
        public string PropertyName
        {
            get { return Property!.Name; }
        }

    }
}
