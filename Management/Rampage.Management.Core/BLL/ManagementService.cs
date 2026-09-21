using Rampage.DependencyInjection;
using Rampage.Hierarchy;
using Rampage.Management.DAL;
using Rampage.Web;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Management.BLL
{

    /// <summary>
    /// 后台业务逻辑基类：统一注入后台四个核心 Dao
    /// Management business logic base class: injects the four core management Daos
    /// </summary>
    public abstract class ManagementService<M, Dao> : SuperService<M, Dao>
        where M : class
        where Dao : ISuperDao<M>
    {

        /// <summary>管理员数据访问 / Admin data access</summary>
        [Auto]
        protected IAdminDao AdminDao
        {
            get;
            set;
        } = default!;

        /// <summary>管理组数据访问 / Management group data access</summary>
        [Auto]
        protected IManageGroupDao ManageGroupDao
        {
            get;
            set;
        } = default!;

        /// <summary>管理员授权数据访问 / Admin permission data access</summary>
        [Auto]
        protected IAdminPermissionDao AdminPermissionDao
        {
            get;
            set;
        } = default!;

        /// <summary>管理组授权数据访问 / Management group permission data access</summary>
        [Auto]
        protected IManageGroupPermissionDao ManageGroupPermissionDao
        {
            get;
            set;
        } = default!;

    }
}
