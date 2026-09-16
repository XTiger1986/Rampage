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
    /// 
    /// </summary>
    public interface ITemplateService
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<TemplateSetting> GetTemplateSettings();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableInfo"></param>
        /// <param name="templateSetting"></param>
        /// <param name="templateFile"></param>
        /// <param name="parseLanguageTypeHandler"></param>
        /// <param name="parseDbParameterTypeHandler"></param>
        /// <returns></returns>
        bool Parse(DbTableInfo tableInfo, TemplateSetting templateSetting, TemplateFile templateFile, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateSetting"></param>
        /// <returns></returns>
        string GetOutputDirectoryPath(TemplateSetting templateSetting);

    }

    /// <summary>
    /// 
    /// </summary>
    public class TemplateService : ITemplateService
    {

        private readonly GenerateSetting _generateSetting;

        /// <summary>
        /// 
        /// </summary>
        public TemplateService()
        {
            _generateSetting = GlobalConfiguration.GetSectionConfig<GenerateSetting>("GenerateSetting")!;
        }

        #region Privates

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        private readonly Regex _regexRootNamespaceName = new Regex(@"@{\s*Namespace\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexEntityName = new Regex(@"@{\s*EntityName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexLowerCaseEntityName = new Regex(@"@{\s*entityName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexTableName = new Regex(@"@{\s*TableName\s*}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexTableComment = new Regex(@"@{\s*TableComment\s*}", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="tableInfo"></param>
        /// <returns></returns>
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

        private readonly Regex _regexEachColumnLoop = new Regex(@"{\s*@\s*each\s+ColumnList\s+as\s+column\s*(,\s*index)?\s*}(?<Code>.+?){\s*/\s*each\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexIfColumn = new Regex(@"{\s*@\s*if\s*(\(|\s+)?\s*column\.(?<Property>[a-zA-Z0-9_]+)\s*(\)|\s+)?}(?<Code>.+?){\s*/\s*if\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexIfColumnIndex = new Regex(@"{\s*@\s*if\s*(\(|\s+)?\s*index\s*(?<Operator>[\>\<\=]+)\s*(?<Value>\d+)\s*(\)|\s+)?}(?<Code>.+?){\s*/\s*if\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexColumnName = new Regex(@"@{\s*column\.ColumnName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexColumnComment = new Regex(@"@{\s*column\.Comment\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexPropertyName = new Regex(@"@{\s*column\.PropertyName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexFieldName = new Regex(@"@{\s*column\.FieldName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexVariableName = new Regex(@"@{\s*column\.VariableName\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexLanguageType = new Regex(@"@{\s*column\.LanguageType\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        private readonly Regex _regexDbParameterType = new Regex(@"@{\s*column\.DbParameterType\s*}", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="columnInfos"></param>
        /// <param name="parseLanguageTypeHandler"></param>
        /// <param name="parseDbParameterTypeHandler"></param>
        /// <returns></returns>
        private string ParseColumnLoop(string code, List<DbColumnInfo> columnInfos, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler)
        {
            StringBuilder codeBuilder = new StringBuilder(code);
            if (_regexEachColumnLoop.IsMatch(code))
            {
                MatchCollection matches = _regexEachColumnLoop.Matches(code);
                for (int m = 0; m < matches.Count; m++)
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

                    codeBuilder.Replace(match.Value, parsedLoopCode, match.Index, match.Length);
                }
            }

            return codeBuilder.ToString();
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="templateSetting"></param>
        /// <returns></returns>
        public string GetOutputDirectoryPath(TemplateSetting templateSetting)
        {
            string outputDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _generateSetting.OutputRoot!, templateSetting.DirectoryName!);
            return outputDirectoryPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableInfo"></param>
        /// <param name="templateSetting"></param>
        /// <param name="templateFile"></param>
        /// <param name="parseLanguageTypeHandler"></param>
        /// <param name="parseDbParameterTypeHandler"></param>
        /// <returns></returns>
        public bool Parse(DbTableInfo tableInfo, TemplateSetting templateSetting, TemplateFile templateFile, Func<DbColumnInfo, string> parseLanguageTypeHandler, Func<DbColumnInfo, string> parseDbParameterTypeHandler)
        {
            bool result = false;

            try
            {
                string templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _generateSetting.TemplateRoot!, templateSetting.DirectoryName!, templateFile.FileName!);
                
                string code = FileTool.ReadFile(templateFilePath);

                //解析表信息
                code = ParseTableInfo(code, tableInfo, templateSetting);
                //解析表字段
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
