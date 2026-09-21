using Rampage.Data;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Web;
using System;

namespace Rampage.Management.Core.BLL
{
    /// <summary>管理员操作日志业务逻辑 / Administrator operation log business logic</summary>
    public interface IAdminOperationLogService : ISuperService<AdminOperationLog, IAdminOperationLogDao>
    {

        /// <summary>
        /// 添加管理员操作日志
        /// Add administrator operation log
        /// </summary>
        /// <param name="log">操作日志数据（创建/修改时间由服务端填充） / The operation log data (create/update time filled by the server)</param>
        /// <returns>插入成功返回 true / true if the log was inserted successfully</returns>
        bool AddAdminOperationLog(AdminOperationLog log);

        /// <summary>
        /// 获取指定id的管理员操作日志
        /// Retrieve the operation log of the administrator with the specified ID
        /// </summary>
        /// <param name="logId">日志ID / Log ID</param>
        /// <returns>操作日志DTO（含所属管理员）；不存在时为 null / The operation log DTO (with the admin); null if not found</returns>
        AdminOperationLogDto? GetAdminOperationLogById(long logId);

        /// <summary>
        /// 根据筛选器获取分页管理员操作日志列表数据
        /// Get paginated administrator operation log list data by the filter
        /// </summary>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <param name="filter">筛选器 / The filter</param>
        /// <returns>按筛选器过滤、按创建时间降序的分页操作日志列表（含所属管理员） / The paginated operation log list filtered by the filter, ordered by create time descending (with admins)</returns>
        List<AdminOperationLogDto> SearchAdminOperationLogPageList(PagerInfo pagerInfo, AdminOperationLogFilter filter);

    }
}
