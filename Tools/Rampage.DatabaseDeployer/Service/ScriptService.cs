using Rampage.DatabaseDeployer.Model;
using Rampage.DependencyInjection;
using Rampage.Hierarchy;
using Rampage.Utilities;
using System;
using System.Text.RegularExpressions;

namespace Rampage.DatabaseDeployer.Service
{

    /// <summary>
    /// 脚本服务接口：负责扫描脚本目录、按文件名解析版本号并筛选待执行脚本 /
    /// Script service interface: scans script directories, extracts versions from file names and filters scripts to run
    /// </summary>
    public interface IScriptService : IBaseService
    {
        /// <summary>
        /// 获取部署的SQL脚本目录名称集 / Gets the names of deployable SQL script directories
        /// </summary>
        /// <returns>脚本根目录下的一级目录名列表；根目录不存在时返回空列表 / Names of first-level directories under the script root; an empty list when the root does not exist</returns>
        List<string> GetSqlScriptDirectoryNames();

        /// <summary>
        /// 获取指定目录从指定版本开始的SQL脚本列表 / Gets the SQL scripts of the given directory newer than the given version
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <param name="fromVersion">起始版本号（不含），仅收集版本号大于它的脚本 / Baseline version (exclusive); only scripts with a greater version are collected</param>
        /// <returns>版本号大于 fromVersion 的脚本列表；目录无配置或不存在时返回空列表 / Scripts with version greater than fromVersion; an empty list when the setting or directory is missing</returns>
        List<SqlScript> GetSqlScriptListFromVersion(string directoryName, long fromVersion);

    }

    /// <summary>
    /// 脚本服务实现：按 VersionPattern 正则从文件名提取版本号，筛选待执行的部署脚本 /
    /// Script service implementation: extracts versions from file names via the VersionPattern regex and filters scripts to deploy
    /// </summary>
    public class ScriptService : BaseService, IScriptService
    {

        /// <summary>部署服务（自动注入）/ Deploy service (auto-injected)</summary>
        [Auto]
        protected IDeployService DeployService = default!;

        private readonly ScriptSetting _scriptSetting;
        private readonly Regex _versionScript;

        /// <summary>
        /// 初始化服务：读取 ScriptSetting 配置节，并按 VersionPattern 编译版本号正则（忽略大小写）/
        /// Initializes the service: loads the ScriptSetting section and compiles the version regex (case-insensitive)
        /// </summary>
        public ScriptService() : base()
        {
            _scriptSetting = GlobalConfiguration.GetSectionConfig<ScriptSetting>("ScriptSetting")!;
            _versionScript = new Regex(_scriptSetting.VersionPattern!, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
        }

        /// <summary>
        /// 从脚本文件解析 SqlScript：用 VersionPattern 从文件名提取版本号并读取文件内容 /
        /// Parses a SqlScript from a file: extracts the version via VersionPattern and reads the file content
        /// </summary>
        /// <param name="scriptFile">脚本文件 / The script file</param>
        /// <returns>解析结果；文件名不匹配 VersionPattern 时返回 null / The parsed script; null when the file name does not match VersionPattern</returns>
        private SqlScript? ReadFromFile(FileInfo scriptFile)
        {
            if (false == _versionScript.IsMatch(scriptFile.Name))
            {
                return null;
            }

            Match versionMatch = _versionScript.Match(scriptFile.Name);
            string strVersion = versionMatch.Groups["Version"].Value; // 取命名分组 Version / capture the named group "Version"
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
        /// 获取部署的SQL脚本目录名称集 / Gets the names of deployable SQL script directories
        /// </summary>
        /// <returns>脚本根目录下的一级目录名列表；根目录不存在时返回空列表 / Names of first-level directories under the script root; an empty list when the root does not exist</returns>
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
        /// 获取指定目录从指定版本开始的SQL脚本列表 / Gets the SQL scripts of the given directory newer than the given version
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <param name="fromVersion">起始版本号（不含）/ Baseline version (exclusive)</param>
        /// <returns>版本号大于 fromVersion 的脚本列表；目录无 deploy.json 或目录不存在时返回空列表 / Scripts newer than fromVersion; an empty list when deploy.json or the directory is missing</returns>
        /// <remarks>
        /// 脚本目录取 deploy.json 的 ScriptDirectoryPath（可指向任意路径），未配置时为“脚本根目录+目录名”。
        /// 会同时扫描该目录本身与其一级子目录；所有脚本（含子目录中的）的 DirectoryName 均记为一级目录名。
        /// 返回列表按版本号升序排序，即脚本的执行顺序。
        /// The script directory comes from ScriptDirectoryPath in deploy.json (any path), falling back to "script root + directory name".
        /// Both the directory itself and its first-level subdirectories are scanned; DirectoryName is always the first-level directory name.
        /// The returned list is sorted ascending by version, which is the execution order.
        /// </remarks>
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

                // 仅收集版本号大于当前版本的脚本 / keep only scripts newer than the current version
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

                    // 仅收集版本号大于当前版本的脚本 / keep only scripts newer than the current version
                    if (sqlScript != null && sqlScript.Version > fromVersion)
                    {
                        sqlScript.DirectoryName = directoryName;
                        list.Add(sqlScript);
                    }
                }
            }

            // 按版本号升序执行，避免非零填充版本（1、10、2）时顺序错乱 / sort ascending by version so non-zero-padded versions (1, 10, 2) execute in the right order
            list.Sort((a, b) => a.Version.CompareTo(b.Version));
            return list;
        }

    }
}
