using Rampage.Utilities;
using System;

namespace Rampage.Management.Dto
{
    /// <summary>管理员操作日志数据传输对象 / Administrator operation log data transfer object</summary>
    public class AdminOperationLogDto
    {
        
        /// <summary>主键ID / Primary key ID</summary>
        public long Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        public int AdminId { get; set; }
        
        /// <summary>操作名称 / Operation name</summary>
        public string? OperationName { get; set; }

        /// <summary>区域名 / Area name</summary>
        public string? AreaName { get; set; }

        /// <summary>控制器名 / Controller name</summary>
        public string? ControllerName { get; set; }

        /// <summary>视图名(Action名) / Action name</summary>
        public string? ActionName { get; set; }
        
        /// <summary>原始请求地址（含查询串） / Raw request URL (with query string)</summary>
        public string? RawUrl { get; set; }
        
        /// <summary>操作数据类型的完整名称 / Full type name of the operation data</summary>
        public string? DataTypeName { get; set; }
        
        /// <summary>入口数据值（请求参数序列化） / Input data value (serialized request parameters)</summary>
        public string? DataIn { get; set; }
        
        /// <summary>出口数据值（响应数据序列化） / Output data value (serialized response data)</summary>
        public string? DataOut { get; set; }
        
        /// <summary>表单数据 / Form (POST) data</summary>
        public string? PostData { get; set; }
        
        /// <summary>Cookie数据 / Cookie data</summary>
        public string? CookieData { get; set; }
        
        /// <summary>请求头信息 / Request headers</summary>
        public string? Headers { get; set; }
        
        /// <summary>引用路径(Referrer) / Referrer</summary>
        public string? Referrer { get; set; }
        
        /// <summary>操作者IP / Operator IP address</summary>
        public string? OperationIp { get; set; }
        
        /// <summary>操作者客户端(User-Agent) / Operator user agent</summary>
        public string? UserAgent { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        public DateTime UpdateTime { get; set; }



        /// <summary>所属管理员 / The administrator who performed the operation</summary>
        public AdminDto? Admin
        {
            get; set;
        }

    }
}
