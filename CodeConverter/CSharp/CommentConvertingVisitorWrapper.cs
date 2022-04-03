﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp;

[System.Diagnostics.DebuggerStepThrough]
internal class CommentConvertingVisitorWrapper
{
    private readonly VBasic.VisualBasicSyntaxVisitor<Task<CS.CSharpSyntaxNode>> _wrappedVisitor;
    private readonly SyntaxTree _syntaxTree;
    private static readonly CSSyntax.LiteralExpressionSyntax _dummyLiteral = ValidSyntaxFactory.DefaultExpression;
    private static readonly CSSyntax.StatementSyntax _dummyStatement = CS.SyntaxFactory.EmptyStatement();
    private static readonly CSSyntax.CompilationUnitSyntax _dummyCompilationUnit = CS.SyntaxFactory.CompilationUnit();

    public CommentConvertingVisitorWrapper(VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>> wrappedVisitor, SyntaxTree syntaxTree)
    {
        _wrappedVisitor = wrappedVisitor;
        _syntaxTree = syntaxTree;
    }

    public async Task<T> AcceptAsync<T>(VisualBasicSyntaxNode vbNode, SourceTriviaMapKind sourceTriviaMap) where T : CS.CSharpSyntaxNode =>
        await ConvertHandledAsync<T>(vbNode, sourceTriviaMap);

    public async Task<SeparatedSyntaxList<TOut>> AcceptAsync<TIn, TOut>(SeparatedSyntaxList<TIn> vbNodes, SourceTriviaMapKind sourceTriviaMap) where TIn : VBasic.VisualBasicSyntaxNode where TOut : CS.CSharpSyntaxNode
    {
        var convertedNodes = await vbNodes.SelectAsync(n => ConvertHandledAsync<TOut>(n, sourceTriviaMap));
        var convertedSeparators = vbNodes.GetSeparators().Select(s =>
            CS.SyntaxFactory.Token(CS.SyntaxKind.CommaToken)
                .WithConvertedTrailingTriviaFrom(s, TriviaKinds.FormattingOnly)
                .WithSourceMappingFrom(s)
        );
        return CS.SyntaxFactory.SeparatedList(convertedNodes, convertedSeparators);
    }

    private async Task<T> ConvertHandledAsync<T>(VisualBasicSyntaxNode vbNode, SourceTriviaMapKind sourceTriviaMap) where T : CS.CSharpSyntaxNode
    {
        try {
            var converted = (T)await _wrappedVisitor.Visit(vbNode);
            return sourceTriviaMap == SourceTriviaMapKind.None || _syntaxTree != vbNode.SyntaxTree
                ? converted.WithoutSourceMapping()
                : sourceTriviaMap == SourceTriviaMapKind.SubNodesOnly
                    ? converted
                    : WithSourceMapping(vbNode, converted);
        } catch (Exception e) when (_dummyLiteral is T dummy) {
            return dummy.WithCsTrailingErrorComment(vbNode, e);
        } catch (Exception e) when (_dummyStatement is T dummy) {
            return dummy.WithCsTrailingErrorComment(vbNode, e);
        } catch (Exception e) when (_dummyCompilationUnit is T dummy) {
            return dummy.WithCsTrailingErrorComment(vbNode, e);
        } catch (Exception e) when (!(e is ExceptionWithNodeInformation)) {
            throw e.WithNodeInformation(vbNode);
        }
    }

    /// <remarks>
    /// If lots of special cases, move to wrapping the wrappedVisitor in another visitor, but I'd rather use a simple switch here initially.
    /// </remarks>
    private static T WithSourceMapping<T>(SyntaxNode vbNode, T converted) where T : CS.CSharpSyntaxNode
    {
        converted = vbNode.CopyAnnotationsTo(converted);
        switch (vbNode) {
            case VBSyntax.CompilationUnitSyntax vbCus when converted is CSSyntax.CompilationUnitSyntax csCus:
                return (T)(object)csCus
                    .WithSourceStartLineAnnotation(vbNode.SyntaxTree.GetLineSpan(new TextSpan(0, 0)))
                    .WithEndOfFileToken(csCus.EndOfFileToken
                        .WithConvertedLeadingTriviaFrom(vbCus.EndOfFileToken).WithSourceMappingFrom(vbCus.EndOfFileToken)
                    );
        }
        return converted.WithVbSourceMappingFrom(vbNode);
    }
}