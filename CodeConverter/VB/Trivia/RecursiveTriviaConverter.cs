using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using CSharpToVBCodeConverter.DestVisualBasic;
using ICSharpCode.CodeConverter.Util;

namespace CSharpToVBCodeConverter.Util
{
    /// <summary>
    /// Ported from https://github.com/paul1956/CSharpToVB/blob/fb33d1e7b938255b215a71c54552d2db29d535fb/CodeConverter/Extensions/SyntaxNodeExtensions.vb#L938
    /// With permission of Paul1956: https://github.com/icsharpcode/CodeConverter/issues/8#issuecomment-554716065
    /// </summary>
    internal class RecursiveTriviaConverter
    {
        public static IEnumerable<SyntaxTrivia> ConvertTopLevel(IReadOnlyCollection<SyntaxTrivia> triviaToConvert)
        {
            return new RecursiveTriviaConverter().ConvertTrivia(triviaToConvert);
        }

        private int TriviaDepth = 0;

        private string RemoveLeadingSpacesStar(string line)
        {
            var NewStringBuilder = new StringBuilder();
            bool SkipSpace = true;
            bool SkipStar = true;
            foreach (char c in line) {
                switch (c) {
                    case ' ': {
                            if (SkipSpace) {
                                continue;
                            }
                            NewStringBuilder.Append(c);
                            break;
                        }

                    case '*': {
                            if (SkipStar) {
                                SkipSpace = false;
                                SkipStar = false;
                                continue;
                            }
                            NewStringBuilder.Append(c);
                            break;
                        }

                    default: {
                            SkipSpace = false;
                            SkipStar = false;
                            NewStringBuilder.Append(c);
                            break;
                        }
                }
            }
            return NewStringBuilder.ToString();
        }

        private string ReplaceLeadingSlashes(string CommentTriviaBody)
        {
            for (int i = 0, loopTo = CommentTriviaBody.Length - 1; i <= loopTo; i++) {
                if ((CommentTriviaBody.Substring(i, 1) ?? "") == "/") {
                    CommentTriviaBody = CommentTriviaBody.Remove(i, 1).Insert(i, "'");
                } else {
                    break;
                }
            }
            return CommentTriviaBody;
        }

        internal T WithAppendedTriviaFromEndOfDirectiveToken<T>(T node, SyntaxToken Token) where T : SyntaxNode
        {
            var NewTrailingTrivia = new List<SyntaxTrivia>();
            if (Token.HasLeadingTrivia) {
                NewTrailingTrivia.AddRange(Token.LeadingTrivia.ConvertTrivia());
            }
            if (Token.HasTrailingTrivia) {
                NewTrailingTrivia.AddRange(Token.TrailingTrivia.ConvertTrivia());
            }

            return WithTrailingEOL(node.WithAppendedTrailingTrivia(NewTrailingTrivia));
        }

        /// <summary>
        /// Make sure the node (usually a statement) ends with an EOL and possibly whitespace
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        internal T WithTrailingEOL<T>(T node) where T : SyntaxNode
        {
            var TrailingTrivia = node.GetTrailingTrivia().ToList();
            int Count = TrailingTrivia.Count;
            if (Count == 0) {
                return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
            }

            var switchExpr = Count;
            switch (switchExpr) {
                case 1: {
                        var switchExpr1 = TrailingTrivia.Last().RawKind;
                        switch (switchExpr1) {
                            case (int)VB.SyntaxKind.WhitespaceTrivia:
                            case (int)VB.SyntaxKind.EndOfLineTrivia: {
                                    return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }

                            default: {
                                    TrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    return node.WithTrailingTrivia(TrailingTrivia);
                                }
                        }
                    }

                case 2: {
                        var switchExpr2 = TrailingTrivia.First().RawKind;
                        switch (switchExpr2) {
                            case (int)VB.SyntaxKind.WhitespaceTrivia: {
                                    var switchExpr3 = TrailingTrivia.Last().RawKind;
                                    switch (switchExpr3) {
                                        case (int)VB.SyntaxKind.WhitespaceTrivia:
                                        case (int)VB.SyntaxKind.EndOfLineTrivia: {
                                                // Replace Whitespace, Whitespace and Whitespace, EOL with just EOL
                                                TrailingTrivia = new List<SyntaxTrivia>();
                                                break;
                                            }

                                        case (int)VB.SyntaxKind.CommentTrivia: {
                                                break;
                                            }

                                        default: {

                                                break;
                                            }
                                    }

                                    break;
                                }

                            case (int)VB.SyntaxKind.EndOfLineTrivia: {
                                    if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.WhitespaceTrivia)) {
                                        return node;
                                    } else if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia)) {
                                        return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }

                                    break;
                                }

