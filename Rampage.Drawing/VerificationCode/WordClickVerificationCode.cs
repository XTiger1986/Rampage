using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码 - 依次点击英文单词（派生自中文点选，仅取材与默认尺寸不同） /
    /// Click captcha: click the English words in the prompted order (derived from the character mode; only seeding and defaults differ).
    /// </summary>
    /// <remarks>
    ///  Description:    英文单词点选验证码
    ///                  复用中文点选的背景降级、碰撞排版、绘制与校验全链路，取材改从英文常用词表
    ///                  （EnglishWordPool，约 400 词，全小写；可经 ClickVerificationCodeOptions.EnglishWordPool
    ///                  覆盖）抽取互异单词，提示前缀为“Please click in order: ”。
    ///                  默认 RandomizeCounts=true：每次生成在“2 目标词+2 干扰词”与“3 目标词+1 干扰词”间随机；
    ///                  手动配置 3+2（5 词）时画布与字号自适应仍可容纳。
    ///  Description:    The character mode's background fallback, collision layout, drawing and verification are
    ///  reused as-is; seeding draws distinct words from the built-in common English pool (~400 lowercase words,
    ///  overridable via ClickVerificationCodeOptions.EnglishWordPool) with the prompt prefix
    ///  "Please click in order: ". RandomizeCounts defaults to true: each generation randomly uses
    ///  "2 targets + 2 distractors" or "3 targets + 1 distractor"; a manual 3+2 (five words) still fits
    ///  thanks to the wider canvas and adaptive font size.
    ///
    /// </remarks>
    public class WordClickVerificationCode : ClickVerificationCode
    {

        /// <summary>
        /// 初始化英文单词点选验证码（画布 440×160、字号 28、随机 2+2 / 3+1 组合、英文提示前缀） /
        /// Initializes the word click captcha (440×160 canvas, font size 28, randomized 2+2 / 3+1 mix, English prompt prefix).
        /// </summary>
        public WordClickVerificationCode()
        {
            this.ImageWidth = 440;
            this.FontSize = 28;
            this.RandomizeCounts = true;
            this.PromptPrefix = "Please click in order: ";
        }

        /// <summary>
        /// 获取样式名称：点选单词 / Gets the style name: click-the-words.
        /// </summary>
        public override string Name
        {
            get { return "点选单词"; }
        }

        /// <summary>
        /// 从英文词池不重复抽取指定个数的单词（词源：ClickVerificationCodeOptions.EnglishWordPool 覆盖优先，
        /// 否则内置常用词表；统一转小写、去空白并去重后抽取） /
        /// Picks the requested number of distinct words from the English pool (the options override wins over the
        /// built-in pool; entries are trimmed, lowercased and deduplicated before picking).
        /// </summary>
        /// <param name="count">要抽取的个数（调用方保证 ≥ 1） / How many to pick (the caller guarantees ≥ 1).</param>
        /// <param name="excluded">须排除的已用词集合 / The set of words to exclude.</param>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>抽中的单词列表（全小写） / The picked words (all lowercase).</returns>
        /// <exception cref="InvalidOperationException">剩余可用单词不足时抛出 / Thrown when the pool has too few usable words left.</exception>
        protected override List<string> PickSeeds(int count, HashSet<string> excluded, Random random)
        {
            string[]? configured = ClickVerificationCodeOptions.EnglishWordPool;
            IEnumerable<string> source = configured != null && configured.Length > 0
                ? configured
                : EnglishWordPool.DefaultWords;

            // 统一小写并去重（调用方传入的词表未必规范） / Normalize case and dedupe (caller pools may be messy).
            string[] candidates = source
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Select(word => word.Trim().ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            List<string> picked = new List<string>(count);
            int attempts = 0;
            int maxAttempts = Math.Max(candidates.Length * 20, 1000);

            while (picked.Count < count)
            {
                if (attempts++ > maxAttempts)
                {
                    throw new InvalidOperationException(
                        $"剩余可用单词不足以抽取 {count} 个不重复项 / Not enough distinct words left to pick {count}.");
                }

                string candidate = candidates[random.Next(candidates.Length)];
                if (excluded.Contains(candidate) || picked.Contains(candidate))
                {
                    continue;
                }
                picked.Add(candidate);
            }

            return picked;
        }
    }
}
