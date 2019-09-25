using System;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class Env
    {
        /// <summary>
        /// Limited until I've figured out a way to parallelise making use of the Simplifier (since it returns a new project each time and I want a project containing all results).
        /// </summary>
        public static byte MaxDop = 1;
    }
}