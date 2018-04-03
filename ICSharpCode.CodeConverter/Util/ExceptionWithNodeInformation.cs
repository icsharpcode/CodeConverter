using System;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal class ExceptionWithNodeInformation : Exception
    {
        public SyntaxNode ExceptionCause { get; }

        public ExceptionWithNodeInformation(Exception innerException, SyntaxNode exceptionCause) : base(innerException.Message, innerException)
        {
            ExceptionCause = exceptionCause;
        }

        public override string ToString()
        {
            return $"CONVERSION ERROR: {Message} in {ExceptionCause.GetBriefNodeDescription()}{Environment.NewLine}{StackTrace}";
        }
    }
}