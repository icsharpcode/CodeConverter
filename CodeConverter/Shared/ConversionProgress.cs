namespace ICSharpCode.CodeConverter.Shared
{
    public struct ConversionProgress
    {
        internal ConversionProgress(string message, int nestingLevel = 0)
        {
            Message = message;
            NestingLevel = nestingLevel;
        }

        public string Message { get; }
        public int NestingLevel { get; }
    }
}