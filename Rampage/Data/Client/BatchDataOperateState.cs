using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 批量数据变更状态信息
    /// Batch data operation status information
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class BatchDataOperateState<M> where M : class
    {
        /// <summary>
        /// 批量数据
        /// Batch Data
        /// </summary>
        public List<M> DataList
        {
            get; set;
        } = default!;

        /// <summary>
        /// 第几批次
        /// Batch Index
        /// </summary>
        public int BatchIndex
        {
            get; set;
        }

        /// <summary>
        /// 批量数据执行返回影响行数
        /// Current Batch operation Effects
        /// </summary>
        public int Effects
        {
            get; set;
        }

    }
}
