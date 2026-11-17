using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Splits a converted local declaration into a declaration of the chosen locals as <c>= default</c>, and the assignment of their initializers.
/// </summary>
/// <remarks>
/// A C# local is scoped to its block, but a VB local lives for the whole method and starts zeroed, so declaring it earlier as <c>= default</c> is exact.
/// The declaration becomes an assignment, or nothing when VB had no initializer. Constants move whole.
/// </remarks>
internal sealed class DeclarationSplitter
{
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;

    public DeclarationSplitter(SemanticModel semanticModel, CommonConversions commonConversions)
    {
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
    }

    /// <param name="vbDeclaration">The declaration that <paramref name="csStatements"/> were converted from.</param>
    /// <param name="shouldSplit">Which of its locals to split.</param>
    /// <param name="declarations">Receives the declarations to place before the statements that are returned.</param>
    /// <param name="transformAssignment">Replaces the assignment of a split local's initializer, e.g. to guard it. Receives the local and the assignment.</param>
    /// <returns>The statements that stay where the declaration was.</returns>
    public IEnumerable<StatementSyntax> Split(VBSyntax.LocalDeclarationStatementSyntax vbDeclaration, IEnumerable<StatementSyntax> csStatements,
        Func<ILocalSymbol, bool> shouldSplit, List<StatementSyntax> declarations, Func<ILocalSymbol, StatementSyntax, StatementSyntax> transformAssignment = null)
    {
        var splitByName = vbDeclaration.Declarators
            .SelectMany(v => v.Names.Select(n => (Symbol: _semanticModel.GetDeclaredSymbol(n) as ILocalSymbol, NeedsAssignment: NeedsAssignment(v, n))))
            .Where(x => x.Symbol != null && shouldSplit(x.Symbol))
            .ToDictionary(x => x.Symbol.Name, x => x, StringComparer.OrdinalIgnoreCase);
        if (!splitByName.Any()) return csStatements;

        var inPlace = new List<StatementSyntax>();
        foreach (var csStatement in csStatements) {
            if (csStatement is not LocalDeclarationStatementSyntax csDeclaration ||
                !csDeclaration.Declaration.Variables.Any(v => splitByName.ContainsKey(v.Identifier.ValueText))) {
                inPlace.Add(csStatement);
            } else if (csDeclaration.IsConst) {
                declarations.Add(csDeclaration);
            } else {
                inPlace.AddRange(SplitDeclaration(csDeclaration, splitByName, declarations, transformAssignment ?? ((_, assignment) => assignment)));
            }
        }
        return inPlace;
    }

    private IEnumerable<StatementSyntax> SplitDeclaration(LocalDeclarationStatementSyntax csDeclaration,
        Dictionary<string, (ILocalSymbol Symbol, bool NeedsAssignment)> splitByName, List<StatementSyntax> declarations,
        Func<ILocalSymbol, StatementSyntax, StatementSyntax> transformAssignment)
    {
        var inOrder = new List<StatementSyntax>();
        var kept = new List<VariableDeclaratorSyntax>();
        foreach (var variable in csDeclaration.Declaration.Variables) {
            if (!splitByName.TryGetValue(variable.Identifier.ValueText, out var split)) {
                kept.Add(variable);
                continue;
            }
            AddKept();
            declarations.Add(DeclareDefault(variable.Identifier, split.Symbol));
            if (split.NeedsAssignment && variable.Initializer != null) {
                inOrder.Add(transformAssignment(split.Symbol, SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()), variable.Initializer.Value.WithoutTrivia()))));
            }
        }
        AddKept();
        return inOrder;

        void AddKept()
        {
            if (!kept.Any()) return;
            inOrder.Add(csDeclaration.WithDeclaration(csDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(kept))));
            kept.Clear();
        }
    }

    private LocalDeclarationStatementSyntax DeclareDefault(SyntaxToken identifier, ILocalSymbol local)
    {
        var declarator = SyntaxFactory.VariableDeclarator(identifier.WithoutTrivia())
            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)));
        var type = _commonConversions.GetTypeSyntax(local.Type);
        return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(declarator)));
    }

    private static bool NeedsAssignment(VBSyntax.VariableDeclaratorSyntax declarator, VBSyntax.ModifiedIdentifierSyntax name) =>
        declarator.Initializer != null || declarator.AsClause is VBSyntax.AsNewClauseSyntax || name.ArrayBounds != null;
}
