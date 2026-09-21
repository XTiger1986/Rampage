using System;

namespace Rampage.Utilities
{
    /// <summary>ID 生成工具 / ID generation utility</summary>
    public class IdTool
    {

        /// <summary>
        /// 生成 32 位无连字符的 UUID 字符串："N" 格式本身即为 32 位十六进制（无连字符），
        /// 后续 Replace("-") 属防御性写法实际不生效；适用于对唯一性与并发性能无严格要求的场景 /
        /// Generate a 32-character UUID string without hyphens: the "N" format is already 32 hex digits
        /// (no hyphens), so the subsequent Replace("-") is defensive and has no effect; suitable where
        /// strict uniqueness and concurrency are not required
        /// </summary>
        /// <returns>32 位十六进制 UUID 字符串 / The 32-character hexadecimal UUID string</returns>
        public static string UUID()
        {
            return Guid.NewGuid().ToString("N").Replace("-", string.Empty);
        }

    }
}
