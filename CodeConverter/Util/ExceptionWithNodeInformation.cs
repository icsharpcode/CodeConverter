using System;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal class ExceptionWithNodeInformation : Exception
    {
        private readonly string _title;
        public SyntaxNode ExceptionCause { get; }

        public ExceptionWithNodeInformation(Exception innerException, SyntaxNode exceptionCause, string title = "CONVERSION ERROR") : base(innerException.Message, innerException)
        {
            _title = title;
            ExceptionCause = exceptionCause;
        }

        public override string ToString()
        {
            return $"{_title}: {Message} in {ExceptionCause.GetBriefNodeDescription()}{Environment.NewLine}{InnerException.StackTrace}";
        }
    }
}
