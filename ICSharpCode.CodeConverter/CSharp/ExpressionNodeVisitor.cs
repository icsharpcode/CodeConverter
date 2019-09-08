using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using IOperation = Microsoft.CodeAnalysis.IOperation;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// To understand the difference between how expressions are expressed, compare:
    /// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Binder/Binder_Expressions.cs,365
    /// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.VisualBasic/Binding/Binder_Expressions.vb,43
    /// 
    /// </summary>
    internal class ExpressionNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
    {
        private static readonly Type ConvertType = typeof(Microsoft.VisualBasic.CompilerServices.Conversions);
        public CommentConvertingVisitorWrapper<CSharpSyntaxNode> TriviaConvertingVisitor { get; }
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly bool _optionCompareText = false;
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
        private readonly Stack<ExpressionSyntax> _withBlockLhs = new Stack<ExpressionSyntax>();
        private readonly AdditionalLocals _additionalLocals;
        private readonly MethodsWithHandles _methodsWithHandles;
        private readonly QueryConverter _queryConverter;
        private readonly Dictionary<ITypeSymbol, string> _convertMethodsLookupByReturnType;
        private readonly Compilation _csCompilation;
        private readonly LambdaConverter _lambdaConverter;

        public ExpressionNodeVisitor(SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison, AdditionalLocals additionalLocals, Compilation csCompilation, MethodsWithHandles methodsWithHandles, CommonConversions commonConversions, TriviaConverter triviaConverter, HashSet<string> extraUsingDirectives)
        {
            CommonConversions = commonConversions;
            _semanticModel = semanticModel;
            _lambdaConverter = new LambdaConverter(commonConversions, semanticModel);
            _visualBasicEqualityComparison = visualBasicEqualityComparison;
            _additionalLocals = additionalLocals;
            TriviaConvertingVisitor = new CommentConvertingVisitorWrapper<CSharpSyntaxNode>(this, triviaConverter);
            _queryConverter = new QueryConverter(commonConversions, TriviaConvertingVisitor);
            _csCompilation = csCompilation;
            _methodsWithHandles = methodsWithHandles;
            _extraUsingDirectives = extraUsingDirectives;
            _convertMethodsLookupByReturnType = CreateConvertMethodsLookupByReturnType(semanticModel);
        }

        private static Dictionary<ITypeSymbol, string> CreateConvertMethodsLookupByReturnType(SemanticModel semanticModel)
        {
            var systemDotConvert = ConvertType.FullName;
            var convertMethods = semanticModel.Compilation.GetTypeByMetadataName(systemDotConvert).GetMembers().Where(m =>
                m.Name.StartsWith("To", StringComparison.Ordinal) && m.GetParameters().Length == 1);
            var methodsByType = convertMethods
                .GroupBy(m => new { ReturnType = m.GetReturnType(), Name = $"{systemDotConvert}.{m.Name}" })
                .ToDictionary(m => m.Key.ReturnType, m => m.Key.Name);
            return methodsByType;
        }

        public CommonConversions CommonConversions { get; }

        public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        public override async Task<CSharpSyntaxNode> VisitXmlElement(VBasic.Syntax.XmlElementSyntax node)
        {
            _extraUsingDirectives.Add("System.Xml.Linq");
            var aggregatedContent = node.Content.Select(n => n.ToString()).Aggregate(String.Empty, (a, b) => a + b);
            var xmlAsString = $"{node.StartTag}{aggregatedContent}{node.EndTag}".Trim();
            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("XElement"),
                        SyntaxFactory.IdentifierName("Parse")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(xmlAsString))))));
        }

        public override async Task<CSharpSyntaxNode> VisitGetTypeExpression(VBasic.Syntax.GetTypeExpressionSyntax node)
        {
            return SyntaxFactory.TypeOfExpression((TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitGlobalName(VBasic.Syntax.GlobalNameSyntax node)
        {
            return SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
        }

        public override async Task<CSharpSyntaxNode> VisitAwaitExpression(VBasic.Syntax.AwaitExpressionSyntax node)
        {
            return SyntaxFactory.AwaitExpression((ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitCatchBlock(VBasic.Syntax.CatchBlockSyntax node)
        {
            var stmt = node.CatchStatement;
            CatchDeclarationSyntax catcher;
            if (stmt.IdentifierName == null)
                catcher = null;
            else {
                var typeInfo = _semanticModel.GetTypeInfo(stmt.IdentifierName).Type;
                catcher = SyntaxFactory.CatchDeclaration(
                    SyntaxFactory.ParseTypeName(typeInfo.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart)),
                    ConvertIdentifier(stmt.IdentifierName.Identifier)
                );
            }

            var filter = (CatchFilterClauseSyntax) await stmt.WhenClause.AcceptAsync(TriviaConvertingVisitor);
            var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
            var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
            return SyntaxFactory.CatchClause(
                catcher,
                filter,
                SyntaxFactory.Block(stmts)
            );
        }

        public override async Task<CSharpSyntaxNode> VisitCatchFilterClause(VBasic.Syntax.CatchFilterClauseSyntax node)
        {
            return SyntaxFactory.CatchFilterClause((ExpressionSyntax) await node.Filter.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitFinallyBlock(VBasic.Syntax.FinallyBlockSyntax node)
        {
            var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
            var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
            return SyntaxFactory.FinallyClause(SyntaxFactory.Block(stmts));
        }

        public override async Task<CSharpSyntaxNode> VisitCTypeExpression(VBasic.Syntax.CTypeExpressionSyntax node)
        {
            var convertMethodForKeywordOrNull = GetConvertMethodForKeywordOrNull(node.Type);
            return await ConvertCastExpression(node, convertMethodForKeywordOrNull);
        }

        public override async Task<CSharpSyntaxNode> VisitDirectCastExpression(VBasic.Syntax.DirectCastExpressionSyntax node)
        {
            return await ConvertCastExpression(node);
        }

        public override async Task<CSharpSyntaxNode> VisitPredefinedCastExpression(VBasic.Syntax.PredefinedCastExpressionSyntax node)
        {
            var expressionSyntax = (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor);
            if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.CDateKeyword)) {

                _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
                return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Conversions.ToDate"), SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(expressionSyntax))));
            }

            var convertMethodForKeywordOrNull = GetConvertMethodForKeywordOrNull(node);

            return convertMethodForKeywordOrNull != null ? (ExpressionSyntax)
                SyntaxFactory.InvocationExpression(convertMethodForKeywordOrNull,
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(expressionSyntax)))
                ) // Hopefully will be a compile error if it's wrong
                : ValidSyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(node.Keyword.ConvertToken()), (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitTryCastExpression(VBasic.Syntax.TryCastExpressionSyntax node)
        {
            return VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(node, SyntaxFactory.BinaryExpression(
                SyntaxKind.AsExpression,
                (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor),
                (TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor)
            ));
        }

        public override async Task<CSharpSyntaxNode> VisitLiteralExpression(VBasic.Syntax.LiteralExpressionSyntax node)
        {
            var convertedType = _semanticModel.GetTypeInfo(node).ConvertedType;
            if (node.Token.Value == null) {
                if (convertedType == null) {
                    return CommonConversions.Literal(null); //In future, we'll be able to just say "default" instead of guessing at "null" in this case
                }

                return !convertedType.IsReferenceType ? SyntaxFactory.DefaultExpression(CommonConversions.GetTypeSyntax(convertedType)) : CommonConversions.Literal(null);
            } else if (TypeConversionAnalyzer.ConvertStringToCharLiteral(node, convertedType, out char chr)) {
                return CommonConversions.Literal(chr);
            }
            return CommonConversions.Literal(node.Token.Value, node.Token.Text);
        }

        public override async Task<CSharpSyntaxNode> VisitInterpolation(VBasic.Syntax.InterpolationSyntax node)
        {
            return SyntaxFactory.Interpolation((ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor), (InterpolationAlignmentClauseSyntax) await node.AlignmentClause.AcceptAsync(TriviaConvertingVisitor), (InterpolationFormatClauseSyntax) await node.FormatClause.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitInterpolatedStringExpression(VBasic.Syntax.InterpolatedStringExpressionSyntax node)
        {
            var useVerbatim = node.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
            var startToken = useVerbatim ?
                SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedVerbatimStringStartToken, "$@\"", "$@\"", default(SyntaxTriviaList))
                : SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringStartToken, "$\"", "$\"", default(SyntaxTriviaList));
            var contents = await node.Contents.SelectAsync(async c => (InterpolatedStringContentSyntax) await c.AcceptAsync(TriviaConvertingVisitor));
            InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax = SyntaxFactory.InterpolatedStringExpression(startToken, SyntaxFactory.List(contents), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
            return interpolatedStringExpressionSyntax;
        }

        public override async Task<CSharpSyntaxNode> VisitInterpolatedStringText(VBasic.Syntax.InterpolatedStringTextSyntax node)
        {
            var useVerbatim = node.Parent.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
            var textForUser = CommonConversions.EscapeQuotes(node.TextToken.Text, node.TextToken.ValueText, useVerbatim);
            InterpolatedStringTextSyntax interpolatedStringTextSyntax = SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringTextToken, textForUser, node.TextToken.ValueText, default(SyntaxTriviaList)));
            return interpolatedStringTextSyntax;
        }

        public override async Task<CSharpSyntaxNode> VisitInterpolationAlignmentClause(VBasic.Syntax.InterpolationAlignmentClauseSyntax node)
        {
            return SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), (ExpressionSyntax) await node.Value.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitInterpolationFormatClause(VBasic.Syntax.InterpolationFormatClauseSyntax node)
        {
            SyntaxToken formatStringToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, node.FormatStringToken.Text, node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
            return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatStringToken);
        }

        public override async Task<CSharpSyntaxNode> VisitMeExpression(VBasic.Syntax.MeExpressionSyntax node)
        {
            return SyntaxFactory.ThisExpression();
        }

        public override async Task<CSharpSyntaxNode> VisitMyBaseExpression(VBasic.Syntax.MyBaseExpressionSyntax node)
        {
            return SyntaxFactory.BaseExpression();
        }

        public override async Task<CSharpSyntaxNode> VisitParenthesizedExpression(VBasic.Syntax.ParenthesizedExpressionSyntax node)
        {
            var cSharpSyntaxNode = await node.Expression.AcceptAsync(TriviaConvertingVisitor);
            // If structural changes are necessary the expression may have been lifted a statement (e.g. Type inferred lambda)
            return cSharpSyntaxNode is ExpressionSyntax expr ? SyntaxFactory.ParenthesizedExpression(expr) : cSharpSyntaxNode;
        }

        public override async Task<CSharpSyntaxNode> VisitMemberAccessExpression(VBasic.Syntax.MemberAccessExpressionSyntax node)
        {
            var simpleNameSyntax = (SimpleNameSyntax) await node.Name.AcceptAsync(TriviaConvertingVisitor);

            var nodeSymbol = _semanticModel.GetSymbolInfo(node.Name).Symbol;
            var isDefaultProperty = nodeSymbol is IPropertySymbol p && VBasic.VisualBasicExtensions.IsDefault(p);
            ExpressionSyntax left = null;
            if (node.Expression is VBasic.Syntax.MyClassExpressionSyntax) {
                if (nodeSymbol.IsStatic) {
                    var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                    left = CommonConversions.GetTypeSyntax(typeInfo.Type);
                } else {
                    left = SyntaxFactory.ThisExpression();
                    if (nodeSymbol.IsVirtual && !nodeSymbol.IsAbstract) {
                        simpleNameSyntax = SyntaxFactory.IdentifierName($"MyClass{ConvertIdentifier(node.Name.Identifier).ValueText}");
                    }
                }
            }
            if (left == null && nodeSymbol?.IsStatic == true) {
                var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                var expressionSymbolInfo = _semanticModel.GetSymbolInfo(node.Expression);
                if (typeInfo.Type != null && !expressionSymbolInfo.Symbol.IsType()) {
                    left = CommonConversions.GetTypeSyntax(typeInfo.Type);
                }
            }
            if (left == null) {
                left = (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor);
            }
            if (left == null) {
                if (IsSubPartOfConditionalAccess(node)) {
                    return isDefaultProperty ? SyntaxFactory.ElementBindingExpression()
                        : (ExpressionSyntax)SyntaxFactory.MemberBindingExpression(simpleNameSyntax);
                }
                left = _withBlockLhs.Peek();
            } else if (TryGetTypePromotedModuleSymbol(node, out var promotedModuleSymbol)) {
                left = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left,
                    SyntaxFactory.IdentifierName(promotedModuleSymbol.Name));
            }

            if (node.Expression.IsKind(VBasic.SyntaxKind.GlobalName)) {
                return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)left, simpleNameSyntax);
            }

            if (isDefaultProperty && left != null) {
                return left;
            }

            var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, simpleNameSyntax);
            return AddEmptyArgumentListIfImplicit(node, memberAccessExpressionSyntax);
        }

        public override async Task<CSharpSyntaxNode> VisitConditionalAccessExpression(VBasic.Syntax.ConditionalAccessExpressionSyntax node)
        {
            var leftExpression = (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor) ?? _withBlockLhs.Peek();
            return SyntaxFactory.ConditionalAccessExpression(leftExpression, (ExpressionSyntax) await node.WhenNotNull.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitArgumentList(VBasic.Syntax.ArgumentListSyntax node)
        {
            if (node.Parent.IsKind(VBasic.SyntaxKind.Attribute)) {
                return CommonConversions.CreateAttributeArgumentList(await node.Arguments.SelectAsync(ToAttributeArgument));
            }
            var argumentSyntaxes = await ConvertArguments(node);
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
        }

        public override async Task<CSharpSyntaxNode> VisitSimpleArgument(VBasic.Syntax.SimpleArgumentSyntax node)
        {
            var invocation = node.Parent.Parent;
            if (invocation is VBasic.Syntax.ArrayCreationExpressionSyntax)
                return await node.Expression.AcceptAsync(TriviaConvertingVisitor);
            var symbol = GetInvocationSymbol(invocation);
            SyntaxToken token = default(SyntaxToken);
            string argName = null;
            RefKind refKind = RefKind.None;
            if (symbol is IMethodSymbol methodSymbol) {
                int argId = ((VBasic.Syntax.ArgumentListSyntax)node.Parent).Arguments.IndexOf(node);
                var parameters = (CommonConversions.GetCsOriginalSymbolOrNull(methodSymbol.OriginalDefinition) ?? methodSymbol).GetParameters();
                //WARNING: If named parameters can reach here it won't work properly for them
                if (argId < parameters.Count()) {
                    refKind = parameters[argId].RefKind;
                    argName = parameters[argId].Name;
                }
                switch (refKind) {
                    case RefKind.None:
                        token = default(SyntaxToken);
                        break;
                    case RefKind.Ref:
                        token = SyntaxFactory.Token(SyntaxKind.RefKeyword);
                        break;
                    case RefKind.Out:
                        token = SyntaxFactory.Token(SyntaxKind.OutKeyword);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            var expression = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor), alwaysExplicit: refKind != RefKind.None);
            AdditionalLocal local = null;
            if (refKind != RefKind.None && NeedsVariableForArgument(node)) {
                var expressionTypeInfo= _semanticModel.GetTypeInfo(node.Expression);
                bool useVar = expressionTypeInfo.Type?.Equals(expressionTypeInfo.ConvertedType) == true;
                var typeSyntax = CommonConversions.GetTypeSyntax(expressionTypeInfo.ConvertedType, useVar);
                string prefix = $"arg{argName}";
                local = _additionalLocals.AddAdditionalLocal(new AdditionalLocal(prefix, expression, typeSyntax));
            }
            var nameColon = node.IsNamed ? SyntaxFactory.NameColon((IdentifierNameSyntax) await node.NameColonEquals.Name.AcceptAsync(TriviaConvertingVisitor)) : null;
            if (local == null) {
                return SyntaxFactory.Argument(nameColon, token, expression);
            } else {
                return SyntaxFactory.Argument(nameColon, token, SyntaxFactory.IdentifierName(local.ID).WithAdditionalAnnotations(AdditionalLocals.Annotation));
            }
        }

        public override async Task<CSharpSyntaxNode> VisitNameOfExpression(VBasic.Syntax.NameOfExpressionSyntax node)
        {
            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax) await node.Argument.AcceptAsync(TriviaConvertingVisitor)))));
        }

        public override async Task<CSharpSyntaxNode> VisitEqualsValue(VBasic.Syntax.EqualsValueSyntax node)
        {
            return SyntaxFactory.EqualsValueClause((ExpressionSyntax) await node.Value.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitObjectMemberInitializer(VBasic.Syntax.ObjectMemberInitializerSyntax node)
        {
            var initializers = await Task.WhenAll(node.Initializers.Select(initializer => initializer.AcceptAsync(TriviaConvertingVisitor)));
            var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                initializers.Cast<ExpressionSyntax>());
            return SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, memberDeclaratorSyntaxs);
        }

        public override async Task<CSharpSyntaxNode> VisitAnonymousObjectCreationExpression(VBasic.Syntax.AnonymousObjectCreationExpressionSyntax node)
        {
            var initializers = await Task.WhenAll(node.Initializer.Initializers.Select(initializer => initializer.AcceptAsync(TriviaConvertingVisitor)));
            var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                initializers.Cast<AnonymousObjectMemberDeclaratorSyntax>());
            return SyntaxFactory.AnonymousObjectCreationExpression(memberDeclaratorSyntaxs);
        }

        public override async Task<CSharpSyntaxNode> VisitInferredFieldInitializer(VBasic.Syntax.InferredFieldInitializerSyntax node)
        {
            return SyntaxFactory.AnonymousObjectMemberDeclarator((ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitObjectCreationExpression(VBasic.Syntax.ObjectCreationExpressionSyntax node)
        {
            return SyntaxFactory.ObjectCreationExpression(
                (TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor),
                // VB can omit empty arg lists:
                (ArgumentListSyntax) await node.ArgumentList.AcceptAsync(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList(),
                (InitializerExpressionSyntax) await node.Initializer.AcceptAsync(TriviaConvertingVisitor)
            );
        }

        public override async Task<CSharpSyntaxNode> VisitArrayCreationExpression(VBasic.Syntax.ArrayCreationExpressionSyntax node)
        {
            var bounds = await CommonConversions.ConvertArrayRankSpecifierSyntaxes(node.RankSpecifiers, node.ArrayBounds);

            var allowInitializer = node.Initializer.Initializers.Any()
                || node.RankSpecifiers.Any()
                || node.ArrayBounds == null
                || (node.Initializer.Initializers.Any() && node.ArrayBounds.Arguments.All(b => b.IsOmitted || _semanticModel.GetConstantValue(b.GetExpression()).HasValue));

            var initializerToConvert = allowInitializer ? node.Initializer : null;
            return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType((TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor), bounds),
                (InitializerExpressionSyntax) await initializerToConvert.AcceptAsync(TriviaConvertingVisitor)
            );
        }

        public override async Task<CSharpSyntaxNode> VisitCollectionInitializer(VBasic.Syntax.CollectionInitializerSyntax node)
        {
            var isExplicitCollectionInitializer = node.Parent is VBasic.Syntax.ObjectCollectionInitializerSyntax
                                                  || node.Parent is VBasic.Syntax.CollectionInitializerSyntax
                                                  || node.Parent is VBasic.Syntax.ArrayCreationExpressionSyntax;
            var initializerType = isExplicitCollectionInitializer ? SyntaxKind.CollectionInitializerExpression : SyntaxKind.ArrayInitializerExpression;
            var initializers = (await Task.WhenAll(node.Initializers.Select(i => i.AcceptAsync(TriviaConvertingVisitor)))).Cast<ExpressionSyntax>();
            var initializer = SyntaxFactory.InitializerExpression(initializerType, SyntaxFactory.SeparatedList(initializers));
            return isExplicitCollectionInitializer
                ? initializer
                : (CSharpSyntaxNode)SyntaxFactory.ImplicitArrayCreationExpression(initializer);
        }

        public override async Task<CSharpSyntaxNode> VisitQueryExpression(VBasic.Syntax.QueryExpressionSyntax node)
        {
            return await _queryConverter.ConvertClauses(node.Clauses);
        }

        public override async Task<CSharpSyntaxNode> VisitOrdering(VBasic.Syntax.OrderingSyntax node)
        {
            var convertToken = node.Kind().ConvertToken();
            var expressionSyntax = (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor);
            var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
            return SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
        }

        public override async Task<CSharpSyntaxNode> VisitNamedFieldInitializer(VBasic.Syntax.NamedFieldInitializerSyntax node)
        {
            if (node?.Parent?.Parent is VBasic.Syntax.AnonymousObjectCreationExpressionSyntax) {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Name.Identifier))),
                    (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor));
            }

            return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                (ExpressionSyntax) await node.Name.AcceptAsync(TriviaConvertingVisitor),
                (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor)
            );
        }

        public override async Task<CSharpSyntaxNode> VisitObjectCollectionInitializer(VBasic.Syntax.ObjectCollectionInitializerSyntax node)
        {
            return await node.Initializer.AcceptAsync(TriviaConvertingVisitor); //Dictionary initializer comes through here despite the FROM keyword not being in the source code
        }

        public override async Task<CSharpSyntaxNode> VisitBinaryConditionalExpression(VBasic.Syntax.BinaryConditionalExpressionSyntax node)
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                (ExpressionSyntax) await node.FirstExpression.AcceptAsync(TriviaConvertingVisitor),
                (ExpressionSyntax) await node.SecondExpression.AcceptAsync(TriviaConvertingVisitor)
            );
        }

        public override async Task<CSharpSyntaxNode> VisitTernaryConditionalExpression(VBasic.Syntax.TernaryConditionalExpressionSyntax node)
        {
            var expr = SyntaxFactory.ConditionalExpression(
                (ExpressionSyntax) await node.Condition.AcceptAsync(TriviaConvertingVisitor),
                (ExpressionSyntax) await node.WhenTrue.AcceptAsync(TriviaConvertingVisitor),
                (ExpressionSyntax) await node.WhenFalse.AcceptAsync(TriviaConvertingVisitor)
            );

            if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || VbSyntaxNodeExtensions.PrecedenceCouldChange(node))
                return SyntaxFactory.ParenthesizedExpression(expr);

            return expr;
        }

        public override async Task<CSharpSyntaxNode> VisitTypeOfExpression(VBasic.Syntax.TypeOfExpressionSyntax node)
        {
            var expr = SyntaxFactory.BinaryExpression(
                SyntaxKind.IsExpression,
                (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor),
                (TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor)
            );
            return node.IsKind(VBasic.SyntaxKind.TypeOfIsNotExpression) ? expr.InvertCondition() : expr;
        }

        public override async Task<CSharpSyntaxNode> VisitUnaryExpression(VBasic.Syntax.UnaryExpressionSyntax node)
        {
            var expr = (ExpressionSyntax) await node.Operand.AcceptAsync(TriviaConvertingVisitor);
            if (node.IsKind(VBasic.SyntaxKind.AddressOfExpression))
                return expr;
            var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
            SyntaxKind csTokenKind = CSharpUtil.GetExpressionOperatorTokenKind(kind);
            return SyntaxFactory.PrefixUnaryExpression(
                kind,
                SyntaxFactory.Token(csTokenKind),
                expr.AddParensIfRequired()
            );
        }

        public override async Task<CSharpSyntaxNode> VisitBinaryExpression(VBasic.Syntax.BinaryExpressionSyntax node)
        {
            if (node.IsKind(VBasic.SyntaxKind.IsExpression)) {
                ExpressionSyntax otherArgument = null;
                if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax) await node.Right.AcceptAsync(TriviaConvertingVisitor);
                }
                if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax) await node.Left.AcceptAsync(TriviaConvertingVisitor);
                }
                if (otherArgument != null) {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                }
            }
            if (node.IsKind(VBasic.SyntaxKind.IsNotExpression)) {
                ExpressionSyntax otherArgument = null;
                if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax) await node.Right.AcceptAsync(TriviaConvertingVisitor);
                }
                if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax) await node.Left.AcceptAsync(TriviaConvertingVisitor);
                }
                if (otherArgument != null) {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                }
            }

            var lhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Left, (ExpressionSyntax) await node.Left.AcceptAsync(TriviaConvertingVisitor));
            var rhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, (ExpressionSyntax) await node.Right.AcceptAsync(TriviaConvertingVisitor));

            var stringType = _semanticModel.Compilation.GetTypeByMetadataName("System.String");
            var lhsTypeInfo = _semanticModel.GetTypeInfo(node.Left);
            var rhsTypeInfo = _semanticModel.GetTypeInfo(node.Right);

            if (node.IsKind(VBasic.SyntaxKind.ConcatenateExpression)) {
                if (lhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                    lhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String &&
                    rhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                    rhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String) {
                    lhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConvertTo(node.Left, lhs, stringType);
                }
            }

            var objectEqualityType = _visualBasicEqualityComparison.GetObjectEqualityType(node, lhsTypeInfo, rhsTypeInfo);
            switch (objectEqualityType) {
                case VisualBasicEqualityComparison.RequiredType.StringOnly:
                    if (lhsTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String &&
                        rhsTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String &&
                        _visualBasicEqualityComparison.TryConvertToNullOrEmptyCheck(node, lhs, rhs, out CSharpSyntaxNode visitBinaryExpression)) {
                        return visitBinaryExpression;
                    }
                    (lhs, rhs) = _visualBasicEqualityComparison.AdjustForVbStringComparison(node.Left, lhs, lhsTypeInfo, node.Right, rhs, rhsTypeInfo);
                    break;
                case VisualBasicEqualityComparison.RequiredType.Object:
                    return _visualBasicEqualityComparison.GetFullExpressionForVbObjectComparison(node, lhs, rhs);
            }

            if (node.IsKind(VBasic.SyntaxKind.ExponentiateExpression,
                VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                return SyntaxFactory.InvocationExpression(
                    ValidSyntaxFactory.MemberAccess(nameof(Math), nameof(Math.Pow)),
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
            }

            if (node.IsKind(VBasic.SyntaxKind.LikeExpression)) {
                var compareText = ValidSyntaxFactory.MemberAccess("CompareMethod", _optionCompareText ? "Text" : "Binary");
                var likeString = ValidSyntaxFactory.MemberAccess("LikeOperator", "LikeString");
                _extraUsingDirectives.Add("Microsoft.VisualBasic");
                _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
                return SyntaxFactory.InvocationExpression(
                    likeString,
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs, compareText)
                );
            }

            var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
            var op = SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));

            var csBinExp = SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node, csBinExp);
        }

        public override async Task<CSharpSyntaxNode> VisitInvocationExpression(VBasic.Syntax.InvocationExpressionSyntax node)
        {
            var invocationSymbol = _semanticModel.GetSymbolInfo(node).ExtractBestMatch();
            var expressionSymbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch();
            var expressionReturnType = expressionSymbol?.GetReturnType() ?? _semanticModel.GetTypeInfo(node.Expression).Type;
            var operation = _semanticModel.GetOperation(node);
            if (expressionSymbol?.ContainingNamespace.MetadataName == "VisualBasic" && await SubstituteVisualBasicMethodOrNull(node) is CSharpSyntaxNode csEquivalent) {
                return csEquivalent;
            }

            var (overrideIdentifier, extraArg) = await CommonConversions.GetParameterizedPropertyAccessMethod(operation);
            if (overrideIdentifier != null) {
                var expr = await node.Expression.AcceptAsync(TriviaConvertingVisitor);
                var idToken = expr.DescendantTokens().Last(t => t.IsKind(SyntaxKind.IdentifierToken));
                expr = ReplaceRightmostIdentifierText(expr, idToken, overrideIdentifier);

                var args = await ConvertArgumentListOrEmpty(node.ArgumentList);
                if (extraArg != null) {
                    args = args.WithArguments(args.Arguments.Add(SyntaxFactory.Argument(extraArg)));
                }
                return SyntaxFactory.InvocationExpression((ExpressionSyntax)expr, args);
            }

            // VB doesn't have a specialized node for element access because the syntax is ambiguous. Instead, it just uses an invocation expression or dictionary access expression, then figures out using the semantic model which one is most likely intended.
            // https://github.com/dotnet/roslyn/blob/master/src/Workspaces/VisualBasic/Portable/LanguageServices/VisualBasicSyntaxFactsService.vb#L768
            var (convertedExpression, shouldBeElementAccess) = await ConvertInvocationSubExpression();
            if (shouldBeElementAccess) {
                return await CreateElementAccess();
            }

            if (expressionSymbol != null && expressionSymbol.IsKind(SymbolKind.Property)) {
                return convertedExpression; //Parameterless property access
            }

            if (expressionSymbol != null && (invocationSymbol?.Name == nameof(Enumerable.ElementAtOrDefault) && !expressionSymbol.Equals(invocationSymbol))) {
                _extraUsingDirectives.Add(nameof(System) + "." + nameof(System.Linq));
                convertedExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, convertedExpression,
                    SyntaxFactory.IdentifierName(nameof(Enumerable.ElementAtOrDefault)));
            }

            return SyntaxFactory.InvocationExpression(convertedExpression, await ConvertArgumentListOrEmpty(node.ArgumentList));

            async Task<(ExpressionSyntax, bool isElementAccess)> ConvertInvocationSubExpression()
            {
                var isElementAccess = IsPropertyElementAccess(operation) ||
                                  IsArrayElementAccess(operation) ||
                                  ProbablyNotAMethodCall(node, expressionSymbol, expressionReturnType);

                var expr = await node.Expression.AcceptAsync(TriviaConvertingVisitor);
                return ((ExpressionSyntax)expr, isElementAccess);
            }

            async Task<CSharpSyntaxNode> CreateElementAccess()
            {
                var args = await node.ArgumentList.Arguments.AcceptAsync<CSharpSyntaxNode, ArgumentSyntax>(TriviaConvertingVisitor);
                var bracketedArgumentListSyntax = SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(
                    args
                ));
                if (convertedExpression is ElementBindingExpressionSyntax binding && !binding.ArgumentList.Arguments.Any()) {
                    // Special case where structure changes due to conditional access (See VisitMemberAccessExpression)
                    return binding.WithArgumentList(bracketedArgumentListSyntax);
                } else {
                    return SyntaxFactory.ElementAccessExpression(convertedExpression, bracketedArgumentListSyntax);
                }
            }
        }

        public override async Task<CSharpSyntaxNode> VisitSingleLineLambdaExpression(VBasic.Syntax.SingleLineLambdaExpressionSyntax node)
        {
            IReadOnlyCollection<StatementSyntax> convertedStatements;
            if (node.Body is VBasic.Syntax.StatementSyntax statement) {
                convertedStatements = await statement.Accept(CreateMethodBodyVisitor(node));
            } else {
                var csNode = await node.Body.AcceptAsync(TriviaConvertingVisitor);
                convertedStatements = new[] { SyntaxFactory.ExpressionStatement((ExpressionSyntax)csNode)};
            }
            var param = (ParameterListSyntax) await node.SubOrFunctionHeader.ParameterList.AcceptAsync(TriviaConvertingVisitor);
            return _lambdaConverter.Convert(node, param, convertedStatements);
        }

        public override async Task<CSharpSyntaxNode> VisitMultiLineLambdaExpression(VBasic.Syntax.MultiLineLambdaExpressionSyntax node)
        {
            var methodBodyVisitor = CreateMethodBodyVisitor(node);
            var body = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
            var param = (ParameterListSyntax) await node.SubOrFunctionHeader.ParameterList.AcceptAsync(TriviaConvertingVisitor);
            return _lambdaConverter.Convert(node, param, body.ToList());
        }

        public override async Task<CSharpSyntaxNode> VisitParameterList(VBSyntax.ParameterListSyntax node)
        {
            var parameters = await node.Parameters.SelectAsync(async p => (ParameterSyntax) await p.AcceptAsync(TriviaConvertingVisitor));
            if (node.Parent is VBSyntax.PropertyStatementSyntax && CommonConversions.IsDefaultIndexer(node.Parent)) {
                return SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameters));
            }
            return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
        }

        public override async Task<CSharpSyntaxNode> VisitParameter(VBSyntax.ParameterSyntax node)
        {
            var id = CommonConversions.ConvertIdentifier(node.Identifier.Identifier);
            var paramType = (TypeSyntax) await (node.AsClause?.Type).AcceptAsync(TriviaConvertingVisitor);
            if (node.Parent?.Parent?.IsKind(VBasic.SyntaxKind.FunctionStatement,
                    VBasic.SyntaxKind.SubStatement) == true) {
                paramType = paramType ?? SyntaxFactory.ParseTypeName("object");
            }

            var rankSpecifiers = await CommonConversions.ConvertArrayRankSpecifierSyntaxes(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
            if (rankSpecifiers.Any() && paramType != null) {
                paramType = SyntaxFactory.ArrayType(paramType, rankSpecifiers);
            }

            if (paramType != null && !SyntaxTokenExtensions.IsKind(node.Identifier.Nullable, SyntaxKind.None)) {
                var arrayType = paramType as ArrayTypeSyntax;
                if (arrayType == null) {
                    paramType = SyntaxFactory.NullableType(paramType);
                } else {
                    paramType = arrayType.WithElementType(SyntaxFactory.NullableType(arrayType.ElementType));
                }
            }

            var attributes = (await node.AttributeLists.SelectManyAsync(ConvertAttribute)).ToList();
            var csParamSymbol = CommonConversions.GetDeclaredCsOriginalSymbolOrNull(node) as IParameterSymbol;
            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Local);
            if (csParamSymbol?.RefKind == RefKind.Out || node.AttributeLists.Any(CommonConversions.HasOutAttribute)) {
                modifiers = modifiers.Replace(SyntaxFactory.Token(SyntaxKind.RefKeyword), SyntaxFactory.Token(SyntaxKind.OutKeyword));
            }

            EqualsValueClauseSyntax @default = null;
            if (node.Default != null) {
                if (node.Default.Value is VBSyntax.LiteralExpressionSyntax les && les.Token.Value is DateTime dt) {
                    var dateTimeAsLongCsLiteral = CommonConversions.GetLiteralExpression(dt.Ticks, dt.Ticks + "L");
                    var dateTimeArg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(dateTimeAsLongCsLiteral));
                    _extraUsingDirectives.Add("System.Runtime.InteropServices");
                    _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                    var optionalDateTimeAttributes = new[] {
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("Optional")),
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("DateTimeConstant"), dateTimeArg)
                    };
                    attributes.Insert(0,
                        SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                } else {
                    @default = SyntaxFactory.EqualsValueClause(
                        (ExpressionSyntax) await node.Default.Value.AcceptAsync(TriviaConvertingVisitor));
                }
            }

            if (node.Parent.Parent is VBSyntax.MethodStatementSyntax mss
                && mss.AttributeLists.Any(CommonConversions.HasExtensionAttribute) && node.Parent.ChildNodes().First() == node) {
                modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.ThisKeyword));
            }
            return SyntaxFactory.Parameter(
                SyntaxFactory.List(attributes),
                modifiers,
                paramType,
                id,
                @default
            );
        }

        public async Task<SyntaxList<AttributeListSyntax>> ConvertAttributes(SyntaxList<VBSyntax.AttributeListSyntax> attributeListSyntaxs)
        {
            return SyntaxFactory.List(await attributeListSyntaxs.SelectManyAsync(ConvertAttribute));
        }

        public async Task<IEnumerable<AttributeListSyntax>> ConvertAttribute(VBSyntax.AttributeListSyntax attributeList)
        {
            // These attributes' semantic effects are expressed differently in CSharp.
            return await attributeList.Attributes.Where(a => !CommonConversions.IsExtensionAttribute(a) && !CommonConversions.IsOutAttribute(a))
                .SelectAsync(async a => (AttributeListSyntax) await a.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitAttribute(VBSyntax.AttributeSyntax node)
        {
            return SyntaxFactory.AttributeList(
                node.Target == null ? null : SyntaxFactory.AttributeTargetSpecifier(node.Target.AttributeModifier.ConvertToken()),
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute((NameSyntax) await node.Name.AcceptAsync(TriviaConvertingVisitor), (AttributeArgumentListSyntax) await node.ArgumentList.AcceptAsync(TriviaConvertingVisitor)))
            );
        }

        public override async Task<CSharpSyntaxNode> VisitTupleType(VBasic.Syntax.TupleTypeSyntax node)
        {
            var elements = await node.Elements.SelectAsync(async e => (TupleElementSyntax) await e.AcceptAsync(TriviaConvertingVisitor));
            return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
        }

        public override async Task<CSharpSyntaxNode> VisitTypedTupleElement(VBasic.Syntax.TypedTupleElementSyntax node)
        {
            return SyntaxFactory.TupleElement((TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitNamedTupleElement(VBasic.Syntax.NamedTupleElementSyntax node)
        {
            return SyntaxFactory.TupleElement((TypeSyntax) await node.AsClause.Type.AcceptAsync(TriviaConvertingVisitor), CommonConversions.ConvertIdentifier(node.Identifier));
        }

        public override async Task<CSharpSyntaxNode> VisitTupleExpression(VBasic.Syntax.TupleExpressionSyntax node)
        {
            var args = await node.Arguments.SelectAsync(async a => {
                var expr = (ExpressionSyntax) await a.Expression.AcceptAsync(TriviaConvertingVisitor);
                return SyntaxFactory.Argument(expr);
            });
            return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(args));
        }

        public override async Task<CSharpSyntaxNode> VisitPredefinedType(VBasic.Syntax.PredefinedTypeSyntax node)
        {
            if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.DateKeyword)) {
                return SyntaxFactory.IdentifierName("DateTime");
            }
            return SyntaxFactory.PredefinedType(node.Keyword.ConvertToken());
        }

        public override async Task<CSharpSyntaxNode> VisitNullableType(VBasic.Syntax.NullableTypeSyntax node)
        {
            return SyntaxFactory.NullableType((TypeSyntax) await node.ElementType.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitArrayType(VBasic.Syntax.ArrayTypeSyntax node)
        {
            var ranks = await node.RankSpecifiers.SelectAsync(async r => (ArrayRankSpecifierSyntax) await r.AcceptAsync(TriviaConvertingVisitor));
            return SyntaxFactory.ArrayType((TypeSyntax) await node.ElementType.AcceptAsync(TriviaConvertingVisitor), SyntaxFactory.List(ranks));
        }

        public override async Task<CSharpSyntaxNode> VisitArrayRankSpecifier(VBasic.Syntax.ArrayRankSpecifierSyntax node)
        {
            return SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), node.Rank)));
        }

        public override async Task<CSharpSyntaxNode> VisitIdentifierName(VBasic.Syntax.IdentifierNameSyntax node)
        {
            var identifier = SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier, node.GetAncestor<VBasic.Syntax.AttributeSyntax>() != null));

            var qualifiedIdentifier = !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer)
                                      || node.Parent is VBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression == node
                                      || node.Parent is VBasic.Syntax.QualifiedNameSyntax qns && qns.Left == node
                ? QualifyNode(node, identifier) : identifier;

            var withArgList = AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
            var sym = GetSymbolInfoInDocument(node);
            if (sym != null && sym.Kind == SymbolKind.Local) {
                var vbMethodBlock = node.Ancestors().OfType<VBasic.Syntax.MethodBlockBaseSyntax>().FirstOrDefault();
                if (vbMethodBlock != null &&
                    !node.Parent.IsKind(VBasic.SyntaxKind.NameOfExpression) &&
                    node.Identifier.ValueText.Equals(GetMethodBlockBaseIdentifierForImplicitReturn(vbMethodBlock).ValueText, StringComparison.OrdinalIgnoreCase)) {
                    var retVar = GetRetVariableNameOrNull(vbMethodBlock);
                    if (retVar != null) {
                        return retVar;
                    }
                }
            }
            return withArgList;
        }

        public override async Task<CSharpSyntaxNode> VisitQualifiedName(VBasic.Syntax.QualifiedNameSyntax node)
        {
            var lhsSyntax = (NameSyntax) await node.Left.AcceptAsync(TriviaConvertingVisitor);
            var rhsSyntax = (SimpleNameSyntax) await node.Right.AcceptAsync(TriviaConvertingVisitor);

            VBasic.Syntax.NameSyntax topLevelName = node;
            while (topLevelName.Parent is VBasic.Syntax.NameSyntax parentName) {
                topLevelName = parentName;
            }
            var partOfNamespaceDeclaration = topLevelName.Parent.IsKind(VBasic.SyntaxKind.NamespaceStatement);
            var leftIsGlobal = node.Left.IsKind(VBasic.SyntaxKind.GlobalName);

            ExpressionSyntax qualifiedName;
            if (partOfNamespaceDeclaration || !(lhsSyntax is SimpleNameSyntax sns)) {
                if (leftIsGlobal) return rhsSyntax;
                qualifiedName = lhsSyntax;
            } else {
                qualifiedName = QualifyNode(node.Left, sns);
            }

            return leftIsGlobal
                ? (CSharpSyntaxNode)SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)lhsSyntax, rhsSyntax)
                : SyntaxFactory.QualifiedName((NameSyntax)qualifiedName, rhsSyntax);
        }

        public override async Task<CSharpSyntaxNode> VisitGenericName(VBasic.Syntax.GenericNameSyntax node)
        {
            return SyntaxFactory.GenericName(ConvertIdentifier(node.Identifier), (TypeArgumentListSyntax) await node.TypeArgumentList.AcceptAsync(TriviaConvertingVisitor));
        }

        public override async Task<CSharpSyntaxNode> VisitTypeArgumentList(VBasic.Syntax.TypeArgumentListSyntax node)
        {
            var args = await node.Arguments.SelectAsync(async a => (TypeSyntax) await a.AcceptAsync(TriviaConvertingVisitor));
            return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(args));
        }

        public IdentifierNameSyntax GetRetVariableNameOrNull(VBasic.Syntax.MethodBlockBaseSyntax node)
        {
            if (!node.AllowsImplicitReturn()) return null;

            bool assignsToMethodNameVariable = false;

            if (!node.Statements.IsEmpty()) {
                string methodName = GetMethodBlockBaseIdentifierForImplicitReturn(node).ValueText;
                Func<ISymbol, bool> equalsMethodName = s => s.IsKind(SymbolKind.Local) && s.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase);
                var flow = _semanticModel.AnalyzeDataFlow(node.Statements.First(), node.Statements.Last());

                if (flow.Succeeded) {
                    assignsToMethodNameVariable = flow.ReadInside.Any(equalsMethodName) || flow.WrittenInside.Any(equalsMethodName);
                }
            }

            IdentifierNameSyntax csReturnVariable = null;

            if (assignsToMethodNameVariable) {
                // In VB, assigning to the method name implicitly creates a variable that is returned when the method exits
                var csReturnVariableName =
                    CommonConversions.ConvertIdentifier(GetMethodBlockBaseIdentifierForImplicitReturn(node)).ValueText + "Ret";
                csReturnVariable = SyntaxFactory.IdentifierName(csReturnVariableName);
            }

            return csReturnVariable;
        }

        public VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> CreateMethodBodyVisitor(VBasic.VisualBasicSyntaxNode node, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
        {
            var methodBodyVisitor = new MethodBodyExecutableStatementVisitor(node, _semanticModel, TriviaConvertingVisitor, CommonConversions, _withBlockLhs, _extraUsingDirectives, _additionalLocals, _methodsWithHandles, TriviaConvertingVisitor.TriviaConverter) {
                IsIterator = isIterator,
                ReturnVariable = csReturnVariable,
            };
            return methodBodyVisitor.CommentConvertingVisitor;
        }

        private async Task<CSharpSyntaxNode> ConvertCastExpression(VBasic.Syntax.CastExpressionSyntax node, ExpressionSyntax convertMethodOrNull = null)
        {
            var expressionSyntax = (ExpressionSyntax) await node.Expression.AcceptAsync(TriviaConvertingVisitor);

            if (convertMethodOrNull != null) {
                return
                    SyntaxFactory.InvocationExpression(convertMethodOrNull,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(expressionSyntax)))
                    );
            }

            var castExpr = ValidSyntaxFactory.CastExpression((TypeSyntax) await node.Type.AcceptAsync(TriviaConvertingVisitor), expressionSyntax);
            if (node.Parent is VBasic.Syntax.MemberAccessExpressionSyntax) {
                return SyntaxFactory.ParenthesizedExpression(castExpr);
            }
            return castExpr;
        }

        private ExpressionSyntax GetConvertMethodForKeywordOrNull(SyntaxNode type)
        {
            var convertedType = _semanticModel.GetTypeInfo(type).Type;
            _extraUsingDirectives.Add(ConvertType.Namespace);
            return _convertMethodsLookupByReturnType.TryGetValue(convertedType, out var convertMethodName)
                ? SyntaxFactory.ParseExpression(convertMethodName) : null;
        }

        /// <remarks>https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion</remarks>
        private bool TryGetTypePromotedModuleSymbol(VBasic.Syntax.MemberAccessExpressionSyntax node, out INamedTypeSymbol moduleSymbol)
        {
            if (_semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch() is INamespaceSymbol
                    expressionSymbol &&
                _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch()?.ContainingSymbol is INamedTypeSymbol
                    nameContainingSymbol &&
                nameContainingSymbol.ContainingSymbol.Equals(expressionSymbol)) {
                moduleSymbol = nameContainingSymbol;
                return true;
            }

            moduleSymbol = null;
            return false;
        }

        private static bool IsSubPartOfConditionalAccess(VBasic.Syntax.MemberAccessExpressionSyntax node)
        {
            var firstPossiblyConditionalAncestor = node.Parent;
            while (firstPossiblyConditionalAncestor != null &&
                   firstPossiblyConditionalAncestor.IsKind(VBasic.SyntaxKind.InvocationExpression,
                       VBasic.SyntaxKind.SimpleMemberAccessExpression)) {
                firstPossiblyConditionalAncestor = firstPossiblyConditionalAncestor.Parent;
            }

            return firstPossiblyConditionalAncestor?.IsKind(VBasic.SyntaxKind.ConditionalAccessExpression) == true;
        }

        private async Task<IEnumerable<ArgumentSyntax>> ConvertArguments(VBasic.Syntax.ArgumentListSyntax node)
        {
            ISymbol invocationSymbolForForcedNames = null;
            var argumentSyntaxs = (await node.Arguments.SelectAsync(async (a, i) => {
                if (a.IsOmitted) {
                    invocationSymbolForForcedNames = GetInvocationSymbol(node.Parent);
                    if (invocationSymbolForForcedNames != null) {
                        return null;
                    }

                    var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                        .WithTrailingTrivia(
                            SyntaxFactory.Comment("/* Conversion error: Set to default value for this argument */"));
                    return SyntaxFactory.Argument(nullLiteral);
                }

                var argumentSyntax = (ArgumentSyntax) await a.AcceptAsync(TriviaConvertingVisitor);

                if (invocationSymbolForForcedNames != null) {
                    var elementAtOrDefault = invocationSymbolForForcedNames.GetParameters().ElementAt(i).Name;
                    return argumentSyntax.WithNameColon(SyntaxFactory.NameColon(elementAtOrDefault));
                }

                return argumentSyntax;
            })).Where(a => a != null);
            return argumentSyntaxs;
        }

        private bool NeedsVariableForArgument(VBasic.Syntax.SimpleArgumentSyntax node)
        {
            bool isIdentifier = node.Expression is VBasic.Syntax.IdentifierNameSyntax;
            bool isMemberAccess = node.Expression is VBasic.Syntax.MemberAccessExpressionSyntax;

            var symbolInfo = GetSymbolInfoInDocument(node.Expression);
            bool isProperty = symbolInfo != null && symbolInfo.IsKind(SymbolKind.Property);
            bool isUsing = symbolInfo?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.Parent?.Parent?.IsKind(VBasic.SyntaxKind.UsingStatement) == true;

            var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
            bool isTypeMismatch = typeInfo.Type == null || !typeInfo.Type.Equals(typeInfo.ConvertedType);

            return (!isIdentifier && !isMemberAccess) || isProperty || isTypeMismatch || isUsing;
        }

        private ISymbol GetInvocationSymbol(SyntaxNode invocation)
        {
            var symbol = invocation.TypeSwitch(
                (VBasic.Syntax.InvocationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch(),
                (VBasic.Syntax.ObjectCreationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch(),
                (VBasic.Syntax.RaiseEventStatementSyntax e) => _semanticModel.GetSymbolInfo(e.Name).ExtractBestMatch(),
                _ => { throw new NotSupportedException(); }
            );
            return symbol;
        }

        private async Task<AttributeArgumentSyntax> ToAttributeArgument(VBasic.Syntax.ArgumentSyntax arg)
        {
            if (!(arg is VBasic.Syntax.SimpleArgumentSyntax))
                throw new NotSupportedException();
            var a = (VBasic.Syntax.SimpleArgumentSyntax)arg;
            var attr = SyntaxFactory.AttributeArgument((ExpressionSyntax) await a.Expression.AcceptAsync(TriviaConvertingVisitor));
            if (a.IsNamed) {
                attr = attr.WithNameEquals(SyntaxFactory.NameEquals((IdentifierNameSyntax) await a.NameColonEquals.Name.AcceptAsync(TriviaConvertingVisitor)));
            }
            return attr;
        }

        private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false)
        {
            return CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
        }

        private static CSharpSyntaxNode ReplaceRightmostIdentifierText(CSharpSyntaxNode expr, SyntaxToken idToken, string overrideIdentifier)
        {
            return expr.ReplaceToken(idToken, SyntaxFactory.Identifier(overrideIdentifier).WithTriviaFrom(idToken).WithAdditionalAnnotations(idToken.GetAnnotations()));
        }

        private static bool IsPropertyElementAccess(IOperation operation)
        {
            return operation is IPropertyReferenceOperation pro && pro.Arguments.Any() && VBasic.VisualBasicExtensions.IsDefault(pro.Property);
        }

        private static bool IsArrayElementAccess(IOperation operation)
        {
            return operation != null && operation.Kind == OperationKind.ArrayElementReference;
        }

        /// <summary>
        /// Chances of having an unknown delegate stored as a field/local seem lower than having an unknown non-delegate type with an indexer stored.
        /// So for a standalone identifier err on the side of assuming it's an indexer.
        /// </summary>
        private static bool ProbablyNotAMethodCall(VBasic.Syntax.InvocationExpressionSyntax node, ISymbol symbol, ITypeSymbol symbolReturnType)
        {
            return !(symbol is IMethodSymbol) && symbolReturnType.IsErrorType() && node.Expression is VBasic.Syntax.IdentifierNameSyntax && node.ArgumentList.Arguments.Any();
        }

        private async Task<ArgumentListSyntax> ConvertArgumentListOrEmpty(VBasic.Syntax.ArgumentListSyntax argumentListSyntax)
        {
            return (ArgumentListSyntax) await argumentListSyntax.AcceptAsync(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList();
        }

        private async Task<CSharpSyntaxNode> SubstituteVisualBasicMethodOrNull(VBasic.Syntax.InvocationExpressionSyntax node)
        {
            CastExpressionSyntax cSharpSyntaxNode = null;
            var symbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch();
            if (symbol?.Name == "ChrW" || symbol?.Name == "Chr") {
                var args = await ConvertArguments(node.ArgumentList);
                cSharpSyntaxNode = ValidSyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("char"),
                    args.Single().Expression);
            }

            return cSharpSyntaxNode;
        }

        private static SyntaxToken GetMethodBlockBaseIdentifierForImplicitReturn(SyntaxNode vbMethodBlock)
        {
            if (vbMethodBlock.Parent is VBasic.Syntax.PropertyBlockSyntax pb) {
                return pb.PropertyStatement.Identifier;
            } else if (vbMethodBlock is VBasic.Syntax.MethodBlockSyntax mb) {
                return mb.SubOrFunctionStatement.Identifier;
            } else {
                return VBasic.SyntaxFactory.Token(VBasic.SyntaxKind.EmptyToken);
            }
        }

        private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(SyntaxNode node, ExpressionSyntax id)
        {
            return _semanticModel.GetOperation(node)?.Kind == OperationKind.Invocation
                ? SyntaxFactory.InvocationExpression(id, SyntaxFactory.ArgumentList())
                : id;
        }

        private ExpressionSyntax QualifyNode(SyntaxNode node, SimpleNameSyntax left)
        {
            var nodeSymbolInfo = GetSymbolInfoInDocument(node);
            if (left != null &&
                nodeSymbolInfo?.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol &&
                !ContextImplicitlyQualfiesSymbol(node, containingSymbol)) {

                if (containingSymbol is ITypeSymbol containingTypeSymbol &&
                    !nodeSymbolInfo.IsConstructor() /* Constructors are implicitly qualified with their type */) {
                    // Qualify with a type to handle VB's type promotion https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion
                    var qualification =
                        containingTypeSymbol.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart);
                    return Qualify(qualification, left);
                } else if (nodeSymbolInfo.IsNamespace()) {
                    // Turn partial namespace qualification into full namespace qualification
                    var qualification =
                        containingSymbol.ToCSharpDisplayString();
                    return Qualify(qualification, left);
                }
            }

            return left;
        }

        private bool ContextImplicitlyQualfiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
        {
            return symbolToCheck is INamespaceSymbol ns && ns.IsGlobalNamespace ||
                   EnclosingTypeImplicitlyQualifiesSymbol(syntaxNodeContext, symbolToCheck);
        }

        private bool EnclosingTypeImplicitlyQualifiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
        {
            ISymbol typeContext = syntaxNodeContext.GetEnclosingDeclaredTypeSymbol(_semanticModel);
            var implicitCsQualifications = ((ITypeSymbol)typeContext).GetBaseTypesAndThis()
                .Concat(typeContext.FollowProperty(n => n.ContainingSymbol))
                .ToList();

            return implicitCsQualifications.Contains(symbolToCheck);
        }

        private static QualifiedNameSyntax Qualify(string qualification, ExpressionSyntax toBeQualified)
        {
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.ParseName(qualification),
                (SimpleNameSyntax)toBeQualified);
        }

        /// <returns>The ISymbol if available in this document, otherwise null</returns>
        private ISymbol GetSymbolInfoInDocument(SyntaxNode node)
        {
            return _semanticModel.SyntaxTree == node.SyntaxTree ? _semanticModel.GetSymbolInfo(node).Symbol : null;
        }
    }
}