using Rampage.Data;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>
    /// 管理员操作记录
    /// </summary>
    [TableMapping("admin_operation_log")]
    public class AdminOperationLog
    {
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public long Id { get; set; }
        
        /// <summary>
        /// 管理员ID
        /// </summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }
        
        /// <summary>
        /// 操作名称
        /// </summary>
        [FieldMapping("operation_name")]
        public string? OperationName { get; set; }

        /// <summary>
        /// 区域名
        /// </summary>
        [FieldMapping("area_name")]
        public string? AreaName { get; set; }

        /// <summary>
        /// 控制器名
        /// </summary>
        [FieldMapping("controller_name")]
        public string? ControllerName { get; set; }

        /// <summary>
        /// 视图名
        /// </summary>
        [FieldMapping("action_name")]
        public string? ActionName { get; set; }
        
        /// <summary>
        /// 原始请求地址
        /// </summary>
        [FieldMapping("raw_url")]
        public string? RawUrl { get; set; }
        
        /// <summary>
        /// 操作数据类型完整名称
        /// </summary>
        [FieldMapping("data_type_name")]
        public string? DataTypeName { get; set; }
        
        /// <summary>
        /// 入口数据值
        /// </summary>
        [FieldMapping("data_in")]
        public string? DataIn { get; set; }
        
        /// <summary>
        /// 出口数据值
        /// </summary>
        [FieldMapping("data_out")]
        public string? DataOut { get; set; }
        
        /// <summary>
        /// 表单数据
        /// </summary>
        [FieldMapping("post_data")]
        public string? PostData { get; set; }
        
        /// <summary>
        /// COOKIE数据
        /// </summary>
        [FieldMapping("cookie_data")]
        public string? CookieData { get; set; }
        
        /// <summary>
        /// 头信息
        /// </summary>
        [FieldMapping("headers")]
        public string? Headers { get; set; }
        
        /// <summary>
        /// 引用路径
        /// </summary>
        [FieldMapping("referrer")]
        public string? Referrer { get; set; }
        
        /// <summary>
        /// 操作者IP
        /// </summary>
        [FieldMapping("operation_ip")]
        public string? OperationIp { get; set; }
        
        /// <summary>
        /// 操作者客户端
        /// </summary>
        [FieldMapping("user_agent")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }


    }
}
