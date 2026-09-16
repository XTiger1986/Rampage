using Rampage.Utilities;
using System;

namespace Rampage.Management.Dto
{
    /// <summary>
    /// 管理员操作记录
    /// </summary>
    public class AdminOperationLogDto
    {
        
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// 管理员ID
        /// </summary>
        public int AdminId { get; set; }
        
        /// <summary>
        /// 操作名称
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// 区域名
        /// </summary>
        public string? AreaName { get; set; }

        /// <summary>
        /// 控制器名
        /// </summary>
        public string? ControllerName { get; set; }

        /// <summary>
        /// 视图名
        /// </summary>
        public string? ActionName { get; set; }
        
        /// <summary>
        /// 原始请求地址
        /// </summary>
        public string? RawUrl { get; set; }
        
        /// <summary>
        /// 操作数据类型完整名称
        /// </summary>
        public string? DataTypeName { get; set; }
        
        /// <summary>
        /// 入口数据值
        /// </summary>
        public string? DataIn { get; set; }
        
        /// <summary>
        /// 出口数据值
        /// </summary>
        public string? DataOut { get; set; }
        
        /// <summary>
        /// 表单数据
        /// </summary>
        public string? PostData { get; set; }
        
        /// <summary>
        /// COOKIE数据
        /// </summary>
        public string? CookieData { get; set; }
        
        /// <summary>
        /// 头信息
        /// </summary>
        public string? Headers { get; set; }
        
        /// <summary>
        /// 引用路径
        /// </summary>
        public string? Referrer { get; set; }
        
        /// <summary>
        /// 操作者IP
        /// </summary>
        public string? OperationIp { get; set; }
        
        /// <summary>
        /// 操作者客户端
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        public DateTime UpdateTime { get; set; }



        /// <summary>
        /// 所属管理员
        /// </summary>
        public AdminDto? Admin
        {
            get; set;
        }

    }
}
