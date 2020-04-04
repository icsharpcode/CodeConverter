using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    [Command(Name = "codeconv", Description = "Convert code from VB.NET to C# or C# to VB.NET",
        ExtendedHelpText = @"
Remarks:
  Converts all projects in a solution from VB.NET to C#.
  See https://github.com/icsharpcode/CodeConverter for the source code, issues, Visual Studio extension and other info.
")]
    [HelpOption("-h|--help")]
    public class CodeConvProgram
    {
        /// <remarks>Calls <see cref="OnExecuteAsync(CommandLineApplication)"/></remarks>
        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<CodeConvProgram>(args);


        [FileExists]
        [Required]
        [Argument(0, "Souce solution path", "The solution containing project(s) to be converted.")]
        public string SolutionPath { get; }

        [Option("-i|--include", "Regex matching project file paths to convert. Can be used multiple times", CommandOptionType.MultipleValue)]
        public string[] Include { get; } = new string[0];

        [Option("-e|--exclude", "Regex matching project file paths to exclude from conversion. Can be used multiple times", CommandOptionType.MultipleValue)]
        public string[] Exclude { get; } = new string[0];

        [Option("-t|--target-language", "The language to convert to.", CommandOptionType.SingleValue, ValueName = nameof(Language.CS) + " | " + nameof(Language.VB))]
        public Language? TargetLanguage { get; }

        [FileNotExists]
        [Option("-o|--output-directory", "Empty or non-existent directory to be used for output.", CommandOptionType.SingleValue)]
        public string OutputDirectory { get; }

        [Option("-f|--force", "Wipe the output directory before conversion", CommandOptionType.NoValue)]
        public bool Force { get; }

        [Option("-b|--best-effort", "Overrides warnings about compilation issues with input, and attempts a best effort conversion anyway", CommandOptionType.NoValue)]
        public bool BestEffort { get; }

        /// <remarks>
        /// Also allows semicolon and comma splitting of build properties to be compatible with https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2019#switches
        /// </remarks>
        [Option("-b|--build-property", "Set build properties in format: propertyName=propertyValue. Can be used multiple times", CommandOptionType.MultipleValue, ValueName = "Configuration=Release")]
        public string[] BuildProperty { get; } = new string[0];

        public enum Language
        {
            CS,
            VB
        }

        private Dictionary<string, string> ParsedProperties()
        {
            var props = BuildProperty.SelectMany(bp => bp.Split(';', ',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=')));
            return props.ToLookup(s => s[0], GetValidatedPropertyValue).ToDictionary();
        }

        private string GetValidatedPropertyValue(string[] s)
        {
            return s.Length == 2 ? s[1] : throw new ArgumentOutOfRangeException(nameof(BuildProperty), BuildProperty, $"{s[0]} must have exactly one value, e.g. `{s[0]}=1`");
        }

        /// <remarks>Used by reflection in CommandLineApplication.ExecuteAsync</remarks>
        private async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            try {
                var progress = new Progress<ConversionProgress>(s => Console.Out.WriteLine(s.ToString()));
                await ExecuteUnhandledAsync(progress, CancellationToken.None);
            } catch (Exception ex) {
                await Console.Error.WriteLineAsync(ex.ToString());
                await Console.Error.WriteLineAsync();
                await Console.Error.WriteLineAsync("Please report issues at github.com/icsharpcode/CodeConverter");
                return ProgramExitCodes.EX_SOFTWARE;
            }

            Console.WriteLine();
            Console.WriteLine("Exiting successfully. Report any issues at github.com/icsharpcode/CodeConverter to help us improve the accuracy of future conversions");
            return 0;
        }

        private async Task ExecuteUnhandledAsync(IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            var properties = ParsedProperties();
            var targetLanguage = TargetLanguage ??
                (string.Equals(Path.GetExtension(SolutionPath), ".csproj", StringComparison.OrdinalIgnoreCase) ? Language.VB : Language.CS);
            var strProgress = new Progress<string>(p => progress.Report(new ConversionProgress(p)));
            var msbuildWorkspaceConverter = new MSBuildWorkspaceConverter(SolutionPath, strProgress, BestEffort, properties);

            var converterResultsEnumerable = msbuildWorkspaceConverter.ConvertProjectsWhereAsync(ShouldIncludeProject, targetLanguage, progress, cancellationToken);
            await ConversionResultWriter.WriteConvertedAsync(converterResultsEnumerable, SolutionPath, OutputDirectory, Force, true);
        }

        private bool ShouldIncludeProject(Project project)
        {
            var isIncluded = !Include.Any() || Include.Any(regex => Regex.IsMatch(project.FilePath, regex));
            return isIncluded && Exclude.All(regex => !Regex.IsMatch(project.FilePath, regex));
        }
    }
}
