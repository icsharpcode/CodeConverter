using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class ExpressionSyntaxExtensions
    {
        public static ArgumentListSyntax CreateArgList(params ExpressionSyntax[] args)
        {
            return CreateCsArgList(args);
        }

        public static VBSyntax.ArgumentListSyntax CreateArgList<T>(params T[] args) where T : VBSyntax.ExpressionSyntax
        {
            return CreateVbArgList((IEnumerable<T>) args);
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
            bool isNameDictionaryAccess;
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

                            isNameDictionaryAccess = input.Kind() == VBasic.SyntaxKind.DictionaryAccessExpression;
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
    }
}
