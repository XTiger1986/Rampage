using System;

namespace Rampage.Model
{
    public class PageListData<M>
    {


        private int _pageIndex = 1;
        /// <summary>
        /// 第几页
        /// </summary>
        public int PageIndex
        {
            get { return _pageIndex; }
            set { _pageIndex = value; }
        }

        private int _pageSize = 10;
        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        /// <summary>
        /// 总数量
        /// </summary>
        public long RecordCount
        {
            get;
            set;
        }


        public List<M>? DataList { get; set; }

    }
}
