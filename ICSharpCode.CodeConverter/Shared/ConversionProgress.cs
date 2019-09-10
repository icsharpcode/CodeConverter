namespace ICSharpCode.CodeConverter.Shared
{
    public class ConversionProgress
    {
        internal ConversionProgress(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}