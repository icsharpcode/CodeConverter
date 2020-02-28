using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace CodeConverter.Tests
{
    /// <summary>
    /// https://putridparrot.com/blog/replacing-multiple-configureawait-with-the-synchronizationcontextremover/
    /// </summary>
    internal struct SynchronizationContextRemover : INotifyCompletion
    {
        public bool IsCompleted => SynchronizationContext.Current == null;

        public void OnCompleted(Action continuation)
        {
            var prev = SynchronizationContext.Current;
            try {
                SynchronizationContext.SetSynchronizationContext(null);
                continuation();
            } finally {
                SynchronizationContext.SetSynchronizationContext(prev);
            }
        }

        public SynchronizationContextRemover GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }
    }
}
