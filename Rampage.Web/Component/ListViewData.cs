using Rampage.Data.Filter;
using System;
using System.Collections;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 列表视图数据，封装分页信息与数据列表，供列表页视图使用 / List view data that wraps paging info and the data list for list views.
    /// </summary>
    public class ListViewData
    {

        /// <summary>
        /// 分页信息 / The paging information.
        /// </summary>
        public DataPager? DataPager { get; set; }

        /// <summary>
        /// 数据列表 / The data list.
        /// </summary>
        public virtual IEnumerable? DataList { get; set; }

    }


    /// <summary>
    /// 强类型列表视图数据 / Strongly-typed list view data.
    /// </summary>
    /// <typeparam name="M">数据模型类型 / The data model type.</typeparam>
    public class ListViewData<M> : ListViewData where M : class
    {
        /// <summary>
        /// 数据列表（隐藏基类同名属性，返回强类型列表）/ The data list (hides the base property and returns a strongly-typed list).
        /// </summary>
        public new List<M> DataList { get; set; } = default!;
    }

    /// <summary>
    /// 带查询条件的强类型列表视图数据 / Strongly-typed list view data with a query filter.
    /// </summary>
    /// <typeparam name="M">数据模型类型 / The data model type.</typeparam>
    /// <typeparam name="F">查询条件类型 / The data filter type.</typeparam>
    public class ListViewData<M, F> : ListViewData <M>
        where M : class
        where F : DataFilter
    {

        /// <summary>
        /// 查询条件 / The query filter.
        /// </summary>
        public F DataFilter { get; set; } = default!;

    }

}
