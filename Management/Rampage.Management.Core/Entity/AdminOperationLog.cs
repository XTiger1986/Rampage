using Rampage.Data;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理员操作日志（映射 admin_operation_log 表） / Administrator operation log (mapped to admin_operation_log)</summary>
    /// <remarks>记录请求的全量审计上下文：区域/控制器/视图、原始地址、入口与出口数据、表单、Cookie、Header、引用路径、操作者IP与客户端。/ Captures the full audit context of a request: area/controller/action, raw URL, data in/out, form, cookies, headers, referrer, operator IP and user agent.</remarks>
    [TableMapping("admin_operation_log")]
    public class AdminOperationLog
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public long Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }
        
        /// <summary>操作名称 / Operation name</summary>
        [FieldMapping("operation_name")]
        public string? OperationName { get; set; }

        /// <summary>区域名 / Area name</summary>
        [FieldMapping("area_name")]
        public string? AreaName { get; set; }

        /// <summary>控制器名 / Controller name</summary>
        [FieldMapping("controller_name")]
        public string? ControllerName { get; set; }

        /// <summary>视图名(Action名) / Action name</summary>
        [FieldMapping("action_name")]
        public string? ActionName { get; set; }
        
        /// <summary>原始请求地址（含查询串） / Raw request URL (with query string)</summary>
        [FieldMapping("raw_url")]
        public string? RawUrl { get; set; }
        
        /// <summary>操作数据类型的完整名称 / Full type name of the operation data</summary>
        [FieldMapping("data_type_name")]
        public string? DataTypeName { get; set; }
        
        /// <summary>入口数据值（请求参数序列化） / Input data value (serialized request parameters)</summary>
        [FieldMapping("data_in")]
        public string? DataIn { get; set; }
        
        /// <summary>出口数据值（响应数据序列化） / Output data value (serialized response data)</summary>
        [FieldMapping("data_out")]
        public string? DataOut { get; set; }
        
        /// <summary>表单数据 / Form (POST) data</summary>
        [FieldMapping("post_data")]
        public string? PostData { get; set; }
        
        /// <summary>Cookie数据 / Cookie data</summary>
        [FieldMapping("cookie_data")]
        public string? CookieData { get; set; }
        
        /// <summary>请求头信息 / Request headers</summary>
        [FieldMapping("headers")]
        public string? Headers { get; set; }
        
        /// <summary>引用路径(Referrer) / Referrer</summary>
        [FieldMapping("referrer")]
        public string? Referrer { get; set; }
        
        /// <summary>操作者IP / Operator IP address</summary>
        [FieldMapping("operation_ip")]
        public string? OperationIp { get; set; }
        
        /// <summary>操作者客户端(User-Agent) / Operator user agent</summary>
        [FieldMapping("user_agent")]
        public string? UserAgent { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }


    }
}
