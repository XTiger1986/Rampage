using System;

namespace Rampage
{
    /// <summary>
    /// 值区间段协议
    /// </summary>
    public interface IValueScope
    {

        /// <summary>
        /// 获取开始值
        /// </summary>
        object? GetStartValue();

        /// <summary>
        /// 获取结束值
        /// </summary>
        object? GetEndValue();

    }

    /// <summary>
    /// 值区间段
    /// </summary>
    /// </remarks>
    public class ValueScope<V> : IValueScope
    {

        /// <summary>
        /// 
        /// </summary>
        public ValueScope()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public ValueScope(V? start, V? end)
        {
            this.StartValue = start;
            this.EndValue = end;
        }

        /// <summary>
        /// 开始值
        /// </summary>
        public V? StartValue
        {
            get; set;
        }

        /// <summary>
        /// 结束值
        /// </summary>
        public V? EndValue
        {
            get; set;
        }


        /// <summary>
        /// 获取开始值
        /// </summary>
        public object? GetStartValue()
        {
            return this.StartValue;
        }

        /// <summary>
        /// 获取结束值
        /// </summary>
        public object? GetEndValue()
        {
            return this.EndValue;
        }

    }
}