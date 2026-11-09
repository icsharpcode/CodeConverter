using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Builds a C# query left to right while tracking the VB range variables in scope (<see cref="Scope"/>) and the C# expression for each.
/// </summary>
/// <remarks>
/// The VB "element" of a query at any point is the set of range variables in scope: a single variable is the value itself, multiple form an anonymous type.
/// C# keeps variables in scope that VB has dropped (e.g. after a VB Select), so the element is only ever produced from <see cref="Scope"/>, never from the C# query's own variables.
/// When an operator has no C# query clause (e.g. Distinct), the element is materialized with a select, the operator applied as a method call, and the query resumed with a new from clause.
/// See QueryConversion.md for the full design.
/// </remarks>
internal class CsQueryBuilder
{
    private const string GeneratedVariableName = "x";
    private readonly Func<string, bool> _isNameFreeOutsideQuery;
    private readonly List<(List<QueryClauseSyntax> Clauses, SelectOrGroupClauseSyntax SelectOrGroup, SyntaxToken Into)> _completedBodies = new();
    private readonly HashSet<string> _csRangeVariableNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allDeclaredNames = new(StringComparer.OrdinalIgnoreCase);
    private FromClauseSyntax _from;
    private List<QueryClauseSyntax> _clauses = new();
    private ExpressionSyntax _source;
    private List<QueryRangeVariable> _scope = new();

    /// <param name="isNameFreeOutsideQuery">Whether a generated name would clash with anything else visible, e.g. locals or the range variables of an enclosing query</param>
    public CsQueryBuilder(Func<string, bool> isNameFreeOutsideQuery) => _isNameFreeOutsideQuery = isNameFreeOutsideQuery;

    public IReadOnlyList<QueryRangeVariable> Scope => _scope;

    /// <summary>
    /// Avoids reusing names from earlier in the query even where it'd be valid, since it's confusing to read
    /// </summary>
    public bool IsNameFree(string name) => !_allDeclaredNames.Contains(name) && _isNameFreeOutsideQuery(name);

    public void SetScope(IEnumerable<QueryRangeVariable> variables) => _scope = variables.ToList();

    public void AddToScope(QueryRangeVariable variable) => _scope.Add(variable);

    public void AddFrom(QueryRangeVariable variable, TypeSyntax type, ExpressionSyntax collection)
    {
        if (_from == null && _source == null) {
            _from = SyntaxFactory.FromClause(type, DeclareVariableName(variable), collection);
        } else {
            AddClause(SyntaxFactory.FromClause(type, DeclareRangeVariable(variable), collection));
        }
        AddToScope(variable);
    }

    /// <summary>
    /// Adds the variable as a C# range variable, and returns its identifier which may have been renamed to avoid clashes.
    /// </summary>
    public SyntaxToken DeclareRangeVariable(QueryRangeVariable variable)
    {
        EnsureQuery();
        return DeclareVariableName(variable);
    }

    private SyntaxToken DeclareVariableName(QueryRangeVariable variable)
    {
        var identifier = DeclareName(variable.Name, variable.Symbol == null);
        variable.CsExpression = SyntaxFactory.IdentifierName(identifier);
        return identifier;
    }

    /// <summary>
    /// For example the joined variable of a C# join...into goes out of scope
    /// </summary>
    public void RemoveRangeVariable(SyntaxToken identifier) => _csRangeVariableNames.Remove(identifier.ValueText);

    /// <summary>
    /// Makes the variable evaluate to the given expression, either by substituting the expression wherever it's used, or by introducing a let clause.
    /// </summary>
    /// <param name="canInline">True if substituting the expression won't evaluate it more often than VB would</param>
    /// <remarks>An inlined expression is stored without parentheses (they're added where needed when it's substituted), and without source mapping since it moves elsewhere</remarks>
    public void Define(QueryRangeVariable variable, ExpressionSyntax value, bool canInline)
    {
        if (canInline || IsCheapToRepeat(value)) {
            variable.CsExpression = (value is ParenthesizedExpressionSyntax p ? p.Expression : value).WithoutSourceMapping();
        } else {
            AddClause(SyntaxFactory.LetClause(DeclareRangeVariable(variable), value));
        }
    }

