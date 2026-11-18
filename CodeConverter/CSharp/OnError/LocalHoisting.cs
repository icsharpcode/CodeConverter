using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Declares before a <c>try</c> the locals that are declared in it but used after it or in the handler.
/// </summary>
/// <remarks>
/// See <see cref="DeclarationSplitter"/> for how a declaration is split. <c>Static</c> locals already become fields, so are left alone.
/// </remarks>
internal sealed class LocalHoisting
{
    private readonly OnErrorConversionContext _context;
    private readonly HashSet<ILocalSymbol> _hoisted;
    private readonly DeclarationSplitter _splitter;

    public LocalHoisting(OnErrorConversionContext context, OnErrorModel model, IEnumerable<StatementRange> segments)
    {
        _context = context;
        _splitter = new DeclarationSplitter(context.SemanticModel, context.CommonConversions);
        _hoisted = new HashSet<ILocalSymbol>(segments.SelectMany(s => FindUsedOutside(model, s)), SymbolEqualityComparer.Default);
    }

    public bool CanDeclareAll => _hoisted.All(DeclarationSplitter.CanSplit);

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
            converted.AddRange(statement is VBSyntax.LocalDeclarationStatementSyntax declaration
                ? _splitter.Split(declaration, csStatements, _hoisted.Contains, Declarations)
                : csStatements);
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
}
