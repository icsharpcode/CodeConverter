using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.CSharp;

internal class PerScopeState
{
    public record ScopeState(List<IHoistedNode> HoistedNodes, VBasic.SyntaxKind ExitableKind, bool IsBreakableInCs = false)
    {
        public void Add<T>(T additionalLocal) where T : IHoistedNode => HoistedNodes.Add(additionalLocal);
        public IEnumerable<T> OfType<T>() => HoistedNodes.OfType<T>();
    }

    public static SyntaxAnnotation AdditionalLocalAnnotation = new("CodeConverter.AdditionalLocal");

    private readonly Stack<ScopeState> _hoistedNodesPerScope;

    public PerScopeState() =>_hoistedNodesPerScope = new Stack<ScopeState>();

    public void PushScope(VBasic.SyntaxKind exitableKind = default, bool isBreakableInCs = false)
    {
        _hoistedNodesPerScope.Push(new ScopeState(new List<IHoistedNode>(), exitableKind, isBreakableInCs));
    }

    public void PopScope() => _hoistedNodesPerScope.Pop();

    public void PopExpressionScope()
    {
        var statements = GetParameterlessFunctions();
        PopScope();
        foreach (var statement in statements) {
            Hoist(statement);
        }
    }

    public T Hoist<T>(T additionalLocal) where T : IHoistedNode
    {
        _hoistedNodesPerScope.Peek().Add(additionalLocal);
        return additionalLocal;
    }

    public void HoistToTopLevel<T>(T additionalField) where T : IHoistedNode
    {
        _hoistedNodesPerScope.Last().Add(additionalField);
    }

    public IReadOnlyCollection<AdditionalDeclaration> GetDeclarations()
    {
        return _hoistedNodesPerScope.Peek().OfType<AdditionalDeclaration>().ToArray();
    }

    private StatementSyntax[] GetPostStatements()
    {
        var scopeState = _hoistedNodesPerScope.Peek();
        return scopeState.OfType<AdditionalAssignment>().Select(AdditionalAssignment.CreateAssignment)
            .Concat(scopeState.OfType<IfTrueBreak>().Select(arg => arg.CreateIfTrueBreakStatement()))
            .ToArray();
    }

    public IReadOnlyCollection<HoistedParameterlessFunction> GetParameterlessFunctions()
    {
        return _hoistedNodesPerScope.Peek().OfType<HoistedParameterlessFunction>().ToArray();
    }

    public IReadOnlyCollection<HoistedFieldFromVbStaticVariable> GetFields()
    {
        return _hoistedNodesPerScope.Peek().OfType<HoistedFieldFromVbStaticVariable>().ToArray();
    }

