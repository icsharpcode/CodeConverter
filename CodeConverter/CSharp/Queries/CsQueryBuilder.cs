using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Builds a C# query left to right while tracking the VB range variables in <see cref="Scope"/> and the C# expression that evaluates each.
/// </summary>
/// <remarks>
/// C# keeps range variables in scope that VB has dropped, so the element (the value flowing through the query) is always built from <see cref="Scope"/>, never from the C# range variables.
/// See README.md.
/// </remarks>
internal class CsQueryBuilder
{
    private const string GeneratedElementName = "x";
    private readonly Func<string, bool> _isNameFreeOutsideQuery;
    private readonly CsQueryBuilder _enclosingQuery;
    private readonly List<ContinuedBody> _continuedBodies = new();
    private readonly HashSet<string> _rangeVariableNamesInScope = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allDeclaredNames = new(StringComparer.OrdinalIgnoreCase);
    private FromClauseSyntax _from;
    private List<QueryClauseSyntax> _clauses = new();
    private ExpressionSyntax _methodCallResult;
    private List<QueryRangeVariable> _scope = new();
    private ResumedElement _resumedElement;

    private sealed record ContinuedBody(List<QueryClauseSyntax> Clauses, SelectOrGroupClauseSyntax SelectOrGroup, SyntaxToken Into);

    /// <summary>
    /// After resuming from a method call, the element is the from variable itself until the scope changes
    /// </summary>
    private sealed record ResumedElement(List<QueryRangeVariable> Scope, List<ExpressionSyntax> Expressions, IdentifierNameSyntax Identifier);

    /// <param name="isNameFreeOutsideQuery">Whether a generated name would clash with anything visible to the query (e.g. locals) or declared within it (e.g. lambda parameters)</param>
    public CsQueryBuilder(Func<string, bool> isNameFreeOutsideQuery, CsQueryBuilder enclosingQuery)
    {
        _isNameFreeOutsideQuery = isNameFreeOutsideQuery;
        _enclosingQuery = enclosingQuery;
    }

    public IReadOnlyList<QueryRangeVariable> Scope => _scope;

    private bool IsQueryOpen => _from != null;

    /// <remarks>
    /// Names from earlier in the query aren't reused even where it'd be valid, since it's confusing to read
    /// </remarks>
    public bool IsNameFree(string name) => !_allDeclaredNames.Contains(name) && _isNameFreeOutsideQuery(name) && _enclosingQuery?.IsNameFree(name) != false;

    public string GenerateName(string baseName)
    {
        var name = NameGenerator.Generic.GenerateUniqueName(baseName, IsNameFree);
        _allDeclaredNames.Add(name);
        return name;
    }

    public void SetScope(IEnumerable<QueryRangeVariable> variables) => _scope = variables.ToList();

    public void AddToScope(QueryRangeVariable variable) => _scope.Add(variable);

    public void AddFrom(QueryRangeVariable variable, TypeSyntax type, ExpressionSyntax collection)
    {
        var fromClause = SyntaxFactory.FromClause(type, ChooseIdentifier(variable), collection);
        if (IsQueryOpen) {
            _clauses.Add(fromClause);
        } else {
            _from = fromClause;
        }
        AddToScope(variable);
    }

    /// <returns>The identifier, which is only renamed if it clashes</returns>
    public SyntaxToken DeclareRangeVariable(QueryRangeVariable variable) => ChooseIdentifier(variable);

    /// <summary>
    /// Makes each VB variable a member of a single C# range variable, e.g. one joined from a subquery
    /// </summary>
    public SyntaxToken DeclareElementRangeVariable(IReadOnlyList<QueryRangeVariable> element)
    {
        var identifier = ChooseElementIdentifier(element);
        RecordName(identifier.ValueText);
        ReferToElementVia(element, identifier);
        return identifier;
    }

    /// <summary>
    /// For example the joined variable of a C# join...into, which C# then lets you redeclare
    /// </summary>
    public void RemoveRangeVariable(SyntaxToken identifier) => _rangeVariableNamesInScope.Remove(identifier.ValueText);

