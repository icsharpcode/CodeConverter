using System;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class Env
    {
        public static byte MaxDop = (byte)Math.Min(Math.Max(Environment.ProcessorCount, byte.MinValue + 1), byte.MaxValue);
    }
}