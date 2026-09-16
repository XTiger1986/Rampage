using Rampage.DependencyInjection;
using Rampage.Hierarchy;
using Rampage.Management.DAL;
using Rampage.Web;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Management.BLL
{

    /// <summary>
    /// 后台业务逻辑基类
    /// Management business logic base class
    /// </summary>
    public abstract class ManagementService<M, Dao> : SuperService<M, Dao>
        where M : class
        where Dao : ISuperDao<M>
    {

        [Auto]
        protected IAdminDao AdminDao
        {
            get;
            set;
        } = default!;

        [Auto]
        protected IManageGroupDao ManageGroupDao
        {
            get;
            set;
        } = default!;

        [Auto]
        protected IAdminPermissionDao AdminPermissionDao
        {
            get;
            set;
        } = default!;

        [Auto]
        protected IManageGroupPermissionDao ManageGroupPermissionDao
        {
            get;
            set;
        } = default!;

    }
}
