
namespace Rampage.CodeGenerator
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            RampageBooter.Run<CodeGeneratorApplication>(args);

            Console.WriteLine("按任意键退出~");
            Console.ReadKey();
        }

    }
}
