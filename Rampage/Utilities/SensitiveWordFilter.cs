using System;
using System.Collections.Generic;

namespace Rampage.Utilities
{
    /// <summary>
    /// 敏感词过滤器（DFA 前缀树） /
    /// Sensitive word filter (DFA prefix trie)
    /// </summary>
    /// <remarks>
    /// Load 在整棵新树构建完成后通过 volatile 根节点原子替换旧树，运行期匹配读取无锁；
    /// 词表可随时重新 Load 热更新。
    /// Load builds a whole new trie and atomically swaps it in via the volatile root, so runtime matching
    /// reads are lock-free; the word list can be reloaded at any time to hot-swap it.
    /// </remarks>
    public class SensitiveWordFilter
    {
        private sealed class TrieNode
        {
            public Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();
            public bool IsEnd;
        }

        private volatile TrieNode _root = new TrieNode();

        /// <summary>
        /// 加载词表（整棵树构建完成后原子替换旧树；null 与空白词被忽略） /
        /// Load the word list (the fully built trie atomically replaces the old one; null and blank words are ignored)
        /// </summary>
        /// <param name="words">敏感词集合，null 时不做任何动作 / The word collection; a null value is a no-op</param>
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
        /// 是否包含敏感词 /
        /// Check whether the text contains any loaded word
        /// </summary>
        /// <param name="text">待检测文本 / The text to check</param>
        /// <returns>包含返回 true / true when any word matches</returns>
        public bool Contains(string text)
        {
            return FindFirst(text) != null;
        }

        /// <summary>
        /// 查找第一个命中的敏感词（每个起点取最长匹配）；未命中返回 null /
        /// Find the first matched word (longest match at each start position); returns null when nothing matches
        /// </summary>
        /// <param name="text">待检测文本 / The text to check</param>
        /// <returns>命中的敏感词，未命中为 null / The first matched word, or null</returns>
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
        /// 将命中的敏感词替换为掩码字符，返回替换后的文本 /
        /// Replace every matched word with the mask character and return the result
        /// </summary>
        /// <param name="text">待处理文本 / The text to process</param>
        /// <param name="mask">掩码字符，默认 '*' / The mask character, '*' by default</param>
        /// <returns>替换后的文本 / The masked text</returns>
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
                    // 回退1，配合循环自增跳过已掩码片段 / step back 1; the loop increment then skips past the masked segment
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
                    // 记录到目前为止的最长完整词结束位置（继续尝试更长匹配）/ record end of the longest complete word so far (keep trying longer matches)
                    matchedEnd = i + 1;
                }
            }
            return matchedEnd;
        }
    }
}
