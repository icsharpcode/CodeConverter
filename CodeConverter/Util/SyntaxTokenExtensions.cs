using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using ICSharpCode.CodeConverter.Shared;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class SyntaxTokenExtensions
    {
        public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.GetAncestor<SyntaxNode>(predicate);
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.FirstAncestorOrSelf(predicate)
                    : default(T);
        }

        public static IEnumerable<T> GetAncestors<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().OfType<T>()
                    : Enumerable.Empty<T>();
        }

        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().Where(predicate)
                    : Enumerable.Empty<SyntaxNode>();
        }

        public static int Width(this SyntaxToken token)
        {
            return token.Span.Length;
        }

        public static int FullWidth(this SyntaxToken token)
        {
            return token.FullSpan.Length;
        }

        public static bool IsKindOrHasMatchingText(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind)
        {
            return VisualBasicExtensions.Kind(token) == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind)
        {
            return token.ToString() == VBasic.SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2;
        }

        public static bool IsKind(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind1, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind2)
        {
            return VisualBasicExtensions.Kind(token) == kind1
                || VisualBasicExtensions.Kind(token) == kind2;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3;
        }

        public static bool IsKind(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind1, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind2, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind3)
        {
            return VisualBasicExtensions.Kind(token) == kind1
                || VisualBasicExtensions.Kind(token) == kind2
                || VisualBasicExtensions.Kind(token) == kind3;
        }

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return kinds.Contains(token.Kind());
        }

        public static bool IsKind(this SyntaxToken token, params Microsoft.CodeAnalysis.VisualBasic.SyntaxKind[] kinds)
        {
            return kinds.Contains(VisualBasicExtensions.Kind(token));
        }

        public static bool IsVbVisibility(this SyntaxToken token, bool isVariableOrConst, bool isConstructor)
        {
            return token.IsKind(VBasic.SyntaxKind.PublicKeyword, VBasic.SyntaxKind.FriendKeyword, VBasic.SyntaxKind.ProtectedKeyword, VBasic.SyntaxKind.PrivateKeyword)
                   || isVariableOrConst && token.IsKind(VBasic.SyntaxKind.ConstKeyword)
                   || isConstructor && token.IsKind(VBasic.SyntaxKind.SharedKeyword);
        }

        public static bool IsCsVisibility(this SyntaxToken token, bool isVariableOrConst, bool isConstructor)
        {
            return token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PrivateKeyword)
                   || isVariableOrConst && token.IsKind(SyntaxKind.ConstKeyword)
                   || isConstructor && token.IsKind(SyntaxKind.StaticKeyword);
        }

        public static SyntaxToken WithSourceMappingFrom(this SyntaxToken converted, SyntaxNodeOrToken fromToken)
        {
            var origLinespan = fromToken.SyntaxTree.GetLineSpan(fromToken.Span);
            if (fromToken.IsToken) converted = fromToken.AsToken().CopyAnnotationsTo(converted);
            return converted.WithSourceStartLineAnnotation(origLinespan).WithSourceEndLineAnnotation(origLinespan);
        }

        public static SyntaxToken WithSourceStartLineAnnotation(this SyntaxToken node, FileLinePositionSpan sourcePosition)
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceStartLine(sourcePosition));
        }

        public static SyntaxToken WithSourceEndLineAnnotation(this SyntaxToken node, FileLinePositionSpan sourcePosition)
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceEndLine(sourcePosition));
        }

        public static SyntaxToken WithoutSourceMapping(this SyntaxToken token)
        {
            return token.WithoutAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).WithoutAnnotations(AnnotationConstants.SourceEndLineAnnotationKind);
        }

        public static SyntaxTokenList RemoveOnly(this SyntaxTokenList list, Func<SyntaxToken, bool> where)
        {
            var toRemove = list.OnlyOrDefault(where);
            if (toRemove != default) list = list.Remove(toRemove);
            return list;
        }
    }
}