    public void AddClause(QueryClauseSyntax clause)
    {
        EnsureQuery();
        _clauses.Add(clause);
    }

    /// <summary>
    /// Ends the current query body with a select or group, and continues the query with a new range variable referring to the result.
    /// </summary>
    public SyntaxToken ContinueInto(SelectOrGroupClauseSyntax selectOrGroup, string preferredName, bool isGeneratedName)
    {
        EnsureQuery();
        _csRangeVariableNames.Clear();
        var into = DeclareName(preferredName, isGeneratedName);
        _completedBodies.Add((_clauses, selectOrGroup, into));
        _clauses = new List<QueryClauseSyntax>();
        return into;
    }

    /// <summary>
    /// Applies a method (e.g. Distinct) to the sequence of elements, the next clause will resume the query from the result
    /// </summary>
    /// <param name="vbClause">Used to place the generated select on the same line as the method call</param>
    public void ApplyMethod(VisualBasicSyntaxNode vbClause, string methodName, params ExpressionSyntax[] arguments)
    {
        var target = Materialize(vbClause);
        var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParenthesizeIfNeeded(target), ValidSyntaxFactory.IdentifierName(methodName));
        _source = SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument))));
        _from = null;
        _completedBodies.Clear();
        _clauses = new List<QueryClauseSyntax>();
        _csRangeVariableNames.Clear();
    }

    /// <summary>
    /// The sequence of VB elements as a C# expression, i.e. the result of the query if it ended here.
    /// </summary>
    public ExpressionSyntax Materialize(VisualBasicSyntaxNode vbSourceMapping = null)
    {
        if (_from == null) return _source;
        var select = SyntaxFactory.SelectClause(CreateElement(Scope));
        return Build(vbSourceMapping == null ? select : select.WithVbSourceMappingFrom(vbSourceMapping));
    }

    public ExpressionSyntax Build(SelectOrGroupClauseSyntax selectOrGroup)
    {
        EnsureQuery();
        if (IsIdentitySelect(selectOrGroup)) return _from.Expression;

        var body = SyntaxFactory.QueryBody(SyntaxFactory.List(_clauses), selectOrGroup, null);
        for (int i = _completedBodies.Count - 1; i >= 0; i--) {
            var (clauses, completedSelectOrGroup, into) = _completedBodies[i];
            body = SyntaxFactory.QueryBody(SyntaxFactory.List(clauses), completedSelectOrGroup, SyntaxFactory.QueryContinuation(into, body));
        }
        return SyntaxFactory.QueryExpression(_from, body);
    }

    /// <summary>
    /// Creates a lambda whose parameter is an element made of the given variables, e.g. for the argument of an aggregate function
    /// </summary>
    public async Task<LambdaExpressionSyntax> CreateElementLambdaAsync(IReadOnlyList<QueryRangeVariable> element, Func<Task<ExpressionSyntax>> convertBody)
    {
        var parameterName = GetElementParameterName(element);
        var originalExpressions = element.Select(v => v.CsExpression).ToArray();
        try {
            ReferToElementVia(element, parameterName);
            var body = await convertBody();
            return SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameterName), body);
        } finally {
            for (int i = 0; i < element.Count; i++) element[i].CsExpression = originalExpressions[i];
        }
    }

    public static ExpressionSyntax CreateElement(IReadOnlyList<QueryRangeVariable> variables) =>
        variables.Count == 1 ? variables[0].CsExpression : CreateAnonymousObject(variables.Select(v => (v.Name, v.CsExpression)));

    public static AnonymousObjectCreationExpressionSyntax CreateAnonymousObject(IEnumerable<(string Name, ExpressionSyntax Expression)> members) =>
        SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(members.Select(m => CreateAnonymousMember(m.Name, m.Expression))));

    public static AnonymousObjectMemberDeclaratorSyntax CreateAnonymousMember(string name, ExpressionSyntax expression)
    {
        var nameEquals = GetInferredMemberName(expression) == name ? null : SyntaxFactory.NameEquals(ValidSyntaxFactory.IdentifierName(name));
        return SyntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
    }

    public static string GetInferredMemberName(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax id => id.Identifier.ValueText,
        MemberAccessExpressionSyntax { Name: var name } => name.Identifier.ValueText,
        _ => null
    };

    /// <summary>
    /// Repeating these is about as cheap as referencing a range variable, so a let clause isn't worthwhile
    /// </summary>
    public static bool IsCheapToRepeat(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax or ThisExpressionSyntax or LiteralExpressionSyntax => true,
        MemberAccessExpressionSyntax mae => IsCheapToRepeat(mae.Expression),
        ParenthesizedExpressionSyntax pe => IsCheapToRepeat(pe.Expression),
        _ => false
    };

    public static ExpressionSyntax ParenthesizeIfNeeded(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax
            or ParenthesizedExpressionSyntax or LiteralExpressionSyntax or ThisExpressionSyntax
            or ObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax => expression,
        _ => expression.AddParens()
    };

    /// <summary>
    /// After a method call boundary there's no query open, so start a new one from the sequence of elements.
    /// Must be called before converting expressions that may reference range variables, since it changes how they're referenced.
    /// </summary>
    public void EnsureQuery()
    {
        if (_from != null || _source == null) return;
        var parameterName = GetElementParameterName(Scope);
        AddName(parameterName.ValueText);
        _from = SyntaxFactory.FromClause(parameterName, _source);
        _source = null;
        ReferToElementVia(Scope, parameterName);
    }

    private SyntaxToken GetElementParameterName(IReadOnlyList<QueryRangeVariable> element) =>
        element.Count == 1 && element[0].Symbol != null
            ? CommonConversions.CsEscapedIdentifier(element[0].Name)
            : CommonConversions.CsEscapedIdentifier(GenerateName(GeneratedVariableName, n => IsNameFree(n) && element.All(v => !string.Equals(v.Name, n, StringComparison.OrdinalIgnoreCase))));

    private static void ReferToElementVia(IReadOnlyList<QueryRangeVariable> element, SyntaxToken parameterName)
    {
        var parameter = SyntaxFactory.IdentifierName(parameterName);
        if (element.Count == 1) {
            element[0].CsExpression = parameter;
        } else {
            foreach (var variable in element) {
                variable.CsExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parameter, ValidSyntaxFactory.IdentifierName(variable.Name));
            }
        }
    }

    /// <param name="isGeneratedName">VB has already checked names it declares don't clash with names outside the query, so they only need checking against the C# range variables</param>
    private SyntaxToken DeclareName(string preferredName, bool isGeneratedName)
    {
        var name = isGeneratedName || _csRangeVariableNames.Contains(preferredName)
            ? GenerateName(preferredName, IsNameFree)
            : preferredName;
        AddName(name);
        return CommonConversions.CsEscapedIdentifier(name);
    }

    private void AddName(string name)
    {
        _csRangeVariableNames.Add(name);
        _allDeclaredNames.Add(name);
    }

    private static string GenerateName(string baseName, Func<string, bool> canUse) => NameGenerator.Generic.GenerateUniqueName(baseName, canUse);

    private bool IsIdentitySelect(SelectOrGroupClauseSyntax selectOrGroup) =>
        _completedBodies.Count == 0 && _clauses.Count == 0 && _from.Type == null &&
        selectOrGroup is SelectClauseSyntax { Expression: IdentifierNameSyntax id } && id.Identifier.ValueText == _from.Identifier.ValueText;
}
