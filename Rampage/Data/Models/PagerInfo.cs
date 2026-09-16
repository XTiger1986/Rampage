using System;

namespace Rampage.Data
{
    /// <summary>
    /// data query pager info
    /// </summary>
    public class PagerInfo
    {
        private int _pageIndex = 1;
        /// <summary>
        /// page index(from 1)
        /// </summary>
        public int PageIndex
        {
            get
            {
                if (_pageIndex <= 0)
                {
                    _pageIndex = 1;
                }
                return _pageIndex;
            }
            set { _pageIndex = value; }
        }

        private int _pageSize = 20;
        /// <summary>
        /// page data size (default 20)
        /// </summary>
        public int PageSize
        {
            get
            {
                if (_pageSize <= 0)
                {
                    _pageSize = 1;
                }
                return _pageSize;
            }
            set 
            { 
                _pageSize = value; 
            }
        }

        /// <summary>
        /// data result record count (if set this value, it means you know the record count beforehand, data query command will not build "select count" sql)
        /// </summary>
        public long? RecordCount
        {
            get;
            set;
        }

        private int? _totalPage;
        /// <summary>
        /// data result total page
        /// </summary>
        public int? TotalPage
        {
            get
            {
                if (_totalPage == null)
                {
                    if (RecordCount != null)
                    {
                        _totalPage = Convert.ToInt32((RecordCount.Value / PageSize) + (RecordCount.Value % PageSize > 0 ? 1 : 0));
                    }
                    else
                    {
                        _totalPage = null;
                    }
                }
                return _totalPage;
            }
            private set
            {
                _totalPage = value;
            }
        }
    }
}
