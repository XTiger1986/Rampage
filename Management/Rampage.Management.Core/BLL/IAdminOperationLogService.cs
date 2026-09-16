using Rampage.Data;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Web;
using System;

namespace Rampage.Management.Core.BLL
{
    /// <summary>
    /// 管理员业务逻辑
    /// Admin Operation Log business logic
    /// </summary>
    public interface IAdminOperationLogService : ISuperService<AdminOperationLog, IAdminOperationLogDao>
    {

        /// <summary>
        /// 添加管理员操作日志
        /// Add administrator operation log
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        bool AddAdminOperationLog(AdminOperationLog log);

        /// <summary>
        /// 获取指定id的管理员操作日志
        /// Retrieve the operation log of the administrator with the specified ID
        /// </summary>
        /// <param name="logId"></param>
        /// <returns></returns>
        AdminOperationLogDto? GetAdminOperationLogById(long logId);

        /// <summary>
        /// 根据筛选器获取分页管理员操作日志列表数据
        /// Get paginated administrator operation log list data by the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        List<AdminOperationLogDto> SearchAdminOperationLogPageList(PagerInfo pagerInfo, AdminOperationLogFilter filter);

    }
}
