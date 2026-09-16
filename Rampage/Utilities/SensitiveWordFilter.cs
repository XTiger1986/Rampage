using System;
using System.Collections.Generic;

namespace Rampage.Utilities
{
    /// <summary>
    /// 敏感词过滤器（DFA 前缀树）。
    /// Load 构建完成后整树原子替换，运行期读取无锁；词表更新可随时重新 Load。
    /// </summary>
    public class SensitiveWordFilter
    {
        private sealed class TrieNode
        {
            public Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();
            public bool IsEnd;
        }

        private volatile TrieNode _root = new TrieNode();

        /// <summary>
        /// 加载词表（整棵树构建完成后原子替换旧树；忽略空白词）
        /// </summary>
        /// <param name="words"></param>
        public void Load(IEnumerable<string> words)
        {
            if (words == null)
            {
                return;
            }
            TrieNode root = new TrieNode();
            foreach (string word in words)
            {
                if (string.IsNullOrEmpty(word))
                {
                    continue;
                }
                TrieNode node = root;
                foreach (char c in word)
                {
                    if (!node.Children.TryGetValue(c, out TrieNode? child))
                    {
                        child = new TrieNode();
                        node.Children.Add(c, child);
                    }
                    node = child;
                }
                node.IsEnd = true;
            }
            _root = root;
        }

        /// <summary>
        /// 是否包含敏感词
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Contains(string text)
        {
            return FindFirst(text) != null;
        }

        /// <summary>
        /// 查找第一个命中的敏感词；未命中返回 null
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string? FindFirst(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            for (int start = 0; start < text.Length; start++)
            {
                int end = MatchFrom(text, start);
                if (end > start)
                {
                    return text[start..end];
                }
            }
            return null;
        }

        /// <summary>
        /// 将命中的敏感词替换为掩码字符，返回替换后的文本
        /// </summary>
        /// <param name="text"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public string Replace(string text, char mask = '*')
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            char[] buffer = text.ToCharArray();
            for (int start = 0; start < text.Length; start++)
            {
                int end = MatchFrom(text, start);
                if (end > start)
                {
                    for (int i = start; i < end; i++)
                    {
                        buffer[i] = mask;
                    }
                    start = end - 1;
                }
            }
            return new string(buffer);
        }

        private int MatchFrom(string text, int start)
        {
            TrieNode node = _root;
            int matchedEnd = -1;
            for (int i = start; i < text.Length; i++)
            {
                TrieNode? next;
                if (!node.Children.TryGetValue(text[i], out next) || next == null)
                {
                    break;
                }
                node = next;
                if (node.IsEnd)
                {
                    matchedEnd = i + 1;
                }
            }
            return matchedEnd;
        }
    }
}
