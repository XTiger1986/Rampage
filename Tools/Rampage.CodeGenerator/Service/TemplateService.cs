using Microsoft.Extensions.Configuration;
using Rampage.CodeGenerator.Model;
using Rampage.Data;
using Rampage.Utilities;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Rampage.CodeGenerator.Service
{
    /// <summary>
    /// 模板服务接口：扫描模板目录、用自研模板语法解析模板并输出代码文件 /
    /// Template service interface: scans template directories, parses templates with the built-in template syntax and writes output code files
    /// </summary>
    public interface ITemplateService
    {

        /// <summary>
        /// 扫描模板根目录，加载各含 template.json 子目录的模板设置列表 /
        /// Scans the template root and loads the settings of every subdirectory containing template.json
        /// </summary>
        /// <returns>模板设置列表（DirectoryName 自动填充为子目录名）/ The template setting list (DirectoryName auto-filled with the subdirectory name)</returns>
        List<TemplateSetting> GetTemplateSettings();

        /// <summary>
        /// 针对一张表解析一个模板文件并写出代码文件 / Parses one template file for one table and writes the output code file
        /// </summary>
        /// <param name="tableInfo">表信息（含列信息）/ Table info (including columns)</param>
        /// <param name="templateSetting">模板设置 / Template setting</param>
        /// <param name="templateFile">模板文件设置 / Template file setting</param>
        /// <param name="parseLanguageTypeHandler">列→语言类型名的映射回调 / Callback mapping a column to its language type name</param>
        /// <param name="parseDbParameterTypeHandler">列→DbType 参数类型的映射回调 / Callback mapping a column to its DbType parameter type</param>
        /// <returns>成功返回 true；任何异常（未向外抛出）返回 false / True on success; false on any exception (which is swallowed)</returns>
        bool Parse(DbTableInfo tableInfo, TemplateSetting templateSetting, TemplateFile templateFile, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler);

        /// <summary>
        /// 获取模板的代码输出目录（程序集基目录 + OutputRoot + 模板目录名）/
        /// Gets the output directory for a template (base directory + OutputRoot + template directory name)
        /// </summary>
        /// <param name="templateSetting">模板设置 / Template setting</param>
        /// <returns>输出目录的绝对路径 / The absolute output directory path</returns>
        string GetOutputDirectoryPath(TemplateSetting templateSetting);

    }

    /// <summary>
    /// 模板服务实现：基于一组预编译正则的自研模板引擎 /
    /// Template service implementation: a home-grown template engine built on precompiled regexes
    /// </summary>
    /// <remarks>
    /// 模板语法 / Template syntax:
    /// <list type="bullet">
    /// <item>表级占位符 / table-level placeholders：@{Namespace}（RootNamespace，未配置时回退默认值）、
    /// @{EntityName}（表名转 PascalCase 实体名）、@{entityName}（首字母小写的实体名）、@{TableName}（原始表名）、@{TableComment}（表注释）</item>
    /// <item>列循环块 / column loop block：{@each ColumnList as column, index}...{/each}，
    /// ", index" 可省略，index 为列下标（从 0 开始），循环体对每一列展开一次</item>
    /// <item>列属性条件块 / column-property conditional block：{@if column.PrimaryKey}...{/if}，
    /// 属性支持 PrimaryKey、AutoIncrement、IsNullable、IsUnsigned，为假时整块替换为空串</item>
    /// <item>索引条件块 / index conditional block：{@if index 运算符 数值}...{/if}，
    /// 运算符支持 ==、&gt;、&lt;、&gt;=、&lt;=，为假时整块替换为空串</item>
    /// <item>列级占位符 / column-level placeholders：@{column.ColumnName}（原始列名）、@{column.Comment}（列注释）、
    /// @{column.PropertyName}（PascalCase 属性名）、@{column.FieldName}（"_"+camelCase 字段名）、
    /// @{column.VariableName}（camelCase 变量名）、@{column.LanguageType}（C# 类型名）、@{column.DbParameterType}（DbType.Xxx）</item>
    /// </list>
    /// </remarks>
    public class TemplateService : ITemplateService
    {

        private readonly GenerateSetting _generateSetting;

        /// <summary>
        /// 初始化服务：读取 GenerateSetting 配置节 / Initializes the service: loads the GenerateSetting section
        /// </summary>
        public TemplateService()
        {
            _generateSetting = GlobalConfiguration.GetSectionConfig<GenerateSetting>("GenerateSetting")!;
        }

        #region Privates

        /// <summary>
        /// 将下划线分隔的名称转为 PascalCase（如 user_name → UserName）/
        /// Converts an underscore-separated name to PascalCase (e.g. user_name → UserName)
        /// </summary>
        /// <param name="name">原始名称（以下划线分段）/ The raw name (underscore-separated)</param>
        /// <returns>PascalCase 名称 / The PascalCase name</returns>
        private string GetCodeName(string name)
        {
            string[] nameParts = name.Split('_');
            string codeName = string.Empty;
            for (int i = 0; i < nameParts.Length; i++)
            {
                string namePart = nameParts[i];

                codeName += string.Concat(namePart.Substring(0, 1).ToUpper(), namePart.Substring(1));
            }
            return codeName;
        }

        /// <summary>
        /// 将下划线分隔的名称转为 camelCase（首段首字母小写、其余段首字母大写，如 user_name → userName）/
        /// Converts an underscore-separated name to camelCase (first segment lower-case, the rest capitalized, e.g. user_name → userName)
        /// </summary>
        /// <param name="name">原始名称（以下划线分段）/ The raw name (underscore-separated)</param>
        /// <returns>camelCase 名称 / The camelCase name</returns>
        private string GetCodeNameFirstLowerCase(string name)
        {
            string[] nameParts = name.Split('_');
            string codeName = string.Empty;
            for (int i = 0; i < nameParts.Length; i++)
            {
                string namePart = nameParts[i];

                codeName += string.Concat(i == 0 ? namePart.Substring(0, 1).ToLower() : namePart.Substring(0, 1).ToUpper(), namePart.Substring(1));
            }
            return codeName;
        }

        // 表级占位符正则 / table-level placeholder regexes
        private readonly Regex _regexRootNamespaceName = new Regex(@"@{\s*Namespace\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{Namespace} / matches @{Namespace}
        private readonly Regex _regexEntityName = new Regex(@"@{\s*EntityName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{EntityName}（PascalCase 实体名）/ matches @{EntityName} (PascalCase)
        private readonly Regex _regexLowerCaseEntityName = new Regex(@"@{\s*entityName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{entityName}（camelCase 实体名）/ matches @{entityName} (camelCase)
        private readonly Regex _regexTableName = new Regex(@"@{\s*TableName\s*}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{TableName}（原始表名）/ matches @{TableName} (raw table name)
        private readonly Regex _regexTableComment = new Regex(@"@{\s*TableComment\s*}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{TableComment}（表注释）/ matches @{TableComment}
        /// <summary>
        /// 替换模板文本中的表级占位符（@{Namespace}、@{EntityName}、@{entityName}、@{TableName}、@{TableComment}）/
        /// Replaces the table-level placeholders in the template text
        /// </summary>
        /// <param name="code">模板文本 / Template text</param>
        /// <param name="tableInfo">表信息 / Table info</param>
        /// <param name="template">模板设置（提供 RootNamespace，为空时回退默认值）/ Template setting (provides RootNamespace, with a default fallback)</param>
        /// <returns>替换后的文本 / The text after replacement</returns>
        private string ParseTableInfo(string code, DbTableInfo tableInfo, TemplateSetting template)
        {
            string tableName = tableInfo.TableName!;
            string entityName = GetCodeName(tableName);
            string entityNameLowerCase = GetCodeNameFirstLowerCase(tableName);

            code = _regexRootNamespaceName.Replace(code, template.RootNamespace ?? "Rampage.CodeGenerator.Templates.RampageFrame");
            code = _regexEntityName.Replace(code, entityName);
            code = _regexLowerCaseEntityName.Replace(code, entityNameLowerCase);
            code = _regexTableName.Replace(code, tableName);
            code = _regexTableComment.Replace(code, tableInfo.Comment!);

            return code;
        }

        // 列循环与条件块正则 / column loop and conditional block regexes
        private readonly Regex _regexEachColumnLoop = new Regex(@"{\s*@\s*each\s+ColumnList\s+as\s+column\s*(,\s*index)?\s*}(?<Code>.+?){\s*/\s*each\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 {@each ColumnList as column, index}...{/each} 循环块 / matches the column loop block
        private readonly Regex _regexIfColumn = new Regex(@"{\s*@\s*if\s*(\(|\s+)?\s*column\.(?<Property>[a-zA-Z0-9_]+)\s*(\)|\s+)?}(?<Code>.+?){\s*/\s*if\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 {@if column.属性}...{/if} 列属性条件块 / matches column-property conditionals
        private readonly Regex _regexIfColumnIndex = new Regex(@"{\s*@\s*if\s*(\(|\s+)?\s*index\s*(?<Operator>[\>\<\=]+)\s*(?<Value>\d+)\s*(\)|\s+)?}(?<Code>.+?){\s*/\s*if\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 {@if index 运算符 数值}...{/if} 索引条件块 / matches index conditionals
        // 列级占位符正则 / column-level placeholder regexes
        private readonly Regex _regexColumnName = new Regex(@"@{\s*column\.ColumnName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.ColumnName}（原始列名）/ matches @{column.ColumnName}
        private readonly Regex _regexColumnComment = new Regex(@"@{\s*column\.Comment\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.Comment}（列注释）/ matches @{column.Comment}
        private readonly Regex _regexPropertyName = new Regex(@"@{\s*column\.PropertyName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.PropertyName}（PascalCase 属性名）/ matches @{column.PropertyName}
        private readonly Regex _regexFieldName = new Regex(@"@{\s*column\.FieldName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.FieldName}（"_"+camelCase 字段名）/ matches @{column.FieldName}
        private readonly Regex _regexVariableName = new Regex(@"@{\s*column\.VariableName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.VariableName}（camelCase 变量名）/ matches @{column.VariableName}
        private readonly Regex _regexLanguageType = new Regex(@"@{\s*column\.LanguageType\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.LanguageType}（C# 类型名）/ matches @{column.LanguageType}
        private readonly Regex _regexDbParameterType = new Regex(@"@{\s*column\.DbParameterType\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace); // 匹配 @{column.DbParameterType}（DbType.Xxx）/ matches @{column.DbParameterType}
        /// <summary>
        /// 展开模板中的列循环块：对每一列求值条件块并替换列级占位符 /
        /// Expands the column loop blocks: evaluates conditionals and replaces column placeholders for every column
        /// </summary>
        /// <param name="code">模板文本 / Template text</param>
        /// <param name="columnInfos">列信息列表 / Column info list</param>
        /// <param name="parseLanguageTypeHandler">列→语言类型名的映射回调 / Callback mapping a column to its language type name</param>
        /// <param name="parseDbParameterTypeHandler">列→DbType 参数类型的映射回调 / Callback mapping a column to its DbType parameter type</param>
        /// <returns>循环块全部展开后的文本 / The text with all loop blocks expanded</returns>
        /// <remarks>
        /// 每列的处理顺序：先按列属性（PrimaryKey/AutoIncrement/IsNullable/IsUnsigned）与列下标求值 {@if} 条件块
        /// （为假整块置空），再替换列级占位符；同一循环模板按列拼接后整体替换原循环块。
        /// Per column: first the {@if} blocks are evaluated by column property and index (false blocks are emptied),
        /// then column placeholders are replaced; the per-column snippets are concatenated and replace the whole loop block.
        /// </remarks>
        private string ParseColumnLoop(string code, List<DbColumnInfo> columnInfos, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler)
        {
            StringBuilder codeBuilder = new StringBuilder(code);
            if (_regexEachColumnLoop.IsMatch(code))
            {
                MatchCollection matches = _regexEachColumnLoop.Matches(code);
                // 从后往前替换：match.Index/Length 基于原始模板，先替换靠后的块不会影响靠前块的索引 / replace from last to first: match.Index/Length refer to the original template, so replacing later blocks first never shifts earlier blocks' indices
                for (int m = matches.Count - 1; m >= 0; m--)
                {
                    Match match = matches[m];
                    string loopTemplateCode = match.Groups["Code"].Value;

                    StringBuilder loopBuilder = new StringBuilder(string.Empty);
                    for (int index = 0; index < columnInfos.Count; index++)
                    {
                        string loopCode = loopTemplateCode;
                        DbColumnInfo columnInfo = columnInfos[index];
                        string columnName = columnInfo.ColumnName!;
                        string columnComment = columnInfo.Comment!;
                        string propertyName = GetCodeName(columnName);
                        string variableName = GetCodeNameFirstLowerCase(columnName);
                        string fieldName = string.Concat("_", variableName);
                        string languageType = parseLanguageTypeHandler(columnInfo);
                        string dbParameterType = parseDbParameterTypeHandler(columnInfo);

                        #region If判断属性条件
                        if (_regexIfColumn.IsMatch(loopTemplateCode))
                        {
                            MatchCollection ifMatches = _regexIfColumn.Matches(loopTemplateCode);
                            foreach (Match ifMatch in ifMatches)
                            {
                                string ifProperty = ifMatch.Groups["Property"].Value;
                                string ifCode = ifMatch.Groups["Code"].Value;

                                bool ifTrue = false;
                                switch (ifProperty)
                                {
                                    case "PrimaryKey":
                                        ifTrue = columnInfo.PrimaryKey;
                                        break;
                                    case "AutoIncrement":
                                        ifTrue = columnInfo.AutoIncrement;
                                        break;
                                    case "IsNullable":
                                        ifTrue = columnInfo.IsNullable;
                                        break;
                                    case "IsUnsigned":
                                        ifTrue = columnInfo.IsUnsigned;
                                        break;
                                }

                                if (ifTrue)
                                {
                                    loopCode = loopCode.Replace(ifMatch.Value, ifCode);
                                }
                                else
                                {
                                    loopCode = loopCode.Replace(ifMatch.Value, string.Empty);
                                }
                            }
                        }
                        #endregion

                        #region If判断索引条件
                        if (_regexIfColumnIndex.IsMatch(loopTemplateCode))
                        {
                            MatchCollection ifMatches = _regexIfColumnIndex.Matches(loopTemplateCode);
                            foreach (Match ifMatch in ifMatches)
                            {
                                string ifOperator = ifMatch.Groups["Operator"].Value;
                                string ifValue = ifMatch.Groups["Value"].Value;
                                string ifCode = ifMatch.Groups["Code"].Value;
                                int intIfValue = StringTool.TryParse<int>(ifValue, 0);

                                bool ifTrue = false;
                                switch (ifOperator)
                                {
                                    case "==":
                                        ifTrue = index == intIfValue;
                                        break;
                                    case ">":
                                        ifTrue = index > intIfValue;
                                        break;
                                    case "<":
                                        ifTrue = index < intIfValue;
                                        break;
                                    case ">=":
                                        ifTrue = index >= intIfValue;
                                        break;
                                    case "<=":
                                        ifTrue = index <= intIfValue;
                                        break;
                                }

                                if (ifTrue)
                                {
                                    loopCode = loopCode.Replace(ifMatch.Value, ifCode);
                                }
                                else
                                {
                                    loopCode = loopCode.Replace(ifMatch.Value, string.Empty);
                                }
                            }
                        }
                        #endregion

                        #region 字段信息
                        loopCode = _regexColumnName.Replace(loopCode, columnName);
                        loopCode = _regexColumnComment.Replace(loopCode, columnComment);
                        loopCode = _regexPropertyName.Replace(loopCode, propertyName);
                        loopCode = _regexFieldName.Replace(loopCode, fieldName);
                        loopCode = _regexVariableName.Replace(loopCode, variableName);
                        loopCode = _regexLanguageType.Replace(loopCode, languageType);
                        loopCode = _regexDbParameterType.Replace(loopCode, dbParameterType);
                        #endregion

                        loopBuilder.Append(loopCode);
                    }

                    string parsedLoopCode = loopBuilder.ToString();

                    // 在匹配位置整块替换 / replace the whole block at its match position
                    codeBuilder.Replace(match.Value, parsedLoopCode, match.Index, match.Length);
                }
            }

            return codeBuilder.ToString();
        }

        #endregion


        /// <summary>
        /// 扫描模板根目录，加载各含 template.json 子目录的模板设置列表 /
        /// Scans the template root and loads the settings of every subdirectory containing template.json
        /// </summary>
        /// <returns>模板设置列表（DirectoryName 自动填充为子目录名；无 template.json 的子目录被跳过）/
        /// The template setting list (DirectoryName auto-filled; subdirectories without template.json are skipped)</returns>
        public List<TemplateSetting> GetTemplateSettings()
        {
            List<TemplateSetting> list = new List<TemplateSetting>();
            string rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _generateSetting.TemplateRoot!);
            DirectoryInfo root = new DirectoryInfo(rootPath);
            foreach(DirectoryInfo templateDirectory in root.GetDirectories())
            {
                string templateSettingPath = Path.Combine(templateDirectory.FullName, "template.json");
                if (!File.Exists(templateSettingPath))
                {
                    continue;
                }

                ConfigurationManager configuration = new ConfigurationManager();
                configuration.AddJsonFile(templateSettingPath);

                TemplateSetting templateSetting = configuration.Get<TemplateSetting>()!;
                templateSetting.DirectoryName = templateDirectory.Name;
                list.Add(templateSetting);
            }

            return list;
        }

        /// <summary>
        /// 获取模板的代码输出目录（程序集基目录 + OutputRoot + 模板目录名）/
        /// Gets the output directory for a template (base directory + OutputRoot + template directory name)
        /// </summary>
        /// <param name="templateSetting">模板设置 / Template setting</param>
        /// <returns>输出目录的绝对路径 / The absolute output directory path</returns>
        public string GetOutputDirectoryPath(TemplateSetting templateSetting)
        {
            string outputDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _generateSetting.OutputRoot!, templateSetting.DirectoryName!);
            return outputDirectoryPath;
        }

        /// <summary>
        /// 针对一张表解析一个模板文件并写出代码文件 / Parses one template file for one table and writes the output code file
        /// </summary>
        /// <param name="tableInfo">表信息（含列信息）/ Table info (including columns)</param>
        /// <param name="templateSetting">模板设置 / Template setting</param>
        /// <param name="templateFile">模板文件设置 / Template file setting</param>
        /// <param name="parseLanguageTypeHandler">列→语言类型名的映射回调 / Callback mapping a column to its language type name</param>
        /// <param name="parseDbParameterTypeHandler">列→DbType 参数类型的映射回调 / Callback mapping a column to its DbType parameter type</param>
        /// <returns>成功返回 true；任何异常（未向外抛出）返回 false / True on success; false on any exception (which is swallowed)</returns>
        /// <remarks>
        /// 依次：读模板文件 → 替换表级占位符 → 展开列循环块 → 以 OutputFile 格式（同样支持表级占位符）拼出输出文件名，
        /// 在输出目录不存在时自动创建后写出文件。
        /// Steps: read the template → replace table placeholders → expand column loops → derive the output file name
        /// from OutputFile (which also supports table placeholders), creating the output directory on demand and writing the file.
        /// </remarks>
        public bool Parse(DbTableInfo tableInfo, TemplateSetting templateSetting, TemplateFile templateFile, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler)
        {
            bool result = false;

            try
            {
                string templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _generateSetting.TemplateRoot!, templateSetting.DirectoryName!, templateFile.FileName!);
                
                string code = FileTool.ReadFile(templateFilePath);

                // 解析表信息 / replace table-level placeholders
                code = ParseTableInfo(code, tableInfo, templateSetting);
                // 解析表字段 / expand column loops
                if (tableInfo.Columns != null && tableInfo.Columns.Count > 0)
                {
                   code = ParseColumnLoop(code, tableInfo.Columns, parseLanguageTypeHandler, parseDbParameterTypeHandler);
                }

                string outputFileDirectoryPath = GetOutputDirectoryPath(templateSetting);
                if (!Directory.Exists(outputFileDirectoryPath))
                {
                    Directory.CreateDirectory(outputFileDirectoryPath);
                }
                string outputFileName = ParseTableInfo(templateFile.OutputFile!, tableInfo, templateSetting);
                string outputFilePath = Path.Combine(outputFileDirectoryPath, outputFileName);

                FileTool.WriteFile(outputFilePath, code);

                result = true;
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }


    }
}
