using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>
    ///  Grammar info: http://kursinfo.himolde.no/in-kurs/IBE150/VBspec.htm#_Toc248253288
    /// </remarks>
    internal class QueryConverter
    {
        private readonly CommentConvertingVisitorWrapper _triviaConvertingVisitor;

        public QueryConverter(CommonConversions commonConversions, CommentConvertingVisitorWrapper triviaConvertingVisitor)
        {
            CommonConversions = commonConversions;
            _triviaConvertingVisitor = triviaConvertingVisitor;
        }

        private CommonConversions CommonConversions { get; }

        public async Task<CSharpSyntaxNode> ConvertClauses(SyntaxList<VBSyntax.QueryClauseSyntax> clauses)
        {
            var vbBodyClauses = new Queue<VBSyntax.QueryClauseSyntax>(clauses);
            var vbStartClause = vbBodyClauses.Dequeue();
            var agg = vbStartClause as VBSyntax.AggregateClauseSyntax;
            if (agg != null) {
                foreach (var queryOperators in agg.AdditionalQueryOperators) {
                    vbBodyClauses.Enqueue(queryOperators);
                }
            }
            var fromClauseSyntax = vbStartClause is VBSyntax.FromClauseSyntax fcs ? await ConvertFromClauseSyntax(fcs) : await ConvertAggregateToFromClauseSyntax((VBSyntax.AggregateClauseSyntax) vbStartClause);
            CSharpSyntaxNode rootExpression;
            if (vbBodyClauses.Any()) {
                var querySegments = await GetQuerySegments(vbBodyClauses);
                rootExpression = await ConvertQuerySegments(querySegments, fromClauseSyntax);
            } else {
                rootExpression = fromClauseSyntax.Expression;
            }

            if (agg != null) {
                if (agg.AggregationVariables.Count == 1 &&
                    agg.AggregationVariables.Single().Aggregation is VBSyntax.FunctionAggregationSyntax fas) {
                    if (rootExpression is CSSyntax.QueryExpressionSyntax qes)
                        rootExpression = SyntaxFactory.ParenthesizedExpression(qes);
                    var collectionRangeVariableSyntax = agg.Variables.Single();
                    var toAggregate = await fas.Argument.AcceptAsync(_triviaConvertingVisitor);
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
        private async Task<List<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)>> GetQuerySegments(Queue<VBSyntax.QueryClauseSyntax> vbBodyClauses)
        {
            var querySegments =
                new List<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>,
                    VBSyntax.QueryClauseSyntax)>();
            while (vbBodyClauses.Any()) {
                var querySectionsReversed =
                    new Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>();
                while (vbBodyClauses.Any() && !RequiresMethodInvocation(vbBodyClauses.Peek())) {
                    var convertedClauses = new List<CSSyntax.QueryClauseSyntax>();
                    while (vbBodyClauses.Any() && !RequiredContinuation(vbBodyClauses.Peek())) {
                        convertedClauses.Add(await ConvertQueryBodyClause(vbBodyClauses.Dequeue()));
                    }

                    var convertQueryBodyClauses = (SyntaxFactory.List(convertedClauses),
                        vbBodyClauses.Any() ? vbBodyClauses.Dequeue() : null);
                    querySectionsReversed.Enqueue(convertQueryBodyClauses);
                }
                querySegments.Add((querySectionsReversed, vbBodyClauses.Any() ? vbBodyClauses.Dequeue() : null));
            }
            return querySegments;
        }

        private async Task<CSharpSyntaxNode> ConvertQuerySegments(IEnumerable<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)> querySegments, CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            CSSyntax.ExpressionSyntax query = null;
            foreach (var (queryContinuation, queryEnd) in querySegments) {
                query = (CSSyntax.ExpressionSyntax) await ConvertQueryWithContinuations(queryContinuation, fromClauseSyntax);
                if (queryEnd == null) return query;
                var reusableFromCsId = fromClauseSyntax.Identifier.WithoutSourceMapping();
                query = await ConvertQueryToLinq(reusableFromCsId, queryEnd, query);
                fromClauseSyntax = SyntaxFactory.FromClause(reusableFromCsId, query);
            }

            return query ?? throw new ArgumentOutOfRangeException(nameof(querySegments), querySegments, null);
        }

        private async Task<CSSyntax.InvocationExpressionSyntax> ConvertQueryToLinq(SyntaxToken reusableCsFromId, VBSyntax.QueryClauseSyntax queryEnd,
            CSSyntax.ExpressionSyntax query)
        {
            var linqMethodName = GetLinqMethodName(queryEnd);
            var parenthesizedQuery = query is CSSyntax.QueryExpressionSyntax ? SyntaxFactory.ParenthesizedExpression(query) : query;
            var linqMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parenthesizedQuery,
                SyntaxFactory.IdentifierName(linqMethodName));
            var linqArguments = await GetLinqArguments(reusableCsFromId, queryEnd);
            var linqArgumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(linqArguments.Select(SyntaxFactory.Argument)));
            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(linqMethod, linqArgumentList);
            return invocationExpressionSyntax;
        }

        private async Task<CSharpSyntaxNode> ConvertQueryWithContinuations(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)> queryContinuation, CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            var subQuery = await ConvertQueryWithContinuation(queryContinuation, fromClauseSyntax.Identifier.WithoutSourceMapping());
            return subQuery != null ? SyntaxFactory.QueryExpression(fromClauseSyntax, subQuery) : fromClauseSyntax.Expression;
        }

        private async Task<CSSyntax.QueryBodySyntax> ConvertQueryWithContinuation(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)> querySectionsReversed, SyntaxToken reusableCsFromId)
        {
            if (!querySectionsReversed.Any()) return null;
            var (convertedClauses, clauseEnd) = querySectionsReversed.Dequeue();

            var nestedClause = await ConvertQueryWithContinuation(querySectionsReversed, reusableCsFromId);
            return await ConvertSubQuery(reusableCsFromId, clauseEnd, nestedClause, convertedClauses); ;
        }

        private async Task<CSSyntax.QueryBodySyntax> ConvertSubQuery(SyntaxToken reusableCsFromId, VBSyntax.QueryClauseSyntax clauseEnd,
            CSSyntax.QueryBodySyntax nestedClause, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses)
        {
            CSSyntax.SelectOrGroupClauseSyntax selectOrGroup;
            CSSyntax.QueryContinuationSyntax queryContinuation = null;
            switch (clauseEnd) {
                case null:
                    selectOrGroup = CreateDefaultSelectClause(reusableCsFromId);
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
                        selectOrGroup = SyntaxFactory.GroupClause(identifierNameSyntax, await GetGroupExpression(gcs));
                    } else {
                        var item = (CSSyntax.IdentifierNameSyntax) await gcs.Items.Single().Expression.AcceptAsync(_triviaConvertingVisitor);
                        var keyExpression = (CSSyntax.ExpressionSyntax) await gcs.Keys.Single().Expression.AcceptAsync(_triviaConvertingVisitor);
                        selectOrGroup = SyntaxFactory.GroupClause(item, keyExpression);
                    }
                    queryContinuation = nestedClause != null ? CreateGroupByContinuation(gcs, continuationClauses, nestedClause) : null;
                    break;
                case VBSyntax.SelectClauseSyntax scs:
                    selectOrGroup = await ConvertSelectClauseSyntax(scs);
                    break;
                default:
                    throw new NotImplementedException($"Clause kind '{clauseEnd.Kind()}' is not yet implemented");
            }

            return SyntaxFactory.QueryBody(convertedClauses, selectOrGroup, queryContinuation);
        }

        private CSSyntax.QueryContinuationSyntax CreateGroupByContinuation(VBSyntax.GroupByClauseSyntax gcs, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses, CSSyntax.QueryBodySyntax body)
        {
            var queryBody = convertedClauses.Any() ? SyntaxFactory.QueryBody(convertedClauses, body?.SelectOrGroup, null) : SyntaxFactory.QueryBody(body?.SelectOrGroup);
            SyntaxToken groupName = GetGroupIdentifier(gcs);
            return SyntaxFactory.QueryContinuation(groupName, queryBody);
        }

        private async Task<IEnumerable<CSSyntax.ExpressionSyntax>> GetLinqArguments(SyntaxToken reusableCsFromId,
            VBSyntax.QueryClauseSyntax linqQuery)
        {
            switch (linqQuery) {
                case VBSyntax.DistinctClauseSyntax _:
                    return Enumerable.Empty<CSSyntax.ExpressionSyntax>();
                case VBSyntax.PartitionClauseSyntax pcs:
                    return new[] {(CSSyntax.ExpressionSyntax) await pcs.Count.AcceptAsync(_triviaConvertingVisitor)};
                case VBSyntax.PartitionWhileClauseSyntax pwcs: {
                    var lambdaParam = SyntaxFactory.Parameter(reusableCsFromId);
                    var lambdaBody = (CSSyntax.ExpressionSyntax) await pwcs.Condition.AcceptAsync(_triviaConvertingVisitor);
                    return new[] {(CSSyntax.ExpressionSyntax) SyntaxFactory.SimpleLambdaExpression(lambdaParam, lambdaBody)};
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

        private static bool RequiredContinuation(VBSyntax.QueryClauseSyntax queryClauseSyntax)
        {
            return queryClauseSyntax is VBSyntax.GroupByClauseSyntax
                   || queryClauseSyntax is VBSyntax.SelectClauseSyntax;
        }

        private async Task<CSSyntax.FromClauseSyntax> ConvertFromClauseSyntax(VBSyntax.FromClauseSyntax vbFromClause)
        {
            var collectionRangeVariableSyntax = vbFromClause.Variables.Single();
            var fromClauseSyntax = SyntaxFactory.FromClause(
                CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
                (CSSyntax.ExpressionSyntax) await collectionRangeVariableSyntax.Expression.AcceptAsync(_triviaConvertingVisitor));
            return fromClauseSyntax;
        }

        private async Task<CSSyntax.FromClauseSyntax> ConvertAggregateToFromClauseSyntax(VBSyntax.AggregateClauseSyntax vbAggClause)
        {
            var collectionRangeVariableSyntax = vbAggClause.Variables.Single();
            var fromClauseSyntax = SyntaxFactory.FromClause(
                CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
                (CSSyntax.ExpressionSyntax) await collectionRangeVariableSyntax.Expression.AcceptAsync(_triviaConvertingVisitor));
            return fromClauseSyntax;
        }

        private async Task<CSSyntax.SelectClauseSyntax> ConvertSelectClauseSyntax(VBSyntax.SelectClauseSyntax vbSelectClause)
        {
            var selectedVariables = await vbSelectClause.Variables.SelectAsync(async v => {
                    var nameEquals = (CSSyntax.NameEqualsSyntax) await v.NameEquals.AcceptAsync(_triviaConvertingVisitor);
                    var expression = (CSSyntax.ExpressionSyntax) await v.Expression.AcceptAsync(_triviaConvertingVisitor);
                    return SyntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
                });

            if (selectedVariables.Count() == 1)
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

        private async Task<CSSyntax.QueryClauseSyntax> ConvertQueryBodyClause(VBSyntax.QueryClauseSyntax node)
        {
            return await node
                .TypeSwitch<VBSyntax.QueryClauseSyntax, VBSyntax.FromClauseSyntax, VBSyntax.JoinClauseSyntax,
                    VBSyntax.LetClauseSyntax, VBSyntax.OrderByClauseSyntax, VBSyntax.WhereClauseSyntax,
                    Task<CSSyntax.QueryClauseSyntax>>(
                    //(VBSyntax.AggregateClauseSyntax ags) => null,
                    async syntax => (CSSyntax.QueryClauseSyntax) await ConvertFromClauseSyntax(syntax),
                    ConvertJoinClause,
                    ConvertLetClause,
                    ConvertOrderByClause,
                    ConvertWhereClause,
                    _ => throw new NotImplementedException(
                        $"Conversion for query clause with kind '{node.Kind()}' not implemented"));
        }

        private async Task<CSSyntax.ExpressionSyntax> GetGroupExpression(VBSyntax.GroupByClauseSyntax gs)
        {
            var groupExpressions = (await gs.Keys.SelectAsync(async k => (vb: k.Expression, cs: (CSSyntax.ExpressionSyntax)await k.Expression.AcceptAsync(_triviaConvertingVisitor)))).ToList();
            return (groupExpressions.Count == 1) ? groupExpressions.Single().cs : CreateAnonymousType(groupExpressions);
        }

        private CSSyntax.ExpressionSyntax CreateAnonymousType(List<(ExpressionSyntax vb, CSSyntax.ExpressionSyntax cs)> groupExpressions)
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
            var name = gs.AggregationVariables.Select(v => v.Aggregation is VBSyntax.FunctionAggregationSyntax f
                    ? f.FunctionName : v.Aggregation is VBSyntax.GroupAggregationSyntax g ? v.NameEquals?.Identifier.Identifier : default(SyntaxToken?))
                .SingleOrDefault(x => x != null) ?? gs.Keys.Select(k => k.NameEquals.Identifier.Identifier).Single();
            return CommonConversions.ConvertIdentifier(name);
        }

        private IEnumerable<string> GetGroupKeyIdentifiers(VBSyntax.GroupByClauseSyntax gs)
        {
            return gs.Keys.Select(k => k.NameEquals?.Identifier.Identifier.Text)
                .Where(x => x != null);
        }

        private async Task<CSSyntax.QueryClauseSyntax> ConvertWhereClause(VBSyntax.WhereClauseSyntax ws)
        {
            return SyntaxFactory.WhereClause((CSSyntax.ExpressionSyntax) await ws.Condition.AcceptAsync(_triviaConvertingVisitor));
        }

        private async Task<CSSyntax.QueryClauseSyntax> ConvertLetClause(VBSyntax.LetClauseSyntax ls)
        {
            var singleVariable = ls.Variables.Single();
            return SyntaxFactory.LetClause(CommonConversions.ConvertIdentifier(singleVariable.NameEquals.Identifier.Identifier), (CSSyntax.ExpressionSyntax) await singleVariable.Expression.AcceptAsync(_triviaConvertingVisitor));
        }

        private async Task<CSSyntax.QueryClauseSyntax> ConvertOrderByClause(VBSyntax.OrderByClauseSyntax os)
        {
            var orderingSyntaxs = await os.Orderings.SelectAsync(async o => (CSSyntax.OrderingSyntax) await o.AcceptAsync(_triviaConvertingVisitor));
            return SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(orderingSyntaxs));
        }

        private async Task<CSSyntax.QueryClauseSyntax> ConvertJoinClause(VBSyntax.JoinClauseSyntax js)
        {
            var variable = js.JoinedVariables.Single();
            var joinLhs = SingleExpression(await js.JoinConditions.SelectAsync(async c => (CSSyntax.ExpressionSyntax) await c.Left.AcceptAsync(_triviaConvertingVisitor)));
            var joinRhs = SingleExpression(await js.JoinConditions.SelectAsync(async c => (CSSyntax.ExpressionSyntax) await c.Right.AcceptAsync(_triviaConvertingVisitor)));
            var convertIdentifier = CommonConversions.ConvertIdentifier(variable.Identifier.Identifier);
            var expressionSyntax = (CSSyntax.ExpressionSyntax) await variable.Expression.AcceptAsync(_triviaConvertingVisitor);

            CSSyntax.JoinIntoClauseSyntax joinIntoClauseSyntax = null;
            if (js is VBSyntax.GroupJoinClauseSyntax gjs) {
                joinIntoClauseSyntax = gjs.AggregationVariables
                    .Where(a => a.Aggregation is VBSyntax.GroupAggregationSyntax)
                    .Select(a => SyntaxFactory.JoinIntoClause(CommonConversions.ConvertIdentifier(a.NameEquals.Identifier.Identifier)))
                    .SingleOrDefault();
            }
            return SyntaxFactory.JoinClause(null, convertIdentifier, expressionSyntax, joinLhs, joinRhs, joinIntoClauseSyntax);
        }

        private CSSyntax.ExpressionSyntax SingleExpression(IReadOnlyCollection<CSSyntax.ExpressionSyntax> expressions)
        {
            if (expressions.Count == 1) return expressions.Single();
            return SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(expressions.Select((e, i) =>
                SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals($"key{i}"), e)
            )));
        }
    }
}
