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
    /// 
    /// </summary>
    public class AdminOperationLogService : ManagementService<AdminOperationLog, IAdminOperationLogDao>, IAdminOperationLogService
    {
        [Auto]
        protected IAdminOperationLogDao AdminOperationLogDao = default!;


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

        public bool AddAdminOperationLog(AdminOperationLog log)
        {
            log.CreateTime = UtcNow;
            log.UpdateTime = UtcNow;
            bool result = AdminOperationLogDao.Insert(log);
            return result;
        }

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
