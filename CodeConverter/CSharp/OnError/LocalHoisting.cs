using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Declares before a <c>try</c> the locals that are declared in it but used after it or in the handler.
/// </summary>
/// <remarks>
/// A C# local is scoped to its block, but a VB local lives for the whole method and starts zeroed, so declaring it earlier as <c>= default</c> is exact.
/// The declaration in the <c>try</c> becomes an assignment, or nothing when VB had no initializer.
/// Constants move whole. <c>Static</c> locals already become fields.
/// </remarks>
internal sealed class LocalHoisting
{
    private readonly OnErrorConversionContext _context;
    private readonly HashSet<ILocalSymbol> _hoisted;

    public LocalHoisting(OnErrorConversionContext context, OnErrorModel model, IEnumerable<StatementRange> segments)
    {
        _context = context;
        _hoisted = new HashSet<ILocalSymbol>(segments.SelectMany(s => FindUsedOutside(model, s)), SymbolEqualityComparer.Default);
    }

    /// <summary>The declarations that go before the first <c>try</c>, in order.</summary>
    public List<StatementSyntax> Declarations { get; } = new();

    /// <summary>
    /// Converts statements that will be in a <c>try</c>, adding to <see cref="Declarations"/> what must be declared before it.
    /// </summary>
    public async Task<List<StatementSyntax>> ConvertAsync(IEnumerable<VBSyntax.StatementSyntax> statements)
    {
        var converted = new List<StatementSyntax>();
        foreach (var statement in statements) {
            var csStatements = await _context.ConvertAsync(new[] { statement });
            converted.AddRange(statement is VBSyntax.LocalDeclarationStatementSyntax declaration ? Split(declaration, csStatements) : csStatements);
        }
        return converted;
    }

    private IEnumerable<ILocalSymbol> FindUsedOutside(OnErrorModel model, StatementRange segment)
    {
        var declared = model.Statements.Skip(segment.Start).Take(segment.End - segment.Start)
            .OfType<VBSyntax.LocalDeclarationStatementSyntax>()
            .Where(d => !d.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.StaticKeyword)))
            .SelectMany(d => d.Declarators.SelectMany(v => v.Names))
            .Select(n => _context.SemanticModel.GetDeclaredSymbol(n) as ILocalSymbol)
            .Where(l => l != null)
            .ToArray();
        if (!declared.Any()) return declared;
        var names = new HashSet<string>(declared.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);
        var referencedOutside = model.Statements.Where((_, i) => !segment.Contains(i))
            .SelectMany(s => s.DescendantNodes().OfType<VBSyntax.IdentifierNameSyntax>())
            .Where(i => names.Contains(i.Identifier.ValueText))
            .Select(i => _context.SemanticModel.GetSymbolInfo(i).Symbol)
            .ToArray();
        return declared.Where(l => referencedOutside.Contains(l, SymbolEqualityComparer.Default));
    }

    private IEnumerable<StatementSyntax> Split(VBSyntax.LocalDeclarationStatementSyntax vbDeclaration, IEnumerable<StatementSyntax> csStatements)
    {
        var hoistedByName = vbDeclaration.Declarators
            .SelectMany(v => v.Names.Select(n => (Symbol: _context.SemanticModel.GetDeclaredSymbol(n) as ILocalSymbol, NeedsAssignment: NeedsAssignment(v, n))))
            .Where(x => x.Symbol != null && _hoisted.Contains(x.Symbol))
            .ToDictionary(x => x.Symbol.Name, x => x, StringComparer.OrdinalIgnoreCase);
        if (!hoistedByName.Any()) return csStatements;

        var inPlace = new List<StatementSyntax>();
        foreach (var csStatement in csStatements) {
            if (csStatement is not LocalDeclarationStatementSyntax csDeclaration ||
                !csDeclaration.Declaration.Variables.Any(v => hoistedByName.ContainsKey(v.Identifier.ValueText))) {
                inPlace.Add(csStatement);
            } else if (csDeclaration.IsConst) {
                Declarations.Add(csDeclaration);
            } else {
                inPlace.AddRange(SplitDeclaration(csDeclaration, hoistedByName));
            }
        }
        return inPlace;
    }

    private IEnumerable<StatementSyntax> SplitDeclaration(LocalDeclarationStatementSyntax csDeclaration,
        Dictionary<string, (ILocalSymbol Symbol, bool NeedsAssignment)> hoistedByName)
    {
        var inOrder = new List<StatementSyntax>();
        var kept = new List<VariableDeclaratorSyntax>();
        foreach (var variable in csDeclaration.Declaration.Variables) {
            if (!hoistedByName.TryGetValue(variable.Identifier.ValueText, out var hoisted)) {
                kept.Add(variable);
                continue;
            }
            AddKept();
            Declarations.Add(DeclareDefault(variable.Identifier, hoisted.Symbol));
            if (hoisted.NeedsAssignment && variable.Initializer != null) {
                inOrder.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()), variable.Initializer.Value.WithoutTrivia())));
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
        var type = _context.CommonConversions.GetTypeSyntax(local.Type);
        return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(declarator)));
    }

    private static bool NeedsAssignment(VBSyntax.VariableDeclaratorSyntax declarator, VBSyntax.ModifiedIdentifierSyntax name) =>
        declarator.Initializer != null || declarator.AsClause is VBSyntax.AsNewClauseSyntax || name.ArrayBounds != null;
}
