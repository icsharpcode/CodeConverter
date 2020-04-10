using System;

namespace ICSharpCode.CodeConverter.Shared
{
    public static class Env
    {
        /// <summary>
        /// Should be used to control any degree of parallelism in the codebase
        /// </summary>
        public static byte MaxDop =
#if DEBUG
            System.Diagnostics.Debugger.IsAttached ? (byte) 1 :
#endif
            (byte)Math.Min(Math.Max(Environment.ProcessorCount, byte.MinValue + 1), byte.MaxValue);
    }
}