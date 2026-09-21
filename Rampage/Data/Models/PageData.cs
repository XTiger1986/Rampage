using System;
using System.Collections;

namespace Rampage.Data.Models
{
    /// <summary>
    /// 分页数据结果（非泛型载体）
    /// Paged data result (non-generic carrier)
    /// </summary>
    public class PageData
    {

        /// <summary>
        /// 分页信息（含记录总数与总页数）
        /// Pager information (including record count and total pages)
        /// </summary>
        public PagerInfo? Pager { get; set; }

        /// <summary>
        /// 当前页数据列表
        /// Data list of the current page
        /// </summary>
        public virtual IEnumerable? DataList { get; set; }

    }

    /// <summary>
    /// 分页数据结果（强类型）
    /// Paged data result (strongly typed)
    /// </summary>
    /// <typeparam name="M">数据实体类型 / entity type</typeparam>
    public class PageData<M> : PageData where M : class
    {
        /// <summary>
        /// 当前页强类型数据列表（用 new 隐藏基类的非泛型 DataList）
        /// Strongly typed data list of the current page (hides the non-generic base DataList)
        /// </summary>
        public new List<M> DataList { get; set; } = default!;
    }

}