    public SyntaxList<StatementSyntax> CreateStatements(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<StatementSyntax> statements, HashSet<string> generatedNames, SemanticModel semanticModel)
    {
        var localFunctions = GetParameterlessFunctions(); 
        var newNames = localFunctions.ToDictionary(f => f.Id, f =>
            NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, vbNode, f.Prefix)
        );
        var functions = localFunctions.Select(f => f.AsLocalFunction(newNames[f.Id]));
        statements = ReplaceNames(functions.Concat(statements), newNames);
        return SyntaxFactory.List(statements);
    }

    public async Task<SyntaxList<StatementSyntax>> CreateLocalsAsync(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<StatementSyntax> csNodes, HashSet<string> generatedNames, SemanticModel semanticModel)
    {
        var preDeclarations = new List<StatementSyntax>();
        var postAssignments = new List<StatementSyntax>();

        var additionalDeclarationInfo = GetDeclarations();
        var newNames = additionalDeclarationInfo.ToDictionary(l => l.Id, l =>
            NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, vbNode, l.Prefix)
        );
        foreach (var additionalLocal in additionalDeclarationInfo) {
            var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Id],
                additionalLocal.Initializer, additionalLocal.Type);
            preDeclarations.Add(CS.SyntaxFactory.LocalDeclarationStatement(decl));
        }

        foreach (var additionalAssignment in GetPostStatements()) {
            postAssignments.Add(additionalAssignment);
        }

        var statementsWithUpdatedIds = ReplaceNames(preDeclarations.Concat(csNodes).Concat(postAssignments), newNames);

        return CS.SyntaxFactory.List(statementsWithUpdatedIds);
    }


    public async Task<SyntaxList<MemberDeclarationSyntax>> CreateVbStaticFieldsAsync(VBasic.VisualBasicSyntaxNode typeNode, INamedTypeSymbol namedTypeSymbol,
        IEnumerable<MemberDeclarationSyntax> csNodes, HashSet<string> generatedNames, SemanticModel semanticModel)
    {
        var declarations = new List<FieldDeclarationSyntax>();

        var fieldInfo = GetFields();
        var newNames = fieldInfo.ToDictionary(f => f.OriginalVariableName, f =>
            NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, typeNode, f.FieldName)
        );
        foreach (var field in fieldInfo) {
            var decl = (field.Initializer != null) 
                ? CommonConversions.CreateVariableDeclarationAndAssignment(newNames[field.OriginalVariableName], field.Initializer, field.Type)
                : SyntaxFactory.VariableDeclaration(field.Type, SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(newNames[field.OriginalVariableName])));
            var modifiers = new List<SyntaxToken> { CS.SyntaxFactory.Token(SyntaxKind.PrivateKeyword) };
            if (field.IsStatic || namedTypeSymbol.IsModuleType()) {
                modifiers.Add(CS.SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
            declarations.Add(CS.SyntaxFactory.FieldDeclaration(CS.SyntaxFactory.List<AttributeListSyntax>(), CS.SyntaxFactory.TokenList(modifiers), decl));
        }

        var statementsWithUpdatedIds = ReplaceNames(declarations.Concat(csNodes), newNames);

        return CS.SyntaxFactory.List(statementsWithUpdatedIds);
    }

    public static IEnumerable<T> ReplaceNames<T>(IEnumerable<T> csNodes, Dictionary<string, string> newNames) where T : SyntaxNode
    {
        csNodes = csNodes.Select(csNode => ReplaceNames(csNode, newNames)).ToList();
        return csNodes;
    }

    public static T ReplaceNames<T>(T csNode, Dictionary<string, string> newNames) where T : SyntaxNode
    {
        return csNode.ReplaceNodes(csNode.DescendantNodes().OfType<IdentifierNameSyntax>(), (_, idns) => {
            if (newNames.TryGetValue(idns.Identifier.ValueText, out var newName)) {
                return idns.WithoutAnnotations(AdditionalLocalAnnotation).WithIdentifier(CS.SyntaxFactory.Identifier(newName));
            }
            return idns;
        });
    }

    public IEnumerable<StatementSyntax> ConvertExit(VBasic.SyntaxKind vbBlockKeywordKind)
    {
        var scopesToExit = _hoistedNodesPerScope.Where(x => x.ExitableKind != VBasic.SyntaxKind.None).TakeWhile(x => x.ExitableKind != vbBlockKeywordKind && x.IsBreakableInCs).ToArray();
        var assignmentExpression = CommonConversions.Literal(true);
        foreach (var scope in scopesToExit) {
            var exitScopeVar = new AdditionalDeclaration("exit" + VBasic.SyntaxFactory.Token(vbBlockKeywordKind), CommonConversions.Literal(false), SyntaxFactory.ParseTypeName("bool"));
            var ifTrueBreak = new IfTrueBreak(exitScopeVar.IdentifierName);
            scope.HoistedNodes.Add(exitScopeVar);
            scope.HoistedNodes.Add(ifTrueBreak);
            assignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, exitScopeVar.IdentifierName, assignmentExpression);
        }
        if (scopesToExit.Any()) yield return SyntaxFactory.ExpressionStatement(assignmentExpression);
        yield return SyntaxFactory.BreakStatement();
    }
}