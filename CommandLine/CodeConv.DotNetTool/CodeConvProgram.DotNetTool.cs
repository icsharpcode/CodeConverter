using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CommandLine.Util;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using System.Reflection;

namespace ICSharpCode.CodeConverter.CommandLine
{

    public partial class CodeConvProgram
    {
        private const string FrameworkOptionDefinition = "--framework";

        [Option(FrameworkOptionDefinition, "One or more of the projects to be converted are .NET Framework (rather than .NET Core)", CommandOptionType.NoValue)]
        public bool Framework { get; }

        public static async Task<int> Main(string[] args)
        {
            if (args.Contains(FrameworkOptionDefinition)) {
                Console.WriteLine("Running framework conversion");
                var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var netFrameworkExe = Path.Combine(assemblyPath, "NetFramework", "ICSharpCode.CodeConverter.CodeConv.NetFramework.exe");
                var process = await ProcessRunner.StartRedirectedToConsoleAsync(netFrameworkExe, args);
                return process.ExitCode;
            } else {
                return await ExecuteCurrentFrameworkAsync(args);
            }
        }
    }
}
