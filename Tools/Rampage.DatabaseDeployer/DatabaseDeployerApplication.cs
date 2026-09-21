using Rampage.DatabaseDeployer.Model;
using Rampage.DatabaseDeployer.Service;
using Rampage.DependencyInjection;
using Rampage.Utilities;
using System;

namespace Rampage.DatabaseDeployer
{
    /// <summary>
    /// 数据库部署应用程序：交互式选择脚本目录，检测/创建版本表后按版本号逐个执行部署脚本 /
    /// Database deployer application: interactively selects a script directory, checks/creates the version table,
    /// then executes pending scripts one by one
    /// </summary>
    public class DatabaseDeployerApplication : RampageBasicApplication, IRampageApplication
    {
        /// <summary>脚本服务（自动注入）/ Script service (auto-injected)</summary>
        [Auto]
        protected IScriptService ScriptBo = default!;
        /// <summary>部署服务（自动注入）/ Deploy service (auto-injected)</summary>
        [Auto]
        protected IDeployService DeployBo = default!;

        /// <summary>
        /// 初始化服务（当前仅调用基类实现）/ Initializes services (currently only calls the base implementation)
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        public override void InitializeServices(string[] args)
        {
            base.InitializeServices(args);
        }

        /// <summary>
        /// 应用主流程：先执行基类启动逻辑，再进入部署交互 / Main flow: runs the base startup logic and then the deployment interaction
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        public override void Run(string[] args)
        {
            base.Run(args);
            Deploy();
        }

        /// <summary>
        /// 执行一轮完整的部署交互流程 / Runs one full round of the deployment interaction
        /// </summary>
        /// <remarks>
        /// 流程：列出脚本根目录下的一级目录供选择 → 检测/创建版本表 → 读取当前版本 → 获取版本号更大的脚本并逐个执行
        /// （任一脚本返回 -500 哨兵值即中断）→ 输出最新版本。结束后询问“是否继续”，输入 Y 则递归重复整轮流程。
        /// Flow: list script directories for selection → check/create the version table → read the current version →
        /// execute each newer script (aborting on the -500 sentinel) → print the final version; answering "Y" to the
        /// "continue?" prompt restarts the whole round recursively.
        /// </remarks>
        private void Deploy()
        {
            try
            {
                List<string> scriptDirectoryNames = ScriptBo.GetSqlScriptDirectoryNames();
                if (scriptDirectoryNames != null && scriptDirectoryNames.Count > 0)
                {
                    Console.WriteLine("请选择要部署的SQL脚本：");
                    for (int i = 0; i < scriptDirectoryNames.Count; i++)
                    {
                        string scriptDirectoryName = scriptDirectoryNames[i];

                        Console.WriteLine("{0}: {1}", (i + 1), scriptDirectoryName);
                    }

                    int selectedNum = 0;
                    while (selectedNum <= 0 || selectedNum > scriptDirectoryNames.Count)
                    {
                        string? input = Console.ReadLine();
                        if (!string.IsNullOrEmpty(input))
                        {
                            selectedNum = StringTool.TryParse<int>(input, 0);
                        }
                    }

                    string selectedDirectoryName = scriptDirectoryNames[selectedNum - 1];

                    Console.Write("检测版本表...");
                    bool dbOK = DeployBo.CheckOrCreateVersionTable(selectedDirectoryName);
                    if (dbOK)
                    {
                        Console.Write("  正常 :-)");
                    }
                    else
                    {
                        throw new InvalidOperationException("版本信息表不存在或无法创建，退出！");
                    }
                    Console.WriteLine();

                    long currentVersion = DeployBo.GetVersion(selectedDirectoryName);
                    Console.WriteLine("已选择：{0}，当前版本：{1}", selectedDirectoryName, currentVersion);

                    List<SqlScript> scriptList = ScriptBo.GetSqlScriptListFromVersion(selectedDirectoryName, currentVersion);
                    Console.WriteLine("检测到可执行脚本：{0}个，开始执行部署...", scriptList.Count);

                    bool isFailed = false;
                    for (int i = 0; i < scriptList.Count; i++)
                    {
                        SqlScript script = scriptList[i];

                        Console.Write("执行脚本：{0}...    ", script.FileName);
                        int effects = DeployBo.ExecuteVersionScript(script);
                        switch (effects)
                        {
                            case -500:
                                Console.Write("脚本执行失败：{0}", script.FileName);
                                Console.WriteLine();
                                isFailed = true;
                                break;
                            default:
                                Console.Write("影响行数：{0}", effects);
                                Console.WriteLine();
                                break;
                        }

                        if (isFailed)
                        {
                            break;
                        }
                    }

                    currentVersion = DeployBo.GetVersion(selectedDirectoryName);
                    if (isFailed)
                    {
                        Console.WriteLine("部署出错中断，当前版本：{0}", currentVersion);
                    }
                    else
                    {
                        Console.WriteLine("部署完毕，当前版本：{0}", currentVersion);
                    }
                }
                else
                {
                    Console.WriteLine("未检测到任何部署SQL脚本");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("程序出错：{0}", ex.Message);
            }

            Console.WriteLine("是否继续？(Y/N)");
            string? yn = Console.ReadLine();
            if (!string.IsNullOrEmpty(yn))
            {
                if (StringTool.EqualsIgnoreCase(yn, "Y"))
                {
                    Deploy();
                }
            }
        }

    }
}
