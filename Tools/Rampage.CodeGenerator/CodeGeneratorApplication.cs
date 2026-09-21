using Rampage.CodeGenerator.Model;
using Rampage.CodeGenerator.Service;
using Rampage.Data;
using Rampage.DependencyInjection;
using Rampage.Log;
using Rampage.Utilities;
using System;

namespace Rampage.CodeGenerator
{
    /// <summary>
    /// 代码生成应用程序：交互式选择数据库连接与代码模板，为指定表批量解析模板并输出代码文件 /
    /// Code generator application: interactively selects a database connection and a template,
    /// then parses templates for each table and writes the output code files
    /// </summary>
    public class CodeGeneratorApplication : RampageBasicApplication, IRampageApplication
    {
        /// <summary>代码生成服务（自动注入）/ Generate service (auto-injected)</summary>
        [Auto]
        protected IGenerateService GenerateBo = default!;
        /// <summary>模板解析服务（自动注入）/ Template service (auto-injected)</summary>
        [Auto]
        protected ITemplateService TemplateBo = default!;

        /// <summary>
        /// 初始化服务（当前仅调用基类实现）/ Initializes services (currently only calls the base implementation)
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        public override void InitializeServices(string[] args)
        {
            base.InitializeServices(args);
        }

        /// <summary>
        /// 应用主流程：先执行基类启动逻辑，再进入代码生成交互 / Main flow: runs the base startup logic and then the generation interaction
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        public override void Run(string[] args)
        {
            base.Run(args);
            Generate();
        }

        /// <summary>
        /// 执行一轮完整的代码生成交互流程 / Runs one full round of the generation interaction
        /// </summary>
        /// <remarks>
        /// 流程：列出 ConnectionSetting 配置供选择 → 读取该库的表信息 → 列出可用模板供选择 →
        /// 对每张表逐一解析模板的全部文件（列→语言类型与列→DbType 的映射以回调委托传入 TemplateBo.Parse），
        /// 任一模板文件解析失败即整体中断。结束后询问“是否继续”，输入 Y 则递归重复整轮流程。
        /// Flow: list ConnectionSetting entries → load table metadata → list templates → parse every template file
        /// for each table (column-to-language-type and column-to-DbType mappings are passed to TemplateBo.Parse as callbacks),
        /// aborting on the first failed file; answering "Y" to the "continue?" prompt restarts the round recursively.
        /// </remarks>
        private void Generate()
        {
            try
            {
                List<ConnectionSetting> connectionSettings = GlobalConfiguration.GetSectionConfig<List<ConnectionSetting>>("ConnectionSetting")!;

                if (connectionSettings != null && connectionSettings.Count > 0)
                {
                    Console.WriteLine("请选择要连接的数据库：");
                    for (int i = 0; i < connectionSettings.Count; i++)
                    {
                        var connectionSetting = connectionSettings[i];

                        Console.WriteLine("{0}: {1}", (i + 1), connectionSetting.Name);
                    }

                    int selectedNum = 0;
                    while (selectedNum <= 0 || selectedNum > connectionSettings.Count)
                    {
                        string? input = Console.ReadLine();
                        if (!string.IsNullOrEmpty(input))
                        {
                            selectedNum = StringTool.TryParse<int>(input, 0);
                        }
                    }

                    var selectedConnectionSetting = connectionSettings[selectedNum - 1];

                    Console.WriteLine("已选择数据配置：{0}，连接器：{1}.{2}，连接串：{3}", selectedConnectionSetting.Name, selectedConnectionSetting.DatabaseLibrary, selectedConnectionSetting.Provider, selectedConnectionSetting.ConnectionString);
                    List<DbTableInfo> tableList = GenerateBo.GetTableInfoList(selectedConnectionSetting.Name!);
                    if (tableList != null && tableList.Count > 0)
                    {
                        Console.WriteLine("共设定生成{0}张表的代码：{1}", tableList.Count, StringTool.Implode(tableList.Select(t => t.TableName)));

                        int selectedTemplateNum = 0;
                        List<TemplateSetting> templateList = TemplateBo.GetTemplateSettings();
                        Console.WriteLine("请选择要使用的生成模板：");
                        for (int i = 0; i < templateList.Count; i++)
                        {
                            var template = templateList[i];

                            Console.WriteLine("{0}: {1}", (i + 1), template.Name);
                        }

                        while (selectedTemplateNum <= 0 || selectedTemplateNum > templateList.Count)
                        {
                            string? input = Console.ReadLine();
                            if (!string.IsNullOrEmpty(input))
                            {
                                selectedTemplateNum = StringTool.TryParse<int>(input, 0);
                            }
                        }

                        var selectedTemplate = templateList[selectedTemplateNum - 1];
                        if (selectedTemplate.Files == null || selectedTemplate.Files.Count == 0)
                        {
                            throw new InvalidOperationException("未设置模板文件，模板配置：" + selectedTemplate.Name);
                        }
                        Console.WriteLine("已选择模板：{0}，生成代码语言：{1}", selectedTemplate.Name, selectedTemplate.Language);

                        Console.WriteLine("开始生成代码...");

                        foreach (var tableInfo in tableList)
                        {
                            bool isFailed = false;
                            foreach (var templateFile in selectedTemplate.Files)
                            {
                                bool result = TemplateBo.Parse(tableInfo, selectedTemplate, templateFile, delegate (DbColumnInfo columnInfo)
                                {
                                    return GenerateBo.ParseLanguageType(selectedConnectionSetting.Name!, columnInfo);
                                }, delegate (DbColumnInfo columnInfo)
                                {
                                    return GenerateBo.ParseDbParameterType(selectedConnectionSetting.Name!, columnInfo);
                                });
                                if (!result)
                                {
                                    isFailed = true;
                                    Console.WriteLine("模板文件：{0}，生成代码失败，退出", templateFile.FileName);
                                    break;
                                }
                            }

                            if (isFailed)
                            {
                                break;
                            }
                            Console.WriteLine("表【{0}】代码生成完毕", tableInfo.TableName);
                        }

                        string outputDirectoryPath = TemplateBo.GetOutputDirectoryPath(selectedTemplate);
                        Console.WriteLine("生成完毕，输出目录：{0}", outputDirectoryPath);
                    }
                    else
                    {
                        Console.WriteLine("未获取到要生成代码的数据表！");
                    }
                }
                else
                {
                    Console.WriteLine("未检测到任何数据库连接配置！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("程序出错：{0}", ex.Message);
                RampageLogger.Error("程序出错", ex);
            }

            Console.WriteLine("是否继续？(Y/N)");
            string? yn = Console.ReadLine();
            if (!string.IsNullOrEmpty(yn))
            {
                if (StringTool.EqualsIgnoreCase(yn, "Y"))
                {
                    Generate();
                }
            }
        }


    }
}
