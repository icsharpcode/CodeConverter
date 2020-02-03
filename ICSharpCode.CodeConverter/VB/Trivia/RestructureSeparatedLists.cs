using CSharpToVBCodeConverter.Util;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using VB = Microsoft.CodeAnalysis.VisualBasic;

public static class RestuructureSeparatedLists
{
    public static int IgnoredIfDepth { get; set; } = 0;

    internal static void RestructureNodesAndSeparators<T>(ref SyntaxToken _OpenToken, ref List<T> Items, ref List<SyntaxToken> Separators, ref SyntaxToken _CloseToken) where T : VB.VisualBasicSyntaxNode
    {
        var TokenLeadingTrivia = new List<SyntaxTrivia>();
        _OpenToken = _OpenToken.WithModifiedTokenTrivia(LeadingToken: true, AfterEOL: false);
        for (int i = 0, loopTo = Items.Count - 2; i <= loopTo; i++)
        {
            Items[i] = Items[i].WithModifiedNodeTrivia(SeparatorFollows: true);
            Separators[i] = Separators[i].WithModifiedTokenTrivia(LeadingToken: false, AfterEOL: false);
        }
        bool LastItemEndsWithEOL = false;
        if (Items.Count > 0)
        {
            Items[Items.Count - 1] = Items.Last().WithModifiedNodeTrivia(SeparatorFollows: false);
            LastItemEndsWithEOL = Items.Last().HasTrailingTrivia && Items.Last().GetTrailingTrivia().Last().IsKind(VB.SyntaxKind.EndOfLineTrivia);
        }

        _CloseToken = _CloseToken.WithModifiedTokenTrivia(LeadingToken: false, LastItemEndsWithEOL);
    }
}

