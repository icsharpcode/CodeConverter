using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ICSharpCode.CodeConverter.Shared
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