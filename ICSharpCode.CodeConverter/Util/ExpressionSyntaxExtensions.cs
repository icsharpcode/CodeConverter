using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class ExpressionSyntaxExtensions
    {

        /// <summary>
        /// Returns the predefined keyword kind for a given specialtype.
        /// </summary>
        /// <param name="specialType">The specialtype of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        public static SyntaxKind GetPredefinedKeywordKind(this SpecialType specialType)
        {
            switch (specialType) {
                case SpecialType.System_Boolean:
                    return SyntaxKind.BoolKeyword;
                case SpecialType.System_Byte:
                    return SyntaxKind.ByteKeyword;
                case SpecialType.System_SByte:
                    return SyntaxKind.SByteKeyword;
                case SpecialType.System_Int32:
                    return SyntaxKind.IntKeyword;
                case SpecialType.System_UInt32:
                    return SyntaxKind.UIntKeyword;
                case SpecialType.System_Int16:
                    return SyntaxKind.ShortKeyword;
                case SpecialType.System_UInt16:
                    return SyntaxKind.UShortKeyword;
                case SpecialType.System_Int64:
                    return SyntaxKind.LongKeyword;
                case SpecialType.System_UInt64:
                    return SyntaxKind.ULongKeyword;
                case SpecialType.System_Single:
                    return SyntaxKind.FloatKeyword;
                case SpecialType.System_Double:
                    return SyntaxKind.DoubleKeyword;
                case SpecialType.System_Decimal:
                    return SyntaxKind.DecimalKeyword;
                case SpecialType.System_String:
                    return SyntaxKind.StringKeyword;
                case SpecialType.System_Char:
                    return SyntaxKind.CharKeyword;
                case SpecialType.System_Object:
                    return SyntaxKind.ObjectKeyword;
                case SpecialType.System_Void:
                    return SyntaxKind.VoidKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static ArgumentListSyntax CreateArgList(params ExpressionSyntax[] args)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args.Select(SyntaxFactory.Argument)));
        }

        public static VBSyntax.ArgumentListSyntax CreateArgList(params VBSyntax.ExpressionSyntax[] args)
        {
            return VBasic.SyntaxFactory.ArgumentList(VBasic.SyntaxFactory.SeparatedList(args.Select(e => (VBSyntax.ArgumentSyntax) VBasic.SyntaxFactory.SimpleArgument(e))));
        }

        public static bool HasOperandOfUnconvertedType(this Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax node, string operandType, SemanticModel semanticModel)
        {
            return new[] {node.Left, node.Right}.Any(e => UnconvertedIsType(e, operandType, semanticModel));
        }

        public static bool UnconvertedIsType(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax e, string fullTypeName, SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(e).Type?.GetFullMetadataName() == fullTypeName;
        }

        public static bool IsIntegralType(this Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax e, SemanticModel semanticModel)
        {
            return IsIntegralType(semanticModel.GetTypeInfo(e).Type?.SpecialType);
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/integral-types-table
        /// </summary>
        private static bool IsIntegralType(SpecialType? specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;
                default:
                    return false;
            }
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
