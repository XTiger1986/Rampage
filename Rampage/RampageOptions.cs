using System;

namespace Rampage
{
    /// <summary>
    /// Rampage 应用程序启动选项（集中承载启动参数，便于后续扩展更多启动配置项） /
    /// Rampage application boot options (centralizes boot arguments, leaving room for future startup parameters)
    /// </summary>
    /// <remarks>
    /// 供 <see cref="RampageBooter.Run{Z}(RampageOptions)"/> 使用；当前包含原始启动参数与环境名两项，
    /// 后续新增启动参数时应扩展本类而非增加 Run 重载。
    /// Consumed by <see cref="RampageBooter.Run{Z}(RampageOptions)"/>; it currently carries the raw arguments
    /// and the environment name — future startup parameters should extend this class instead of adding Run overloads.
    /// </remarks>
    public class RampageOptions
    {

        /// <summary>
        /// 以原始启动参数（及可选的显式环境名）初始化选项 / Initializes the options with the raw boot arguments (and an optional explicit environment name)
        /// </summary>
        /// <param name="args">原始启动参数，透传给应用各初始化步骤；未显式指定环境名时由 <see cref="EnvironmentName"/> 之外的解析路径从中取值 / The raw boot arguments, passed through to every initialization step; parsed for the environment name when none is set explicitly</param>
        /// <param name="environmentName">显式环境名；非空时优先于从 args 解析，null 表示由应用的 InitializeEnvironment 从 args 解析 / Explicit environment name; takes precedence over parsing from args when non-null, null lets the application's InitializeEnvironment parse it from args</param>
        public RampageOptions(string[] args, string? environmentName = null)
        {
            RawArgs = args;
            EnvironmentName = environmentName;
        }

        /// <summary>
        /// 原始启动参数数组，透传给应用全部初始化步骤与 Run / The raw boot arguments, passed through to all initialization steps and Run
        /// </summary>
        public string[] RawArgs
        {
            get; private set;
        }

        /// <summary>
        /// 显式指定的环境名；非空时优先生效，null 时由 InitializeEnvironment 从 RawArgs 解析 /
        /// The explicitly specified environment name; takes precedence when non-null, otherwise InitializeEnvironment parses it from RawArgs
        /// </summary>
        public string? EnvironmentName
        {
            get; set;
        }

    }
}
