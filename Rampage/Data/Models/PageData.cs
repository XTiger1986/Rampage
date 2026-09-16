using System;
using System.Collections;

namespace Rampage.Data.Models
{
    public class PageData
    {

        /// <summary>
        /// 
        /// </summary>
        public PagerInfo? Pager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual IEnumerable? DataList { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class PageData<M> : PageData where M : class
    {
        /// <summary>
        /// 
        /// </summary>
        public new List<M> DataList { get; set; } = default!;
    }

}
