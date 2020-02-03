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
using VisualBasicSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using CSharpToVBCodeConverter.DestVisualBasic;
using ICSharpCode.CodeConverter.Util;

namespace CSharpToVBCodeConverter.Util
{
    public static class SyntaxNodeExtensions
    {

        /// <summary>
        /// Used at the end of a statement to adjust trivia from two items (like semicolon) the second
        /// of which will be removed. Directives are allowed.
        /// </summary>
        /// <param name="TriviaList"></param>
        /// <param name="NewTrailingTrivia"></param>
        /// <param name="FoundEOL"></param>
        /// <param name="FoundWhiteSpace"></param>
        private static void AdjustTrailingTrivia(IEnumerable<SyntaxTrivia> TriviaList, List<SyntaxTrivia> NewTrailingTrivia, ref bool FoundEOL, ref bool FoundWhiteSpace)
        {
            for (int i = 0, loopTo = TriviaList.Count() - 1; i <= loopTo; i++)
            {
                var Trivia = TriviaList.ElementAtOrDefault(i);
                var NextTrivia = i < TriviaList.Count() - 1 ? TriviaList.ElementAtOrDefault(i + 1) : default(SyntaxTrivia);
                var switchExpr = Trivia.RawKind;
                switch (switchExpr)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                        {
                            if (!FoundWhiteSpace && !NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                NewTrailingTrivia.Add(Trivia);
                                FoundEOL = false;
                                FoundWhiteSpace = true;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (!FoundEOL)
                            {
                                NewTrailingTrivia.Add(Trivia);
                                FoundEOL = true;
                            }
                            FoundWhiteSpace = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            NewTrailingTrivia.Add(Trivia);
                            if (!NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                NewTrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                FoundEOL = true;
                            }
                            FoundWhiteSpace = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.EndRegionDirectiveTrivia:
                        {
                            if (!FoundEOL)
                            {
                                NewTrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                            }
                            NewTrailingTrivia.Add(Trivia);
                            FoundEOL = false;
                            FoundWhiteSpace = false;
                            break;
                        }

                    default:
                        {
                            if (Trivia.IsDirective)
                            {
                                if (!FoundEOL)
                                {
                                    NewTrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }
                                NewTrailingTrivia.Add(Trivia);
                                FoundEOL = false;
                                FoundWhiteSpace = false;
                            }
                            else
                            {

                            }

                            break;
                        }
                }
            }
        }

        private static string RemoveLeadingSpacesStar(string line)
        {
            var NewStringBuilder = new StringBuilder();
            bool SkipSpace = true;
            bool SkipStar = true;
            foreach (char c in line)
            {
                switch (c)
                {
                    case ' ':
                        {
                            if (SkipSpace)
                            {
                                continue;
                            }
                            NewStringBuilder.Append(c);
                            break;
                        }

                    case '*':
                        {
                            if (SkipStar)
                            {
                                SkipSpace = false;
                                SkipStar = false;
                                continue;
                            }
                            NewStringBuilder.Append(c);
                            break;
                        }

                    default:
                        {
                            SkipSpace = false;
                            SkipStar = false;
                            NewStringBuilder.Append(c);
                            break;
                        }
                }
            }
            return NewStringBuilder.ToString();
        }

        private static string ReplaceLeadingSlashes(string CommentTriviaBody)
        {
            for (int i = 0, loopTo = CommentTriviaBody.Length - 1; i <= loopTo; i++)
            {
                if ((CommentTriviaBody.Substring(i, 1) ?? "") == "/")
                {
                    CommentTriviaBody = CommentTriviaBody.Remove(i, 1).Insert(i, "'");
                }
                else
                {
                    break;
                }
            }
            return CommentTriviaBody;
        }

        internal static T With<T>(this T node, IEnumerable<SyntaxTrivia> leadingTrivia, IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
        {
            return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
        }

        internal static List<SyntaxTrivia> ConvertDirectiveTrivia(this string OriginalText)
        {
            string Text = OriginalText.Trim(' ');
            var ResultTrivia = new List<SyntaxTrivia>();
            Debug.Assert(Text.StartsWith("#"), "All directives must start with #");

            if (Text.StartsWith("#if") || Text.StartsWith("#elif"))
            {
                string Expression1 = Text.Replace("#if ", "").Replace("#elif ", "").Replace("!", "Not ").Replace("==", "=").Replace("!=", "<>").Replace("&&", "And").Replace("||", "Or").Replace("  ", " ").Replace("false", "False").Replace("true", "True").Replace("//", " ' ").Replace("  ", " ");

                var Kind = Text.StartsWith("#if") ? VB.SyntaxKind.IfDirectiveTrivia : VB.SyntaxKind.ElseIfDirectiveTrivia;
                var IfOrElseIfKeyword = Text.StartsWith("#if") ? global::VisualBasicSyntaxFactory.IfKeyword : global::VisualBasicSyntaxFactory.ElseIfKeyword;
                var Expr = VBFactory.ParseExpression(Expression1);
                var IfDirectiveTrivia = VBFactory.IfDirectiveTrivia(IfOrElseIfKeyword, Expr);
                ResultTrivia.Add(VBFactory.Trivia(IfDirectiveTrivia));
                return ResultTrivia;
            }
            if (Text.StartsWith("#region") || Text.StartsWith("# region"))
            {
                ResultTrivia.AddRange(CS.SyntaxFactory.ParseLeadingTrivia(Text).ConvertTrivia());
                return ResultTrivia;
            }
            if (Text.StartsWith("#endregion"))
            {
                ResultTrivia.Add(VBFactory.Trivia(VBFactory.EndRegionDirectiveTrivia()));
                Text = Text.Replace("#endregion", "");
                if (Text.Length > 0)
                {

                }
                return ResultTrivia;
            }
            if (Text.StartsWith("#else"))
            {
                var ElseKeywordWithTrailingTrivia = global::VisualBasicSyntaxFactory.ElseKeyword.WithTrailingTrivia(CS.SyntaxFactory.ParseTrailingTrivia(Text.Replace("#else", "")).ConvertTrivia());
                ResultTrivia.Add(VBFactory.Trivia(VBFactory.ElseDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, ElseKeywordWithTrailingTrivia)));
                return ResultTrivia;
            }
            if (Text.StartsWith("#endif"))
            {
                Text = Text.Replace("#endif", "");
                var IfKeywordWithTrailingTrivia = global::VisualBasicSyntaxFactory.IfKeyword.WithTrailingTrivia(CS.SyntaxFactory.ParseTrailingTrivia(Text.Replace("#endif", "")).ConvertTrivia());
                ResultTrivia.Add(VBFactory.Trivia(VBFactory.EndIfDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.EndKeyword, IfKeywordWithTrailingTrivia)));
                return ResultTrivia;
            }
            if (Text.StartsWith("#pragma warning"))
            {
                ResultTrivia.AddRange(CS.SyntaxFactory.ParseLeadingTrivia(Text).ConvertTrivia());
                return ResultTrivia;
            }
            else
            {
                throw new NotImplementedException($"Directive \"{Text}\" Is unknown");
            }
        }

