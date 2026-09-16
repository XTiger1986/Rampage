using Microsoft.AspNetCore.Http;
using Rampage.Data;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 数据分页信息
    /// </summary>
    public class DataPager
    {
        private HttpContext? _httpContext = RequestContext.Current;
        /// <summary>
        /// 
        /// </summary>
        private HttpContext? HttpContext
        {
            get 
            {           
                return _httpContext;
            }
        }

        public DataPager()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pagerInfo"></param>
        public DataPager(PagerInfo pagerInfo)
        {
            this.PageIndex = pagerInfo.PageIndex;
            this.PageSize = pagerInfo.PageSize;
            this.RecordCount = pagerInfo.RecordCount != null ? pagerInfo.RecordCount.Value : 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="recordCount"></param>
        public DataPager(int pageIndex, int pageSize, int recordCount)
        {
            this.PageIndex = pageIndex < 1 ? 1 : pageIndex;
            this.PageSize = pageSize < 1 ? 10 : pageSize;
            this.RecordCount = recordCount;
        }

        private string? _id;
        /// <summary>
        /// 页码表唯一标识
        /// </summary>
        public string Id
        {
            get { 
                if (string.IsNullOrEmpty(_id))
                {
                    _id = Guid.NewGuid().ToString("N");
                }
                return _id;
            }
            set { _id = value; }
        }

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

        private int _cellSize = 8;
        /// <summary>
        /// 页码显示数
        /// </summary>
        public int CellSize
        {
            get { return _cellSize; }
            set { _cellSize = value; }
        }

        private string? _urlFormat;
        /// <summary>
        /// URL地址格式（占位符"{0}":当前页数;占位符"{1}":每页大小;）
        /// </summary>
        public string? UrlFormat
        {
            get
            {
                if (string.IsNullOrEmpty(_urlFormat) && HttpContext != null)
                {
                    _urlFormat = PathTool.SetUrlQuery(HttpContext.Request.RawUrl(), "page", "{0}", false);
                }
                return _urlFormat;
            }
            set { _urlFormat = value; }
        }

        /// <summary>
        /// 锚点名称
        /// </summary>
        public string? Anchor
        {
            get;
            set;
        }

        private string? _countInfo;
        /// <summary>
        /// 统计信息内容格式
        /// </summary>
        public string? CountInfo
        {
            get
            {
                return string.IsNullOrEmpty(_countInfo) ? "共{0}条数据" : string.Empty;
            }
            set { _countInfo = value; }
        }

        private string? _noDataInfo;
        /// <summary>
        /// 无数据信息
        /// </summary>
        public string? NoDataInfo
        {
            get
            {
                return string.IsNullOrEmpty(_noDataInfo) ? "暂无数据" : string.Empty;
            }
            set { _noDataInfo = value; }
        }

        private int? _totalPage = null;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPage
        {
            get
            {
                if (_totalPage == null)
                {
                    _totalPage = GetTotalPage();
                }
                return _totalPage.Value;
            }
            private set { _totalPage = value; }
        }

        private int? _startPage = null;
        /// <summary>
        /// 开始页码
        /// </summary>
        public int StartPage
        {
            get
            {
                if (_startPage == null)
                {
                    _startPage = GetStartPage();
                }
                return _startPage.Value;
            }
            set { _startPage = value; }
        }

        private long? _endPage = null;
        /// <summary>
        /// 结束页码
        /// </summary>
        public long EndPage
        {
            get
            {
                if (_endPage == null)
                {
                    _endPage = GetEndPage();
                }
                return _endPage.Value;
            }
            set { _endPage = value; }
        }

        private string? _prevUrl;
        /// <summary>
        /// 上一页URL
        /// </summary>
        public string? PrevUrl
        {
            get
            {
                if (_prevUrl == null)
                {
                    _prevUrl = GetPrevUrl();
                }
                return _prevUrl;
            }
            private set { _prevUrl = value; }
        }

        private string? _nextUrl;
        /// <summary>
        /// 下一页URL
        /// </summary>
        public string? NextUrl
        {
            get
            {
                if (_nextUrl == null)
                {
                    _nextUrl = GetNextUrl();
                }
                return _nextUrl;
            }
            private set { _nextUrl = value; }
        }

        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <returns></returns>
        public int GetTotalPage()
        {
            int totalPage = 0;
            if (this.RecordCount < 0)
            {
                totalPage = 0;
            }
            else
            {
                totalPage = Convert.ToInt32(Math.Floor((decimal)(this.RecordCount / this.PageSize)));
                if (this.RecordCount % this.PageSize != 0)
                {
                    totalPage += 1;
                }
            }
            return totalPage;
        }


        /// <summary>
        /// 获取开始页码
        /// </summary>
        /// <returns></returns>
        public int GetStartPage()
        {
            int totalPage = this.TotalPage;
            if (this.PageIndex > totalPage)
            {
                this.PageIndex = totalPage;
            }
            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            int leftRange = this.CellSize / 2;
            if (this.CellSize % 2 > 0)
            {
                leftRange += 1;
            }
            int rightRange = this.CellSize - leftRange;
            int startPage = this.PageIndex - leftRange;
            int endPage = this.PageIndex + rightRange;

            if (endPage > totalPage)
            {
                startPage = startPage - rightRange;
            }

            if (startPage < 1)
            {
                startPage = 1;
            }
            else if (startPage > totalPage)
            {
                startPage = totalPage;
            }
            return startPage;
        }

        /// <summary>
        /// 获取结束页码
        /// </summary>
        /// <returns></returns>
        public int GetEndPage()
        {
            int totalPage = this.TotalPage;
            if (this.PageIndex > totalPage)
            {
                this.PageIndex = totalPage;
            }
            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            int leftRange = this.CellSize / 2;
            if (this.CellSize % 2 > 0)
            {
                leftRange += 1;
            }
            int rightRange = this.CellSize - leftRange;
            int startPage = this.PageIndex - leftRange;
            int endPage = this.PageIndex + rightRange;

            if (startPage < 1)
            {
                endPage = endPage + leftRange;
            }

            if (endPage < 1)
            {
                endPage = 1;
            }
            else if (endPage > totalPage)
            {
                endPage = totalPage;
            }
            return endPage;
        }

        /// <summary>
        /// 获取URL
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public string GetUrl(long pageIndex)
        {
            string url = string.Format(this.UrlFormat!, pageIndex, this.PageSize);
            if (!string.IsNullOrEmpty(this.Anchor))
            {
                url = string.Concat(url, "#", this.Anchor);
            }
            return url;
        }

        /// <summary>
        /// 获取上一页URL
        /// </summary>
        /// <returns></returns>
        public string GetPrevUrl()
        {
            long prevPageIndex = this.PageIndex - 1;
            if (prevPageIndex <= 0)
            {
                prevPageIndex = 1;
            }
            return this.GetUrl(prevPageIndex);
        }

        /// <summary>
        /// 获取下一页URL
        /// </summary>
        /// <returns></returns>
        public string GetNextUrl()
        {
            int nextPageIndex = this.PageIndex + 1;
            int totalPage = this.TotalPage;
            if (nextPageIndex >= totalPage)
            {
                nextPageIndex = totalPage;
            }
            return this.GetUrl(nextPageIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        public long BeginIndex
        {
            get
            {
                long i = (this.PageIndex - 1) * this.PageSize + 1;
                return i > RecordCount ? RecordCount : i;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public long EndIndex
        {
            get
            {
                long i = BeginIndex + this.PageSize - 1;
                return i > RecordCount ? RecordCount : i;
            }
        }

    }
}
