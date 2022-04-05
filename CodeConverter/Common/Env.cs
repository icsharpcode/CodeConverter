namespace ICSharpCode.CodeConverter.Common;

public static class Env
{
    /// <summary>
    /// Should be used to control any degree of parallelism in the codebase
    /// </summary>
    public static readonly byte MaxDop =
#if DEBUG
        System.Diagnostics.Debugger.IsAttached ? (byte) 1 :
#endif
            (byte)Math.Min(Math.Max(Environment.ProcessorCount, byte.MinValue + 1), byte.MaxValue);
}