using System;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// If you have a JoinableTaskFactory, set it here to help avoid deadlocks
    /// </summary>
    internal static class JoinableTaskFactorySingleton
    {
        public static JoinableTaskFactory Instance { get; private set; }

        internal static void EnsureInitialized()
        {
            Instance = new JoinableTaskFactory(new JoinableTaskContext());
        }
    }
}