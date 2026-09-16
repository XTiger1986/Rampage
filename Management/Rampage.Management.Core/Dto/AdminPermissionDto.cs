using Rampage.Enums;
using Rampage.Utilities;
using System;

namespace Rampage.Management.Dto
{
    /// <summary>
    /// 管理员授权
    /// </summary>
    public class AdminPermissionDto
    {
        
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 管理员ID
        /// </summary>
        public int AdminId { get; set; }

        /// <summary>
        /// 资源标识
        /// </summary>
        public string? Resource { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        public DateTime UpdateTime { get; set; }



        /// <summary>
        /// 创建时间
        /// created time
        /// </summary>
        public DateTime LocalCreateTime
        {
            get { return DateTimeTool.UtcToLocal(CreateTime); }
        }

        /// <summary>
        /// 修改时间
        /// last updated time
        /// </summary>
        public DateTime LocalUpdateTime
        {
            get { return DateTimeTool.UtcToLocal(UpdateTime); }
        }


    }
}
