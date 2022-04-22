using Constants = Microsoft.VisualBasic.Constants;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace ICSharpCode.CodeConverter.VB.Trivia;

internal static class TriviaListSupport
{

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
                        if (!TokenText.StartsWith(" ", StringComparison.InvariantCulture))
                        {
                            TokenText = " " + TokenText;
                            ValueText = " " + ValueText;
                        }
                    }
                    else
                    {

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

                    break;
                }
            }
        }
        return NewTokenList;
    }
}