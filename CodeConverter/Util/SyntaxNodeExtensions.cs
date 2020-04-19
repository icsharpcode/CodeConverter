using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using FieldDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class SyntaxNodeExtensions
    {
        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxNode node)
        {
            var current = node.Parent;

            while (current != null) {
                yield return current;

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null) {
                if (current is TNode) {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null) {
                return default(TNode);
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null) {
                return default(TNode);
            }

            return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null) {
                if (current is TNode) {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static bool HasAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.GetAncestors<TNode>().Any();
        }

        public static ISymbol GetEnclosingDeclaredTypeSymbol(this SyntaxNode node, SemanticModel semanticModel)
        {
            var typeBlockSyntax = (SyntaxNode)node.GetAncestor<TypeBlockSyntax>()
                ?? node.GetAncestor<TypeSyntax>();
            if (typeBlockSyntax == null) return null;
            return semanticModel.GetDeclaredSymbol(typeBlockSyntax);
        }

        public static SyntaxList<T> WithVbSourceMappingFrom<T>(this SyntaxList<T> converted, SyntaxNode node) where T : CSharpSyntaxNode
        {
            if (converted.Count != 1) return WithSourceMappingFrom(converted, node);
            var single = converted.Single();
            return converted.Replace(single, single.WithVbSourceMappingFrom(node));
        }

        public static SyntaxList<T> WithCsSourceMappingFrom<T>(this SyntaxList<T> converted, SyntaxNode node) where T : VisualBasicSyntaxNode
        {
            if (converted.Count != 1) return WithSourceMappingFrom(converted, node);
            var single = converted.Single();
            return converted.Replace(single, single.WithCsSourceMappingFrom(node));
        }

        private static SyntaxList<T> WithSourceMappingFrom<T>(this SyntaxList<T> converted, SyntaxNode node) where T : SyntaxNode
        {
            if (!converted.Any()) return converted;
            var origLinespan = node.SyntaxTree.GetLineSpan(node.Span);
            var first = converted.First();
            converted = converted.Replace(first, node.CopyAnnotationsTo(first).WithSourceStartLineAnnotation(origLinespan));
            var last = converted.Last();
            return converted.Replace(last, last.WithSourceEndLineAnnotation(origLinespan));
        }

        public static T WithVbSourceMappingFrom<T>(this T converted, SyntaxNodeOrToken fromSource) where T : CSharpSyntaxNode
        {
            if (converted == null) return null;
            var lastCsConvertedToken = converted.GetLastToken();
            if (lastCsConvertedToken.IsKind(CSSyntaxKind.CloseBraceToken) && IsBlockParent(converted, lastCsConvertedToken) && fromSource.AsNode()?.ChildNodes().LastOrDefault() is EndBlockStatementSyntax lastVbSourceNode) {
                converted = converted.ReplaceToken(lastCsConvertedToken, lastCsConvertedToken.WithSourceMappingFrom(lastVbSourceNode));
            }
            return converted.WithSourceMappingFrom(fromSource);
        }

        public static T WithCsSourceMappingFrom<T>(this T converted, SyntaxNodeOrToken fromSource) where T : VisualBasicSyntaxNode
        {
            if (converted == null) return null;
            var lastCsSourceToken = fromSource.AsNode()?.GetLastToken();
            if (lastCsSourceToken?.IsKind(CSSyntaxKind.CloseBraceToken) == true && IsBlockParent(fromSource.AsNode(), lastCsSourceToken.Value) && converted.ChildNodes().LastOrDefault() is EndBlockStatementSyntax lastVbConvertedNode) {
                converted = converted.ReplaceNode(lastVbConvertedNode, lastVbConvertedNode.WithSourceMappingFrom(lastCsSourceToken.Value));
            }
            return converted.WithSourceMappingFrom(fromSource);
        }

        private static T WithSourceMappingFrom<T>(this T converted, SyntaxNodeOrToken fromSource) where T : SyntaxNode
        {
            if (converted == null) return null;
            var linespan = fromSource.SyntaxTree.GetLineSpan(fromSource.Span);
            return converted.WithSourceStartLineAnnotation(linespan).WithSourceEndLineAnnotation(linespan);
        }

        public static T WithSourceStartLineAnnotation<T>(this T node, FileLinePositionSpan sourcePosition) where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceStartLine(sourcePosition));
        }

        public static T WithSourceEndLineAnnotation<T>(this T node, FileLinePositionSpan sourcePosition) where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceEndLine(sourcePosition));
        }

        public static T WithoutSourceMapping<T>(this T converted) where T : SyntaxNode
        {
            converted = converted.ReplaceTokens(converted.DescendantTokens(), (o, r) =>
                r.WithoutSourceMapping()
            );
            return converted.ReplaceNodes(converted.DescendantNodes(), (o, r) =>
                WithoutSourceMappingNonRecursive(r)
            ).WithoutSourceMappingNonRecursive();
        }

        private static T WithoutSourceMappingNonRecursive<T>(this T node) where T : SyntaxNode
        {
            return node.WithoutAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).WithoutAnnotations(AnnotationConstants.SourceEndLineAnnotationKind);
        }

        private static bool IsBlockParent(SyntaxNode converted, SyntaxToken lastCsConvertedToken)
        {
            return lastCsConvertedToken.Parent == converted || lastCsConvertedToken.Parent is BlockSyntax b && b.Parent == converted;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3, CSSyntaxKind kind4)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3, CSSyntaxKind kind4, CSSyntaxKind kind5)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
        }

        public static bool IsInStaticCsContext(this SyntaxNode node)
        {
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null) {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null) {
                return false;
            }

            switch (memberDeclaration.Kind()) {
                case CSSyntaxKind.MethodDeclaration:
                case CSSyntaxKind.ConstructorDeclaration:
                case CSSyntaxKind.PropertyDeclaration:
                case CSSyntaxKind.EventDeclaration:
                case CSSyntaxKind.IndexerDeclaration:
                    return memberDeclaration.GetModifiers().Any(CSSyntaxKind.StaticKeyword);

                case CSSyntaxKind.FieldDeclaration:
                    // Inside a field one can only access static members of a type.
                    return true;

                case CSSyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null) {
                return false;
            }

            // any other location is considered static
            return true;
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0) {
                return node;
            }

            return node.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0) {
                return node;
            }

            return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithPrependedLeadingTrivia(Microsoft.CodeAnalysis.CSharp.SyntaxExtensions.ToSyntaxTriviaList(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0) {
                return node;
            }

            return node.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0) {
                return node;
            }

            return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithAppendedTrailingTrivia(Microsoft.CodeAnalysis.CSharp.SyntaxExtensions.ToSyntaxTriviaList(trivia));
        }

        public static T With<T>(
            this T node,
            IEnumerable<SyntaxTrivia> leadingTrivia,
            IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
        {
            return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
        }

        public static SyntaxToken WithConvertedLeadingTriviaFrom(this SyntaxToken node, SyntaxToken? sourceToken)
        {
            if (sourceToken == null) return node;
            var convertedTrivia = ConvertTrivia(sourceToken.Value.LeadingTrivia);
            return node.WithLeadingTrivia(convertedTrivia);
        }

        public static T WithConvertedTrailingTriviaFrom<T>(this T node, SyntaxToken fromToken, TriviaKinds triviaKinds = null) where T : SyntaxNode
        {
            var lastConvertedToken = node.GetLastToken();
            return node.ReplaceToken(lastConvertedToken, lastConvertedToken.WithConvertedTrailingTriviaFrom(fromToken, triviaKinds));
        }

        public static SyntaxToken WithConvertedTrailingTriviaFrom(this SyntaxToken node, SyntaxToken? otherToken, TriviaKinds triviaKinds = null)
        {
            triviaKinds ??= TriviaKinds.All;
            if (!otherToken.HasValue || !otherToken.Value.HasTrailingTrivia) return node;
            var convertedTrivia = ConvertTrivia(otherToken.Value.TrailingTrivia.Where(triviaKinds.ShouldAccept).ToArray());
            return node.WithTrailingTrivia(node.ImportantTrailingTrivia().Concat(convertedTrivia));
        }

        public static IEnumerable<SyntaxTrivia> ImportantTrailingTrivia(this SyntaxToken node)
        {
            return node.TrailingTrivia.Where(x => !x.IsWhitespaceOrEndOfLine());
        }

        public static bool ParentHasSameTrailingTrivia(this SyntaxNode otherNode)
        {
            return otherNode.Parent.GetLastToken() == otherNode.GetLastToken();
        }

        public static IEnumerable<SyntaxTrivia> ConvertTrivia(this IReadOnlyCollection<SyntaxTrivia> triviaToConvert)
        {
            try {
                if (triviaToConvert.Any() && triviaToConvert.First().Language == LanguageNames.CSharp) {
                    return CSharpToVBCodeConverter.Util.RecursiveTriviaConverter.ConvertTopLevel(triviaToConvert).Where(x => x != default(SyntaxTrivia));
                }
                return triviaToConvert.SelectMany(ConvertVBTrivia).Where(x => x != default(SyntaxTrivia));
            } catch (Exception) {
                return Enumerable.Empty<SyntaxTrivia>(); //TODO Log this somewhere, or write enough tests to be really confident it won't throw
            }
        }

        private static IEnumerable<SyntaxTrivia> ConvertVBTrivia(SyntaxTrivia t)
        {
            if (t.IsKind(VBSyntaxKind.CommentTrivia)) {
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.SingleLineCommentTrivia, $"// {t.GetCommentText()}");
                yield break;
            }
            if (t.IsKind(VBSyntaxKind.DocumentationCommentTrivia)) {
                var previousWhitespace = t.GetPreviousTrivia(t.SyntaxTree, CancellationToken.None).ToString().Trim('\r', '\n');
                var commentTextLines = t.GetCommentText().Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                var outputCommentText = "/// " + String.Join($"\r\n{previousWhitespace}/// ", commentTextLines) + Environment.NewLine;
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.SingleLineCommentTrivia, outputCommentText); //It's always single line...even when it has multiple lines
                yield break;
            }

            if (t.IsKind(VBSyntaxKind.WhitespaceTrivia)) {
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.WhitespaceTrivia, t.ToString());
                yield break;
            }

            if (t.IsKind(VBSyntaxKind.EndOfLineTrivia)) {
                // Mapping one to one here leads to newlines appearing where the natural line-end was in VB.
                // e.g. ToString\r\n()
                // Because C Sharp needs those brackets. Handling each possible case of this is far more effort than it's worth.
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.EndOfLineTrivia, t.ToString());
                yield break;
            }

            //Each of these would need its own method to recreate for C# with the right structure probably so let's just warn about them for now.
            var convertedKind = t.GetCSKind();
            yield return convertedKind.HasValue
                ? SyntaxFactory.Comment($"/* TODO ERROR: Skipped {convertedKind.Value} */")
                : default(SyntaxTrivia);
        }

        public static SyntaxTokenList GetModifiers(this CSharpSyntaxNode member)
        {
            if (member != null) {
                switch (CSharpExtensions.Kind(member)) {
                    case CSSyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ClassDeclaration:
                    case CSSyntaxKind.InterfaceDeclaration:
                    case CSSyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.GetAccessorDeclaration:
                    case CSSyntaxKind.SetAccessorDeclaration:
                    case CSSyntaxKind.AddAccessorDeclaration:
                    case CSSyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).Modifiers;
                }
            }

            return default(SyntaxTokenList);
        }

        public static SyntaxTree WithAnnotatedNode(this SyntaxNode root, SyntaxNode selectedNode, string annotationKind, string annotationData = "")
        {
            var annotatatedNode =
                selectedNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotationKind, annotationData));
            return root.ReplaceNode(selectedNode, annotatatedNode).SyntaxTree.WithFilePath(root.SyntaxTree.FilePath);
        }

        public static string GetBriefNodeDescription(this SyntaxNode node)
        {
            var sb = new StringBuilder();
            sb.Append($"'{node.ToString().Truncate()}' at character {node.SpanStart}");
            return sb.ToString();
        }

        public static string DescribeConversionError(this SyntaxNode node, Exception e)
        {
            return $"Cannot convert {node.GetType().Name}, {e}{Environment.NewLine}{Environment.NewLine}" +
                $"Input:{Environment.NewLine}{node.ToFullString()}{Environment.NewLine}";
        }

        public static string DescribeConversionWarning(this SyntaxNode node, string addtlInfo)
        {
            return $"{addtlInfo}{Environment.NewLine}" +
                $"{node.NormalizeWhitespace().ToFullString()}{Environment.NewLine}";
        }

        private static string Truncate(this string input, int maxLength = 30, string truncationIndicator = "...")
        {
            input = input.Replace(Environment.NewLine, "\\r\\n").Replace("    ", " ").Replace("\t", " ");
            if (input.Length <= maxLength) return input;
            return input.Substring(0, maxLength - truncationIndicator.Length) + truncationIndicator;
        }

        public static T WithCsTrailingErrorComment<T>(this T dummyDestNode,
            VisualBasicSyntaxNode sourceNode,
            Exception exception) where T : CSharpSyntaxNode
        {
            var errorDirective = SyntaxFactory.ParseTrailingTrivia($"#error Cannot convert {sourceNode.GetType().Name} - see comment for details{Environment.NewLine}");
            var errorDescription = sourceNode.DescribeConversionError(exception);
            var commentedText = "/* " + errorDescription + " */";
            var trailingTrivia = SyntaxFactory.TriviaList(errorDirective.Concat(SyntaxFactory.Comment(commentedText)));

            return dummyDestNode
                .WithTrailingTrivia(trailingTrivia)
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind, exception.ToString()));
        }

        public static T WithVbTrailingErrorComment<T>(
            this T dummyDestNode, CSharpSyntaxNode problematicSourceNode, Exception exception) where T : VisualBasicSyntaxNode
        {
            var errorDescription = problematicSourceNode.DescribeConversionError(exception);
            var commentedText = "''' " + errorDescription.Replace("\r\n", "\r\n''' ");
            return dummyDestNode
                .WithTrailingTrivia(VBSyntaxFactory.CommentTrivia(commentedText))
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind,
                    exception.ToString()));
        }

        public static bool ContainsDeclaredVisibility(this SyntaxTokenList modifiers, bool isVariableOrConst = false, bool isConstructor = false)
        {
            return modifiers.Any(m => m.IsCsVisibility(isVariableOrConst, isConstructor));
        }

        public static SyntaxToken FindNonZeroWidthToken(this SyntaxNode node, int position)
        {
            var syntaxToken = node.FindToken(position);
            if (syntaxToken.FullWidth() == 0) {
                return syntaxToken.GetPreviousToken();
            } else {
                return syntaxToken;
            }
        }
    }
}