    /// <param name="canSubstitute">If true, the variable evaluates to the value directly rather than via a let clause</param>
    /// <param name="keepSourceMapping">Only true if a substituted value stays in the same place relative to other clauses, otherwise its comments would be out of order</param>
    public void DefineVariable(QueryRangeVariable variable, ExpressionSyntax value, bool canSubstitute, bool keepSourceMapping)
    {
        if (canSubstitute) {
            // Parentheses are added where needed when it's substituted
            var unparenthesized = value is ParenthesizedExpressionSyntax p ? p.Expression : value;
            variable.CsExpression = keepSourceMapping ? unparenthesized : unparenthesized.WithoutSourceMapping();
        } else {
            AddClause(SyntaxFactory.LetClause(DeclareRangeVariable(variable), value));
        }
    }

    public void AddClause(QueryClauseSyntax clause) => _clauses.Add(clause);

    /// <summary>
    /// Ends the query body with <paramref name="selectOrGroup"/>, leaving only <paramref name="continuationVariable"/> in C# scope
    /// </summary>
    public void ContinueInto(SelectOrGroupClauseSyntax selectOrGroup, QueryRangeVariable continuationVariable)
    {
        _rangeVariableNamesInScope.Clear();
        var into = ChooseIdentifier(continuationVariable);
        _continuedBodies.Add(new ContinuedBody(_clauses, selectOrGroup, into));
        _clauses = new List<QueryClauseSyntax>();
    }

