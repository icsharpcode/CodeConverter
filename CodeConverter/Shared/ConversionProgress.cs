using System;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// Overrides ToString with sensible default output
    /// </summary>
    public struct ConversionProgress
    {
        public ConversionProgress(string message, int nestingLevel = 0)
        {
            Message = message;
            NestingLevel = nestingLevel;
        }

        public string Message { get; }
        public int NestingLevel { get; }

        public override string ToString()
        {
            string preMessage = Environment.NewLine;
            switch (NestingLevel) {
                case 0:
                    return preMessage + Environment.NewLine + Message;
                case 1:
                    return preMessage + "* " + Message;
            }

            return "";
        }
    }
}