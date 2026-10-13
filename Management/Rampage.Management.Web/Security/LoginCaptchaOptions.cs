using System;
using System.Collections.Generic;

namespace Rampage.Management.Security
{
    /// <summary>
    /// 登录验证码配置（配置节 LoginCaptcha）/ Login captcha options (config section LoginCaptcha)
    /// </summary>
    /// <remarks>
    /// 类型选择由 CodeTypes 单数组表达三种模式：
    ///   null / 空数组 / 全部非法值 → 全部类型随机（1–10 图形 + 11 点选文字 + 12 点选单词）；
    ///   多个元素 → 指定类型集合内随机；
    ///   单个元素 → 固定指定类型。
    /// Type selection is expressed by the single CodeTypes array in three modes:
    ///   null / empty / all-invalid → random across every type (1–10 graphic + 11 click-text + 12 click-word);
    ///   multiple entries → random within the specified set;
    ///   a single entry → the fixed specified type.
    /// </remarks>
    public class LoginCaptchaOptions
    {
        /// <summary>
        /// 配置节名常量 "LoginCaptcha" / The config section name constant "LoginCaptcha"
        /// </summary>
        public const string SectionName = "LoginCaptcha";

        /// <summary>
        /// 点选文字验证码类型编号（1–10 为图形验证码，11/12 为点选式，与 VerificationCodeManager 的编号体系衔接）
        /// / The click-text captcha type id (1–10 are graphic captchas; 11/12 are click styles, bridging VerificationCodeManager's numbering)
        /// </summary>
        public const int ClickTextType = 11;

        /// <summary>
        /// 点选单词验证码类型编号 / The click-word captcha type id
        /// </summary>
        public const int ClickWordType = 12;

        /// <summary>是否启用登录验证码，默认 false（不启用）/ Whether the login captcha is enabled, default false (disabled)</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 参与随机的验证码类型编号集合（1–10 图形、11 点选文字、12 点选单词）；null/空/全非法时回退全部类型
        /// / The captcha type ids to draw from (1–10 graphic, 11 click-text, 12 click-word); null/empty/all-invalid falls back to every type
        /// </summary>
        public int[]? CodeTypes { get; set; }

        /// <summary>验证码有效期（秒），默认 300 / Captcha validity in seconds, default 300</summary>
        public int ExpireSeconds { get; set; } = 300;

        /// <summary>
        /// 从 GlobalConfiguration 读取；缺省或读取异常时返回默认值
        /// / Load from GlobalConfiguration; returns defaults when missing or on error
        /// </summary>
        /// <returns>配置实例 / The options instance</returns>
        public static LoginCaptchaOptions Load()
        {
            try
            {
                return GlobalConfiguration.GetSectionConfig<LoginCaptchaOptions>(SectionName) ?? new LoginCaptchaOptions();
            }
            catch
            {
                return new LoginCaptchaOptions();
            }
        }

        /// <summary>
        /// 解析实际生效的类型集合：去重、剔除非法编号，结果为空时回退全部类型
        /// / Resolve the effective type set: deduplicate, drop invalid ids, and fall back to every type when nothing valid remains
        /// </summary>
        /// <returns>生效类型编号数组（至少一项）/ The effective type id array (never empty)</returns>
        public int[] ResolveEffectiveTypes()
        {
            if (CodeTypes != null && CodeTypes.Length > 0)
            {
                HashSet<int> valid = new HashSet<int>();
                foreach (int type in CodeTypes)
                {
                    if ((type >= 1 && type <= 10) || type == ClickTextType || type == ClickWordType)
                    {
                        valid.Add(type);
                    }
                }
                if (valid.Count > 0)
                {
                    int[] result = new int[valid.Count];
                    valid.CopyTo(result);
                    return result;
                }
            }
            int[] all = new int[12];
            for (int i = 0; i < 10; i++)
            {
                all[i] = i + 1;
            }
            all[10] = ClickTextType;
            all[11] = ClickWordType;
            return all;
        }
    }
}
