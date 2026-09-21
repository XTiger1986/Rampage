using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 批量数据变更状态信息
    /// Batch data operation status information
    /// </summary>
    /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
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
        /// <remarks>
        /// 注意：当前实现传入的是本批数据在源列表中的起始下标（0、batchSize、2*batchSize…），而不是从 0 开始的批次序号。
        /// Note: the current implementation passes the batch's start index in the source list (0, batchSize, 2*batchSize, ...), not a zero-based batch ordinal.
        /// </remarks>
        public int BatchIndex
        {
            get; set;
        }

        /// <summary>
        /// 批量数据执行返回影响行数
        /// Affected row count of the current batch operation
        /// </summary>
        public int Effects
        {
            get; set;
        }

    }
}
