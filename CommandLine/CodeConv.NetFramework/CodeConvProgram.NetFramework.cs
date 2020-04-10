using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.CommandLine
{
    public partial class CodeConvProgram
    {
        public static async Task<int> Main(string[] args) => await ExecuteCurrentFrameworkAsync(args);
    }
}
