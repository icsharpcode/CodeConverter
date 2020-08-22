using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    public static class CompilationWarnings
    {
        public static string WarningsForCompilation(Compilation finalCompilation, string compilationDescription)
        {
            return GetErrorString(finalCompilation.GetDiagnostics(), compilationDescription);
        }

        public static string GetErrorString(this IReadOnlyCollection<Diagnostic> targetErrors, string compilationDescription)
        {
            var errorStrings = GetErrorDiagnosticStrings(targetErrors);
            if (errorStrings.Count == 0) return null;
            return $"{Environment.NewLine}{errorStrings.Count} {compilationDescription} compilation errors:{Environment.NewLine}{String.Join(Environment.NewLine, errorStrings)}";
        }

        public static string GetErrorString(this IReadOnlyCollection<WorkspaceDiagnostic> targetErrors)
        {
            var errorStrings = GetErrorDiagnosticStrings(targetErrors);
            if (errorStrings.Count == 0) return null;
            return $"{Environment.NewLine}{errorStrings.Count} workspace errors:{Environment.NewLine}{String.Join(Environment.NewLine, errorStrings)}";
        }

        private static List<string> GetErrorDiagnosticStrings(IReadOnlyCollection<Diagnostic> diagnosticList)
        {
            return diagnosticList.Where(d => d.Severity >= DiagnosticSeverity.Error).Select(d => $"{d.Id}: {d.GetMessage()}")
                .Distinct().ToList();
        }

        private static List<string> GetErrorDiagnosticStrings(IReadOnlyCollection<WorkspaceDiagnostic> diagnosticList)
        {
            return diagnosticList.Where(d => d.Kind <= WorkspaceDiagnosticKind.Failure).Select(d => d.Message)
                .Distinct().ToList();
        }
    }
}
