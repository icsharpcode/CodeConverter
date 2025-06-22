using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.Util;

internal static class ExpressionSyntaxExtensions
{
    public static ArgumentListSyntax CreateArgList(params ExpressionSyntax[] args)
    {
        return CreateCsArgList(args);
    }

    public static VBSyntax.ArgumentListSyntax CreateArgList<T>(params T[] args) where T : VBSyntax.ExpressionSyntax
    {
        return CreateVbArgList(args);
    }

    public static VBSyntax.ArgumentListSyntax CreateVbArgList<T>(this IEnumerable<T> argExpressions) where T : VBSyntax.ExpressionSyntax
    {
        return VBasic.SyntaxFactory.ArgumentList(VBasic.SyntaxFactory.SeparatedList(argExpressions.Select(e => (VBSyntax.ArgumentSyntax) VBasic.SyntaxFactory.SimpleArgument(e))));
    }

    public static ArgumentListSyntax CreateCsArgList<T>(this IEnumerable<T> argExpressions, params SyntaxKind?[] refTokenKinds) where T : ExpressionSyntax
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argExpressions.Select((e, i) => {
            var arg = SyntaxFactory.Argument(e);
            if (i < refTokenKinds.Length && refTokenKinds[i].HasValue) arg = arg.WithRefKindKeyword(SyntaxFactory.Token(refTokenKinds[i].Value));
            return arg;
        })));
    }

    /// <summary>
    /// This is a conversion heavily based on Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxExtensions.ExtractAnonymousTypeMemberName from 1bbbfc28a8e4493b4057e171310343a4c7ba826c
    /// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0
    /// </summary>
    public static SyntaxToken? ExtractAnonymousTypeMemberName(this VBSyntax.ExpressionSyntax input)
    {
        Stack<VBSyntax.ConditionalAccessExpressionSyntax> conditionalAccessStack = null;
        while (true) {
            switch (input.Kind()) {
                case VBasic.SyntaxKind.IdentifierName: {
                    return ((VBSyntax.IdentifierNameSyntax)input).Identifier;
                }

                case VBasic.SyntaxKind.XmlName: {
                    var xmlNameInferredFrom = (VBSyntax.XmlNameSyntax)input;
                    var name = xmlNameInferredFrom.LocalName;
                    // CONVERSION NOTE: Slightly skimped on the details here for brevity
                    return VBasic.SyntaxFacts.IsValidIdentifier(name.ToString()) ? name : default(SyntaxToken?);
                }

                case VBasic.SyntaxKind.XmlBracketedName: {
                    // handles something like <a-a>
                    var xmlNameInferredFrom = (VBSyntax.XmlBracketedNameSyntax)input;
                    input = xmlNameInferredFrom.Name;
                    continue;
                }

                case VBasic.SyntaxKind.SimpleMemberAccessExpression:
                case VBasic.SyntaxKind.DictionaryAccessExpression: {
                    var memberAccess = (VBSyntax.MemberAccessExpressionSyntax)input;
                    var receiver = memberAccess.Expression ?? conditionalAccessStack.Pop();

                    if (input.Kind() == VBasic.SyntaxKind.SimpleMemberAccessExpression) {
                        // See if this is an identifier qualified with XmlElementAccessExpression or XmlDescendantAccessExpression
                        if (receiver != null) {
                            switch (receiver.Kind()) {
                                case VBasic.SyntaxKind.XmlElementAccessExpression:
                                case VBasic.SyntaxKind.XmlDescendantAccessExpression: {
                                    input = receiver;
                                    continue;
                                }
                            }
                        }
                    }

                    conditionalAccessStack = null;
                    input = memberAccess.Name;
                    continue;
                }

                case VBasic.SyntaxKind.XmlElementAccessExpression:
                case VBasic.SyntaxKind.XmlAttributeAccessExpression:
                case VBasic.SyntaxKind.XmlDescendantAccessExpression: {
                    var xmlAccess = (VBSyntax.XmlMemberAccessExpressionSyntax)input;
                    conditionalAccessStack.Clear();

                    input = xmlAccess.Name;
                    continue;
                }

                case VBasic.SyntaxKind.InvocationExpression: {
                    var invocation = (VBSyntax.InvocationExpressionSyntax)input;
                    var target = invocation.Expression ?? conditionalAccessStack.Pop();

                    if (target == null)
                        break;

                    if (invocation.ArgumentList == null || invocation.ArgumentList.Arguments.Count == 0) {
                        input = target;
                        continue;
                    }

                    if (invocation.ArgumentList.Arguments.Count == 1) {
                        // See if this is an indexed XmlElementAccessExpression or XmlDescendantAccessExpression
                        switch (target.Kind()) {
                            case VBasic.SyntaxKind.XmlElementAccessExpression:
                            case VBasic.SyntaxKind.XmlDescendantAccessExpression: {
                                input = target;
                                continue;
                            }
                        }
                    }

                    break;
                }

                case VBasic.SyntaxKind.ConditionalAccessExpression: {
                    var access = (VBSyntax.ConditionalAccessExpressionSyntax)input;

                    if (conditionalAccessStack == null)
                        conditionalAccessStack = new Stack<VBSyntax.ConditionalAccessExpressionSyntax>();

                    conditionalAccessStack.Push(access);

                    input = access.WhenNotNull;
                    continue;
                }
            }

            return null;
        }
    }

    public static CSSyntax.ArgumentListSyntax CreateDelegatingArgList(this CSSyntax.ParameterListSyntax parameterList)
    {
        var refKinds = parameterList.Parameters.Select(GetSingleModifier).ToArray<CS.SyntaxKind?>();
        return parameterList.Parameters.Select(p => ValidSyntaxFactory.IdentifierName(p.Identifier)).CreateCsArgList(refKinds);
    }

    private static CS.SyntaxKind? GetSingleModifier(CSSyntax.ParameterSyntax p)
    {
        var argKinds = new CS.SyntaxKind?[] { CS.SyntaxKind.RefKeyword, CS.SyntaxKind.OutKeyword, CS.SyntaxKind.InKeyword };
        return p.Modifiers.Select(Microsoft.CodeAnalysis.CSharp.CSharpExtensions.Kind)
            .Select<CS.SyntaxKind, CS.SyntaxKind?>(k => k)
            .FirstOrDefault(argKinds.Contains);
    }

    public static CSSyntax.ArrowExpressionClauseSyntax GetDelegatingClause(CSSyntax.ParameterListSyntax parameterList, SyntaxToken csIdentifier,
        bool isSetAccessor)
    {
        if (parameterList != null && isSetAccessor)
            throw new InvalidOperationException("Parameterized setters shouldn't have a delegating clause. " +
                                                $"\r\nInvalid arguments: {nameof(isSetAccessor)} = {true}," +
                                                $" {nameof(parameterList)} has {parameterList.Parameters.Count} parameters");

        var simpleMemberAccess = GetSimpleMemberAccess(csIdentifier);

        var expression = parameterList != null
            ? (CSSyntax.ExpressionSyntax)CS.SyntaxFactory.InvocationExpression(simpleMemberAccess, parameterList.CreateDelegatingArgList())
            : simpleMemberAccess;

        var arrowClauseExpression = isSetAccessor
            ? CS.SyntaxFactory.AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, simpleMemberAccess,
                ValidSyntaxFactory.IdentifierName("value"))
            : expression;

        var arrowClause = CS.SyntaxFactory.ArrowExpressionClause(arrowClauseExpression);
        return arrowClause;
    }

    public static CSSyntax.MemberAccessExpressionSyntax GetSimpleMemberAccess(this SyntaxToken csIdentifier)
    {
        var simpleMemberAccess = CS.SyntaxFactory.MemberAccessExpression(
            CS.SyntaxKind.SimpleMemberAccessExpression, CS.SyntaxFactory.ThisExpression(),
            CS.SyntaxFactory.Token(CS.SyntaxKind.DotToken), ValidSyntaxFactory.IdentifierName(csIdentifier));

        return simpleMemberAccess;
    }
}