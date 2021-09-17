using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CommandLine;

namespace CodeConvConsoleApp
{
    class Program
    {


        static async Task MainAsync(string[] args)
        {
            var result = await CodeConvProgram.Main(args);

            Console.WriteLine($"Done with return value: {result}");

            return;
        }
    }

}


namespace ICSharpCode.CodeConverter.CommandLine
{

    public  partial class CodeConvProgram
    {
        private static readonly bool _runningInNetCore = true;
    }
}