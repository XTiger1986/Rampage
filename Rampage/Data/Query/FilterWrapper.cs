using Rampage.Data.Filter;
using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// Database Filter wrapper
    /// </summary>
    public class FilterWrapper<M> : QueryWrapper<M> where M : class
    {
        /// <summary>
        /// 
        /// </summary>
        public DataFilter? Filter
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initialize a empty Database Filter wrapper
        /// </summary>
        public FilterWrapper() : base()
        {
        }

        /// <summary>
        /// Initialize a Database Filter wrapper by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        public FilterWrapper(DataFilter filter) : base()
        {
            Filter = filter;
        }


        /// <summary>
        /// Initialize a empty Database Lambda wrapper
        /// </summary>
        /// <returns></returns>
        public static FilterWrapper<M> Empty()
        {
            return new FilterWrapper<M>();
        }

        /// <summary>
        /// Initialize a Database Lambda wrapper by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static FilterWrapper<M> Create(DataFilter filter)
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            return wrapper;
        }


    }
}
