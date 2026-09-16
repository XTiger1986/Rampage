using Rampage.Data.Filter;
using System;
using System.Collections;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 
    /// </summary>
    public class ListViewData
    {

        /// <summary>
        /// 
        /// </summary>
        public DataPager? DataPager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual IEnumerable? DataList { get; set; }

    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class ListViewData<M> : ListViewData where M : class
    {
        /// <summary>
        /// 
        /// </summary>
        public new List<M> DataList { get; set; } = default!;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class ListViewData<M, F> : ListViewData <M>
        where M : class
        where F : DataFilter
    {

        /// <summary>
        /// 
        /// </summary>
        public F DataFilter { get; set; } = default!;

    }

}
