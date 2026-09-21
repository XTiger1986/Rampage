using System;

namespace Rampage
{
    /// <summary>
    /// 值区间段协议（非泛型访问视图） / Value range protocol (non-generic access view)
    /// </summary>
    public interface IValueScope
    {

        /// <summary>
        /// 获取区间开始值 / Gets the start value of the range
        /// </summary>
        /// <returns>开始值，可能为 null / The start value, possibly null</returns>
        object? GetStartValue();

        /// <summary>
        /// 获取区间结束值 / Gets the end value of the range
        /// </summary>
        /// <returns>结束值，可能为 null / The end value, possibly null</returns>
        object? GetEndValue();

    }

    /// <summary>
    /// 值区间段（开始值与结束值构成的闭区间描述） / A range described by a start value and an end value
    /// </summary>
    /// <typeparam name="V">区间值类型 / Type of the range values</typeparam>
    public class ValueScope<V> : IValueScope
    {

        /// <summary>
        /// 创建空区间 / Creates an empty range
        /// </summary>
        public ValueScope()
        {
        }

        /// <summary>
        /// 以开始值与结束值创建区间 / Creates a range with the given start and end values
        /// </summary>
        /// <param name="start">开始值 / Start value</param>
        /// <param name="end">结束值 / End value</param>
        public ValueScope(V? start, V? end)
        {
            this.StartValue = start;
            this.EndValue = end;
        }

        /// <summary>
        /// 开始值 / Start value
        /// </summary>
        public V? StartValue
        {
            get; set;
        }

        /// <summary>
        /// 结束值 / End value
        /// </summary>
        public V? EndValue
        {
            get; set;
        }


        /// <summary>
        /// 获取区间开始值 / Gets the start value of the range
        /// </summary>
        /// <returns>开始值，可能为 null / The start value, possibly null</returns>
        public object? GetStartValue()
        {
            return this.StartValue;
        }

        /// <summary>
        /// 获取区间结束值 / Gets the end value of the range
        /// </summary>
        /// <returns>结束值，可能为 null / The end value, possibly null</returns>
        public object? GetEndValue()
        {
            return this.EndValue;
        }

    }
}