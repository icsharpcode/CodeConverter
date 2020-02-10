using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AnonymousObjectCreationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousObjectCreationExpressionSyntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentListSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayRankSpecifierSyntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax;
using CastExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using ConditionalAccessExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using DoStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.DoStatementSyntax;
using EmptyStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax;
using EnumMemberDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax;
using FieldDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
using ForEachStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax;
using ForStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax;
using IfStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax;
using ParameterListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterListSyntax;
using ParenthesizedExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using TypeOfExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeOfExpressionSyntax;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using UsingStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.UsingStatementSyntax;
using WhileStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax;
using VBCommonConversions = ICSharpCode.CodeConverter.VB.CommonConversions;

namespace ICSharpCode.CodeConverter.Util
{

    internal sealed class TriviaKinds
    {
        public static TriviaKinds All = new TriviaKinds(_ => true);
        public static TriviaKinds ImportantOnly = new TriviaKinds(t => !t.IsWhitespaceOrEndOfLine());
        public static TriviaKinds FormattingOnly = new TriviaKinds(t => t.IsWhitespaceOrEndOfLine());
        public Func<SyntaxTrivia, bool> ShouldAccept { get; }

        private TriviaKinds(Func<SyntaxTrivia, bool> shouldAccept)
        {
            ShouldAccept = shouldAccept ?? throw new ArgumentNullException(nameof(shouldAccept));
        }
    }
}