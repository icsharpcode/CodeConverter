using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.DotNetTool;
using ICSharpCode.CodeConverter.CSharp;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Console
{
    [Command(Name = "codeconv", Description = "Convert code from VB.NET to C# or C# to VB.NET",
        ExtendedHelpText = @"
Remarks:
  Currently converts all vb projects in a solution to C#. More options will be added later
")]
    [HelpOption("-h|--help")]
    public class CodeConvProgram
    {
        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<CodeConvProgram>(args);

        [FileExists]
        [Required]
        [Argument(0, "Project or solution path", "The project or solution to be converted.")]
        public string ProjectOrSolutionPath { get; }

        private async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            TextWriter output = System.Console.Out;

            try {
                var progress = new Progress<ConversionProgress>(s => output.WriteLine(s.ToString()));
                await ExecuteUnhandled(progress);
            } catch (Exception ex) {
                app.Error.WriteLine(ex.ToString());
                return ProgramExitCodes.EX_SOFTWARE;
            } finally {
                output.Close();
            }

            return 0;
        }

        private async Task ExecuteUnhandled(Progress<ConversionProgress> progress)
        {
            var msbuildWorkspaceConverter = new MSBuildWorkspaceConverter(ProjectOrSolutionPath);
            var converterResultsEnumerable = msbuildWorkspaceConverter.ConvertProjectsWhere<VBToCSConversion>(p => true, progress, CancellationToken.None);
            await ConversionResultWriter.WriteConvertedAsync(converterResultsEnumerable, ProjectOrSolutionPath, null, false, false);
        }
    }
}
