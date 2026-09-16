using Rampage.DatabaseDeployer.Model;
using Rampage.DependencyInjection;
using Rampage.Hierarchy;
using Rampage.Utilities;
using System;
using System.Text.RegularExpressions;

namespace Rampage.DatabaseDeployer.Service
{

    /// <summary>
    /// 
    /// </summary>
    public interface IScriptService : IBaseService
    {
        /// <summary>
        /// 获取部署的SQL脚本目录名称集
        /// </summary>
        /// <returns></returns>
        List<string> GetSqlScriptDirectoryNames();

        /// <summary>
        /// 获取指定目录从指定版本开始的SQL脚本列表
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="fromVersion"></param>
        /// <returns></returns>
        List<SqlScript> GetSqlScriptListFromVersion(string directoryName, long fromVersion);

    }

    /// <summary>
    /// 
    /// </summary>
    public class ScriptService : BaseService, IScriptService
    {

        [Auto]
        protected IDeployService DeployService = default!;

        private readonly ScriptSetting _scriptSetting;
        private readonly Regex _versionScript;

        /// <summary>
        /// 
        /// </summary>
        public ScriptService() : base()
        {
            _scriptSetting = GlobalConfiguration.GetSectionConfig<ScriptSetting>("ScriptSetting")!;
            _versionScript = new Regex(_scriptSetting.VersionPattern!, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <returns></returns>
        private SqlScript? ReadFromFile(FileInfo scriptFile)
        {
            if (false == _versionScript.IsMatch(scriptFile.Name))
            {
                return null;
            }

            Match versionMatch = _versionScript.Match(scriptFile.Name);
            string strVersion = versionMatch.Groups["Version"].Value;
            long version = StringTool.TryParse<long>(strVersion);
            string sqlContent = FileTool.ReadFile(scriptFile.FullName);

            SqlScript sqlScript = new SqlScript
            {
                FileName = scriptFile.Name,
                Version = version,
                Sql = sqlContent
            };

            return sqlScript;
        }

        /// <summary>
        /// 获取部署的SQL脚本目录名称集
        /// </summary>
        /// <returns></returns>
        public List<string> GetSqlScriptDirectoryNames()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _scriptSetting.Root!);
            if (!Directory.Exists(root))
            {
                return new List<string>();
            }

            List<string> dirs = new List<string>();
            DirectoryInfo rootDirectory = new DirectoryInfo(root);
            DirectoryInfo[] directoryInfos = rootDirectory.GetDirectories();
            foreach(DirectoryInfo directoryInfo in directoryInfos)
            {
                dirs.Add(directoryInfo.Name);
            }

            return dirs;
        }

        /// <summary>
        /// 获取指定目录从指定版本开始的SQL脚本列表
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="fromVersion"></param>
        /// <returns></returns>
        public List<SqlScript> GetSqlScriptListFromVersion(string directoryName, long fromVersion)
        {
            DeploySetting? deploySetting = DeployService.GetDeploySetting(directoryName);
            if (deploySetting == null)
            {
                return new List<SqlScript>();
            }

            string directoryPath;
            if (!string.IsNullOrEmpty(deploySetting.ScriptDirectoryPath))
            {
                directoryPath = deploySetting.ScriptDirectoryPath;
            }
            else
            {
                directoryPath = Path.Combine(_scriptSetting.Root!, directoryName);
            }
            if (false == Directory.Exists(directoryPath))
            {
                return new List<SqlScript>();
            }
            
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            List<SqlScript> list = new List<SqlScript>();

            FileInfo[] scriptFiles = directoryInfo.GetFiles();
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (FileInfo scriptFile in scriptFiles)
            {
                SqlScript? sqlScript = ReadFromFile(scriptFile);

                if (sqlScript != null && sqlScript.Version > fromVersion)
                {
                    sqlScript.DirectoryName = directoryName;
                    list.Add(sqlScript);
                }
            }
            foreach (DirectoryInfo groupDirectory in directoryInfos)
            {
                FileInfo[] subScriptFiles = groupDirectory.GetFiles();
                foreach (FileInfo scriptFile in subScriptFiles)
                {
                    SqlScript? sqlScript = ReadFromFile(scriptFile);

                    if (sqlScript != null && sqlScript.Version > fromVersion)
                    {
                        sqlScript.DirectoryName = directoryName;
                        list.Add(sqlScript);
                    }
                }
            }

            return list;
        }

    }
}