                            case (int)VB.SyntaxKind.CommentTrivia: {
                                    if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.WhitespaceTrivia)) {
                                        TrailingTrivia.RemoveAt(1);
                                        TrailingTrivia.Insert(0, global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        // EOL added below

                                    }

                                    break;
                                }

                            default: {

                                    break;
                                }
                        }
                        TrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                        return node.WithTrailingTrivia(TrailingTrivia);
                    }

                default: {
                        Count -= 1; // Last index
                        var switchExpr4 = TrailingTrivia.Last().RawKind;
                        switch (switchExpr4) {
                            case (int)VB.SyntaxKind.EndOfLineTrivia: {
                                    if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.EndOfLineTrivia)) {
                                        TrailingTrivia.RemoveAt(Count);
                                        return WithTrailingEOL(node.WithTrailingTrivia(TrailingTrivia));
                                    }
                                    return node;
                                }

                            case (int)VB.SyntaxKind.WhitespaceTrivia: {
                                    if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.WhitespaceTrivia)) {
                                        TrailingTrivia.RemoveAt(Count - 1);
                                        return WithTrailingEOL(node.WithTrailingTrivia(TrailingTrivia));
                                    } else if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.EndOfLineTrivia)) {
                                        return node;
                                    } else if (TrailingTrivia[Count - 1].IsCommentOrDirectiveTrivia()) {
                                        TrailingTrivia.Insert(Count, global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                        return node.WithTrailingTrivia(TrailingTrivia);
                                    }
                                    return node.WithTrailingTrivia(TrailingTrivia);
                                }

                            default: {

                                    break;
                                }
                        }

                        break;
                    }
            }
            return node;
        }

        internal SyntaxTrivia ConvertTrivia(SyntaxTrivia t)
        {
            var switchExpr = t.RawKind;

            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            switch (switchExpr) {
                case (int)CS.SyntaxKind.WhitespaceTrivia: {
                        return VBFactory.WhitespaceTrivia(t.ToString());
                    }

                case (int)CS.SyntaxKind.EndOfLineTrivia: {
                        return global::VisualBasicSyntaxFactory.VBEOLTrivia;
                    }

                case (int)CS.SyntaxKind.SingleLineCommentTrivia: {
                        if (t.ToFullString().EndsWith("*/")) {
                            return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2, t.ToFullString().Length - 4))}");
                        }
                        return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2))}");
                    }

                case (int)CS.SyntaxKind.MultiLineCommentTrivia: {
                        if (t.ToFullString().EndsWith("*/")) {
                            return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2, t.ToFullString().Length - 4)).Replace(Constants.vbLf, "").Replace(Constants.vbCr, "")}");
                        }
                        return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2)).Replace(Constants.vbLf, "").Replace(Constants.vbCr, "")}");
                    }

                case (int)CS.SyntaxKind.DocumentationCommentExteriorTrivia: {
                        return VBFactory.SyntaxTrivia(VB.SyntaxKind.CommentTrivia, "'''");
                    }

                case (int)CS.SyntaxKind.DisabledTextTrivia: {
                        if (TriviaDepth > 0) {
                            return VBFactory.DisabledTextTrivia(t.ToString().WithoutNewLines(' '));
                        }
                        return VBFactory.DisabledTextTrivia(t.ToString().ConsistentNewlines());
                    }

                case (int)CS.SyntaxKind.PreprocessingMessageTrivia: {
                        return VBFactory.CommentTrivia($" ' {t}");
                    }

                case (int)CS.SyntaxKind.None: {
                        return default(SyntaxTrivia);
                    }
            }

            /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            CSS.StructuredTriviaSyntax StructuredTrivia = (CSS.StructuredTriviaSyntax)t.GetStructure();
            Debug.Assert(StructuredTrivia != null, $"Found new type of non structured trivia {t.RawKind}");
            var switchExpr1 = t.RawKind;
            switch (switchExpr1) {
                case (int)CS.SyntaxKind.DefineDirectiveTrivia: {
                        CSS.DefineDirectiveTriviaSyntax DefineDirective = (CSS.DefineDirectiveTriviaSyntax)StructuredTrivia;
                        var Name = VBFactory.Identifier(DefineDirective.Name.ValueText);
                        VBS.ExpressionSyntax value = VBFactory.TrueLiteralExpression(global::VisualBasicSyntaxFactory.TrueKeyword);
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(WithConvertedTriviaFrom(VBFactory.ConstDirectiveTrivia(Name, value), DefineDirective), DefineDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.UndefDirectiveTrivia: {
                        CSS.UndefDirectiveTriviaSyntax UndefineDirective = (CSS.UndefDirectiveTriviaSyntax)StructuredTrivia;
                        var Name = VBFactory.Identifier(UndefineDirective.Name.ValueText);
                        VBS.ExpressionSyntax value = global::VisualBasicSyntaxFactory.NothingExpression;
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(WithConvertedTriviaFrom(VBFactory.ConstDirectiveTrivia(Name, value), UndefineDirective), UndefineDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.EndIfDirectiveTrivia: {
                        if (TriviaDepth > 0) {
                            TriviaDepth -= 1;
                            return VBFactory.CommentTrivia($"' TODO VB does not allow directives here, original statement {t.ToFullString().WithoutNewLines(' ')}");
                        }
                        CSS.EndIfDirectiveTriviaSyntax EndIfDirective = (CSS.EndIfDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(VBFactory.EndIfDirectiveTrivia().WithConvertedTrailingTriviaFrom(EndIfDirective.EndIfKeyword), EndIfDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.ErrorDirectiveTrivia: {
                        CSS.ErrorDirectiveTriviaSyntax ErrorDirective = (CSS.ErrorDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.CommentTrivia($"' TODO: Check VB does not support Error Directive Trivia, Original Directive {ErrorDirective.ToFullString()}");
                    }

                case (int)CS.SyntaxKind.IfDirectiveTrivia: {
                        if (t.Token.Parent?.AncestorsAndSelf().OfType<CSS.InitializerExpressionSyntax>().Any() == true) {
                            TriviaDepth += 1;
                        }
                        CSS.IfDirectiveTriviaSyntax IfDirective = (CSS.IfDirectiveTriviaSyntax)StructuredTrivia;
                        string Expression1 = StringReplaceCondition(IfDirective.Condition.ToString());

                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(VBFactory.IfDirectiveTrivia(global::VisualBasicSyntaxFactory.IfKeyword, VBFactory.ParseExpression(Expression1)).With(IfDirective.GetLeadingTrivia().ConvertTrivia(), IfDirective.Condition.GetTrailingTrivia().ConvertTrivia()), IfDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.ElifDirectiveTrivia: {
                        if (t.Token.Parent.AncestorsAndSelf().OfType<CSS.InitializerExpressionSyntax>().Any()) {
                            TriviaDepth += 1;
                        }
                        CSS.ElifDirectiveTriviaSyntax ELIfDirective = (CSS.ElifDirectiveTriviaSyntax)StructuredTrivia;
                        string Expression1 = StringReplaceCondition(ELIfDirective.Condition.ToString());

                        SyntaxToken IfOrElseIfKeyword;
                        if (t.IsKind(CS.SyntaxKind.ElifDirectiveTrivia)) {
                            IfOrElseIfKeyword = global::VisualBasicSyntaxFactory.ElseIfKeyword;
                        } else {
                            IfOrElseIfKeyword = global::VisualBasicSyntaxFactory.IfKeyword;
                        }
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(VBFactory.ElseIfDirectiveTrivia(IfOrElseIfKeyword, VBFactory.ParseExpression(Expression1))
                            .With(ELIfDirective.GetLeadingTrivia().ConvertTrivia(), ELIfDirective.Condition.GetTrailingTrivia().ConvertTrivia())
                            , ELIfDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.LineDirectiveTrivia: {
                        return VBFactory.CommentTrivia($"' TODO: Check VB does not support Line Directive Trivia, Original Directive {t}");
                    }

                case (int)CS.SyntaxKind.ElseDirectiveTrivia: {
                        return VBFactory.Trivia(WithTrailingEOL(VBFactory.ElseDirectiveTrivia().NormalizeWhitespace().WithConvertedTrailingTriviaFrom(((CSS.ElseDirectiveTriviaSyntax)StructuredTrivia).ElseKeyword)));
                    }

                case (int)CS.SyntaxKind.EndRegionDirectiveTrivia: {
                        CSS.EndRegionDirectiveTriviaSyntax EndRegionDirective = (CSS.EndRegionDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(VBFactory.EndRegionDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.EndKeyword, global::VisualBasicSyntaxFactory.RegionKeyword), EndRegionDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.PragmaWarningDirectiveTrivia: {
                        // Dim PragmaWarningDirectiveTrivia As CSS.PragmaWarningDirectiveTriviaSyntax = DirectCast(StructuredTrivia, CSS.PragmaWarningDirectiveTriviaSyntax)
                        // Dim ErrorList As New List(Of VBS.IdentifierNameSyntax)
                        // Dim TrailingTriviaStringBuilder As New StringBuilder
                        // For Each i As CSS.ExpressionSyntax In PragmaWarningDirectiveTrivia.ErrorCodes
                        // Dim ErrorCode As String = i.ToString
                        // If ErrorCode.IsInteger Then
                        // ErrorCode = $"CS_{ErrorCode}"
                        // End If
                        // ErrorList.Add(VBFactory.IdentifierName(ErrorCode))
                        // For Each Trivial As SyntaxTrivia In i.GetTrailingTrivia
                        // TrailingTriviaStringBuilder.Append(Trivial.ToString)
                        // Next
                        // Next
                        // Dim WarningDirectiveTrivia As VBS.DirectiveTriviaSyntax
                        // If PragmaWarningDirectiveTrivia.DisableOrRestoreKeyword.IsKind(CS.SyntaxKind.DisableKeyword) Then
                        // WarningDirectiveTrivia = VBFactory.DisableWarningDirectiveTrivia(ErrorList.ToArray)
                        // Else
                        // WarningDirectiveTrivia = VBFactory.EnableWarningDirectiveTrivia(ErrorList.ToArray)
                        // End If
                        // Return VBFactory.CommentTrivia($" ' TODO {WarningDirectiveTrivia.NormalizeWhitespace}{TrailingTriviaStringBuilder.ToString}")
                        return default(SyntaxTrivia);
                    }

                case (int)CS.SyntaxKind.RegionDirectiveTrivia: {
                        CSS.RegionDirectiveTriviaSyntax RegionDirective = (CSS.RegionDirectiveTriviaSyntax)StructuredTrivia;
                        var EndOfDirectiveToken = RegionDirective.EndOfDirectiveToken;
                        string NameString = $"\"{EndOfDirectiveToken.LeadingTrivia.ToString().Replace("\"", "")}\"";
                        var RegionDirectiveTriviaNode = VBFactory.RegionDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.RegionKeyword, VBFactory.StringLiteralToken(NameString, NameString)
    ).WithConvertedTrailingTriviaFrom(EndOfDirectiveToken);
                        return VBFactory.Trivia(WithTrailingEOL(RegionDirectiveTriviaNode));
                    }

                case (int)CS.SyntaxKind.SingleLineDocumentationCommentTrivia: {
                        CSS.DocumentationCommentTriviaSyntax SingleLineDocumentationComment = (CSS.DocumentationCommentTriviaSyntax)StructuredTrivia;
                        var walker = new XMLVisitor();
                        walker.Visit(SingleLineDocumentationComment);

                        var xmlNodes = new List<VBS.XmlNodeSyntax>();
                        for (int i = 0, loopTo = SingleLineDocumentationComment.Content.Count - 1; i <= loopTo; i++) {
                            var node = SingleLineDocumentationComment.Content[i];
                            if (!node.IsKind(CS.SyntaxKind.XmlText) && node.GetLeadingTrivia().Count > 0 && node.GetLeadingTrivia().First().IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia)) {
                                if (i < SingleLineDocumentationComment.Content.Count - 1) {
                                    var NextNode = SingleLineDocumentationComment.Content[i + 1];
                                    if (!NextNode.IsKind(CS.SyntaxKind.XmlText) || NextNode.GetLeadingTrivia().Count == 0 || !NextNode.GetLeadingTrivia().First().IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia)) {
                                        xmlNodes.Add(VBFactory.XmlText(" ").WithLeadingTrivia(VBFactory.DocumentationCommentExteriorTrivia("'''")));
                                    }
                                }
                                node = node.WithoutLeadingTrivia();
                            }
                            VBS.XmlNodeSyntax Item = (VBS.XmlNodeSyntax)node.Accept(walker);
                            xmlNodes.Add(Item);
                        }
                        var DocumentationCommentTrivia = VBFactory.DocumentationCommentTrivia(VBFactory.List(xmlNodes.ToArray()));
                        if (!DocumentationCommentTrivia.HasLeadingTrivia || !DocumentationCommentTrivia.GetLeadingTrivia()[0].IsKind(VB.SyntaxKind.DocumentationCommentExteriorTrivia)) {
                            DocumentationCommentTrivia = DocumentationCommentTrivia.WithLeadingTrivia(VBFactory.DocumentationCommentExteriorTrivia("''' "));
                        }
                        var _DocumentationComment = VBFactory.Trivia(DocumentationCommentTrivia.WithTrailingTrivia(VBFactory.EndOfLine("")));
                        return _DocumentationComment;
                    }

                case (int)CS.SyntaxKind.PragmaChecksumDirectiveTrivia: {
                        CSS.PragmaChecksumDirectiveTriviaSyntax PragmaChecksumDirective = (CSS.PragmaChecksumDirectiveTriviaSyntax)StructuredTrivia;
                        var Guid1 = VBFactory.ParseToken(PragmaChecksumDirective.Guid.Text.ToUpperInvariant());
                        var Bytes = VBFactory.ParseToken(PragmaChecksumDirective.Bytes.Text);
                        var ExternalSource = VBFactory.ParseToken(PragmaChecksumDirective.File.Text);
                        return VBFactory.Trivia(WithAppendedTriviaFromEndOfDirectiveToken(
                            VBFactory.ExternalChecksumDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.ExternalChecksumKeyword, global::VisualBasicSyntaxFactory.OpenParenToken, ExternalSource, global::VisualBasicSyntaxFactory.CommaToken, Guid1, global::VisualBasicSyntaxFactory.CommaToken, Bytes, global::VisualBasicSyntaxFactory.CloseParenToken), PragmaChecksumDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.SkippedTokensTrivia: {
                        var Builder = new StringBuilder();
                        foreach (SyntaxToken tok in ((CSS.SkippedTokensTriviaSyntax)StructuredTrivia).Tokens)
                            Builder.Append(tok.ToString());
                        return VBFactory.CommentTrivia($"' TODO: Error SkippedTokensTrivia '{Builder}'");
                    }

                case (int)CS.SyntaxKind.BadDirectiveTrivia: {
                        return VBFactory.CommentTrivia($"' TODO: Skipped BadDirectiveTrivia");
                    }

                case (int)CS.SyntaxKind.ConflictMarkerTrivia: {
                        break;
                    }

                case (int)CS.SyntaxKind.LoadDirectiveTrivia: {
                        break;
                    }

                default: {
                        Debug.WriteLine(((VB.SyntaxKind)Conversions.ToUShort(t.RawKind)).ToString());

                        break;
                    }
            }
            throw new NotImplementedException($"t.Kind({(VB.SyntaxKind)Conversions.ToUShort(t.RawKind)}) Is unknown");
        }

        internal string StringReplaceCondition(string csCondition)
        {
            return csCondition.Replace("==", "=").Replace("!=", "<>").Replace("&&", "And").Replace("||", "Or").Replace("  ", " ").Replace("!", "Not ").Replace("false", "False").Replace("true", "True");
        }

        internal IEnumerable<SyntaxTrivia> ConvertTrivia(IReadOnlyCollection<SyntaxTrivia> TriviaToConvert)
        {
            var TriviaList = new List<SyntaxTrivia>();
            if (TriviaToConvert == null) {
                return TriviaList;
            }
            int TriviaCount = TriviaToConvert.Count - 1;
            for (int i = 0, loopTo = TriviaCount; i <= loopTo; i++) {
                var Trivia = TriviaToConvert.ElementAtOrDefault(i);
                var NextTrivia = i < TriviaCount ? TriviaToConvert.ElementAtOrDefault(i + 1) : default(SyntaxTrivia);
                var switchExpr = Trivia.RawKind;
                switch (switchExpr) {
                    case (int)CS.SyntaxKind.MultiLineCommentTrivia: {
                            var Lines = Trivia.ToFullString().Substring(2).Split(new[] { "\r\n" }, StringSplitOptions.None);
                            foreach (string line in Lines) {
                                if (line.EndsWith("*/")) {
                                    TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line.Substring(0, line.Length - 2))}"));
                                    if (Trivia.ToFullString().EndsWith(Constants.vbLf)) {
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }
                                } else {
                                    TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line)}"));
                                    TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }
                                if (Lines.Length == 1 && (i == TriviaCount || !TriviaToConvert.ElementAtOrDefault(i + 1).IsEndOfLine())) {
                                    TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }
                            }

                            break;
                        }

                    case (int)CS.SyntaxKind.MultiLineDocumentationCommentTrivia: {
                            CSS.StructuredTriviaSyntax sld = (CSS.StructuredTriviaSyntax)Trivia.GetStructure();
                            foreach (SyntaxNode t1 in sld.ChildNodes()) {
                                var Lines = t1.ToFullString().ConsistentNewlines().Split(new[] { "\r\n" }, StringSplitOptions.None);
                                foreach (string line in Lines) {
                                    if (line.StartsWith("/*")) {
                                        TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line.Substring(1, line.Length - 1))}"));
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    } else {
                                        TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line)}"));
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }
                                }
                            }

                            break;
                        }

                    default: {
                            var ConvertedTrivia = ConvertTrivia(Trivia);
                            if (ConvertedTrivia == null) {
                                continue;
                            }
                            TriviaList.Add(ConvertedTrivia);
                            if (Trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia)) {
                                if (!NextTrivia.IsKind(CS.SyntaxKind.EndOfLineTrivia)) {
                                    TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }
                            }

                            break;
                        }
                }
            }
            return TriviaList;
        }

        internal T WithConvertedTriviaFrom<T>(T node, SyntaxNode otherNode) where T : SyntaxNode
        {
            if (otherNode == null) {
                return node;
            }
            if (otherNode.HasLeadingTrivia) {
                node = node.WithLeadingTrivia(otherNode.GetLeadingTrivia().ConvertTrivia());
            }
            if (!otherNode.HasTrailingTrivia || otherNode.ParentHasSameTrailingTrivia()) {
                return node;
            }
            return node.WithTrailingTrivia(otherNode.GetTrailingTrivia().ConvertTrivia());
        }
    }
}

