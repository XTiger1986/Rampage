using System;
using System.Text;

namespace Rampage
{
    /// <summary>
    /// StringBuilder 的封装，支持用 + 运算符链式追加各类值 / A StringBuilder wrapper whose + operator appends values of various types for chaining
    /// </summary>
    public class StringBuffer
    {
        private StringBuilder _innerBuilder = new StringBuilder();

        /// <summary>
        /// 内部 StringBuilder 实例 / The wrapped StringBuilder instance
        /// </summary>
        public StringBuilder InnerBuilder
        {
            get { return _innerBuilder; }
        }

        /// <summary>
        /// 当前字符长度 / Current character length
        /// </summary>
        public int Length
        {
            get
            {
                return _innerBuilder.Length;
            }
        }

        /// <summary>
        /// 移除指定范围的字符 / Removes a range of characters
        /// </summary>
        /// <param name="startIndex">起始位置 / Zero-based start index</param>
        /// <param name="length">移除的字符数 / Number of characters to remove</param>
        public void Remove(int startIndex, int length)
        {
            _innerBuilder.Remove(startIndex, length);
        }

        /// <summary>
        /// 将所有匹配的子串替换为新字符串 / Replaces every occurrence of a substring with a new string
        /// </summary>
        /// <param name="oldValue">被替换的子串 / Substring to replace</param>
        /// <param name="newValue">替换后的字符串 / Replacement string</param>
        public void Replace(string oldValue, string newValue)
        {
            _innerBuilder.Replace(oldValue, newValue);
        }

        /// <summary>
        /// 创建空缓冲区 / Creates an empty buffer
        /// </summary>
        public StringBuffer()
        {
            _innerBuilder = new StringBuilder();
        }

        /// <summary>
        /// 以指定初始容量创建缓冲区 / Creates a buffer with the given initial capacity
        /// </summary>
        /// <param name="capacity">初始容量 / Initial capacity</param>
        public StringBuffer(int capacity)
        {
            _innerBuilder = new StringBuilder(capacity);
        }

        /// <summary>
        /// 以指定字符串创建缓冲区 / Creates a buffer from the given string
        /// </summary>
        /// <param name="value">初始字符串 / Initial string</param>
        public StringBuffer(string value)
        {
            _innerBuilder = new StringBuilder(value);
        }

        /// <summary>
        /// 以指定初始容量与最大容量创建缓冲区 / Creates a buffer with initial and maximum capacity
        /// </summary>
        /// <param name="capacity">初始容量 / Initial capacity</param>
        /// <param name="maxCapacity">最大容量 / Maximum capacity</param>
        public StringBuffer(int capacity, int maxCapacity)
        {
            _innerBuilder = new StringBuilder(capacity, maxCapacity);
        }

        /// <summary>
        /// 以指定字符串与初始容量创建缓冲区 / Creates a buffer from a string with the given initial capacity
        /// </summary>
        /// <param name="value">初始字符串 / Initial string</param>
        /// <param name="capacity">初始容量 / Initial capacity</param>
        public StringBuffer(string value, int capacity)
        {
            _innerBuilder = new StringBuilder(value, capacity);
        }

        /// <summary>
        /// 以字符串的子串与初始容量创建缓冲区 / Creates a buffer from a substring of the given string with the given initial capacity
        /// </summary>
        /// <param name="value">源字符串 / Source string</param>
        /// <param name="startIndex">子串起始位置 / Zero-based start index of the substring</param>
        /// <param name="length">子串长度 / Length of the substring</param>
        /// <param name="capacity">初始容量 / Initial capacity</param>
        public StringBuffer(string value, int startIndex, int length, int capacity)
        {
            _innerBuilder = new StringBuilder(value, startIndex, length, capacity);
        }

        /// <summary>
        /// 直接包装现有 StringBuilder 实例（不复制） / Wraps an existing StringBuilder instance without copying
        /// </summary>
        /// <param name="innerBuilder">被包装的实例 / The instance to wrap</param>
        public StringBuffer(StringBuilder innerBuilder)
        {
            _innerBuilder = innerBuilder;
        }

        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, bool value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, byte value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, char value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, char[] value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, decimal value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, double value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, float value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, int value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, long value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, object value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, sbyte value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, short value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, string value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, uint value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, ulong value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }
        /// <summary>
        /// 追加一个值并返回原缓冲区（支持链式追加） / Appends a value and returns the same buffer for chaining
        /// </summary>
        /// <param name="buffer">目标缓冲区 / Target buffer</param>
        /// <param name="value">追加的值 / Value to append</param>
        /// <returns>原缓冲区实例 / The same buffer instance</returns>
        public static StringBuffer operator +(StringBuffer buffer, ushort value)
        {
            buffer.InnerBuilder.Append(value);

            return buffer;
        }

        /// <summary>
        /// 返回内部 StringBuilder 的字符串内容 / Returns the buffered string content
        /// </summary>
        /// <returns>缓冲的字符串 / The buffered string</returns>
        public override string ToString()
        {
            return InnerBuilder.ToString();
        }
    }
}
