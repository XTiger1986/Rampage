using Microsoft.AspNetCore.Http;
using Rampage.Data;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Component
{
    /// <summary>
    /// 数据分页信息，计算页码区间与分页 URL，供分页控件渲染 / Paging data that computes page ranges and paging URLs for pager rendering.
    /// </summary>
    public class DataPager
    {
        private HttpContext? _httpContext = RequestContext.Current;
        /// <summary>
        /// 构造时捕获的当前请求上下文，用于自动生成 <see cref="UrlFormat"/> / The request context captured at construction, used to auto-build <see cref="UrlFormat"/>.
        /// </summary>
        private HttpContext? HttpContext
        {
            get 
            {           
                return _httpContext;
            }
        }

        /// <summary>
        /// 初始化分页信息（须在请求上下文内构造才能自动生成 UrlFormat）/ Initializes the pager (construct within a request context to auto-build UrlFormat).
        /// </summary>
        public DataPager()
        {
        }

        /// <summary>
        /// 以分页信息初始化 / Initializes the pager from a <see cref="PagerInfo"/>.
        /// </summary>
        /// <param name="pagerInfo">查询返回的分页信息 / The paging info returned by a query.</param>
        public DataPager(PagerInfo pagerInfo)
        {
            this.PageIndex = pagerInfo.PageIndex;
            this.PageSize = pagerInfo.PageSize;
            this.RecordCount = pagerInfo.RecordCount != null ? pagerInfo.RecordCount.Value : 0;
        }

        /// <summary>
        /// 以页码、每页数量与总记录数初始化 / Initializes the pager with page index, page size and record count.
        /// </summary>
        /// <param name="pageIndex">页码，小于 1 时按 1 处理 / The page index; values below 1 are treated as 1.</param>
        /// <param name="pageSize">每页数量，小于 1 时按 10 处理 / The page size; values below 1 are treated as 10.</param>
        /// <param name="recordCount">总记录数 / The total record count.</param>
        public DataPager(int pageIndex, int pageSize, int recordCount)
        {
            this.PageIndex = pageIndex < 1 ? 1 : pageIndex;
            this.PageSize = pageSize < 1 ? 10 : pageSize;
            this.RecordCount = recordCount;
        }

        private string? _id;
        /// <summary>
        /// 页码表唯一标识，未设置时自动生成 GUID（N 格式）/ The unique id of the pager; auto-generated as a GUID ("N" format) when not set.
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
        /// 第几页（从 1 开始）/ The current page index (1-based).
        /// </summary>
        public int PageIndex
        {
            get { return _pageIndex; }
            set { _pageIndex = value; }
        }

        private int _pageSize = 10;
        /// <summary>
        /// 每页数量 / The page size.
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        /// <summary>
        /// 总数量 / The total record count.
        /// </summary>
        public long RecordCount
        {
            get;
            set;
        }

        private int _cellSize = 8;
        /// <summary>
        /// 页码显示数（页码条同时展示的页数）/ The number of page cells shown at once in the pager.
        /// </summary>
        public int CellSize
        {
            get { return _cellSize; }
            set { _cellSize = value; }
        }

        private string? _urlFormat;
        /// <summary>
        /// URL地址格式（占位符"{0}"=页码，占位符"{1}"=每页大小）/ The URL format (placeholder "{0}" = page index, "{1}" = page size).
        /// </summary>
        /// <remarks>未显式设置且构造时存在请求上下文，则基于当前请求 URL 将 page 查询参数替换为占位符 {0} 自动生成。</remarks>
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
        /// 锚点名称（拼接到分页 URL 末尾的 #anchor）/ The anchor name appended to paging URLs as #anchor.
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
        /// 总页数（惰性计算并缓存）/ The total page count (lazily computed and cached).
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
        /// 页码条开始页码（惰性计算并缓存）/ The first page number shown in the pager (lazily computed and cached).
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
        /// 页码条结束页码（惰性计算并缓存）/ The last page number shown in the pager (lazily computed and cached).
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
        /// 上一页URL（惰性计算并缓存；第一页时即第一页URL）/ The previous-page URL (lazily computed and cached; equals the first page when already on page 1).
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
        /// 下一页URL（惰性计算并缓存；最后一页时即最后一页URL）/ The next-page URL (lazily computed and cached; equals the last page when already on the final page).
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
        /// 获取总页数 / Computes the total page count.
        /// </summary>
        /// <returns>总页数 / The total page count.</returns>
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
        /// 获取页码条开始页码 / Computes the first page number shown in the pager.
        /// </summary>
        /// <returns>开始页码 / The first page number.</returns>
        /// <remarks>以当前页为中心、按 <see cref="CellSize"/> 划分左右区间，并收敛到 [1, 总页数]；有副作用：会把 <see cref="PageIndex"/> 修正到该区间内。</remarks>
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
        /// 获取页码条结束页码 / Computes the last page number shown in the pager.
        /// </summary>
        /// <returns>结束页码 / The last page number.</returns>
        /// <remarks>以当前页为中心、按 <see cref="CellSize"/> 划分左右区间，并收敛到 [1, 总页数]；有副作用：会把 <see cref="PageIndex"/> 修正到该区间内。</remarks>
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
        /// 获取指定页码的分页URL / Gets the paging URL for the given page index.
        /// </summary>
        /// <param name="pageIndex">页码 / The page index.</param>
        /// <returns>格式化后的URL，设置了 <see cref="Anchor"/> 时追加锚点 / The formatted URL, with the anchor appended when <see cref="Anchor"/> is set.</returns>
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
        /// 获取上一页URL（第一页时返回第一页URL）/ Gets the previous-page URL (returns the first-page URL when on page 1).
        /// </summary>
        /// <returns>上一页URL / The previous-page URL.</returns>
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
        /// 获取下一页URL（已到最后一页时返回最后一页URL）/ Gets the next-page URL (returns the last-page URL when already on the final page).
        /// </summary>
        /// <returns>下一页URL / The next-page URL.</returns>
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
        /// 当前页首条记录的序号（从 1 开始，超出总数时取总数）/ The 1-based index of the first record on the current page (clamped to the record count).
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
        /// 当前页末条记录的序号（从 1 开始，超出总数时取总数）/ The 1-based index of the last record on the current page (clamped to the record count).
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
