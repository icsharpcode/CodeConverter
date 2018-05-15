using System;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static partial class ExceptionExtensions
    {
        public static ExceptionWithNodeInformation WithNodeInformation(this Exception exception, SyntaxNode syntaxNode)
        {
            return new ExceptionWithNodeInformation(exception, syntaxNode);
        }
    }
}