        internal static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node) where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent : current.Parent;
            }
        }

        internal static bool IsKind(this SyntaxNode node, params CS.SyntaxKind[] kind1)
        {
            if (node == null)
            {
                return false;
            }

            foreach (CS.SyntaxKind k in kind1)
            {
                if (node.IsKind(k))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool ParentHasSameTrailingTrivia(this SyntaxNode otherNode)
        {
            if (otherNode.Parent == null)
            {
                return false;
            }
            return otherNode.Parent.GetLastToken() == otherNode.GetLastToken();
        }

        internal static T RelocateDirectivesInLeadingTrivia<T>(this T Statement) where T : VB.VisualBasicSyntaxNode
        {
            var NewLeadingTrivia = new List<SyntaxTrivia>();
            var NewTrailingTrivia = new List<SyntaxTrivia>();
            NewLeadingTrivia.AddRange(Statement.GetLeadingTrivia());
            foreach (SyntaxTrivia Trivia in Statement.GetTrailingTrivia())
            {
                var switchExpr = Trivia.RawKind;
                switch (switchExpr)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            NewTrailingTrivia.Add(Trivia);
                            break;
                        }

                    case (int)VB.SyntaxKind.IfDirectiveTrivia:
                        {
                            NewLeadingTrivia.Add(Trivia);
                            break;
                        }

                    default:
                        {

                            break;
                        }
                }
            }
            return Statement.With(NewLeadingTrivia, NewTrailingTrivia).WithTrailingEOL();
        }

        /// <summary>
        /// Remove Leading EOL and convert multiple EOL's to one.
        /// This is used in statements that are more then 1 line
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns>Node with extra EOL trivia removed</returns>
        internal static T RemoveExtraLeadingEOL<T>(this T node) where T : SyntaxNode
        {
            var LeadingTrivia = node.GetLeadingTrivia().ToList();
            var switchExpr = LeadingTrivia.Count;
            switch (switchExpr)
            {
                case 0:
                    {
                        return node;
                    }

                case 1:
                    {
                        if (LeadingTrivia.First().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                        {
                            return node.WithoutLeadingTrivia();
                        }

                        break;
                    }

                case 2:
                    {
                        var switchExpr1 = LeadingTrivia.First().RawKind;
                        switch (switchExpr1)
                        {
                            case (int)VB.SyntaxKind.WhitespaceTrivia:
                                {
                                    if (LeadingTrivia.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        return node.WithoutLeadingTrivia();
                                    }
                                    return node;
                                }

                            case (int)VB.SyntaxKind.EndOfLineTrivia:
                                {
                                    if (LeadingTrivia.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        return node.WithoutLeadingTrivia();
                                    }
                                    return node.WithLeadingTrivia(LeadingTrivia.Last());
                                }

                            default:
                                {
                                    break;
                                }
                        }

                        break;
                    }

                default:
                    {
                        break;
                    }
            }
            var NewLeadingTrivia = new List<SyntaxTrivia>();
            bool FirstTrivia = true;
            for (int i = 0, loopTo = node.GetLeadingTrivia().Count - 1; i <= loopTo; i++)
            {
                var Trivia = node.GetLeadingTrivia()[i];
                var NextTrivia = i < node.GetLeadingTrivia().Count - 1 ? node.GetLeadingTrivia()[i + 1] : default(SyntaxTrivia);
                if (Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) && (FirstTrivia || NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia)))
                {
                    continue;
                }
                if (Trivia.IsKind(VB.SyntaxKind.WhitespaceTrivia) && NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                {
                    continue;
                }

                FirstTrivia = false;
                NewLeadingTrivia.Add(Trivia);
            }

            return node.WithLeadingTrivia(NewLeadingTrivia);
        }

        internal static T WithAppendedTrailingTrivia<T>(this T node, params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0)
            {
                return node;
            }

            return node.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        internal static T WithAppendedTriviaFromEndOfDirectiveToken<T>(this T node, SyntaxToken Token) where T : SyntaxNode
        {
            var NewTrailingTrivia = new List<SyntaxTrivia>();
            if (Token.HasLeadingTrivia)
            {
                NewTrailingTrivia.AddRange(Token.LeadingTrivia.ConvertTrivia());
            }
            if (Token.HasTrailingTrivia)
            {
                NewTrailingTrivia.AddRange(Token.TrailingTrivia.ConvertTrivia());
            }

            return node.WithAppendedTrailingTrivia(NewTrailingTrivia).WithTrailingEOL();
        }

        /// <summary>
        /// Merge trailing trivia
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="TriviaListToMerge"></param>
        /// <returns></returns>
        internal static T WithMergedTrailingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> TriviaListToMerge) where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }
            if (!TriviaListToMerge?.Any() == true)
            {
                return node;
            }
            var NodeTrailingTrivia = node.GetTrailingTrivia().ToList();
            if (!NodeTrailingTrivia.Any())
            {
                return node.WithTrailingTrivia(TriviaListToMerge);
            }
            var NewTrailingTrivia = new List<SyntaxTrivia>();
            // Both nodes have trivia
            bool FoundEOL = false;
            bool FoundWhiteSpace = false;
            AdjustTrailingTrivia(NodeTrailingTrivia, NewTrailingTrivia, ref FoundEOL, ref FoundWhiteSpace);
            AdjustTrailingTrivia(TriviaListToMerge, NewTrailingTrivia, ref FoundEOL, ref FoundWhiteSpace);
            return node.WithTrailingTrivia(NewTrailingTrivia);
        }

        internal static T WithModifiedNodeTrailingTrivia<T>(this T Node, bool SeparatorFollows) where T : VB.VisualBasicSyntaxNode
        {
            bool AfterLineContinuation = false;
            bool AfterWhiteSpace = false;
            var FinalLeadingTriviaList = new List<SyntaxTrivia>();
            var InitialTriviaList = Node.GetTrailingTrivia().ToList();
            int InitialTriviaListUBound = InitialTriviaList.Count - 1;
            bool AfterComment = false;
            bool AfterLinefeed = false;
            var FinalTrailingTriviaList = new List<SyntaxTrivia>();
            for (int i = 0, loopTo = InitialTriviaListUBound; i <= loopTo; i++)
            {
                var Trivia = InitialTriviaList[i];
                var NextTrivia = i < InitialTriviaListUBound ? InitialTriviaList[i + 1] : VBFactory.ElasticMarker;
                var switchExpr = Trivia.RawKind;
                switch (switchExpr)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                        {
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                continue;
                            }

                            if (NextTrivia.IsKind(VB.SyntaxKind.CommentTrivia) || NextTrivia.IsKind(VB.SyntaxKind.LineContinuationTrivia))
                            {
                                FinalTrailingTriviaList.Add(Trivia);
                                AfterLinefeed = false;
                                AfterComment = false;
                                AfterWhiteSpace = true;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                continue;
                            }
                            if (!AfterLinefeed)
                            {
                                if (AfterComment || AfterLineContinuation)
                                {
                                    FinalTrailingTriviaList.Add(Trivia);
                                }
                                else
                                {
                                    if (SeparatorFollows)
                                    {
                                        FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                        FinalTrailingTriviaList.Add(Trivia);
                                    }
                                }
                                AfterComment = false;
                                AfterLinefeed = true;
                                AfterWhiteSpace = false;
                                AfterLineContinuation = false;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            if (!AfterWhiteSpace)
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                            }
                            if (!AfterLineContinuation)
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                            }
                            FinalTrailingTriviaList.Add(Trivia);
                            if (!NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                AfterLineContinuation = false;
                                AfterLinefeed = true;
                            }
                            AfterComment = true;
                            AfterWhiteSpace = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.LineContinuationTrivia:
                        {
                            if (FinalTrailingTriviaList.Last().IsKind(VB.SyntaxKind.LineContinuationTrivia))
                            {
                                continue;
                            }
                            AfterWhiteSpace = false;
                            AfterLineContinuation = true;
                            FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                            break;
                        }

                    default:
                        {

                            break;
                        }
                }
            }
            return Node.With(FinalLeadingTriviaList, FinalTrailingTriviaList);
        }

        /// <summary>
        /// This function is used where a Token is Followed by a Node followed by a Token
        /// in the middle of a statement where VB does not allow Directives
        /// </summary>
        /// <param name="Node"></param>
        /// <returns>New Node with valid Trivia</returns>
        internal static T WithModifiedNodeTrivia<T>(this T Node, bool SeparatorFollows) where T : VB.VisualBasicSyntaxNode
        {
            bool AfterFirstTrivia = false;
            bool AfterLineContinuation = false;
            bool AfterWhiteSpace = false;
            var FinalLeadingTriviaList = new List<SyntaxTrivia>();
            var InitialTriviaList = Node.GetLeadingTrivia().ToList();
            int InitialTriviaListUBound = InitialTriviaList.Count - 1;
            for (int i = 0, loopTo = InitialTriviaListUBound; i <= loopTo; i++)
            {
                var Trivia = InitialTriviaList[i];
                var NextTrivia = i < InitialTriviaList.Count - 1 ? InitialTriviaList[i + 1] : default(SyntaxTrivia);
                var switchExpr = Trivia.RawKind;
                switch (switchExpr)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                        {
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) || AfterLineContinuation)
                            {
                                continue;
                            }
                            else if (NextTrivia.IsKind(VB.SyntaxKind.WhitespaceTrivia))
                            {
                                if (Trivia.FullSpan.Length < NextTrivia.FullSpan.Length)
                                {
                                    AfterFirstTrivia = false;
                                    continue;
                                }
                                else
                                {
                                    Trivia = NextTrivia;
                                    i += 1;
                                }
                            }
                            AfterFirstTrivia = true;
                            AfterWhiteSpace = true;
                            FinalLeadingTriviaList.Add(Trivia);
                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (!AfterFirstTrivia)
                            {
                                AfterFirstTrivia = true;
                                continue;
                            }
                            FinalLeadingTriviaList.Add(Trivia);
                            AfterWhiteSpace = false;
                            if (FinalLeadingTriviaList.Count == 1 || NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                AfterLineContinuation = true;
                            }
                            else
                            {
                                AfterLineContinuation = false;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            AfterFirstTrivia = true;
                            if (!AfterLineContinuation)
                            {
                                if (!AfterWhiteSpace)
                                {
                                    FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                }
                                FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                AfterLineContinuation = true;
                            }
                            FinalLeadingTriviaList.Add(Trivia);
                            if (!NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.DisableWarningDirectiveTrivia:
                    case (int)VB.SyntaxKind.EnableWarningDirectiveTrivia:
                    case (int)VB.SyntaxKind.IfDirectiveTrivia:
                    case (int)VB.SyntaxKind.DisabledTextTrivia:
                    case (int)VB.SyntaxKind.ElseDirectiveTrivia:
                    case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                        {
                            FinalLeadingTriviaList.AddRange(Util.SyntaxTriviaExtensions.DirectiveNotAllowedHere(Trivia));
                            AfterFirstTrivia = true;
                            AfterLineContinuation = false;
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) || NextTrivia.IsNone())
                            {
                                continue;
                            }
                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                            break;
                        }

                    case (int)VB.SyntaxKind.LineContinuationTrivia:
                        {
                            if (!AfterLineContinuation)
                            {
                                FinalLeadingTriviaList.Add(Trivia);
                            }
                            AfterLineContinuation = true;
                            break;
                        }

                    case (int)VB.SyntaxKind.RegionDirectiveTrivia:
                    case (int)VB.SyntaxKind.EndRegionDirectiveTrivia:
                        {
                            AfterFirstTrivia = true;
                            AfterLineContinuation = false;
                            FinalLeadingTriviaList.Add(Trivia);
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) || NextTrivia.IsNone())
                            {
                                continue;
                            }
                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                            break;
                        }

                    default:
                        {

                            break;
                        }
                }
            }
            InitialTriviaList.Clear();
            InitialTriviaList.AddRange(Node.GetTrailingTrivia());
            InitialTriviaListUBound = InitialTriviaList.Count - 1;
            AfterWhiteSpace = false;
            bool AfterComment = false;
            AfterLineContinuation = false;
            bool AfterLinefeed = false;
            var FinalTrailingTriviaList = new List<SyntaxTrivia>();
            for (int i = 0, loopTo1 = InitialTriviaListUBound; i <= loopTo1; i++)
            {
                var Trivia = InitialTriviaList[i];
                var NextTrivia = i < InitialTriviaListUBound ? InitialTriviaList[i + 1] : VBFactory.ElasticMarker;
                var switchExpr1 = Trivia.RawKind;
                switch (switchExpr1)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                        {
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                continue;
                            }

                            if (NextTrivia.IsKind(VB.SyntaxKind.CommentTrivia) || NextTrivia.IsKind(VB.SyntaxKind.LineContinuationTrivia))
                            {
                                FinalTrailingTriviaList.Add(Trivia);
                                AfterLinefeed = false;
                                AfterComment = false;
                                AfterWhiteSpace = true;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                continue;
                            }
                            if (!AfterLinefeed)
                            {
                                if (AfterComment || AfterLineContinuation)
                                {
                                    FinalTrailingTriviaList.Add(Trivia);
                                }
                                else
                                {
                                    if (SeparatorFollows)
                                    {
                                        FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                        FinalTrailingTriviaList.Add(Trivia);
                                    }
                                }
                                AfterComment = false;
                                AfterLinefeed = true;
                                AfterWhiteSpace = false;
                                AfterLineContinuation = false;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            if (!AfterWhiteSpace)
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                            }
                            if (!AfterLineContinuation)
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                            }
                            FinalTrailingTriviaList.Add(Trivia);
                            if (!NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                            {
                                FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                AfterLineContinuation = false;
                                AfterLinefeed = true;
                            }
                            AfterComment = true;
                            AfterWhiteSpace = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.LineContinuationTrivia:
                        {
                            if (FinalTrailingTriviaList.Last().IsKind(VB.SyntaxKind.LineContinuationTrivia))
                            {
                                continue;
                            }
                            AfterWhiteSpace = false;
                            AfterLineContinuation = true;
                            FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                            break;
                        }

                    default:
                        {

                            break;
                        }
                }
            }
            return Node.With(FinalLeadingTriviaList, FinalTrailingTriviaList);
        }

        internal static T WithPrependedLeadingTrivia<T>(this T node, params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0)
            {
                return node;
            }
            var TriviaList = trivia.ToList();
            if (TriviaList.Last().IsKind(VB.SyntaxKind.CommentTrivia))
            {
                TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
            }
            return node.WithPrependedLeadingTrivia(TriviaList);
        }

        internal static T WithPrependedLeadingTrivia<T>(this T node, SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0)
            {
                return node;
            }
            if (trivia.Last().IsKind(VB.SyntaxKind.CommentTrivia))
            {
                trivia = trivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
            }
            return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
        }

        internal static T WithPrependedLeadingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            if (trivia == null)
            {
                return node;
            }
            return node.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }

        internal static T WithRestructuredingEOLTrivia<T>(this T node) where T : SyntaxNode
        {
            if (!node.HasTrailingTrivia)
            {
                return node;
            }

            var NodeTrailingTrivia = node.GetTrailingTrivia();
            if (NodeTrailingTrivia.ContainsEOLTrivia())
            {
                var NewTriviaList = new List<SyntaxTrivia>();
                foreach (SyntaxTrivia Trivia in NodeTrailingTrivia)
                {
                    if (Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                    {
                        continue;
                    }
                    NewTriviaList.Add(Trivia);
                }
                return node.WithTrailingTrivia(NewTriviaList);
            }
            else
            {
                return node;
            }
        }

        /// <summary>
        /// Make sure the node (usually a statement) ends with an EOL and possibly whitespace
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static T WithTrailingEOL<T>(this T node) where T : SyntaxNode
        {
            var TrailingTrivia = node.GetTrailingTrivia().ToList();
            int Count = TrailingTrivia.Count;
            if (Count == 0)
            {
                return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
            }

            var switchExpr = Count;
            switch (switchExpr)
            {
                case 1:
                    {
                        var switchExpr1 = TrailingTrivia.Last().RawKind;
                        switch (switchExpr1)
                        {
                            case (int)VB.SyntaxKind.WhitespaceTrivia:
                            case (int)VB.SyntaxKind.EndOfLineTrivia:
                                {
                                    return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                }

                            default:
                                {
                                    TrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    return node.WithTrailingTrivia(TrailingTrivia);
                                }
                        }

                        break;
                    }

                case 2:
                    {
                        var switchExpr2 = TrailingTrivia.First().RawKind;
                        switch (switchExpr2)
                        {
                            case (int)VB.SyntaxKind.WhitespaceTrivia:
                                {
                                    var switchExpr3 = TrailingTrivia.Last().RawKind;
                                    switch (switchExpr3)
                                    {
                                        case (int)VB.SyntaxKind.WhitespaceTrivia:
                                        case (int)VB.SyntaxKind.EndOfLineTrivia:
                                            {
                                                // Replace Whitespace, Whitespace and Whitespace, EOL with just EOL
                                                TrailingTrivia = new List<SyntaxTrivia>();
                                                break;
                                            }

                                        case (int)VB.SyntaxKind.CommentTrivia:
                                            {
                                                break;
                                            }

                                        default:
                                            {

                                                break;
                                            }
                                    }

                                    break;
                                }

                            case (int)VB.SyntaxKind.EndOfLineTrivia:
                                {
                                    if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.WhitespaceTrivia))
                                    {
                                        return node;
                                    }
                                    else if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        return node.WithTrailingTrivia(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }

                                    break;
                                }

                            case (int)VB.SyntaxKind.CommentTrivia:
                                {
                                    if (TrailingTrivia.Last().IsKind(VB.SyntaxKind.WhitespaceTrivia))
                                    {
                                        TrailingTrivia.RemoveAt(1);
                                        TrailingTrivia.Insert(0, global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        // EOL added below

                                    }

                                    break;
                                }

                            default:
                                {

                                    break;
                                }
                        }
                        TrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                        return node.WithTrailingTrivia(TrailingTrivia);
                    }

                default:
                    {
                        Count -= 1; // Last index
                        var switchExpr4 = TrailingTrivia.Last().RawKind;
                        switch (switchExpr4)
                        {
                            case (int)VB.SyntaxKind.EndOfLineTrivia:
                                {
                                    if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        TrailingTrivia.RemoveAt(Count);
                                        return node.WithTrailingTrivia(TrailingTrivia).WithTrailingEOL();
                                    }
                                    return node;
                                }

                            case (int)VB.SyntaxKind.WhitespaceTrivia:
                                {
                                    if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.WhitespaceTrivia))
                                    {
                                        TrailingTrivia.RemoveAt(Count - 1);
                                        return node.WithTrailingTrivia(TrailingTrivia).WithTrailingEOL();
                                    }
                                    else if (TrailingTrivia[Count - 1].IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        return node;
                                    }
                                    else if (TrailingTrivia[Count - 1].IsCommentOrDirectiveTrivia())
                                    {
                                        TrailingTrivia.Insert(Count, global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                        return node.WithTrailingTrivia(TrailingTrivia);
                                    }
                                    return node.WithTrailingTrivia(TrailingTrivia);
                                }

                            default:
                                {

                                    break;
                                }
                        }

                        break;
                    }
            }
            return node;
        }

        public static SyntaxTrivia ConvertTrivia(this SyntaxTrivia t)
        {
            var switchExpr = t.RawKind;

            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            switch (switchExpr)
            {
                case (int)CS.SyntaxKind.WhitespaceTrivia:
                    {
                        return VBFactory.WhitespaceTrivia(t.ToString());
                    }

                case (int)CS.SyntaxKind.EndOfLineTrivia:
                    {
                        return global::VisualBasicSyntaxFactory.VBEOLTrivia;
                    }

                case (int)CS.SyntaxKind.SingleLineCommentTrivia:
                    {
                        if (t.ToFullString().EndsWith("*/"))
                        {
                            return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2, t.ToFullString().Length - 4))}");
                        }
                        return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2))}");
                    }

                case (int)CS.SyntaxKind.MultiLineCommentTrivia:
                    {
                        if (t.ToFullString().EndsWith("*/"))
                        {
                            return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2, t.ToFullString().Length - 4)).Replace(Constants.vbLf, "")}");
                        }
                        return VBFactory.CommentTrivia($"'{ReplaceLeadingSlashes(t.ToFullString().Substring(2)).Replace(Constants.vbLf, "")}");
                    }

                case (int)CS.SyntaxKind.DocumentationCommentExteriorTrivia:
                    {
                        return VBFactory.SyntaxTrivia(VB.SyntaxKind.CommentTrivia, "'''");
                    }

                case (int)CS.SyntaxKind.DisabledTextTrivia:
                    {
                        if (global::RestuructureSeparatedLists.IgnoredIfDepth > 0)
                        {
                            return VBFactory.DisabledTextTrivia(t.ToString().WithoutNewLines(' '));
                        }
                        return VBFactory.DisabledTextTrivia(t.ToString().Replace(Constants.vbLf, Constants.vbCrLf));
                    }

                case (int)CS.SyntaxKind.PreprocessingMessageTrivia:
                    {
                        return VBFactory.CommentTrivia($" ' {t}");
                    }

                case (int)CS.SyntaxKind.None:
                    {
                        return default(SyntaxTrivia);
                    }
            }
            if (!t.HasStructure)
            {

            }

            /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            CSS.StructuredTriviaSyntax StructuredTrivia = (CSS.StructuredTriviaSyntax)t.GetStructure();
            Debug.Assert(StructuredTrivia != null, $"Found new type of non structured trivia {t.RawKind}");
            var switchExpr1 = t.RawKind;
            switch (switchExpr1)
            {
                case (int)CS.SyntaxKind.DefineDirectiveTrivia:
                    {
                        CSS.DefineDirectiveTriviaSyntax DefineDirective = (CSS.DefineDirectiveTriviaSyntax)StructuredTrivia;
                        var Name = VBFactory.Identifier(DefineDirective.Name.ValueText);
                        VBS.ExpressionSyntax value = VBFactory.TrueLiteralExpression(global::VisualBasicSyntaxFactory.TrueKeyword);
                        return VBFactory.Trivia(VBFactory.ConstDirectiveTrivia(Name, value).WithConvertedTriviaFrom(DefineDirective).WithAppendedTriviaFromEndOfDirectiveToken(DefineDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.UndefDirectiveTrivia:
                    {
                        CSS.UndefDirectiveTriviaSyntax UndefineDirective = (CSS.UndefDirectiveTriviaSyntax)StructuredTrivia;
                        var Name = VBFactory.Identifier(UndefineDirective.Name.ValueText);
                        VBS.ExpressionSyntax value = global::VisualBasicSyntaxFactory.NothingExpression;
                        return VBFactory.Trivia(VBFactory.ConstDirectiveTrivia(Name, value).WithConvertedTriviaFrom(UndefineDirective).WithAppendedTriviaFromEndOfDirectiveToken(UndefineDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.EndIfDirectiveTrivia:
                    {
                        if (global::RestuructureSeparatedLists.IgnoredIfDepth > 0)
                        {
                            global::RestuructureSeparatedLists.IgnoredIfDepth -= 1;
                            return VBFactory.CommentTrivia($"' TODO VB does not allow directives here, original statement {t.ToFullString().WithoutNewLines(' ')}");
                        }
                        CSS.EndIfDirectiveTriviaSyntax EndIfDirective = (CSS.EndIfDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.Trivia(VBFactory.EndIfDirectiveTrivia().WithConvertedTrailingTriviaFrom(EndIfDirective.EndIfKeyword).WithAppendedTriviaFromEndOfDirectiveToken(EndIfDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.ErrorDirectiveTrivia:
                    {
                        CSS.ErrorDirectiveTriviaSyntax ErrorDirective = (CSS.ErrorDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.CommentTrivia($"' TODO: Check VB does not support Error Directive Trivia, Original Directive {ErrorDirective.ToFullString()}");
                    }

                case (int)CS.SyntaxKind.IfDirectiveTrivia:
                    {
                        if (t.Token.Parent?.AncestorsAndSelf().OfType<CSS.InitializerExpressionSyntax>().Any() == true)
                        {
                            global::RestuructureSeparatedLists.IgnoredIfDepth += 1;
                        }
                        CSS.IfDirectiveTriviaSyntax IfDirective = (CSS.IfDirectiveTriviaSyntax)StructuredTrivia;
                        string Expression1 = IfDirective.Condition.ToString().Replace("!", "Not ").Replace("==", "=").Replace("!=", "<>").Replace("&&", "And").Replace("||", "Or").Replace("  ", " ").Replace("false", "False").Replace("true", "True");

                        return VBFactory.Trivia(VBFactory.IfDirectiveTrivia(global::VisualBasicSyntaxFactory.IfKeyword, VBFactory.ParseExpression(Expression1)).With(IfDirective.GetLeadingTrivia().ConvertTrivia(), IfDirective.Condition.GetTrailingTrivia().ConvertTrivia()).WithAppendedTriviaFromEndOfDirectiveToken(IfDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.ElifDirectiveTrivia:
                    {
                        if (t.Token.Parent.AncestorsAndSelf().OfType<CSS.InitializerExpressionSyntax>().Any())
                        {
                            global::RestuructureSeparatedLists.IgnoredIfDepth += 1;
                        }
                        CSS.ElifDirectiveTriviaSyntax ELIfDirective = (CSS.ElifDirectiveTriviaSyntax)StructuredTrivia;
                        string Expression1 = ELIfDirective.Condition.ToString().Replace("!", "Not ").Replace("==", "=").Replace("!=", "<>").Replace("&&", "And").Replace("||", "Or").Replace("  ", " ").Replace("false", "False").Replace("true", "True");

                        SyntaxToken IfOrElseIfKeyword;
                        if (t.IsKind(CS.SyntaxKind.ElifDirectiveTrivia))
                        {
                            IfOrElseIfKeyword = global::VisualBasicSyntaxFactory.ElseIfKeyword;
                        }
                        else
                        {
                            IfOrElseIfKeyword = global::VisualBasicSyntaxFactory.IfKeyword;
                        }
                        return VBFactory.Trivia(VBFactory.ElseIfDirectiveTrivia(IfOrElseIfKeyword, VBFactory.ParseExpression(Expression1)
    ).With(ELIfDirective.GetLeadingTrivia().ConvertTrivia(), ELIfDirective.Condition.GetTrailingTrivia().ConvertTrivia()).WithAppendedTriviaFromEndOfDirectiveToken(ELIfDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.LineDirectiveTrivia:
                    {
                        return VBFactory.CommentTrivia($"' TODO: Check VB does not support Line Directive Trivia, Original Directive {t}");
                    }

                case (int)CS.SyntaxKind.ElseDirectiveTrivia:
                    {
                        return VBFactory.Trivia(VBFactory.ElseDirectiveTrivia().NormalizeWhitespace().WithConvertedTrailingTriviaFrom(((CSS.ElseDirectiveTriviaSyntax)StructuredTrivia).ElseKeyword).WithTrailingEOL());
                    }

                case (int)CS.SyntaxKind.EndRegionDirectiveTrivia:
                    {
                        CSS.EndRegionDirectiveTriviaSyntax EndRegionDirective = (CSS.EndRegionDirectiveTriviaSyntax)StructuredTrivia;
                        return VBFactory.Trivia(VBFactory.EndRegionDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.EndKeyword, global::VisualBasicSyntaxFactory.RegionKeyword).WithAppendedTriviaFromEndOfDirectiveToken(EndRegionDirective.EndOfDirectiveToken));
                    }

                case (int)CS.SyntaxKind.PragmaWarningDirectiveTrivia:
                    {
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

                case (int)CS.SyntaxKind.RegionDirectiveTrivia:
                    {
                        CSS.RegionDirectiveTriviaSyntax RegionDirective = (CSS.RegionDirectiveTriviaSyntax)StructuredTrivia;
                        var EndOfDirectiveToken = RegionDirective.EndOfDirectiveToken;
                        string NameString = $"\"{EndOfDirectiveToken.LeadingTrivia.ToString().Replace("\"", "")}\"";
                        var RegionDirectiveTriviaNode = VBFactory.RegionDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.RegionKeyword, VBFactory.StringLiteralToken(NameString, NameString)
    ).WithConvertedTrailingTriviaFrom(EndOfDirectiveToken);
                        return VBFactory.Trivia(RegionDirectiveTriviaNode.WithTrailingEOL());
                    }

                case (int)CS.SyntaxKind.SingleLineDocumentationCommentTrivia:
                    {
                        CSS.DocumentationCommentTriviaSyntax SingleLineDocumentationComment = (CSS.DocumentationCommentTriviaSyntax)StructuredTrivia;
                        var walker = new XMLVisitor();
                        walker.Visit(SingleLineDocumentationComment);

                        var xmlNodes = new List<VBS.XmlNodeSyntax>();
                        for (int i = 0, loopTo = SingleLineDocumentationComment.Content.Count - 1; i <= loopTo; i++)
                        {
                            var node = SingleLineDocumentationComment.Content[i];
                            if (!node.IsKind(CS.SyntaxKind.XmlText) && node.GetLeadingTrivia().Count > 0 && node.GetLeadingTrivia().First().IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia))
                            {
                                if (i < SingleLineDocumentationComment.Content.Count - 1)
                                {
                                    var NextNode = SingleLineDocumentationComment.Content[i + 1];
                                    if (!NextNode.IsKind(CS.SyntaxKind.XmlText) || NextNode.GetLeadingTrivia().Count == 0 || !NextNode.GetLeadingTrivia().First().IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia))
                                    {
                                        xmlNodes.Add(VBFactory.XmlText(" ").WithLeadingTrivia(VBFactory.DocumentationCommentExteriorTrivia("'''")));
                                    }
                                }
                                node = node.WithoutLeadingTrivia();
                            }
                            VBS.XmlNodeSyntax Item = (VBS.XmlNodeSyntax)node.Accept(walker);
                            xmlNodes.Add(Item);
                        }
                        var DocumentationCommentTrivia = VBFactory.DocumentationCommentTrivia(VBFactory.List(xmlNodes.ToArray()));
                        if (!DocumentationCommentTrivia.HasLeadingTrivia || !DocumentationCommentTrivia.GetLeadingTrivia()[0].IsKind(VB.SyntaxKind.DocumentationCommentExteriorTrivia))
                        {
                            DocumentationCommentTrivia = DocumentationCommentTrivia.WithLeadingTrivia(VBFactory.DocumentationCommentExteriorTrivia("''' "));
                        }
                        var _DocumentationComment = VBFactory.Trivia(DocumentationCommentTrivia.WithTrailingTrivia(VBFactory.EndOfLine("")));
                        return _DocumentationComment;
                    }

                case (int)CS.SyntaxKind.PragmaChecksumDirectiveTrivia:
                    {
                        CSS.PragmaChecksumDirectiveTriviaSyntax PragmaChecksumDirective = (CSS.PragmaChecksumDirectiveTriviaSyntax)StructuredTrivia;
                        var Guid1 = VBFactory.ParseToken(PragmaChecksumDirective.Guid.Text.ToUpperInvariant());
                        var Bytes = VBFactory.ParseToken(PragmaChecksumDirective.Bytes.Text);
                        var ExternalSource = VBFactory.ParseToken(PragmaChecksumDirective.File.Text);
                        return VBFactory.Trivia(VBFactory.ExternalChecksumDirectiveTrivia(global::VisualBasicSyntaxFactory.HashToken, global::VisualBasicSyntaxFactory.ExternalChecksumKeyword, global::VisualBasicSyntaxFactory.OpenParenToken, ExternalSource, global::VisualBasicSyntaxFactory.CommaToken, Guid1, global::VisualBasicSyntaxFactory.CommaToken, Bytes, global::VisualBasicSyntaxFactory.CloseParenToken).WithAppendedTriviaFromEndOfDirectiveToken(PragmaChecksumDirective.EndOfDirectiveToken)
    );
                    }

                case (int)CS.SyntaxKind.SkippedTokensTrivia:
                    {
                        var Builder = new StringBuilder();
                        foreach (SyntaxToken tok in ((CSS.SkippedTokensTriviaSyntax)StructuredTrivia).Tokens)
                            Builder.Append(tok.ToString());
                        return VBFactory.CommentTrivia($"' TODO: Error SkippedTokensTrivia '{Builder}'");
                    }

                case (int)CS.SyntaxKind.BadDirectiveTrivia:
                    {
                        return VBFactory.CommentTrivia($"' TODO: Skipped BadDirectiveTrivia");
                    }

                case (int)CS.SyntaxKind.ConflictMarkerTrivia:
                    {
                        break;
                    }

                case (int)CS.SyntaxKind.LoadDirectiveTrivia:
                    {
                        break;
                    }

                default:
                    {
                        Debug.WriteLine(((VB.SyntaxKind)Conversions.ToUShort(t.RawKind)).ToString());

                        break;
                    }
            }
            throw new NotImplementedException($"t.Kind({(VB.SyntaxKind)Conversions.ToUShort(t.RawKind)}) Is unknown");
        }

        public static IEnumerable<SyntaxTrivia> ConvertTrivia(this IReadOnlyCollection<SyntaxTrivia> TriviaToConvert)
        {
            var TriviaList = new List<SyntaxTrivia>();
            if (TriviaToConvert == null)
            {
                return TriviaList;
            }
            try
            {
                int TriviaCount = TriviaToConvert.Count - 1;
                for (int i = 0, loopTo = TriviaCount; i <= loopTo; i++)
                {
                    var Trivia = TriviaToConvert.ElementAtOrDefault(i);
                    var NextTrivia = i < TriviaCount ? TriviaToConvert.ElementAtOrDefault(i + 1) : default(SyntaxTrivia);
                    var switchExpr = Trivia.RawKind;
                    switch (switchExpr)
                    {
                        case (int)CS.SyntaxKind.MultiLineCommentTrivia:
                            {
                                var Lines = Trivia.ToFullString().Substring(2).Split(Conversions.ToChar(Constants.vbLf));
                                foreach (string line in Lines)
                                {
                                    if (line.EndsWith("*/"))
                                    {
                                        TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line.Substring(0, line.Length - 2))}"));
                                        if (Trivia.ToFullString().EndsWith(Constants.vbLf))
                                        {
                                            TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                        }
                                    }
                                    else
                                    {
                                        TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line)}"));
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }
                                    if (Lines.Length == 1 && (i == TriviaCount || !TriviaToConvert.ElementAtOrDefault(i + 1).IsEndOfLine()))
                                    {
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }
                                }

                                break;
                            }

                        case (int)CS.SyntaxKind.MultiLineDocumentationCommentTrivia:
                            {
                                CSS.StructuredTriviaSyntax sld = (CSS.StructuredTriviaSyntax)Trivia.GetStructure();
                                foreach (SyntaxNode t1 in sld.ChildNodes())
                                {
                                    var Lines = t1.ToFullString().Split(Conversions.ToChar(Constants.vbLf));
                                    foreach (string line in Lines)
                                    {
                                        if (line.StartsWith("/*"))
                                        {
                                            TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line.Substring(1, line.Length - 1))}"));
                                            TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                        }
                                        else
                                        {
                                            TriviaList.Add(VBFactory.CommentTrivia($"' {RemoveLeadingSpacesStar(line)}"));
                                            TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                        }
                                    }
                                }

                                break;
                            }

                        default:
                            {
                                var ConvertedTrivia = Trivia.ConvertTrivia();
                                if (ConvertedTrivia == null)
                                {
                                    continue;
                                }
                                TriviaList.Add(ConvertedTrivia);
                                if (Trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia))
                                {
                                    if (!NextTrivia.IsKind(CS.SyntaxKind.EndOfLineTrivia))
                                    {
                                        TriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                    }
                                }

                                break;
                            }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception ex)
            {

                throw;
            }
            return TriviaList;
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node) where TNode : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static (SyntaxToken, SyntaxToken) GetBraces(this SyntaxNode node)
        {
            CSS.NamespaceDeclarationSyntax namespaceNode = node as CSS.NamespaceDeclarationSyntax;
            if (namespaceNode != null)
            {
                return ValueTuple.Create(namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
            }

            CSS.BaseTypeDeclarationSyntax baseTypeNode = node as CSS.BaseTypeDeclarationSyntax;
            if (baseTypeNode != null)
            {
                return ValueTuple.Create(baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
            }

            CSS.AccessorListSyntax accessorListNode = node as CSS.AccessorListSyntax;
            if (accessorListNode != null)
            {
                return ValueTuple.Create(accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
            }

            CSS.BlockSyntax blockNode = node as CSS.BlockSyntax;
            if (blockNode != null)
            {
                return ValueTuple.Create(blockNode.OpenBraceToken, blockNode.CloseBraceToken);
            }

            CSS.SwitchStatementSyntax switchStatementNode = node as CSS.SwitchStatementSyntax;
            if (switchStatementNode != null)
            {
                return ValueTuple.Create(switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
            }

            CSS.AnonymousObjectCreationExpressionSyntax anonymousObjectCreationExpression = node as CSS.AnonymousObjectCreationExpressionSyntax;
            if (anonymousObjectCreationExpression != null)
            {
                return ValueTuple.Create(anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
            }

            CSS.InitializerExpressionSyntax initializeExpressionNode = node as CSS.InitializerExpressionSyntax;
            if (initializeExpressionNode != null)
            {
                return ValueTuple.Create(initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
            }

            return (default, default);
        }

        public static bool IsKind(this SyntaxNode node, params VB.SyntaxKind[] kind1)
        {
            if (node == null)
            {
                return false;
            }

            foreach (VB.SyntaxKind k in kind1)
            {
                if ((VB.SyntaxKind)Conversions.ToUShort(node.RawKind) == k)
                {
                    return true;
                }
            }
            return false;
        }

        public static T WithAppendedTrailingTrivia<T>(this T node, SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0)
            {
                return node;
            }
            if (node == null)
            {
                return null;
            }

            return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }
            if (trivia == null)
            {
                return node;
            }
            return node.WithAppendedTrailingTrivia(trivia.ToSyntaxTriviaList());
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        internal static T WithConvertedTriviaFrom<T>(this T node, SyntaxNode otherNode) where T : SyntaxNode
        {
            if (otherNode == null)
            {
                return node;
            }
            if (otherNode.HasLeadingTrivia)
            {
                node = node.WithLeadingTrivia(otherNode.GetLeadingTrivia().ConvertTrivia());
            }
            if (!otherNode.HasTrailingTrivia || otherNode.ParentHasSameTrailingTrivia())
            {
                return node;
            }
            return node.WithTrailingTrivia(otherNode.GetTrailingTrivia().ConvertTrivia());
        }

        internal static SyntaxToken WithConvertedTriviaFrom(this SyntaxToken Token, SyntaxNode otherNode)
        {
            if (otherNode.HasLeadingTrivia)
            {
                Token = Token.WithLeadingTrivia(otherNode.GetLeadingTrivia().ConvertTrivia());
            }
            if (!otherNode.HasTrailingTrivia || otherNode.ParentHasSameTrailingTrivia())
            {
                return Token;
            }
            return Token.WithTrailingTrivia(otherNode.GetTrailingTrivia().ConvertTrivia());
        }

        internal static SyntaxToken WithConvertedTriviaFrom(this SyntaxToken Token, SyntaxToken otherToken)
        {
            try
            {
                if (otherToken.HasLeadingTrivia)
                {
                    Token = Token.WithLeadingTrivia(otherToken.LeadingTrivia.ConvertTrivia().ToList());
                }
                return Token.WithTrailingTrivia(otherToken.TrailingTrivia.ConvertTrivia());
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception ex)
            {

            }

            return default(SyntaxToken);
        }

        public static T WithConvertedTriviaFrom<T>(this T node, SyntaxToken otherToken) where T : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentException($"Parameter {nameof(node)} Is Nothing");
            }
            if (otherToken.HasLeadingTrivia)
            {
                node = node.WithLeadingTrivia(otherToken.LeadingTrivia.ConvertTrivia());
            }
            if (!otherToken.HasTrailingTrivia)
            {
                return node;
            }
            return node.WithTrailingTrivia(otherToken.TrailingTrivia.ConvertTrivia());
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        internal static T WithConvertedLeadingTriviaFrom<T>(this T node, SyntaxNode otherNode) where T : SyntaxNode
        {
            if (otherNode == null || !otherNode.HasLeadingTrivia)
            {
                return node;
            }
            return node.WithLeadingTrivia(otherNode.GetLeadingTrivia().ConvertTrivia());
        }

        internal static T WithConvertedLeadingTriviaFrom<T>(this T node, SyntaxToken otherToken) where T : SyntaxNode
        {
            if (!otherToken.HasLeadingTrivia)
            {
                return node;
            }
            return node.WithLeadingTrivia(otherToken.LeadingTrivia.ConvertTrivia());
        }

        public static SyntaxToken WithConvertedLeadingTriviaFrom(this SyntaxToken node, SyntaxToken otherToken)
        {
            if (!otherToken.HasLeadingTrivia)
            {
                return node;
            }
            return node.WithLeadingTrivia(otherToken.LeadingTrivia.ConvertTrivia());
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        internal static T WithConvertedTrailingTriviaFrom<T>(this T node, SyntaxNode otherNode) where T : SyntaxNode
        {
            if (otherNode == null || !otherNode.HasTrailingTrivia)
            {
                return node;
            }
            if (otherNode.ParentHasSameTrailingTrivia())
            {
                return node;
            }
            return node.WithTrailingTrivia(otherNode.GetTrailingTrivia().ConvertTrivia());
        }

        internal static T WithConvertedTrailingTriviaFrom<T>(this T node, SyntaxToken otherToken) where T : SyntaxNode
        {
            if (!otherToken.HasTrailingTrivia)
            {
                return node;
            }
            return node.WithTrailingTrivia(otherToken.TrailingTrivia.ConvertTrivia());
        }

        public static SyntaxToken WithConvertedTrailingTriviaFrom(this SyntaxToken Token, SyntaxToken otherToken)
        {
            return Token.WithTrailingTrivia(otherToken.TrailingTrivia.ConvertTrivia());
        }
    }
}

