using Rampage.CodeGenerator.Model;
using Rampage.CodeGenerator.Service;
using Rampage.Data;
using Rampage.DependencyInjection;
using Rampage.Log;
using Rampage.Utilities;
using System;

namespace Rampage.CodeGenerator
{
    public class CodeGeneratorApplication : RampageBasicApplication, IRampageApplication
    {
        [Auto]
        protected IGenerateService GenerateBo = default!;
        [Auto]
        protected ITemplateService TemplateBo = default!;

        public override void InitializeServices(string[] args)
        {
            base.InitializeServices(args);
        }

        public override void Run(string[] args)
        {
            base.Run(args);
            Generate();
        }

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
