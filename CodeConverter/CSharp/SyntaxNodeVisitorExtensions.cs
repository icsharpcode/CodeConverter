using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp;

[System.Diagnostics.DebuggerStepThrough]
internal static class SyntaxNodeVisitorExtensions
{
    /// <summary>For TOut, specify the most general type acceptable by the calling code (often ExpressionSyntax), this allows error information to be attached to a dummy return node.</summary>
    public static async Task<TOut> AcceptAsync<TOut>(this VisualBasicSyntaxNode node, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All) where TOut : CSharpSyntaxNode =>
        node == null ? null : await visitorWrapper.AcceptAsync<TOut>(node, sourceTriviaMap);
    /// <summary>
    /// VB allows the parentheses to be omitted when there are no parameters (e.g. <c>Delegate Sub Callback</c>, <c>RaiseEvent</c> accessors, <c>Sub Foo</c>), leaving a null parameter list.
    /// C# always requires one, so every place that converts a VB parameter list should go via this method so they all agree on an empty list.
    /// </summary>
    public static async Task<ParameterListSyntax> AcceptParameterListAsync(this VBSyntax.ParameterListSyntax parameterList, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All) =>
        await parameterList.AcceptAsync<ParameterListSyntax>(visitorWrapper, sourceTriviaMap) ?? Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParameterList();
    public static async Task<SeparatedSyntaxList<TOut>> AcceptSeparatedListAsync<TIn, TOut>(this SeparatedSyntaxList<TIn> nodes, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All) where TIn : VisualBasicSyntaxNode where TOut : CSharpSyntaxNode =>
        await visitorWrapper.AcceptAsync<TIn, TOut>(nodes, sourceTriviaMap);
}