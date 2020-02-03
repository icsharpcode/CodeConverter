using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using Microsoft.VisualBasic.CompilerServices;

namespace CSharpToVBCodeConverter.Util
{
    public static class SyntaxTokenExtensions
    {
        internal static SyntaxToken WithPrependedLeadingTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }

        public static SyntaxToken With(this SyntaxToken token, List<SyntaxTrivia> leading, List<SyntaxTrivia> trailing)
        {
            return token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);
        }

        public static SyntaxToken With(this SyntaxToken token, SyntaxTriviaList leading, SyntaxTriviaList trailing)
        {
            return token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);
        }

        public static bool IsKind(this SyntaxToken token, params Microsoft.CodeAnalysis.CSharp.SyntaxKind[] kinds)
        {
            return kinds.Contains((Microsoft.CodeAnalysis.CSharp.SyntaxKind)Conversions.ToUShort(token.RawKind));
        }

        public static bool IsKind(this SyntaxToken token, params VB.SyntaxKind[] kinds)
        {
            return kinds.Contains((VB.SyntaxKind)Conversions.ToUShort(token.RawKind));
        }

        public static SyntaxToken RemoveExtraEOL(this SyntaxToken Token)
        {
            var LeadingTrivia = new List<SyntaxTrivia>();
            LeadingTrivia.AddRange(Token.LeadingTrivia);
            var switchExpr = LeadingTrivia.Count;
            switch (switchExpr)
            {
                case 0:
                    {
                        return Token;
                    }

                case 1:
                    {
                        if (LeadingTrivia.First().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                        {
                            return Token.WithLeadingTrivia(new SyntaxTriviaList());
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
                                        return Token.WithLeadingTrivia(new SyntaxTriviaList());
                                    }
                                    return Token;
                                }

                            case (int)VB.SyntaxKind.EndOfLineTrivia:
                                {
                                    if (LeadingTrivia.Last().IsKind(VB.SyntaxKind.EndOfLineTrivia))
                                    {
                                        return Token.WithLeadingTrivia(new SyntaxTriviaList());
                                    }
                                    return Token.WithLeadingTrivia(LeadingTrivia.Last());
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
            for (int i = 0, loopTo = Token.LeadingTrivia.Count - 1; i <= loopTo; i++)
            {
                var Trivia = Token.LeadingTrivia[i];
                var NextTrivia = i < Token.LeadingTrivia.Count - 1 ? Token.LeadingTrivia[i + 1] : default(SyntaxTrivia);
                if (Trivia.IsKind(VB.SyntaxKind.WhitespaceTrivia) && NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                {
                    continue;
                }
                if (Trivia.IsKind(VB.SyntaxKind.CommentTrivia) && NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                {
                    NewLeadingTrivia.Add(Trivia);
                    continue;
                }

                if (Trivia.IsKind(VB.SyntaxKind.EndOfLineTrivia) && NextTrivia.IsKind(VB.SyntaxKind.EndOfLineTrivia))
                {
                    continue;
                }
                NewLeadingTrivia.Add(Trivia);
            }

            return Token.WithLeadingTrivia(NewLeadingTrivia);
        }

        public static SyntaxToken WithAppendedTrailingTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithTrailingTrivia(token.TrailingTrivia.Concat(trivia));
        }

        /// <summary>
        /// Used for parameters and arguments where blank lines and
        /// most directives are not allowed
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="LeadingToken"></param>
        /// <param name="AfterEOL"></param>
        /// <returns></returns>
        public static SyntaxToken WithModifiedTokenTrivia(this SyntaxToken Token, bool LeadingToken, bool AfterEOL)
        {
            var FinalLeadingTriviaList = new List<SyntaxTrivia>();
            bool AfterWhiteSpace = false;
            bool AfterLineContinuation = LeadingToken;
            var InitialTriviaList = new List<SyntaxTrivia>();
            int TriviaListUBound;
            if (LeadingToken)
            {
                FinalLeadingTriviaList.AddRange(Token.LeadingTrivia);
            }
            else
            {
                InitialTriviaList.AddRange(Token.LeadingTrivia);
                TriviaListUBound = InitialTriviaList.Count - 1;
                for (int i = 0, loopTo = TriviaListUBound; i <= loopTo; i++)
                {
                    var Trivia = InitialTriviaList[i];
                    var NextTrivia = i < TriviaListUBound ? InitialTriviaList[i + 1] : default(SyntaxTrivia);
                    var switchExpr = Trivia.RawKind;
                    switch (switchExpr)
                    {
                        case (int)VB.SyntaxKind.WhitespaceTrivia:
                            {
                                AfterEOL = false;
                                AfterLineContinuation = false;
                                AfterWhiteSpace = true;
                                FinalLeadingTriviaList.Add(Trivia);
                                break;
                            }

                        case (int)VB.SyntaxKind.EndOfLineTrivia:
                            {
                                AfterLineContinuation = false;
                                AfterWhiteSpace = false;
                                if (AfterEOL)
                                {
                                    continue;
                                }
                                FinalLeadingTriviaList.Add(Trivia);
                                // What I do depends on whats next
                                if (i < TriviaListUBound)
                                {
                                    int j;
                                    string NewWhiteSpaceString = "";
                                    var loopTo1 = TriviaListUBound;
                                    for (j = i + 1; j <= loopTo1; j++)
                                    {
                                        if (InitialTriviaList[j].IsKind(VB.SyntaxKind.WhitespaceTrivia))
                                        {
                                            NewWhiteSpaceString += InitialTriviaList[j].ToString();
                                            i += 1;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    if (j < TriviaListUBound && InitialTriviaList[j].IsKind(VB.SyntaxKind.CommentTrivia))
                                    {
                                        if (string.IsNullOrWhiteSpace(NewWhiteSpaceString))
                                        {
                                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        }
                                        else
                                        {
                                            FinalLeadingTriviaList.Add(VBFactory.WhitespaceTrivia(NewWhiteSpaceString));
                                        }
                                        FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                        AfterLineContinuation = true;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(NewWhiteSpaceString))
                                        {
                                            FinalLeadingTriviaList.Add(VBFactory.WhitespaceTrivia(NewWhiteSpaceString));
                                        }
                                    }
                                }

                                break;
                            }

                        case (int)VB.SyntaxKind.CommentTrivia:
                            {
                                AfterEOL = false;
                                if (!AfterWhiteSpace)
                                {
                                    FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                }
                                if (!AfterLineContinuation)
                                {
                                    FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                    FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                }
                                FinalLeadingTriviaList.Add(Trivia);
                                AfterLineContinuation = false;
                                AfterWhiteSpace = false;
                                break;
                            }

                        case (int)VB.SyntaxKind.IfDirectiveTrivia:
                        case (int)VB.SyntaxKind.DisabledTextTrivia:
                        case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                            {
                                AfterEOL = false;
                                FinalLeadingTriviaList.AddRange(Util.SyntaxTriviaExtensions.DirectiveNotAllowedHere(Trivia));
                                var switchExpr1 = NextTrivia.RawKind;
                                switch (switchExpr1)
                                {
                                    case (int)VB.SyntaxKind.None:
                                        {
                                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                            break;
                                        }

                                    case (int)VB.SyntaxKind.WhitespaceTrivia:
                                        {
                                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                            break;
                                        }

                                    case (int)VB.SyntaxKind.IfDirectiveTrivia:
                                    case (int)VB.SyntaxKind.DisabledTextTrivia:
                                    case (int)VB.SyntaxKind.EndIfDirectiveTrivia:
                                        {
                                            FinalLeadingTriviaList.Add(global::VisualBasicSyntaxFactory.VBEOLTrivia);
                                            break;
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
                }
            }
            InitialTriviaList.Clear();
            InitialTriviaList.AddRange(Token.TrailingTrivia);
            TriviaListUBound = InitialTriviaList.Count - 1;
            AfterWhiteSpace = false;
            AfterLineContinuation = false;

            var FinalTrailingTriviaList = new List<SyntaxTrivia>();
            if (LeadingToken)
            {
                for (int i = 0, loopTo2 = TriviaListUBound; i <= loopTo2; i++)
                {
                    var Trivia = InitialTriviaList[i];
                    var NextTrivia = i < TriviaListUBound ? InitialTriviaList[i + 1] : new SyntaxTrivia();
                    var switchExpr2 = Trivia.RawKind;
                    switch (switchExpr2)
                    {
                        case (int)VB.SyntaxKind.WhitespaceTrivia:
                            {
                                if (NextTrivia.IsKind(VB.SyntaxKind.CommentTrivia) || NextTrivia.IsKind(VB.SyntaxKind.LineContinuationTrivia))
                                {
                                    FinalTrailingTriviaList.Add(Trivia);
                                }

                                break;
                            }

                        case (int)VB.SyntaxKind.EndOfLineTrivia:
                            {
                                // If leading there is a node after this Token
                                var j = default(int);
                                string NewWhiteSpaceString = "";
                                if (i < TriviaListUBound)
                                {
                                    var loopTo3 = TriviaListUBound;
                                    for (j = i + 1; j <= loopTo3; j++)
                                    {
                                        if (InitialTriviaList[j].IsKind(VB.SyntaxKind.WhitespaceTrivia))
                                        {
                                            NewWhiteSpaceString += InitialTriviaList[j].ToString();
                                            i += 1;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (j == 0 || j < TriviaListUBound && InitialTriviaList[j].IsKind(VB.SyntaxKind.CommentTrivia))
                                {
                                    if (!AfterLineContinuation)
                                    {
                                        if (string.IsNullOrWhiteSpace(NewWhiteSpaceString))
                                        {
                                            FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.SpaceTrivia);
                                        }
                                        else
                                        {
                                            FinalTrailingTriviaList.Add(VBFactory.WhitespaceTrivia(NewWhiteSpaceString));
                                        }
                                        FinalTrailingTriviaList.Add(global::VisualBasicSyntaxFactory.LineContinuation);
                                    }
                                    FinalTrailingTriviaList.Add(Trivia);
                                    AfterLineContinuation = true;
                                }
                                else
                                {
                                    FinalTrailingTriviaList.Add(Trivia);
                                    if (!string.IsNullOrWhiteSpace(NewWhiteSpaceString))
                                    {
                                        FinalTrailingTriviaList.Add(VBFactory.WhitespaceTrivia(NewWhiteSpaceString));
                                    }
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
                                AfterLineContinuation = false;
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
            }
            else
            {
                FinalTrailingTriviaList.AddRange(Token.TrailingTrivia);
            }
            return Token.With(FinalLeadingTriviaList, FinalTrailingTriviaList);
        }

        public static SyntaxToken WithPrependedLeadingTrivia(this SyntaxToken token, SyntaxTriviaList trivia)
        {
            if (trivia.Count == 0)
            {
                return token;
            }

            return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
        }
    }
}

