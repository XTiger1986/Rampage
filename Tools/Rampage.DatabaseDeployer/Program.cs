using System;

namespace Rampage.DatabaseDeployer
{
    /// <summary>数据库部署工具程序入口 / Database deployer program entry</summary>
    class Program
    {

        /// <summary>
        /// 程序入口：通过 RampageBooter 启动数据库部署应用 / Entry point that boots the deployer application via RampageBooter
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        static void Main(string[] args)
        {
            RampageBooter.Run<DatabaseDeployerApplication>(args);
            Console.WriteLine("按任意键退出~");
            Console.ReadKey();
        }

    }
}
