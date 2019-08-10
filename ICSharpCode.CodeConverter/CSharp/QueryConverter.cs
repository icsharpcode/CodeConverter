using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>
    ///  Grammar info: http://kursinfo.himolde.no/in-kurs/IBE150/VBspec.htm#_Toc248253288
    /// </remarks>
    internal class QueryConverter
    {
        private readonly CommentConvertingVisitorWrapper<CSharpSyntaxNode> _triviaConvertingVisitor;

        public QueryConverter(CommonConversions commonConversions, CommentConvertingVisitorWrapper<CSharpSyntaxNode> triviaConvertingVisitor)
        {
            CommonConversions = commonConversions;
            _triviaConvertingVisitor = triviaConvertingVisitor;
        }

        private CommonConversions CommonConversions { get; }

        public CSharpSyntaxNode ConvertClauses(SyntaxList<VBSyntax.QueryClauseSyntax> clauses)
        {
            var vbBodyClauses = new Queue<VBSyntax.QueryClauseSyntax>(clauses);
            var vbFromClause = (VBSyntax.FromClauseSyntax) vbBodyClauses.Dequeue();

            var fromClauseSyntax = ConvertFromClauseSyntax(vbFromClause);
            var querySegments = GetQuerySegments(vbBodyClauses);
            return ConvertQuerySegments(querySegments, fromClauseSyntax);
        }

        /// <summary>
        ///  TODO: Don't bother with reversing, rewrite ConvertQueryWithContinuation to recurse on them the right way around
        /// </summary>
        private IEnumerable<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)> GetQuerySegments(Queue<VBSyntax.QueryClauseSyntax> vbBodyClauses)
        {
            while (vbBodyClauses.Any()) {
                var querySectionsReversed =
                    new Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>();
                while (vbBodyClauses.Any() && !RequiresMethodInvocation(vbBodyClauses.Peek())) {
                    var convertedClauses = new List<CSSyntax.QueryClauseSyntax>();
                    while (vbBodyClauses.Any() && !RequiredContinuation(vbBodyClauses.Peek())) {
                        convertedClauses.Add(ConvertQueryBodyClause(vbBodyClauses.Dequeue()));
                    }

                    var convertQueryBodyClauses = (SyntaxFactory.List(convertedClauses),
                        vbBodyClauses.Any() ? vbBodyClauses.Dequeue() : null);
                    querySectionsReversed.Enqueue(convertQueryBodyClauses);
                }
                yield return (querySectionsReversed, vbBodyClauses.Any() ? vbBodyClauses.Dequeue() : null);
            }
        }

        private CSharpSyntaxNode ConvertQuerySegments(IEnumerable<(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)>, VBSyntax.QueryClauseSyntax)> querySegments, CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            CSSyntax.ExpressionSyntax query = null;
            foreach (var (queryContinuation, queryEnd) in querySegments) {
                query = (CSSyntax.ExpressionSyntax) ConvertQueryWithContinuations(queryContinuation, fromClauseSyntax);
                if (queryEnd == null) return query;
                var queryWithoutTrivia = ConvertQueryToLinq(fromClauseSyntax, queryEnd, query);
                query = _triviaConvertingVisitor.TriviaConverter.PortConvertedTrivia(queryEnd, queryWithoutTrivia);
                fromClauseSyntax = SyntaxFactory.FromClause(fromClauseSyntax.Identifier, query);
            }

            return query ?? throw new ArgumentOutOfRangeException(nameof(querySegments), querySegments, null);
        }

        private CSSyntax.InvocationExpressionSyntax ConvertQueryToLinq(CSSyntax.FromClauseSyntax fromClauseSyntax, VBSyntax.QueryClauseSyntax queryEnd,
            CSSyntax.ExpressionSyntax query)
        {
            var linqMethodName = GetLinqMethodName(queryEnd);
            var parenthesizedQuery = query is CSSyntax.QueryExpressionSyntax ? SyntaxFactory.ParenthesizedExpression(query) : query;
            var linqMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parenthesizedQuery,
                SyntaxFactory.IdentifierName(linqMethodName));
            var linqArguments = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(GetLinqArguments(fromClauseSyntax, queryEnd).Select(SyntaxFactory.Argument)));
            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(linqMethod, linqArguments);
            return invocationExpressionSyntax;
        }

        private CSharpSyntaxNode ConvertQueryWithContinuations(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)> queryContinuation, CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            var subQuery = ConvertQueryWithContinuation(queryContinuation, fromClauseSyntax);
            return subQuery != null ? SyntaxFactory.QueryExpression(fromClauseSyntax, subQuery) : fromClauseSyntax.Expression;
        }

        private CSSyntax.QueryBodySyntax ConvertQueryWithContinuation(Queue<(SyntaxList<CSSyntax.QueryClauseSyntax>, VBSyntax.QueryClauseSyntax)> querySectionsReversed, CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            if (!querySectionsReversed.Any()) return null;
            var (convertedClauses, clauseEnd) = querySectionsReversed.Dequeue();

            var nestedClause = ConvertQueryWithContinuation(querySectionsReversed, fromClauseSyntax);
            return ConvertSubQuery(fromClauseSyntax, clauseEnd, nestedClause, convertedClauses);
        }

        private CSSyntax.QueryBodySyntax ConvertSubQuery(CSSyntax.FromClauseSyntax fromClauseSyntax, VBSyntax.QueryClauseSyntax clauseEnd,
            CSSyntax.QueryBodySyntax nestedClause, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses)
        {
            CSSyntax.SelectOrGroupClauseSyntax selectOrGroup = null;
            CSSyntax.QueryContinuationSyntax queryContinuation = null;
            switch (clauseEnd) {
                case null:
                    selectOrGroup = CreateDefaultSelectClause(fromClauseSyntax);
                    break;
                case VBSyntax.GroupByClauseSyntax gcs:
                    var continuationClauses = SyntaxFactory.List<CSSyntax.QueryClauseSyntax>();
                    if (!gcs.Items.Any()) {
                        var identifierNameSyntax =
                            SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(fromClauseSyntax.Identifier));
                        var letGroupKey = SyntaxFactory.LetClause(GetGroupKeyIdentifiers(gcs).Single(), SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(GetGroupIdentifier(gcs)), SyntaxFactory.IdentifierName("Key")));
                        continuationClauses = continuationClauses.Add(letGroupKey);
                        selectOrGroup = SyntaxFactory.GroupClause(identifierNameSyntax, GetGroupExpression(gcs));
                    } else {
                        var item = (CSSyntax.IdentifierNameSyntax)gcs.Items.Single().Expression.Accept(_triviaConvertingVisitor);
                        var keyExpression = (CSSyntax.ExpressionSyntax)gcs.Keys.Single().Expression.Accept(_triviaConvertingVisitor);
                        selectOrGroup = SyntaxFactory.GroupClause(item, keyExpression);
                    }
                    queryContinuation = CreateGroupByContinuation(gcs, continuationClauses, nestedClause);
                    break;
                case VBSyntax.SelectClauseSyntax scs:
                    selectOrGroup = ConvertSelectClauseSyntax(scs);
                    break;
                default:
                    throw new NotImplementedException($"Clause kind '{clauseEnd.Kind()}' is not yet implemented");
            }

            return SyntaxFactory.QueryBody(convertedClauses, selectOrGroup, queryContinuation);
        }

        private CSSyntax.QueryContinuationSyntax CreateGroupByContinuation(VBSyntax.GroupByClauseSyntax gcs, SyntaxList<CSSyntax.QueryClauseSyntax> convertedClauses, CSSyntax.QueryBodySyntax body)
        {
            var queryBody = SyntaxFactory.QueryBody(convertedClauses, body?.SelectOrGroup, null);
            return SyntaxFactory.QueryContinuation(GetGroupIdentifier(gcs), queryBody);
        }

        private IEnumerable<CSSyntax.ExpressionSyntax> GetLinqArguments(CSSyntax.FromClauseSyntax fromClauseSyntax,
            VBSyntax.QueryClauseSyntax linqQuery)
        {
            switch (linqQuery) {
                case VBSyntax.DistinctClauseSyntax _:
                    return Enumerable.Empty<CSSyntax.ExpressionSyntax>();
                case VBSyntax.PartitionClauseSyntax pcs:
                    return new[] {(CSSyntax.ExpressionSyntax)pcs.Count.Accept(_triviaConvertingVisitor)};
                case VBSyntax.PartitionWhileClauseSyntax pwcs: {
                    var lambdaParam = SyntaxFactory.Parameter(fromClauseSyntax.Identifier);
                    var lambdaBody = (CSSyntax.ExpressionSyntax) pwcs.Condition.Accept(_triviaConvertingVisitor);
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

        private CSSyntax.FromClauseSyntax ConvertFromClauseSyntax(VBSyntax.FromClauseSyntax vbFromClause)
        {
            var collectionRangeVariableSyntax = vbFromClause.Variables.Single();
            var fromClauseSyntax = SyntaxFactory.FromClause(
                CommonConversions.ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
                (CSSyntax.ExpressionSyntax) collectionRangeVariableSyntax.Expression.Accept(_triviaConvertingVisitor));
            return fromClauseSyntax;
        }

        private CSSyntax.SelectClauseSyntax ConvertSelectClauseSyntax(VBSyntax.SelectClauseSyntax vbFromClause)
        {
            var selectedVariables = vbFromClause.Variables.Select(v => {
                    var nameEquals = (CSSyntax.NameEqualsSyntax)v.NameEquals?.Accept(_triviaConvertingVisitor);
                    var expression = (CSSyntax.ExpressionSyntax)v.Expression.Accept(_triviaConvertingVisitor);
                    return SyntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
                }).ToList();

            if (selectedVariables.Count() == 1)
                return SyntaxFactory.SelectClause(selectedVariables.Single().Expression);
            return SyntaxFactory.SelectClause(SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(selectedVariables)));
        }

        /// <summary>
        /// TODO: In the case of multiple Froms and no Select, VB returns an anonymous type containing all the variables created by the from clause
        /// </summary>
        private static CSSyntax.SelectClauseSyntax CreateDefaultSelectClause(CSSyntax.FromClauseSyntax fromClauseSyntax)
        {
            return SyntaxFactory.SelectClause(SyntaxFactory.IdentifierName(fromClauseSyntax.Identifier));
        }

        private CSSyntax.QueryClauseSyntax ConvertQueryBodyClause(VBSyntax.QueryClauseSyntax node)
        {
            return node
                .TypeSwitch<VBSyntax.QueryClauseSyntax, VBSyntax.FromClauseSyntax, VBSyntax.JoinClauseSyntax,
                    VBSyntax.LetClauseSyntax, VBSyntax.OrderByClauseSyntax, VBSyntax.WhereClauseSyntax,
                    CSSyntax.QueryClauseSyntax>(
                    //(VBSyntax.AggregateClauseSyntax ags) => null,
                    ConvertFromClauseSyntax,
                    ConvertJoinClause,
                    ConvertLetClause,
                    ConvertOrderByClause,
                    ConvertWhereClause,
                    _ => throw new NotImplementedException(
                        $"Conversion for query clause with kind '{node.Kind()}' not implemented"));
        }

        private CSSyntax.ExpressionSyntax GetGroupExpression(VBSyntax.GroupByClauseSyntax gs)
        {
            return (CSSyntax.ExpressionSyntax) gs.Keys.Single().Expression.Accept(_triviaConvertingVisitor);
        }

        private SyntaxToken GetGroupIdentifier(VBSyntax.GroupByClauseSyntax gs)
        {
            var name = gs.AggregationVariables.Select(v => v.Aggregation is VBSyntax.FunctionAggregationSyntax f
                    ? f.FunctionName.Text : v.Aggregation is VBSyntax.GroupAggregationSyntax g ? g.GroupKeyword.Text : null)
                .SingleOrDefault(x => x != null) ?? gs.Keys.Select(k => k.NameEquals.Identifier.Identifier.Text).Single();
            return SyntaxFactory.Identifier(name);
        }

        private IEnumerable<string> GetGroupKeyIdentifiers(VBSyntax.GroupByClauseSyntax gs)
        {
            return gs.AggregationVariables
                .Select(x => x.NameEquals?.Identifier.Identifier.Text)
                .Concat(gs.Keys.Select(k => k.NameEquals?.Identifier.Identifier.Text))
                .Where(x => x != null);
        }

        private CSSyntax.QueryClauseSyntax ConvertWhereClause(VBSyntax.WhereClauseSyntax ws)
        {
            return SyntaxFactory.WhereClause((CSSyntax.ExpressionSyntax) ws.Condition.Accept(_triviaConvertingVisitor));
        }

        private CSSyntax.QueryClauseSyntax ConvertLetClause(VBSyntax.LetClauseSyntax ls)
        {
            var singleVariable = ls.Variables.Single();
            return SyntaxFactory.LetClause(CommonConversions.ConvertIdentifier(singleVariable.NameEquals.Identifier.Identifier), (CSSyntax.ExpressionSyntax) singleVariable.Expression.Accept(_triviaConvertingVisitor));
        }

        private CSSyntax.QueryClauseSyntax ConvertOrderByClause(VBSyntax.OrderByClauseSyntax os)
        {
            return SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(os.Orderings.Select(o => (CSSyntax.OrderingSyntax) o.Accept(_triviaConvertingVisitor))));
        }

        private CSSyntax.QueryClauseSyntax ConvertJoinClause(VBSyntax.JoinClauseSyntax js)
        {
            var variable = js.JoinedVariables.Single();
            var joinLhs = SingleExpression(js.JoinConditions.Select(c => c.Left.Accept(_triviaConvertingVisitor))
                .Cast<CSSyntax.ExpressionSyntax>().ToList());
            var joinRhs = SingleExpression(js.JoinConditions.Select(c => c.Right.Accept(_triviaConvertingVisitor))
                .Cast<CSSyntax.ExpressionSyntax>().ToList());
            var convertIdentifier = CommonConversions.ConvertIdentifier(variable.Identifier.Identifier);
            var expressionSyntax = (CSSyntax.ExpressionSyntax) variable.Expression.Accept(_triviaConvertingVisitor);

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