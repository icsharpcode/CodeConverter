namespace ICSharpCode.CodeConverter.Util;

internal static class ExceptionExtensions
{
    public static ExceptionWithNodeInformation WithNodeInformation(this Exception exception, SyntaxNode syntaxNode)
    {
        return new ExceptionWithNodeInformation(exception, syntaxNode);
    }
}