
namespace Rampage.CodeGenerator
{
    /// <summary>代码生成工具程序入口 / Code generator program entry</summary>
    class Program
    {
        /// <summary>
        /// 程序入口：通过 RampageBooter 启动代码生成应用 / Entry point that boots the generator application via RampageBooter
        /// </summary>
        /// <param name="args">命令行参数 / Command-line arguments</param>
        static void Main(string[] args)
        {
            RampageBooter.Run<CodeGeneratorApplication>(args);

            Console.WriteLine("按任意键退出~");
            Console.ReadKey();
        }

    }
}
