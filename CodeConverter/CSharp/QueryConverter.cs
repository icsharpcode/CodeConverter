using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <remarks>
///  Grammar info: https://web.archive.org/web/20170715190715/http://kursinfo.himolde.no/in-kurs/IBE150/VBspec.htm#_Toc248253288
/// </remarks>
internal class QueryConverter
{
    private readonly CommentConvertingVisitorWrapper _triviaConvertingVisitor;
    private readonly SemanticModel _semanticModel;
    private static readonly SyntaxAnnotation DefaultSelectAnnotation = new("DefaultSelect");

    public QueryConverter(CommonConversions commonConversions, SemanticModel semanticModel, CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor)
    {
        CommonConversions = commonConversions;
        _semanticModel = semanticModel;
        _triviaConvertingVisitor = triviaConvertingExpressionVisitor;
    }

    private CommonConversions CommonConversions { get; }

    public async Task<CSharpSyntaxNode> ConvertClausesAsync(SyntaxList<VBSyntax.QueryClauseSyntax> clauses)
    {
        var vbBodyClauses = new Queue<VBSyntax.QueryClauseSyntax>(clauses);
        var vbStartClause = vbBodyClauses.Peek();
        CSSyntax.FromClauseSyntax fromClauseSyntaxFromAggregate = null;
        SyntaxToken reusableFromCsId;
        if (vbStartClause is VBSyntax.AggregateClauseSyntax agg) {
            vbBodyClauses.Dequeue();
            foreach (var queryOperators in agg.AdditionalQueryOperators) {
                vbBodyClauses.Enqueue(queryOperators);
            }

            fromClauseSyntaxFromAggregate = await ConvertAggregateToFromClauseSyntaxAsync(agg);
            reusableFromCsId = fromClauseSyntaxFromAggregate.Identifier.WithoutSourceMapping();
        } else if (vbStartClause is VBSyntax.FromClauseSyntax fcs) {
            reusableFromCsId = CommonConversions.ConvertIdentifier(fcs.Variables.First().Identifier.Identifier).WithoutSourceMapping();
            agg = null;
        } else {
            throw new NotImplementedException($"Start clause type '{vbStartClause.GetType()}' not yet implemented");
        }
            
        CSharpSyntaxNode rootExpression;
        if (vbBodyClauses.Any()) {
            var querySegments = await GetQuerySegmentsAsync(vbBodyClauses);
            rootExpression = await ConvertQuerySegmentsAsync(querySegments, reusableFromCsId, fromClauseSyntaxFromAggregate);
        } else {
            rootExpression = fromClauseSyntaxFromAggregate.Expression;
        }

        if (agg != null) {
            if (agg.AggregationVariables.Count == 1 &&
                agg.AggregationVariables.Single().Aggregation is VBSyntax.FunctionAggregationSyntax fas) {
                if (rootExpression is CSSyntax.QueryExpressionSyntax qes)
                    rootExpression = SyntaxFactory.ParenthesizedExpression(qes);
                var collectionRangeVariableSyntax = agg.Variables.Single();
                var toAggregate = await fas.Argument.AcceptAsync<CSharpSyntaxNode>(_triviaConvertingVisitor);
                var methodTocall =
                    SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(fas.FunctionName)); //TODO
                var rootWithMethodCall =
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        (CSSyntax.ExpressionSyntax)rootExpression, methodTocall);
                var parameterSyntax = SyntaxFactory.Parameter(
                    CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier));
                var argumentSyntaxes = toAggregate != null
                    ? new[] {
                        SyntaxFactory.Argument(SyntaxFactory.SimpleLambdaExpression(
                            parameterSyntax, toAggregate))
                    }
                    : Array.Empty<CSSyntax.ArgumentSyntax>();
                var args = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
                var variable = SyntaxFactory.InvocationExpression(rootWithMethodCall, args);
                return variable;
            } else {
                throw new NotImplementedException("Aggregate clause type not implemented");
            }
        }

        return rootExpression;
    }

    /// <summary>
    ///  TODO: Don't bother with reversing, rewrite ConvertQueryWithContinuation to recurse on them the right way around
    /// </summary>
    private async Task<List<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)>> GetQuerySegmentsAsync(Queue<VBSyntax.QueryClauseSyntax> vbBodyClauses)
    {
        var querySegments =
            new List<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>,
                VBSyntax.QueryClauseSyntax)>();
        while (vbBodyClauses.Any()) {
            var querySectionsReversed =
                new Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>();
            while (vbBodyClauses.Any() && !RequiresMethodInvocation(vbBodyClauses.Peek()) && !EndsInSelect(querySectionsReversed)) {
                var convertedClauses = new List<CSSyntax.QueryClauseSyntax>();
                while (IsPartOfSegment(vbBodyClauses)) {
                    convertedClauses.AddRange(await ConvertQueryBodyClauseAsync(vbBodyClauses.Dequeue()));
                }

                var convertQueryBodyClauses = (SyntaxFactory.List(convertedClauses),
                    vbBodyClauses.Any() && !RequiresMethodInvocation(vbBodyClauses.Peek()) ? vbBodyClauses.Dequeue() : null);
                querySectionsReversed.Enqueue(convertQueryBodyClauses);
            }
            querySegments.Add((querySectionsReversed, vbBodyClauses.Any() && !EndsInSelect(querySectionsReversed) ? vbBodyClauses.Dequeue() : null));
        }
        return querySegments;
    }

    private static bool EndsInSelect(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, QueryClauseSyntax)> querySectionsReversed) =>
        querySectionsReversed.LastOrDefault().Item2 is VBSyntax.SelectClauseSyntax;

    private static bool IsPartOfSegment(Queue<QueryClauseSyntax> vbBodyClauses) =>
        vbBodyClauses.Any() && !RequiredContinuation(vbBodyClauses) && !RequiresMethodInvocation(vbBodyClauses.Peek());

    private static bool RequiredContinuation(Queue<QueryClauseSyntax> vbBodyClauses) =>
        RequiredContinuation(vbBodyClauses.Peek(), vbBodyClauses.Count - 1);

    private async Task<CSharpSyntaxNode> ConvertQuerySegmentsAsync(IEnumerable<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)> querySegments, SyntaxToken reusableFromCsId, CSSyntax.FromClauseSyntax fromClauseSyntax = null)
    {
        CSSyntax.ExpressionSyntax query = null;
        foreach (var (queryContinuation, queryEnd) in querySegments) {
            var subQuery = await ConvertQueryWithContinuationAsync(queryContinuation, reusableFromCsId);
            if (fromClauseSyntax == null) {
                fromClauseSyntax = subQuery.Clauses.OfType<CSSyntax.FromClauseSyntax>().First();
                subQuery = subQuery.WithClauses(subQuery.Clauses.Remove(fromClauseSyntax));
            }

            // e.g. `from x in xs select x` is not useful, so just use `xs` directly
            bool isUsefulQuery = subQuery is not null && (!subQuery.SelectOrGroup.HasAnnotation(DefaultSelectAnnotation) || subQuery.Clauses.Any());
            query = isUsefulQuery ? SyntaxFactory.QueryExpression(fromClauseSyntax, subQuery) : fromClauseSyntax.Expression;

            if (queryEnd is not null) {
                query = await ConvertQueryToLinqAsync(reusableFromCsId, queryEnd, query);
            }
            fromClauseSyntax = SyntaxFactory.FromClause(reusableFromCsId, query);
        }

        return query ?? throw new ArgumentOutOfRangeException(nameof(querySegments), querySegments, null);
    }

    private async Task<CSSyntax.InvocationExpressionSyntax> ConvertQueryToLinqAsync(SyntaxToken reusableCsFromId, VBSyntax.QueryClauseSyntax queryEnd,
        CSSyntax.ExpressionSyntax query)
    {
        var linqMethodName = GetLinqMethodName(queryEnd);
        var parenthesizedQuery = query is CSSyntax.QueryExpressionSyntax ? SyntaxFactory.ParenthesizedExpression(query) : query;
        var linqMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parenthesizedQuery,
            SyntaxFactory.IdentifierName(linqMethodName));
        var linqArguments = await GetLinqArgumentsAsync(reusableCsFromId, queryEnd);
        var linqArgumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(linqArguments.Select(SyntaxFactory.Argument)));
        var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(linqMethod, linqArgumentList);
        return invocationExpressionSyntax;
    }

    private async Task<CSSyntax.QueryBodySyntax> ConvertQueryWithContinuationAsync(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)> querySectionsReversed, SyntaxToken reusableCsFromId)
    {
        if (!querySectionsReversed.Any()) return null;
        var (convertedClauses, clauseEnd) = querySectionsReversed.Dequeue();
        var nestedClause = await ConvertQueryWithContinuationAsync(querySectionsReversed, reusableCsFromId);
        var convertSubQueryAsync = await ConvertSubQueryAsync(reusableCsFromId, clauseEnd, nestedClause, convertedClauses);
        return convertSubQueryAsync;
    }

    private async Task<CSSyntax.QueryBodySyntax> ConvertSubQueryAsync(SyntaxToken reusableCsFromId, VBSyntax.QueryClauseSyntax clauseEnd,
        CSSyntax.QueryBodySyntax nestedClause, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses)
    {
        CSSyntax.SelectOrGroupClauseSyntax selectOrGroup;
        CSSyntax.QueryContinuationSyntax queryContinuation = null;
        switch (clauseEnd) {
            case null:
                selectOrGroup = CreateDefaultSelectClause(reusableCsFromId).WithAdditionalAnnotations(DefaultSelectAnnotation);
                break;
            case VBSyntax.GroupByClauseSyntax gcs:
                var groupKeyIds = GetGroupKeyIdentifiers(gcs).ToList();

                var continuationClauses = SyntaxFactory.List<CSSyntax.QueryClauseSyntax>();
                if (groupKeyIds.Count == 1) {
                    var letGroupKey = SyntaxFactory.LetClause(groupKeyIds.First(), SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(GetGroupIdentifier(gcs)), SyntaxFactory.IdentifierName("Key")));
                    continuationClauses = continuationClauses.Add(letGroupKey);
                }
                if (!gcs.Items.Any()) {
                    var identifierNameSyntax =
                        SyntaxFactory.IdentifierName(reusableCsFromId);
                    selectOrGroup = SyntaxFactory.GroupClause(identifierNameSyntax, await GetGroupExpressionAsync(gcs));
                } else {
                    var item = await gcs.Items.Single().Expression.AcceptAsync<CSSyntax.IdentifierNameSyntax>(_triviaConvertingVisitor);
                    var keyExpression = await gcs.Keys.Single().Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor);
                    selectOrGroup = SyntaxFactory.GroupClause(item, keyExpression);
                }
                queryContinuation = nestedClause != null ? CreateGroupByContinuation(gcs, continuationClauses, nestedClause.SelectOrGroup) : null;
                break;
            case VBSyntax.SelectClauseSyntax scs:
                selectOrGroup = await ConvertSelectClauseSyntaxAsync(scs);
                break;
            default:
                throw new NotImplementedException($"Clause kind '{clauseEnd.Kind()}' is not yet implemented");
        }

        return SyntaxFactory.QueryBody(convertedClauses, selectOrGroup, queryContinuation);
    }

    private CSSyntax.QueryContinuationSyntax CreateGroupByContinuation(VBSyntax.GroupByClauseSyntax gcs, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses, CSSyntax.SelectOrGroupClauseSyntax selectOrGroupClauseSyntax)
    {
        var queryBody = convertedClauses.Any() ? SyntaxFactory.QueryBody(convertedClauses, selectOrGroupClauseSyntax, null) : SyntaxFactory.QueryBody(selectOrGroupClauseSyntax);
        SyntaxToken groupName = GetGroupIdentifier(gcs);
        return SyntaxFactory.QueryContinuation(groupName, queryBody);
    }

    private async Task<IEnumerable<CSSyntax.ExpressionSyntax>> GetLinqArgumentsAsync(SyntaxToken reusableCsFromId,
        VBSyntax.QueryClauseSyntax linqQuery)
    {
        switch (linqQuery) {
            case VBSyntax.DistinctClauseSyntax _:
                return Enumerable.Empty<CSSyntax.ExpressionSyntax>();
            case VBSyntax.PartitionClauseSyntax pcs:
                return new[] {await pcs.Count.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor)};
            case VBSyntax.PartitionWhileClauseSyntax pwcs: {
                var lambdaParam = SyntaxFactory.Parameter(reusableCsFromId);
                var lambdaBody = await pwcs.Condition.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor);
                return new CSSyntax.ExpressionSyntax[] {SyntaxFactory.SimpleLambdaExpression(lambdaParam, lambdaBody)};
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(linqQuery), linqQuery.Kind(), null);
        }
    }

    private static string GetLinqMethodName(VBSyntax.QueryClauseSyntax queryEnd)
    {
        switch (queryEnd) {
            case VBSyntax.DistinctClauseSyntax _:
                return nameof(Enumerable.Distinct);
            case VBSyntax.PartitionClauseSyntax pcs:
                return pcs.SkipOrTakeKeyword.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SkipKeyword) ? nameof(Enumerable.Skip) : nameof(Enumerable.Take);
            case VBSyntax.PartitionWhileClauseSyntax pwcs:
                return pwcs.SkipOrTakeKeyword.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SkipKeyword) ? nameof(Enumerable.SkipWhile) : nameof(Enumerable.TakeWhile);
            default:
                throw new ArgumentOutOfRangeException(nameof(queryEnd), queryEnd.Kind(), null);
        }
    }

    private static bool RequiresMethodInvocation(VBSyntax.QueryClauseSyntax queryClauseSyntax)
    {
        return queryClauseSyntax is VBSyntax.PartitionClauseSyntax
               || queryClauseSyntax is VBSyntax.PartitionWhileClauseSyntax
               || queryClauseSyntax is VBSyntax.DistinctClauseSyntax;
    }

    /// <summary>
    /// In VB, multiple selects work like Let clauses, but the last one needs to become the actual select (its name is discarded)
    /// </summary>
    private static bool RequiredContinuation(VBSyntax.QueryClauseSyntax queryClauseSyntax, int clausesAfter) => queryClauseSyntax is VBSyntax.GroupByClauseSyntax
                                                                                                                || queryClauseSyntax is VBSyntax.SelectClauseSyntax sc && (sc.Variables.Any(v => v.NameEquals is null) || clausesAfter == 0);

    private async Task<IEnumerable<CSSyntax.FromClauseSyntax>> ConvertFromClauseSyntaxAsync(VBSyntax.FromClauseSyntax vbFromClause) => await vbFromClause.Variables.SelectAsync(ConvertFromClauseVariableAsync);

    private async Task<CSSyntax.FromClauseSyntax> ConvertFromClauseVariableAsync(CollectionRangeVariableSyntax collectionRangeVariableSyntax)
    {
        var expression = await collectionRangeVariableSyntax.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor);
        var parentOperation = _semanticModel.GetOperation(collectionRangeVariableSyntax.Expression)?.Parent;
        if (parentOperation != null && parentOperation.IsImplicit && parentOperation is IInvocationOperation io &&
            io.TargetMethod.MethodKind == MethodKind.ReducedExtension && io.TargetMethod.Name == nameof(Enumerable.AsEnumerable)) {
            expression = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(expression, io.TargetMethod.Name), SyntaxFactory.ArgumentList());
        }
        var fromClauseSyntax = SyntaxFactory.FromClause(
            CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
            expression);
        return fromClauseSyntax;
    }

    private async Task<CSSyntax.FromClauseSyntax> ConvertAggregateToFromClauseSyntaxAsync(VBSyntax.AggregateClauseSyntax vbAggClause)
    {
        var collectionRangeVariableSyntax = vbAggClause.Variables.Single();
        var fromClauseSyntax = SyntaxFactory.FromClause(
            CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
            await collectionRangeVariableSyntax.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));
        return fromClauseSyntax;
    }

    private async Task<CSSyntax.SelectClauseSyntax> ConvertSelectClauseSyntaxAsync(VBSyntax.SelectClauseSyntax vbSelectClause)
    {
        var selectedVariables = await vbSelectClause.Variables.SelectAsync(async v => {
            var nameEquals = await v.NameEquals.AcceptAsync<CSSyntax.NameEqualsSyntax>(_triviaConvertingVisitor);
            var expression = await v.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor);
            return SyntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
        });

        if (selectedVariables.Length == 1)
            return SyntaxFactory.SelectClause(selectedVariables.Single().Expression);
        return SyntaxFactory.SelectClause(SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(selectedVariables)));
    }

    /// <summary>
    /// TODO: In the case of multiple Froms and no Select, VB returns an anonymous type containing all the variables created by the from clause
    /// </summary>
    private static CSSyntax.SelectClauseSyntax CreateDefaultSelectClause(SyntaxToken reusableCsFromId)
    {
        return SyntaxFactory.SelectClause(SyntaxFactory.IdentifierName(reusableCsFromId));
    }

    private async Task<IEnumerable<CSSyntax.QueryClauseSyntax>> ConvertQueryBodyClauseAsync(VBSyntax.QueryClauseSyntax node)
    {
        return node switch {
            VBSyntax.FromClauseSyntax x => await ConvertFromClauseSyntaxAsync(x),
            VBSyntax.JoinClauseSyntax x => await ConvertJoinClauseAsync(x).YieldAsync(),
            VBSyntax.SelectClauseSyntax x => await ConvertSelectClauseAsync(x).YieldAsync(),
            VBSyntax.LetClauseSyntax x => await ConvertLetClauseAsync(x).YieldAsync(),
            VBSyntax.OrderByClauseSyntax x => await ConvertOrderByClauseAsync(x).YieldAsync(),
            VBSyntax.WhereClauseSyntax x => await ConvertWhereClauseAsync(x).YieldAsync(),
            _ => throw new NotImplementedException($"Conversion for query clause with kind '{node.Kind()}' not implemented")
        };
    }

    private async Task<CSSyntax.ExpressionSyntax> GetGroupExpressionAsync(VBSyntax.GroupByClauseSyntax gs)
    {
        var groupExpressions = (await gs.Keys.SelectAsync(async k => (vb: k.Expression, cs: await k.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor)))).ToList();
        return (groupExpressions.Count == 1) ? groupExpressions.Single().cs : CreateAnonymousType(groupExpressions);
    }

    private static CSSyntax.ExpressionSyntax CreateAnonymousType(List<(ExpressionSyntax vb, CSSyntax.ExpressionSyntax cs)> groupExpressions)
    {
        return SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(groupExpressions.Select(CreateAnonymousMember)));
    }

    private static CSSyntax.AnonymousObjectMemberDeclaratorSyntax CreateAnonymousMember((ExpressionSyntax vb, CSSyntax.ExpressionSyntax cs) expr, int i)
    {
        var name = SyntaxFactory.Identifier(expr.vb.ExtractAnonymousTypeMemberName()?.Text ?? ("key" + i));
        return SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)), expr.cs);
    }

    private SyntaxToken GetGroupIdentifier(VBSyntax.GroupByClauseSyntax gs)
    {
        if (!gs.Items.Any()) return CommonConversions.CsEscapedIdentifier("Group");
        var name = gs.AggregationVariables.Select(v => v.Aggregation switch
            {
                VBSyntax.FunctionAggregationSyntax f => f.FunctionName,
                VBSyntax.GroupAggregationSyntax => v.NameEquals?.Identifier.Identifier,
                _ => default
            }).SingleOrDefault(x => x != null) ?? gs.Keys.Select(k => k.NameEquals.Identifier.Identifier).Single();
        return CommonConversions.ConvertIdentifier(name);
    }

    private static IEnumerable<string> GetGroupKeyIdentifiers(VBSyntax.GroupByClauseSyntax gs)
    {
        return gs.Keys.Select(k => k.NameEquals?.Identifier.Identifier.Text)
            .Where(x => x != null);
    }

    private async Task<CSSyntax.QueryClauseSyntax> ConvertWhereClauseAsync(VBSyntax.WhereClauseSyntax ws)
    {
        return SyntaxFactory.WhereClause(await ws.Condition.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));
    }

    private async Task<CSSyntax.QueryClauseSyntax> ConvertSelectClauseAsync(VBSyntax.SelectClauseSyntax sc)
    {
        var singleVariable = sc.Variables.Single();
        var identifier = CommonConversions.ConvertIdentifier(singleVariable.NameEquals.Identifier.Identifier);
        return SyntaxFactory.LetClause(identifier, await singleVariable.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));
    }

    private async Task<CSSyntax.QueryClauseSyntax> ConvertLetClauseAsync(VBSyntax.LetClauseSyntax ls)
    {
        var singleVariable = ls.Variables.Single();
        var identifier = CommonConversions.ConvertIdentifier(singleVariable.NameEquals.Identifier.Identifier);
        return SyntaxFactory.LetClause(identifier, await singleVariable.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));
    }

    private async Task<CSSyntax.QueryClauseSyntax> ConvertOrderByClauseAsync(VBSyntax.OrderByClauseSyntax os)
    {
        var orderingSyntaxs = await os.Orderings.SelectAsync(async o => await o.AcceptAsync<CSSyntax.OrderingSyntax>(_triviaConvertingVisitor));
        return SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(orderingSyntaxs));
    }

    private async Task<CSSyntax.QueryClauseSyntax> ConvertJoinClauseAsync(VBSyntax.JoinClauseSyntax js)
    {
        var variable = js.JoinedVariables.Single();
        var convertIdentifier = CommonConversions.ConvertIdentifier(variable.Identifier.Identifier);

        var joinLhsExpressions = await js.JoinConditions.SelectAsync(async c =>
            await c.Left.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));

        var joinRhsExpressions = await js.JoinConditions.SelectAsync(async c =>
            await c.Right.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor));

        var (lhsAnonymousExpression, rhsAnonymousExpression) = CreateJoinAnonymousObjectKeys(joinLhsExpressions
            .Zip(joinRhsExpressions, (lhs, rhs) => (Lhs: lhs, Rhs: rhs))
            .ToList(), convertIdentifier);

        var expressionSyntax = await variable.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(_triviaConvertingVisitor);

        CSSyntax.JoinIntoClauseSyntax joinIntoClauseSyntax = null;
        if (js is VBSyntax.GroupJoinClauseSyntax gjs) {
            joinIntoClauseSyntax = gjs.AggregationVariables
                .Where(a => a.Aggregation is VBSyntax.GroupAggregationSyntax)
                .Select(a => SyntaxFactory.JoinIntoClause(CommonConversions.ConvertIdentifier(a.NameEquals.Identifier.Identifier)))
                .SingleOrDefault();
        }

        return SyntaxFactory.JoinClause(null, convertIdentifier, expressionSyntax, lhsAnonymousExpression,
            rhsAnonymousExpression, joinIntoClauseSyntax);
    }

    private static (CSSyntax.ExpressionSyntax Lhs, CSSyntax.ExpressionSyntax Rhs) CreateJoinAnonymousObjectKeys(IEnumerable<(CSSyntax.ExpressionSyntax Lhs, CSSyntax.ExpressionSyntax Rhs)> expressions,
        SyntaxToken convertIdentifier)
    {
        // C# enforces specific ordering of range variables around the equals token inside a join clause (CS1937)
        var swappedExpressions = expressions
            .Select(expression => {
                return expression.Lhs switch
                {
                    CSSyntax.MemberAccessExpressionSyntax mac => mac.Expression is not CSSyntax.IdentifierNameSyntax idNameSyntax ||
                                                                 idNameSyntax.Identifier.ValueText != convertIdentifier.ValueText
                        ? expression
                        : SwapExpressions(expression),


                    CSSyntax.IdentifierNameSyntax idName => idName.Identifier.ValueText != convertIdentifier.ValueText
                        ? expression
                        : SwapExpressions(expression),

                    _ => throw new NotImplementedException($"Conversion for join query clause with condition of kind '{expression.Lhs.Kind()}' not implemented")
                };
            })
            .ToList();

        if (swappedExpressions.Count == 1) return swappedExpressions.Single();

        static CSSyntax.AnonymousObjectCreationExpressionSyntax AnonCreateFunc(
            IEnumerable<(CSSyntax.ExpressionSyntax Lhs, CSSyntax.ExpressionSyntax Rhs)> exp, bool isLhs)
        {
            var keySeparatedList = SyntaxFactory.SeparatedList(exp.Select((se, i) =>
            {
                var keyNameEquals = SyntaxFactory.NameEquals($"key{i}");
                var anonObjectDeclarator = SyntaxFactory.AnonymousObjectMemberDeclarator(keyNameEquals, isLhs ? se.Lhs : se.Rhs);

                return anonObjectDeclarator;
            }));

            var anonObjectExpression = SyntaxFactory.AnonymousObjectCreationExpression(keySeparatedList);

            return anonObjectExpression;
        }

        var lhsAnonymousObjectExpressions = AnonCreateFunc(swappedExpressions, true);
        var rhsAnonymousObjectExpressions = AnonCreateFunc(swappedExpressions, false);

        return (lhsAnonymousObjectExpressions, rhsAnonymousObjectExpressions);
    }

    /// <summary>
    /// This method swaps the lhs and rhs of <paramref name="expression"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private static (CSSyntax.ExpressionSyntax Lhs, CSSyntax.ExpressionSyntax Rhs) SwapExpressions(
        (CSSyntax.ExpressionSyntax Lhs, CSSyntax.ExpressionSyntax Rhs) expression)
    {
        var (lhs, rhs) = expression;
        expression.Lhs = rhs;
        expression.Rhs = lhs;

        return expression;
    }
}