    /// <summary>
    /// For operators with no query clause, e.g. Distinct. The next clause resumes the query from the result.
    /// </summary>
    public void ApplyMethod(string methodName, params ExpressionSyntax[] arguments)
    {
        var target = Materialize();
        var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParenthesizeIfNeeded(target), ValidSyntaxFactory.IdentifierName(methodName));
        _methodCallResult = SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument))));
        _from = null;
        _resumedElement = null;
        _continuedBodies.Clear();
        _clauses = new List<QueryClauseSyntax>();
        _rangeVariableNamesInScope.Clear();
    }

    /// <returns>The sequence of elements, i.e. the result if the query ended here</returns>
    public ExpressionSyntax Materialize()
    {
        if (!IsQueryOpen) return _methodCallResult;
        var select = SyntaxFactory.SelectClause(IsResumedElementUnchanged() ? _resumedElement.Identifier : CreateElement(Scope));
        return BuildQuery(select, canDropIdentitySelect: true);
    }

    public ExpressionSyntax BuildQuery(SelectOrGroupClauseSyntax finalClause) => BuildQuery(finalClause, canDropIdentitySelect: false);

    /// <param name="canDropIdentitySelect">VB adds an identity select to a bare `From x In xs` which can be dropped, but an explicit one changes the result's type</param>
    private ExpressionSyntax BuildQuery(SelectOrGroupClauseSyntax finalClause, bool canDropIdentitySelect)
    {
        if (canDropIdentitySelect && IsIdentitySelect(finalClause)) return _from.Expression;

        var body = SyntaxFactory.QueryBody(SyntaxFactory.List(_clauses), finalClause, null);
        foreach (var continued in Enumerable.Reverse(_continuedBodies)) {
            body = SyntaxFactory.QueryBody(SyntaxFactory.List(continued.Clauses), continued.SelectOrGroup, SyntaxFactory.QueryContinuation(continued.Into, body));
        }
        return WithoutElasticTriviaAfterBraces(SyntaxFactory.QueryExpression(_from, body));
    }

    /// <summary>
    /// The formatter puts a blank line between a closing brace and a following clause separated only by elastic trivia, e.g. after `from x in new[] { 1 }`
    /// </summary>
    private static QueryExpressionSyntax WithoutElasticTriviaAfterBraces(QueryExpressionSyntax query)
    {
        var braceTokensBeforeClauses = query.DescendantTokens()
            .Where(t => t.IsKind(SyntaxKind.CloseBraceToken) && IsFirstTokenOfClause(t.GetNextToken()) && IsOnlyElastic(t.TrailingTrivia) && IsOnlyElastic(t.GetNextToken().LeadingTrivia))
            .ToList();
        var nextTokens = braceTokensBeforeClauses.Select(t => t.GetNextToken()).ToList();
        return query.ReplaceTokens(braceTokensBeforeClauses.Concat(nextTokens), (original, _) =>
            nextTokens.Contains(original) ? original.WithLeadingTrivia(SyntaxTriviaList.Empty) : original.WithTrailingTrivia(SyntaxTriviaList.Empty));
    }

    private static bool IsFirstTokenOfClause(SyntaxToken token) =>
        token.Parent is QueryClauseSyntax or SelectOrGroupClauseSyntax or QueryContinuationSyntax && token.Parent.GetFirstToken() == token;

    private static bool IsOnlyElastic(SyntaxTriviaList trivia) => trivia.All(t => t.HasAnnotation(SyntaxAnnotation.ElasticAnnotation));

    /// <summary>
    /// For example the argument of an aggregate function, which is a lambda over the group's element
    /// </summary>
    public async Task<LambdaExpressionSyntax> CreateElementLambdaAsync(IReadOnlyList<QueryRangeVariable> element, Func<Task<ExpressionSyntax>> convertBody)
    {
        var parameter = ChooseElementIdentifier(element);
        var originalExpressions = element.Select(v => v.CsExpression).ToArray();
        try {
            ReferToElementVia(element, parameter);
            return SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter), await convertBody());
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

    private static string GetInferredMemberName(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax id => id.Identifier.ValueText,
        MemberAccessExpressionSyntax { Name: var name } => name.Identifier.ValueText,
        _ => null
    };

    /// <summary>
    /// About as cheap as referencing a range variable, so a let clause isn't worthwhile
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
            or ObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax { Initializer: not null } or ImplicitArrayCreationExpressionSyntax => expression,
        _ => expression.AddParens()
    };

    /// <summary>
    /// Resumes the query after a method call. Call this before converting each clause (other than one converted to a method call), since it changes how range variables are referenced.
    /// </summary>
    public void EnsureQueryOpen()
    {
        if (IsQueryOpen || _methodCallResult == null) return;
        var identifier = ChooseElementIdentifier(Scope);
        RecordName(identifier.ValueText);
        _from = SyntaxFactory.FromClause(identifier, _methodCallResult);
        _methodCallResult = null;
        ReferToElementVia(Scope, identifier);
        _resumedElement = new ResumedElement(Scope.ToList(), Scope.Select(v => v.CsExpression).ToList(), SyntaxFactory.IdentifierName(identifier));
    }

    /// <remarks>
    /// A single variable keeps its name unless that's still a C# range variable that VB has dropped, since substituted expressions may refer to it
    /// </remarks>
    private SyntaxToken ChooseElementIdentifier(IReadOnlyList<QueryRangeVariable> element)
    {
        var name = element.Count == 1 && element[0].Symbol != null && !IsRangeVariableInScope(element[0].Name)
            ? element[0].Name
            : NameGenerator.Generic.GenerateUniqueName(GeneratedElementName, IsNameFree);
        return CommonConversions.CsEscapedIdentifier(name);
    }

    /// <remarks>
    /// VB has already checked the names it declares don't clash with anything outside the query, but C# keeps range variables in scope that VB doesn't
    /// </remarks>
    private SyntaxToken ChooseIdentifier(QueryRangeVariable variable)
    {
        var isGeneratedName = variable.Symbol == null;
        var name = isGeneratedName || IsRangeVariableInScope(variable.Name)
            ? NameGenerator.Generic.GenerateUniqueName(variable.Name, IsNameFree)
            : variable.Name;
        RecordName(name);
        var identifier = CommonConversions.CsEscapedIdentifier(name);
        variable.CsExpression = SyntaxFactory.IdentifierName(identifier);
        return identifier;
    }

    private bool IsRangeVariableInScope(string name) => _rangeVariableNamesInScope.Contains(name) || _enclosingQuery?.IsRangeVariableInScope(name) == true;

    private void RecordName(string name)
    {
        _rangeVariableNamesInScope.Add(name);
        _allDeclaredNames.Add(name);
    }

    private bool IsResumedElementUnchanged() =>
        _resumedElement != null && _resumedElement.Scope.SequenceEqual(Scope) && _resumedElement.Expressions.SequenceEqual(Scope.Select(v => v.CsExpression));

    private static void ReferToElementVia(IReadOnlyList<QueryRangeVariable> element, SyntaxToken identifier)
    {
        var elementName = SyntaxFactory.IdentifierName(identifier);
        if (element.Count == 1) {
            element[0].CsExpression = elementName;
            return;
        }
        foreach (var variable in element) {
            variable.CsExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, elementName, ValidSyntaxFactory.IdentifierName(variable.Name));
        }
    }

    private bool IsIdentitySelect(SelectOrGroupClauseSyntax selectOrGroup) =>
        _continuedBodies.Count == 0 && _clauses.Count == 0 && _from.Type == null &&
        selectOrGroup is SelectClauseSyntax { Expression: IdentifierNameSyntax id } && id.Identifier.ValueText == _from.Identifier.ValueText;
}
