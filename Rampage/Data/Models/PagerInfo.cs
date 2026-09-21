using System;

namespace Rampage.Data
{
    /// <summary>
    /// 数据查询分页信息
    /// Data query pager information
    /// </summary>
    /// <remarks>
    /// 分页偏移量按 Offset = (PageIndex - 1) * PageSize 计算。
    /// </remarks>
    public class PagerInfo
    {
        private int _pageIndex = 1;
        /// <summary>
        /// 页码，从 1 开始；小于等于 0 的值在读取时归一为 1
        /// Page index starting at 1; values &lt;= 0 are normalized to 1 on read
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
        /// 每页条数，默认 20。注意：小于等于 0 的值在读取时归一为 1（而不是 20）
        /// Page data size, default 20. Note: values &lt;= 0 are normalized to 1 (not 20) on read
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
        /// 记录总数。若预先设置该值（已知总数），分页查询不再生成 count 语句；
        /// 未预置时，分页查询会在同一批 GridReader 结果中追加 COUNT(1) 查询（列名 _record_count_）并回填该值
        /// Total record count. When preset (count already known), the paging query skips the "select count" statement;
        /// otherwise an extra COUNT(1) (column _record_count_) is appended to the same grid-reader batch and written back here
        /// </summary>
        public long? RecordCount
        {
            get;
            set;
        }

        private int? _totalPage;
        /// <summary>
        /// 总页数；由 RecordCount 与 PageSize 计算而来，RecordCount 为 null 时返回 null
        /// Total page count; derived from RecordCount and PageSize, null while RecordCount is null
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
