using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using ICSharpCode.CodeConverter.Util;
using CSharpToVBCodeConverter.Util;

namespace CSharpToVBCodeConverter.DestVisualBasic
{
    public static class TriviaListSupport
    {
        internal static void RelocateAttributeDirectiveDisabledTrivia(SyntaxTriviaList TriviaList, bool FoundDirective, bool IsTheory, ref List<SyntaxTrivia> StatementLeadingTrivia, ref List<SyntaxTrivia> StatementTrailingTrivia)
        {
            if (IsTheory)
            {
                return;
            }
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (!(t.IsDirective || t.MatchesKind(VB.SyntaxKind.DisabledTextTrivia, VB.SyntaxKind.RegionDirectiveTrivia, VB.SyntaxKind.EndRegionDirectiveTrivia)))
                {
                    continue;
                }
                if (FoundDirective)
                {
                    if (StatementTrailingTrivia.Count == 0)
                    {
                        StatementTrailingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                    }
                    StatementTrailingTrivia.Add(t);
                }
                else
                {
                    if (t.IsDirective)
                    {
                        StatementLeadingTrivia.Add(t);
                        FoundDirective = true;
                    }
                    else
                    {
                        StatementTrailingTrivia.Add(t);
                    }
                }
            }
        }

        internal static SyntaxTriviaList RelocateDirectiveDisabledTrivia(SyntaxTriviaList TriviaList, ref List<SyntaxTrivia> StatementTrivia, bool RemoveEOL)
        {
            var NewTrivia = new SyntaxTriviaList();
            bool FoundDirective = false;
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (!(FoundDirective || t.IsDirective || t.IsKind(VB.SyntaxKind.DisabledTextTrivia)))
                {
                    if (t.IsEndOfLine() && RemoveEOL)
                    {
                        NewTrivia = NewTrivia.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                    }
                    else
                    {
                        NewTrivia = NewTrivia.Add(t);
                    }
                }
                else
                {
                    if (t.IsDirective)
                    {
                        StatementTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                        StatementTrivia.Add(t);
                        FoundDirective = true;
                    }
                    else
                    {
                        StatementTrivia.Add(t);
                    }
                }
            }
            return NewTrivia;
        }

        internal static SyntaxTriviaList RelocateLeadingCommentTrivia(SyntaxTriviaList TriviaList, ref List<SyntaxTrivia> StatementLeadingTrivia)
        {
            var NewLeadingTrivia = new SyntaxTriviaList();
            bool FoundComment = false;
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (!(FoundComment || t.IsComment()))
                {
                    if (t.IsEndOfLine())
                    {
                        NewLeadingTrivia = NewLeadingTrivia.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                    }
                    else
                    {
                        NewLeadingTrivia = NewLeadingTrivia.Add(t);
                    }
                }
                else
                {
                    if (t.IsComment())
                    {
                        StatementLeadingTrivia.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                        StatementLeadingTrivia.Add(t);
                        FoundComment = true;
                    }
                    else
                    {
                        StatementLeadingTrivia.Add(t);
                    }
                }
            }
            return NewLeadingTrivia;
        }

        internal static SyntaxTokenList TranslateTokenList(IEnumerable<SyntaxToken> ChildTokens)
        {
            var NewTokenList = new SyntaxTokenList();
            foreach (SyntaxToken token in ChildTokens)
            {
                var NewLeadingTriviaList = new SyntaxTriviaList();
                var NewTrailingTriviaList = new SyntaxTriviaList();

                string TokenText = token.Text;
                string ValueText = token.ValueText;
                if (token.HasLeadingTrivia)
                {
                    foreach (SyntaxTrivia t in token.LeadingTrivia)
                    {
                        if (t.IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia))
                        {
                            NewLeadingTriviaList = NewLeadingTriviaList.Add(VBFactory.DocumentationCommentExteriorTrivia(token.LeadingTrivia[0].ToString().Replace("///", "'''")));
                            if (!TokenText.StartsWith(" "))
                            {
                                TokenText = " " + TokenText;
                                ValueText = " " + ValueText;
                            }
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                }

                var switchExpr = token.RawKind;
                switch (switchExpr)
                {
                    case (int)CS.SyntaxKind.XmlTextLiteralToken:
                        {
                            NewTokenList = NewTokenList.Add(VBFactory.XmlTextLiteralToken(NewLeadingTriviaList, TokenText, ValueText, NewTrailingTriviaList));
                            break;
                        }

                    case (int)CS.SyntaxKind.XmlTextLiteralNewLineToken:
                        {
                            NewTokenList = NewTokenList.Add(VBFactory.XmlTextNewLine(text: Constants.vbCrLf, value: Constants.vbCrLf, NewLeadingTriviaList, NewTrailingTriviaList));
                            break;
                        }

                    case (int)CS.SyntaxKind.XmlEntityLiteralToken:
                        {
                            NewTokenList = NewTokenList.Add(VBFactory.XmlEntityLiteralToken(NewLeadingTriviaList, TokenText, ValueText, NewTrailingTriviaList));
                            break;
                        }

                    default:
                        {
                            Debugger.Break();
                            break;
                        }
                }
            }
            return NewTokenList;
        }

        internal static bool TriviaIsIdentical(SyntaxTriviaList LeadingTrivia, List<SyntaxTrivia> NodeLeadingTrivia)
        {
            if (LeadingTrivia.Count != NodeLeadingTrivia.Count)
                return false;
            for (int i = 0, loopTo = LeadingTrivia.Count - 1; i <= loopTo; i++)
            {
                if ((LeadingTrivia[i].ToFullString().Trim() ?? "") != (NodeLeadingTrivia[i].ToFullString().Trim() ?? ""))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

