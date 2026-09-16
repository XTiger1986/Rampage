using Rampage.DatabaseDeployer.Model;
using Rampage.DatabaseDeployer.Service;
using Rampage.DependencyInjection;
using Rampage.Utilities;
using System;

namespace Rampage.DatabaseDeployer
{
    public class DatabaseDeployerApplication : RampageBasicApplication, IRampageApplication
    {
        [Auto]
        protected IScriptService ScriptBo = default!;
        [Auto]
        protected IDeployService DeployBo = default!;

        public override void InitializeServices(string[] args)
        {
            base.InitializeServices(args);
        }

        public override void Run(string[] args)
        {
            base.Run(args);
            Deploy();
        }


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
