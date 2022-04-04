using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB;

public class CSharpHelperMethodDefinition
{
    private const string InlineAssignMethodName = "__Assign";
    private const string ThrowMethodName = "__Throw";
    private const string CSharpImplClassName = "CSharpImpl";
    public static string QualifiedInlineAssignMethodName { get; } = $"{CSharpImplClassName}.{InlineAssignMethodName}";
    public static string QualifiedThrowMethodName { get; } = $"{CSharpImplClassName}.{ThrowMethodName}";

    private readonly string _assignMethodDefinition =
        $@"<System.Obsolete(""Please refactor calling code to use normal Visual Basic assignment"")>
Shared Function {InlineAssignMethodName}(Of T)(ByRef target As T, value As T) As T
    target = value
    Return value
End Function";

    private readonly string _throwMethodDefinition =
        $@"<System.Obsolete(""Please refactor calling code to use normal throw statements"")>
Shared Function {ThrowMethodName}(Of T)(ByVal e As System.Exception) As T
    Throw e
End Function";

    private static readonly ICompiler Compiler = new VisualBasicCompiler();
    private List<INamedTypeSymbol> AssignMethodTypeSymbols { get; } = new();
    private List<INamedTypeSymbol> ThrowMethodTypeSymbols { get; } = new();

    public void AddAssignMethod(INamedTypeSymbol symbol) {
        if(AssignMethodTypeSymbols.Contains(symbol))
            return;
        AssignMethodTypeSymbols.Add(symbol);
    }
    public void AddThrowMethod(INamedTypeSymbol symbol) {
        if(ThrowMethodTypeSymbols.Contains(symbol))
            return;
        ThrowMethodTypeSymbols.Add(symbol);
    }

    public IEnumerable<StatementSyntax> GetExtraMembers(INamedTypeSymbol symbol)
    {
        var inlineHelperMethods = SyntaxFactory.List(GetInlineHelperMethods(symbol));
        if (inlineHelperMethods.Any()) {
            var classStatementSyntax = SyntaxFactory.ClassStatement(CSharpImplClassName).WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
            yield return SyntaxFactory.ClassBlock(classStatementSyntax,
                new SyntaxList<InheritsStatementSyntax>(), new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(inlineHelperMethods));
        }
    }

    private IEnumerable<StatementSyntax> GetInlineHelperMethods(INamedTypeSymbol symbol)
    {
        if (AssignMethodTypeSymbols.Contains(symbol)) {
            yield return Parse(_assignMethodDefinition);
        }

        if (ThrowMethodTypeSymbols.Contains(symbol)) {
            yield return Parse(_throwMethodDefinition);
        }
    }

    private static StatementSyntax Parse(string methodDefinition)
    {
        return Compiler.CreateTree(methodDefinition)
            .GetRoot().ChildNodes().Single().NormalizeWhitespace() as StatementSyntax;
    }
}