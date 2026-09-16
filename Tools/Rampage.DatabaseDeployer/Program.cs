using System;

namespace Rampage.DatabaseDeployer
{
    class Program
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            RampageBooter.Run<DatabaseDeployerApplication>(args);
            Console.WriteLine("按任意键退出~");
            Console.ReadKey();
        }

    }
}
