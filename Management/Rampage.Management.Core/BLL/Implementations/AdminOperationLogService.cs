using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Management.BLL;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Reflection;
using System;

namespace Rampage.Management.Core.BLL
{
    /// <summary>
    /// 管理员操作日志业务逻辑实现
    /// Administrator operation log business logic implementation
    /// </summary>
    public class AdminOperationLogService : ManagementService<AdminOperationLog, IAdminOperationLogDao>, IAdminOperationLogService
    {
        /// <summary>管理员操作日志数据访问 / Admin operation log data access</summary>
        [Auto]
        protected IAdminOperationLogDao AdminOperationLogDao = default!;


        /// <summary>填充单条操作日志的所属管理员信息 / Populate the admin info for a single operation log</summary>
        /// <param name="log">操作日志DTO / The operation log DTO</param>
        /// <returns>填充后的DTO；入参为 null 时原样返回 / The populated DTO; returned as-is when the input is null</returns>
        public AdminOperationLogDto? ReadRelatedData(AdminOperationLogDto? log)
        {
            if (log == null)
                return log;

            int adminId = log.AdminId;
            Admin? admin = AdminDao.SelectOne(a => a.Id == adminId);
            if (admin != null)
            {
                log.Admin = ReflectionTool.Copy<Admin, AdminDto>(admin);
            }
            return log;
        }

        /// <summary>批量填充操作日志列表的所属管理员信息（一次查询避免 N+1） / Populate the admin info for a list of operation logs (single query to avoid N+1)</summary>
        /// <param name="logList">操作日志列表 / The operation log list</param>
        /// <returns>填充后的列表 / The populated list</returns>
        public List<AdminOperationLogDto> ReadRelatedData(List<AdminOperationLogDto> logList)
        {
            if (logList.Count == 0)
                return logList;

            List<int> adminIdList = logList.Select(a => a.AdminId).ToList();
            List<AdminDto> adminList = AdminDao.SelectList<AdminDto>(a => adminIdList.Contains(a.Id));
            foreach (AdminOperationLogDto log in logList)
            {
                log.Admin = adminList.Find(a => a.Id == log.AdminId);
            }
            return logList;
        }

        /// <summary>添加管理员操作日志（创建/修改时间由服务端填充） / Add an administrator operation log (create/update time filled by the server)</summary>
        /// <param name="log">操作日志数据 / The operation log data</param>
        /// <returns>插入成功返回 true / true if the log was inserted successfully</returns>
        public bool AddAdminOperationLog(AdminOperationLog log)
        {
            log.CreateTime = UtcNow;
            log.UpdateTime = UtcNow;
            bool result = AdminOperationLogDao.Insert(log);
            return result;
        }

        /// <summary>获取指定id的管理员操作日志（含所属管理员） / Retrieve the operation log of the administrator with the specified ID (with the admin)</summary>
        /// <param name="logId">日志ID / Log ID</param>
        /// <returns>操作日志DTO；不存在时为 null / The operation log DTO; null if not found</returns>
        public AdminOperationLogDto? GetAdminOperationLogById(long logId)
        {
            AdminOperationLog? log = AdminOperationLogDao.SelectById(logId);
            if (log != null)
            {
                AdminOperationLogDto dto = ReflectionTool.Copy<AdminOperationLog, AdminOperationLogDto>(log);
                ReadRelatedData(dto);
                return dto;
            }
            return null;
        }

        /// <summary>根据筛选器获取分页管理员操作日志列表数据（按创建时间降序，含所属管理员） / Get paginated administrator operation log list data by the filter (ordered by create time descending, with admins)</summary>
        /// <param name="pagerInfo">分页信息 / Paging information</param>
        /// <param name="filter">筛选器 / The filter</param>
        /// <returns>按筛选器过滤的分页操作日志列表 / The paginated operation log list filtered by the filter</returns>
        public List<AdminOperationLogDto> SearchAdminOperationLogPageList(PagerInfo pagerInfo, AdminOperationLogFilter filter)
        {
            FilterWrapper<AdminOperationLog> wrapper = new FilterWrapper<AdminOperationLog>(filter);
            wrapper.OrderByDescending(l => l.CreateTime);
            List<AdminOperationLogDto> list = AdminOperationLogDao.SelectPage<AdminOperationLogDto>(pagerInfo, wrapper);
            list = ReadRelatedData(list);
            return list;
        }

    }
}
