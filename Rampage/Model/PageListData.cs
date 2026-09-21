using System;

namespace Rampage.Model
{
    /// <summary>
    /// 分页列表数据包装 / Paged list data wrapper
    /// </summary>
    /// <typeparam name="M">列表项类型 / List item type</typeparam>
    public class PageListData<M>
    {


        private int _pageIndex = 1;
        /// <summary>
        /// 页码（从 1 开始，默认 1） / Page index, 1-based, default 1
        /// </summary>
        public int PageIndex
        {
            get { return _pageIndex; }
            set { _pageIndex = value; }
        }

        private int _pageSize = 10;
        /// <summary>
        /// 每页数量（默认 10） / Page size, default 10
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        /// <summary>
        /// 总记录数 / Total record count
        /// </summary>
        public long RecordCount
        {
            get;
            set;
        }


        /// <summary>
        /// 当前页数据列表 / Data list of the current page
        /// </summary>
        public List<M>? DataList { get; set; }

    }
}
