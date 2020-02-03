// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CSharpToVBCodeConverter.Util
{
    public static class SyntaxTriviaExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns>True if any Trivia is a Comment or a Directive</returns>
        internal static bool ContainsCommentOrDirectiveTrivia(this VB.VisualBasicSyntaxNode node)
        {
            var CurrentToken = node.GetFirstToken();
            while (CurrentToken != null)
            {
                if (CurrentToken.LeadingTrivia.ContainsCommentOrDirectiveTrivia() || CurrentToken.TrailingTrivia.ContainsCommentOrDirectiveTrivia())
                {
                    return true;
                }
                CurrentToken = CurrentToken.GetNextToken();
            }

            return false;
        }

        internal static bool ContainsEOLTrivia(this VB.VisualBasicSyntaxNode node)
        {
            if (!node.HasTrailingTrivia)
            {
                return false;
            }
            var TriviaList = node.GetTrailingTrivia();
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsEndOfLine())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remove directive trivia
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static T RemoveDirectiveTrivia<T>(this T node, ref bool FoundEOL) where T : VBS.ArgumentSyntax
        {
            var NewLeadingTrivia = new List<SyntaxTrivia>();
            var NewTrailingTrivia = new List<SyntaxTrivia>();
            foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
            {
                var switchExpr = trivia.RawKind;
                switch (switchExpr)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            NewLeadingTrivia.Add(trivia);
                            FoundEOL = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (!FoundEOL)
                            {
                                NewLeadingTrivia.Add(trivia);
                            }
                            FoundEOL = true;
                            break;
                        }

                    case (int)VB.SyntaxKind.DisabledTextTrivia:
                    case (int)VB.SyntaxKind.IfDirectiveTrivia:
                    case (int)VB.SyntaxKind.ElseDirectiveTrivia:
                    case (int)VB.SyntaxKind.ElseIfDirectiveTrivia:
                    case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                        {
                            break;
                        }

                    default:
                        {
                            Debugger.Break();
                            break;
                        }
                }
            }
            FoundEOL = false;
            foreach (SyntaxTrivia trivia in node.GetTrailingTrivia())
            {
                var switchExpr1 = trivia.RawKind;
                switch (switchExpr1)
                {
                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                    case (int)VB.SyntaxKind.CommentTrivia:
                        {
                            NewTrailingTrivia.Add(trivia);
                            FoundEOL = false;
                            break;
                        }

                    case (int)VB.SyntaxKind.EndOfLineTrivia:
                        {
                            if (!FoundEOL)
                            {
                                NewTrailingTrivia.Add(trivia);
                                FoundEOL = true;
                            }

                            break;
                        }

                    case (int)VB.SyntaxKind.DisableWarningDirectiveTrivia:
                    case (int)VB.SyntaxKind.IfDirectiveTrivia:
                    case (int)VB.SyntaxKind.ElseDirectiveTrivia:
                    case (int)VB.SyntaxKind.ElseIfDirectiveTrivia:
                    case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                        {
                            break;
                        }

                    default:
                        {
                            Debugger.Break();
                            break;
                        }
                }
            }

            return node.With(NewLeadingTrivia, NewTrailingTrivia);
        }

        public static bool ContainsCommentOrDirectiveTrivia(this List<SyntaxTrivia> TriviaList)
        {
            if (TriviaList == null || TriviaList.Count == 0)
            {
                return false;
            }
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsWhitespaceOrEndOfLine())
                {
                    continue;
                }
                if (t.IsNone())
                {
                    continue;
                }
                if (t.IsCommentOrDirectiveTrivia())
                {
                    return true;
                }
                if (t.RawKind == (int)VB.SyntaxKind.DocumentationCommentTrivia)
                {
                    return true;
                }
                if (t.RawKind == (int)VB.SyntaxKind.DocumentationCommentExteriorTrivia)
                {
                    return true;
                }

                throw global::ExceptionUtilities.UnexpectedValue(t.ToString());
            }
            return false;
        }

        /// <summary>
        /// Syntax Trivia in any Language
        /// </summary>
        /// <param name="TriviaList"></param>
        /// <returns>True if any Trivia is a Comment or a Directive</returns>
        public static bool ContainsCommentOrDirectiveTrivia(this SyntaxTriviaList TriviaList)
        {
            if (TriviaList.Count == 0)
                return false;
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsWhitespaceOrEndOfLine())
                {
                    continue;
                }
                if (t.RawKind == 0)
                {
                    continue;
                }
                if (t.IsCommentOrDirectiveTrivia())
                {
                    return true;
                }
                if (t.RawKind == (int)VB.SyntaxKind.LineContinuationTrivia)
                {
                    continue;
                }
                if (t.RawKind == (int)VB.SyntaxKind.SkippedTokensTrivia)
                {
                    continue;
                }
                if (t.RawKind == (int)VB.SyntaxKind.DisabledTextTrivia)
                {
                    continue;
                }
                Debugger.Break();
            }
            return false;
        }

        public static bool ContainsCommentTrivia(this SyntaxTriviaList TriviaList)
        {
            if (TriviaList.Count == 0)
            {
                return false;
            }
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsWhitespaceOrEndOfLine())
                {
                    continue;
                }
                if (t.IsNone())
                {
                    continue;
                }
                if (t.IsDirective)
                {
                    continue;
                }
                if (t.IsComment())
                {
                    return true;
                }
                Debugger.Break();
            }
            return false;
        }

        /// <summary>
        /// Syntax Trivia in any Language
        /// </summary>
        /// <param name="TriviaList"></param>
        /// <returns>True if any Trivia is EndIf Directive</returns>
        public static bool ContainsEndIfTrivia(this SyntaxTriviaList TriviaList)
        {
            if (TriviaList.Count == 0)
                return false;
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsKind(VB.SyntaxKind.EndIfDirectiveTrivia))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsEOLTrivia(this SyntaxToken Token)
        {
            if (!Token.HasTrailingTrivia)
            {
                return false;
            }
            var TriviaList = Token.TrailingTrivia;
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsEndOfLine())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsEOLTrivia(this SyntaxTriviaList TriviaList)
        {
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsEndOfLine())
                {
                    return true;
                }
            }
            return false;
        }

        public static List<SyntaxTrivia> DirectiveNotAllowedHere(SyntaxTrivia Trivia)
        {
            var NewTriviaList = new List<SyntaxTrivia>();
            var LeadingTriviaList = new List<SyntaxTrivia>()
            {
                global::VisualBasicSyntaxFactory.SpaceTrivia,
                global::VisualBasicSyntaxFactory.LineContinuation,
                global::VisualBasicSyntaxFactory.SpaceTrivia
            };
            string TriviaAsString = "";

            if (Trivia.IsKind(VB.SyntaxKind.DisabledTextTrivia))
            {
                NewTriviaList.AddRange(LeadingTriviaList);
                NewTriviaList.Add(VBFactory.CommentTrivia($" ' TODO VB does not allow Disabled Text here, original text:"));
                NewTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                var TextStrings = Trivia.ToFullString().Split(new[] { Constants.vbCrLf }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var TriviaAsString in TextStrings)
                {
                    NewTriviaList.AddRange(LeadingTriviaList);
                    NewTriviaList.Add(VBFactory.CommentTrivia($" ' {TriviaAsString}".Replace("  ", " ", StringComparison.InvariantCulture).TrimEnd()));
                    NewTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                }
                if (NewTriviaList.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                {
                    NewTriviaList.RemoveAt(NewTriviaList.Count - 1);
                }
                return NewTriviaList;
            }

            var switchExpr = Trivia.RawKind;
            switch (switchExpr)
            {
                case (int)VB.SyntaxKind.IfDirectiveTrivia:
                    {
                        TriviaAsString = $"#If {Trivia.ToFullString().Substring("#if".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                case (int)VB.SyntaxKind.ElseDirectiveTrivia:
                    {
                        TriviaAsString = $"#Else {Trivia.ToFullString().Substring("#Else".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                case (int)VB.SyntaxKind.ElseIfDirectiveTrivia:
                    {
                        TriviaAsString = $"#ElseIf {Trivia.ToFullString().Substring("#Else If".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                    {
                        TriviaAsString = $"#EndIf {Trivia.ToFullString().Substring("#End if".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                case (int)VB.SyntaxKind.DisableWarningDirectiveTrivia:
                    {
                        TriviaAsString = $"#Disable Warning Directive {Trivia.ToFullString().Substring("#Disable Warning".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                case (int)VB.SyntaxKind.EnableWarningDirectiveTrivia:
                    {
                        TriviaAsString = $"#Enable Warning Directive {Trivia.ToFullString().Substring("#Enable Warning".Length).Trim().WithoutNewLines(' ')}";
                        break;
                    }

                default:
                    {
                        Debugger.Break();
                        break;
                    }
            }
            const string Msg = " ' TODO VB does not allow directives here, original directive: ";
            NewTriviaList = new List<SyntaxTrivia>()
            {
                global::VisualBasicSyntaxFactory.SpaceTrivia,
                global::VisualBasicSyntaxFactory.LineContinuation,
                global::VisualBasicSyntaxFactory.VBEOLTrivia,
                global::VisualBasicSyntaxFactory.SpaceTrivia,
                global::VisualBasicSyntaxFactory.LineContinuation,
                global::VisualBasicSyntaxFactory.SpaceTrivia,
                VBFactory.CommentTrivia($"{Msg}{TriviaAsString}".Replace("  ", " ", StringComparison.InvariantCulture).TrimEnd())
            };
            return NewTriviaList;
        }

        public static int FullWidth(this SyntaxTrivia trivia)
        {
            return trivia.FullSpan.Length;
        }

        public static bool IsComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineComment() || trivia.IsMultiLineComment();
        }

        public static bool IsCommentOrDirectiveTrivia(this SyntaxTrivia t)
        {
            if (t.IsSingleLineComment())
            {
                return true;
            }
            if (t.IsMultiLineComment())
            {
                return true;
            }
            if (t.IsDirective)
            {
                return true;
            }
            return false;
        }

        public static bool IsDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineDocComment() || trivia.IsMultiLineDocComment();
        }

        public static bool IsEndOfLine(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.EndOfLineTrivia) || trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia);
        }

        public static bool IsMultiLineComment(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia) || trivia.IsKind(CS.SyntaxKind.MultiLineDocumentationCommentTrivia);
        }

        public static bool IsMultiLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.MultiLineDocumentationCommentTrivia);
        }

        public static bool IsNone(this SyntaxTrivia trivia)
        {
            return trivia.RawKind == 0;
        }

        public static bool IsRegularOrDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineComment() || trivia.IsMultiLineComment() || trivia.IsDocComment();
        }

        public static bool IsSingleLineComment(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(CS.SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(VB.SyntaxKind.CommentTrivia);
        }

        public static bool IsSingleLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.SingleLineDocumentationCommentTrivia);
        }

        public static bool IsWhitespaceOrEndOfLine(this SyntaxTrivia trivia)
        {
            return trivia.IsKind(CS.SyntaxKind.WhitespaceTrivia) || trivia.IsKind(CS.SyntaxKind.EndOfLineTrivia) || trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) || trivia.IsKind(VB.SyntaxKind.WhitespaceTrivia);
        }

        public static SyntaxTriviaList ToSyntaxTriviaList(this IEnumerable<SyntaxTrivia> l)
        {
            var NewSyntaxTriviaList = new SyntaxTriviaList();
            return NewSyntaxTriviaList.AddRange(l);
        }
    }
}

