using System;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public static class WorkspaceFactory
    {
        /// <summary>
        /// Library consumers must use this to guard any non-VS workspace creation (i.e. adhoc or msbuild)
        /// Know MEF bug means creating multiple workspaces outside VS context in parallel has race conditions: https://github.com/dotnet/roslyn/issues/24260
        /// </summary>
        public static object WorkspaceCreationLock = new object();
        private static Lazy<Solution> LazyAdhocSolution = new Lazy<Solution>(() => {
            lock (WorkspaceCreationLock) {
                return new AdhocWorkspace().CurrentSolution;
            }
        });
        public static Solution AdhocSolution => LazyAdhocSolution.Value;
    }
}