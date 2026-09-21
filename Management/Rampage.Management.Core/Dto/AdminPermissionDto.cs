using Rampage.Enums;
using Rampage.Utilities;
using System;

namespace Rampage.Management.Dto
{
    /// <summary>管理员授权数据传输对象 / Administrator permission data transfer object</summary>
    public class AdminPermissionDto
    {
        
        /// <summary>主键ID / Primary key ID</summary>
        public int Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        public int AdminId { get; set; }

        /// <summary>资源标识（权限资源组） / Resource identifier (permission resource group)</summary>
        public string? Resource { get; set; }

        /// <summary>状态（启用/禁用，禁用即撤权） / Status (enabled/disabled; disabled means revoked)</summary>
        public CommonStatus Status { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        public DateTime UpdateTime { get; set; }



        /// <summary>创建时间（本地时区） / Created time (local time zone)</summary>
        public DateTime LocalCreateTime
        {
            get { return DateTimeTool.UtcToLocal(CreateTime); }
        }

        /// <summary>修改时间（本地时区） / Last updated time (local time zone)</summary>
        public DateTime LocalUpdateTime
        {
            get { return DateTimeTool.UtcToLocal(UpdateTime); }
        }


    }
}
