using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class ExpressionNodeVisitor : Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private static readonly Type ConvertType = typeof(Convert);
        public CommentConvertingVisitorWrapper<CSharpSyntaxNode> TriviaConvertingVisitor { get; }
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives = new HashSet<string>();
        private readonly bool _optionCompareText = false;
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
        private readonly Stack<string> _withBlockTempVariableNames;
        private readonly AdditionalLocals _additionalLocals;
        private readonly MethodsWithHandles _methodsWithHandles;
        private readonly QueryConverter _queryConverter;
        private readonly Dictionary<ITypeSymbol, string> _convertMethodsLookupByReturnType;
        private readonly Compilation _csCompilation;

        public ExpressionNodeVisitor(SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison, AdditionalLocals additionalLocals, Compilation csCompilation, Stack<string> withBlockTempVariableNames, MethodsWithHandles methodsWithHandles, CommonConversions commonConversions, TriviaConverter triviaConverter)
        {
            CommonConversions = commonConversions;
            _semanticModel = semanticModel;
            _visualBasicEqualityComparison = visualBasicEqualityComparison;
            _additionalLocals = additionalLocals;
            TriviaConvertingVisitor = new CommentConvertingVisitorWrapper<CSharpSyntaxNode>(this, triviaConverter);
            _queryConverter = new QueryConverter(commonConversions, TriviaConvertingVisitor);
            _csCompilation = csCompilation;
            _withBlockTempVariableNames = withBlockTempVariableNames;
            _methodsWithHandles = methodsWithHandles;
            _convertMethodsLookupByReturnType = CreateConvertMethodsLookupByReturnType(semanticModel);
        }

        private static Dictionary<ITypeSymbol, string> CreateConvertMethodsLookupByReturnType(SemanticModel semanticModel)
        {
            var systemDotConvert = ConvertType.FullName;
            var convertMethods = semanticModel.Compilation.GetTypeByMetadataName(systemDotConvert).GetMembers().Where(m =>
                m.Name.StartsWith("To", StringComparison.Ordinal) && m.GetParameters().Length == 1);
            var methodsByType = convertMethods.Where(m => m.Name != nameof(Convert.ToBase64String))
                .GroupBy(m => new { ReturnType = m.GetReturnType(), Name = $"{systemDotConvert}.{m.Name}" })
                .ToDictionary(m => m.Key.ReturnType, m => m.Key.Name);
            return methodsByType;
        }

        public CommonConversions CommonConversions { get; }

        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        public override CSharpSyntaxNode VisitXmlElement(Microsoft.CodeAnalysis.VisualBasic.Syntax.XmlElementSyntax node)
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

        public override CSharpSyntaxNode VisitGetTypeExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.GetTypeExpressionSyntax node)
        {
            return SyntaxFactory.TypeOfExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitGlobalName(Microsoft.CodeAnalysis.VisualBasic.Syntax.GlobalNameSyntax node)
        {
            return SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
        }

        public override CSharpSyntaxNode VisitAwaitExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.AwaitExpressionSyntax node)
        {
            return SyntaxFactory.AwaitExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitCatchBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.CatchBlockSyntax node)
        {
            var stmt = node.CatchStatement;
            CatchDeclarationSyntax catcher;
            if (stmt.IdentifierName == null)
                catcher = null;
            else {
                var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, stmt.IdentifierName).Type;
                catcher = SyntaxFactory.CatchDeclaration(
                    SyntaxFactory.ParseTypeName(typeInfo.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart)),
                    ConvertIdentifier(stmt.IdentifierName.Identifier)
                );
            }

            var filter = (CatchFilterClauseSyntax)stmt.WhenClause?.Accept(TriviaConvertingVisitor);
            var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
            return SyntaxFactory.CatchClause(
                catcher,
                filter,
                SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor)))
            );
        }

        public override CSharpSyntaxNode VisitCatchFilterClause(Microsoft.CodeAnalysis.VisualBasic.Syntax.CatchFilterClauseSyntax node)
        {
            return SyntaxFactory.CatchFilterClause((ExpressionSyntax)node.Filter.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitFinallyBlock(Microsoft.CodeAnalysis.VisualBasic.Syntax.FinallyBlockSyntax node)
        {
            var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
            return SyntaxFactory.FinallyClause(SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor))));
        }

        public override CSharpSyntaxNode VisitCTypeExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.CTypeExpressionSyntax node)
        {
            var convertMethodForKeywordOrNull = GetConvertMethodForKeywordOrNull(node.Type);
            return ConvertCastExpression(node, convertMethodForKeywordOrNull);
        }

        public override CSharpSyntaxNode VisitDirectCastExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.DirectCastExpressionSyntax node)
        {
            return ConvertCastExpression(node);
        }

        public override CSharpSyntaxNode VisitPredefinedCastExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.PredefinedCastExpressionSyntax node)
        {
            var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
            if (SyntaxTokenExtensions.IsKind(node.Keyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CDateKeyword)) {

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
                : ValidSyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(node.Keyword.ConvertToken()), (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitTryCastExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.TryCastExpressionSyntax node)
        {
            return VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(node, SyntaxFactory.BinaryExpression(
                SyntaxKind.AsExpression,
                (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)
            ));
        }

        public override CSharpSyntaxNode VisitLiteralExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.LiteralExpressionSyntax node)
        {
            if (node.Token.Value == null) {
                var type = ModelExtensions.GetTypeInfo(_semanticModel, node).ConvertedType;
                if (type == null) {
                    return CommonConversions.Literal(null); //In future, we'll be able to just say "default" instead of guessing at "null" in this case
                }

                return !type.IsReferenceType ? SyntaxFactory.DefaultExpression(_semanticModel.GetCsTypeSyntax(type, node)) : CommonConversions.Literal(null);
            }
            return CommonConversions.Literal(node.Token.Value, node.Token.Text);
        }

        public override CSharpSyntaxNode VisitInterpolation(Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolationSyntax node)
        {
            return SyntaxFactory.Interpolation((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor), (InterpolationAlignmentClauseSyntax) node.AlignmentClause?.Accept(TriviaConvertingVisitor), (InterpolationFormatClauseSyntax) node.FormatClause?.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitInterpolatedStringExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringExpressionSyntax node)
        {
            var useVerbatim = node.DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
            var startToken = useVerbatim ? 
                SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedVerbatimStringStartToken, "$@\"", "$@\"", default(SyntaxTriviaList))
                : SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringStartToken, "$\"", "$\"", default(SyntaxTriviaList));
            InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax = SyntaxFactory.InterpolatedStringExpression(startToken, SyntaxFactory.List(node.Contents.Select(c => (InterpolatedStringContentSyntax)c.Accept(TriviaConvertingVisitor))), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
            return interpolatedStringExpressionSyntax;
        }

        public override CSharpSyntaxNode VisitInterpolatedStringText(Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringTextSyntax node)
        {
            var useVerbatim = node.Parent.DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
            var textForUser = CommonConversions.EscapeQuotes(node.TextToken.Text, node.TextToken.ValueText, useVerbatim);
            InterpolatedStringTextSyntax interpolatedStringTextSyntax = SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringTextToken, textForUser, node.TextToken.ValueText, default(SyntaxTriviaList)));
            return interpolatedStringTextSyntax;
        }

        public override CSharpSyntaxNode VisitInterpolationAlignmentClause(Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolationAlignmentClauseSyntax node)
        {
            return SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), (ExpressionSyntax) node.Value.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitInterpolationFormatClause(Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolationFormatClauseSyntax node)
        {
            SyntaxToken formatStringToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, node.FormatStringToken.Text, node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
            return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatStringToken);
        }

        public override CSharpSyntaxNode VisitMeExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.MeExpressionSyntax node)
        {
            return SyntaxFactory.ThisExpression();
        }

        public override CSharpSyntaxNode VisitMyBaseExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.MyBaseExpressionSyntax node)
        {
            return SyntaxFactory.BaseExpression();
        }

        public override CSharpSyntaxNode VisitParenthesizedExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ParenthesizedExpressionSyntax node)
        {
            return SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitMemberAccessExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax node)
        {
            var simpleNameSyntax = (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor);

            var nodeSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, node.Name).Symbol;
            var isDefaultProperty = nodeSymbol is IPropertySymbol p && Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.IsDefault(p);
            ExpressionSyntax left = null;
            if (node.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.MyClassExpressionSyntax) {
                if (nodeSymbol.IsStatic) {
                    var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, node.Expression);
                    left = _semanticModel.GetCsTypeSyntax(typeInfo.Type, node);
                } else {
                    left = SyntaxFactory.ThisExpression();
                    if (nodeSymbol.IsVirtual && !nodeSymbol.IsAbstract) {
                        simpleNameSyntax = SyntaxFactory.IdentifierName($"MyClass{ConvertIdentifier(node.Name.Identifier).ValueText}");
                    }
                }
            }
            if (left == null && nodeSymbol?.IsStatic == true) {
                var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, node.Expression);
                var expressionSymbolInfo = ModelExtensions.GetSymbolInfo(_semanticModel, node.Expression);
                if (typeInfo.Type != null && !expressionSymbolInfo.Symbol.IsType()) {
                    left = _semanticModel.GetCsTypeSyntax(typeInfo.Type, node);
                }
            }
            if (left == null) {
                left = (ExpressionSyntax)node.Expression?.Accept(TriviaConvertingVisitor);
            }
            if (left == null) {
                if (IsSubPartOfConditionalAccess(node)) {
                    return isDefaultProperty ? SyntaxFactory.ElementBindingExpression()
                        : (ExpressionSyntax) SyntaxFactory.MemberBindingExpression(simpleNameSyntax);
                }
                left = SyntaxFactory.IdentifierName(_withBlockTempVariableNames.Peek());
            } else if (TryGetTypePromotedModuleSymbol(node, out var promotedModuleSymbol)) {
                left = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left,
                    SyntaxFactory.IdentifierName(promotedModuleSymbol.Name));
            }
                
            if (node.Expression.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GlobalName)) {
                return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)left, simpleNameSyntax);
            }
                
            if (isDefaultProperty && left != null) {
                return left;
            }

            var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, simpleNameSyntax);
            return AddEmptyArgumentListIfImplicit(node, memberAccessExpressionSyntax);
        }

        public override CSharpSyntaxNode VisitConditionalAccessExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ConditionalAccessExpressionSyntax node)
        {
            var leftExpression = (ExpressionSyntax)node.Expression?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.IdentifierName(_withBlockTempVariableNames.Peek());
            return SyntaxFactory.ConditionalAccessExpression(leftExpression, (ExpressionSyntax)node.WhenNotNull.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitArgumentList(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax node)
        {
            if (node.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.Attribute)) {
                return CommonConversions.CreateAttributeArgumentList(node.Arguments.Select(ToAttributeArgument).ToArray());
            }
            var argumentSyntaxes = ConvertArguments(node);
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
        }

        public override CSharpSyntaxNode VisitSimpleArgument(Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax node)
        {
            var invocation = node.Parent.Parent;
            if (invocation is Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayCreationExpressionSyntax)
                return node.Expression.Accept(TriviaConvertingVisitor);
            var symbol = GetInvocationSymbol(invocation);
            SyntaxToken token = default(SyntaxToken);
            string argName = null;
            RefKind refKind = RefKind.None;
            if (symbol is IMethodSymbol methodSymbol) {
                int argId = ((Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax)node.Parent).Arguments.IndexOf(node);
                var parameters = (GetCsSymbolOrNull(methodSymbol) ?? methodSymbol).GetParameters();
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
            var expression = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor), alwaysExplicit: refKind != RefKind.None);
            AdditionalLocals.AdditionalLocal local = null;
            if (refKind != RefKind.None && NeedsVariableForArgument(node)) {
                local = _additionalLocals.AddAdditionalLocal($"arg{argName}", expression);
            }
            var nameColon = node.IsNamed ? SyntaxFactory.NameColon((IdentifierNameSyntax)node.NameColonEquals.Name.Accept(TriviaConvertingVisitor)) : null;
            if (local == null) {
                return SyntaxFactory.Argument(nameColon, token, expression);
            } else {
                return SyntaxFactory.Argument(nameColon, token, SyntaxFactory.IdentifierName(local.ID).WithAdditionalAnnotations(AdditionalLocals.Annotation));
            }
        }

        public override CSharpSyntaxNode VisitNameOfExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.NameOfExpressionSyntax node)
        {
            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax)node.Argument.Accept(TriviaConvertingVisitor)))));
        }

        public override CSharpSyntaxNode VisitEqualsValue(Microsoft.CodeAnalysis.VisualBasic.Syntax.EqualsValueSyntax node)
        {
            return SyntaxFactory.EqualsValueClause((ExpressionSyntax)node.Value.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitObjectMemberInitializer(Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectMemberInitializerSyntax node)
        {
            var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                node.Initializers.Select(initializer => initializer.Accept(TriviaConvertingVisitor)).Cast<ExpressionSyntax>());
            return SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, memberDeclaratorSyntaxs);
        }

        public override CSharpSyntaxNode VisitAnonymousObjectCreationExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.AnonymousObjectCreationExpressionSyntax node)
        {
            var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                node.Initializer.Initializers.Select(initializer => initializer.Accept(TriviaConvertingVisitor)).Cast<AnonymousObjectMemberDeclaratorSyntax>());
            return SyntaxFactory.AnonymousObjectCreationExpression(memberDeclaratorSyntaxs);
        }

        public override CSharpSyntaxNode VisitInferredFieldInitializer(Microsoft.CodeAnalysis.VisualBasic.Syntax.InferredFieldInitializerSyntax node)
        {
            return SyntaxFactory.AnonymousObjectMemberDeclarator((ExpressionSyntax) node.Expression.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitObjectCreationExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCreationExpressionSyntax node)
        {
            return SyntaxFactory.ObjectCreationExpression(
                (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor),
                // VB can omit empty arg lists:
                (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList(),
                (InitializerExpressionSyntax)node.Initializer?.Accept(TriviaConvertingVisitor)
            );
        }

        public override CSharpSyntaxNode VisitArrayCreationExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayCreationExpressionSyntax node)
        {
            var bounds = CommonConversions.ConvertArrayRankSpecifierSyntaxes(node.RankSpecifiers, node.ArrayBounds);
            var allowInitializer = node.Initializer.Initializers.Any() || node.ArrayBounds == null ||
                                   node.ArrayBounds.Arguments.All(b => b.IsOmitted || _semanticModel.GetConstantValue(b.GetExpression()).HasValue);
            var initializerToConvert = allowInitializer ? node.Initializer : null;
            return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor), bounds),
                (InitializerExpressionSyntax)initializerToConvert?.Accept(TriviaConvertingVisitor)
            );
        }

        public override CSharpSyntaxNode VisitCollectionInitializer(Microsoft.CodeAnalysis.VisualBasic.Syntax.CollectionInitializerSyntax node)
        {
            var isExplicitCollectionInitializer = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCollectionInitializerSyntax
                                                  || node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.CollectionInitializerSyntax
                                                  || node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayCreationExpressionSyntax;
            var initializerType = isExplicitCollectionInitializer ? SyntaxKind.CollectionInitializerExpression : SyntaxKind.ArrayInitializerExpression;
            var initializer = SyntaxFactory.InitializerExpression(initializerType, SyntaxFactory.SeparatedList(node.Initializers.Select(i => (ExpressionSyntax)i.Accept(TriviaConvertingVisitor))));
            return isExplicitCollectionInitializer
                ? initializer
                : (CSharpSyntaxNode)SyntaxFactory.ImplicitArrayCreationExpression(initializer);
        }

        public override CSharpSyntaxNode VisitQueryExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.QueryExpressionSyntax node)
        {
            return _queryConverter.ConvertClauses(node.Clauses);
        }

        public override CSharpSyntaxNode VisitOrdering(Microsoft.CodeAnalysis.VisualBasic.Syntax.OrderingSyntax node)
        {
            var convertToken = node.Kind().ConvertToken();
            var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
            var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
            return SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
        }

        public override CSharpSyntaxNode VisitNamedFieldInitializer(Microsoft.CodeAnalysis.VisualBasic.Syntax.NamedFieldInitializerSyntax node)
        {
            if (node?.Parent?.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.AnonymousObjectCreationExpressionSyntax) {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Name.Identifier))),
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                (ExpressionSyntax)node.Name.Accept(TriviaConvertingVisitor),
                (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
            );
        }

        public override CSharpSyntaxNode VisitObjectCollectionInitializer(Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCollectionInitializerSyntax node)
        {
            return node.Initializer.Accept(TriviaConvertingVisitor); //Dictionary initializer comes through here despite the FROM keyword not being in the source code
        }

        public override CSharpSyntaxNode VisitBinaryConditionalExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryConditionalExpressionSyntax node)
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                (ExpressionSyntax)node.FirstExpression.Accept(TriviaConvertingVisitor),
                (ExpressionSyntax)node.SecondExpression.Accept(TriviaConvertingVisitor)
            );
        }

        public override CSharpSyntaxNode VisitTernaryConditionalExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.TernaryConditionalExpressionSyntax node)
        {
            var expr = SyntaxFactory.ConditionalExpression(
                (ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor),
                (ExpressionSyntax)node.WhenTrue.Accept(TriviaConvertingVisitor),
                (ExpressionSyntax)node.WhenFalse.Accept(TriviaConvertingVisitor)
            );

            if (node.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.Interpolation) || VbSyntaxNodeExtensions.PrecedenceCouldChange(node))
                return SyntaxFactory.ParenthesizedExpression(expr);

            return expr;
        }

        public override CSharpSyntaxNode VisitTypeOfExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeOfExpressionSyntax node)
        {
            var expr = SyntaxFactory.BinaryExpression(
                SyntaxKind.IsExpression,
                (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)
            );
            return node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.TypeOfIsNotExpression) ? expr.InvertCondition() : expr;
        }

        public override CSharpSyntaxNode VisitUnaryExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.UnaryExpressionSyntax node)
        {
            var expr = (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor);
            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddressOfExpression))
                return expr;
            var kind = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
            SyntaxKind csTokenKind = CSharpUtil.GetExpressionOperatorTokenKind(kind);
            return SyntaxFactory.PrefixUnaryExpression(
                kind,
                SyntaxFactory.Token(csTokenKind),
                expr.AddParensIfRequired()
            );
        }

        public override CSharpSyntaxNode VisitBinaryExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax node)
        {
            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IsExpression)) {
                ExpressionSyntax otherArgument = null;
                if (node.Left.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                }
                if (node.Right.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                }
                if (otherArgument != null) {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                }
            }
            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IsNotExpression)) {
                ExpressionSyntax otherArgument = null;
                if (node.Left.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                }
                if (node.Right.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                }
                if (otherArgument != null) {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                }
            }

            var lhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Left, (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor));
            var rhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor));

            var stringType = _semanticModel.Compilation.GetTypeByMetadataName("System.String");
            var lhsTypeInfo = ModelExtensions.GetTypeInfo(_semanticModel, node.Left);
            var rhsTypeInfo = ModelExtensions.GetTypeInfo(_semanticModel, node.Right);

            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConcatenateExpression)) {
                if (lhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                    lhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String &&
                    rhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                    rhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String) {
                    lhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConvertTo(node.Left, lhs, stringType);
                }
            }

            var objectEqualityType = _visualBasicEqualityComparison.GetObjectEqualityType(node,  lhsTypeInfo, rhsTypeInfo);
            switch(objectEqualityType) {
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

            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ExponentiateExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                return SyntaxFactory.InvocationExpression(
                    ValidSyntaxFactory.MemberAccess(nameof(Math), nameof(Math.Pow)),
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
            }

            if (node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LikeExpression)) {
                var compareText = ValidSyntaxFactory.MemberAccess("CompareMethod", _optionCompareText ? "Text" : "Binary");
                var likeString = ValidSyntaxFactory.MemberAccess("LikeOperator", "LikeString");
                _extraUsingDirectives.Add("Microsoft.VisualBasic");
                _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
                return SyntaxFactory.InvocationExpression(
                    likeString,
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs, compareText)
                );
            }

            var kind = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
            var op = SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));

            var csBinExp = SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node, csBinExp, addParenthesisIfNeeded: true);
        }

        public override CSharpSyntaxNode VisitInvocationExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax node)
        {
            var invocationSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, node).ExtractBestMatch();
            var expressionSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, node.Expression).ExtractBestMatch();
            var expressionReturnType = expressionSymbol?.GetReturnType() ?? ModelExtensions.GetTypeInfo(_semanticModel, node.Expression).Type;
            var operation = _semanticModel.GetOperation(node);
            if (expressionSymbol?.ContainingNamespace.MetadataName == "VisualBasic" && TrySubstituteVisualBasicMethod(node, out var csEquivalent)) {
                return csEquivalent;
            }

            var overrideIdentifier = CommonConversions.GetParameterizedPropertyAccessMethod(operation, out var extraArg);
            if (overrideIdentifier != null) {
                var expr = node.Expression.Accept(TriviaConvertingVisitor);
                var idToken = expr.DescendantTokens().Last(t => t.IsKind(SyntaxKind.IdentifierToken));
                expr = ReplaceRightmostIdentifierText(expr, idToken, overrideIdentifier);

                var args = ConvertArgumentListOrEmpty(node.ArgumentList);
                if (extraArg != null) {
                    args = args.WithArguments(args.Arguments.Add(SyntaxFactory.Argument(extraArg)));
                }
                return SyntaxFactory.InvocationExpression((ExpressionSyntax) expr, args);
            }

            // VB doesn't have a specialized node for element access because the syntax is ambiguous. Instead, it just uses an invocation expression or dictionary access expression, then figures out using the semantic model which one is most likely intended.
            // https://github.com/dotnet/roslyn/blob/master/src/Workspaces/VisualBasic/Portable/LanguageServices/VisualBasicSyntaxFactsService.vb#L768
            var convertedExpression = ConvertInvocationSubExpression(out var shouldBeElementAccess);
            if (shouldBeElementAccess) {
                return CreateElementAccess();
            }

            if (expressionSymbol != null && expressionSymbol.IsKind(SymbolKind.Property)) {
                return convertedExpression; //Parameterless property access
            }

            if (invocationSymbol?.Name == nameof(Enumerable.ElementAtOrDefault) && !expressionSymbol.Equals(invocationSymbol)) {
                _extraUsingDirectives.Add(nameof(System) + "." + nameof(System.Linq));
                convertedExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, convertedExpression,
                    SyntaxFactory.IdentifierName(nameof(Enumerable.ElementAtOrDefault)));
            }

            return SyntaxFactory.InvocationExpression(convertedExpression, ConvertArgumentListOrEmpty(node.ArgumentList));

            ExpressionSyntax ConvertInvocationSubExpression(out bool isElementAccess)
            {
                isElementAccess = IsPropertyElementAccess(operation) ||
                                  IsArrayElementAccess(operation) ||
                                  ProbablyNotAMethodCall(node, expressionSymbol, expressionReturnType);

                var expr = node.Expression.Accept(TriviaConvertingVisitor);
                return (ExpressionSyntax) expr;
            }

            CSharpSyntaxNode CreateElementAccess()
            {
                var bracketedArgumentListSyntax = SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(
                    node.ArgumentList.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))
                ));
                if (convertedExpression is ElementBindingExpressionSyntax binding && !binding.ArgumentList.Arguments.Any())
                {
                    // Special case where structure changes due to conditional access (See VisitMemberAccessExpression)
                    return binding.WithArgumentList(bracketedArgumentListSyntax);
                }
                else
                {
                    return SyntaxFactory.ElementAccessExpression(convertedExpression,bracketedArgumentListSyntax);
                }
            }
        }

        public override CSharpSyntaxNode VisitSingleLineLambdaExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.SingleLineLambdaExpressionSyntax node)
        {
            CSharpSyntaxNode body;
            if (node.Body is Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax statement) {
                var convertedStatements = statement.Accept(CreateMethodBodyVisitor(node));
                if (convertedStatements.Count == 1
                    && convertedStatements.Single() is ExpressionStatementSyntax exprStmt) {
                    // Assignment is an example of a statement in VB that becomes an expression in C#
                    body = exprStmt.Expression;
                } else {
                    body = SyntaxFactory.Block(convertedStatements).UnpackNonNestedBlock();
                }
            }
            else {
                body = node.Body.Accept(TriviaConvertingVisitor);
            }
            var param = (ParameterListSyntax)node.SubOrFunctionHeader.ParameterList.Accept(TriviaConvertingVisitor);
            return CreateLambdaExpression(param, body);
        }

        public override CSharpSyntaxNode VisitMultiLineLambdaExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.MultiLineLambdaExpressionSyntax node)
        {
            var methodBodyVisitor = CreateMethodBodyVisitor(node);
            var body = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor)));
            var param = (ParameterListSyntax)node.SubOrFunctionHeader.ParameterList.Accept(TriviaConvertingVisitor);
            return CreateLambdaExpression(param, body);
        }

        public override CSharpSyntaxNode VisitTupleType(Microsoft.CodeAnalysis.VisualBasic.Syntax.TupleTypeSyntax node)
        {
            var elements = node.Elements.Select(e => (TupleElementSyntax)e.Accept(TriviaConvertingVisitor));
            return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
        }

        public override CSharpSyntaxNode VisitTypedTupleElement(Microsoft.CodeAnalysis.VisualBasic.Syntax.TypedTupleElementSyntax node)
        {
            return SyntaxFactory.TupleElement((TypeSyntax) node.Type.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitNamedTupleElement(Microsoft.CodeAnalysis.VisualBasic.Syntax.NamedTupleElementSyntax node)
        {
            return SyntaxFactory.TupleElement((TypeSyntax)node.AsClause.Type.Accept(TriviaConvertingVisitor), CommonConversions.ConvertIdentifier(node.Identifier));
        }

        public override CSharpSyntaxNode VisitTupleExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.TupleExpressionSyntax node)
        {
            var args = node.Arguments.Select(a => {
                var expr = (ExpressionSyntax)a.Expression.Accept(TriviaConvertingVisitor);
                return SyntaxFactory.Argument(expr);
            });
            return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(args));
        }

        public override CSharpSyntaxNode VisitPredefinedType(Microsoft.CodeAnalysis.VisualBasic.Syntax.PredefinedTypeSyntax node)
        {
            if (SyntaxTokenExtensions.IsKind(node.Keyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DateKeyword)) {
                return SyntaxFactory.IdentifierName("DateTime");
            }
            return SyntaxFactory.PredefinedType(node.Keyword.ConvertToken());
        }

        public override CSharpSyntaxNode VisitNullableType(Microsoft.CodeAnalysis.VisualBasic.Syntax.NullableTypeSyntax node)
        {
            return SyntaxFactory.NullableType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitArrayType(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayTypeSyntax node)
        {
            return SyntaxFactory.ArrayType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor), SyntaxFactory.List(node.RankSpecifiers.Select(r => (ArrayRankSpecifierSyntax)r.Accept(TriviaConvertingVisitor))));
        }

        public override CSharpSyntaxNode VisitArrayRankSpecifier(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayRankSpecifierSyntax node)
        {
            return SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), node.Rank)));
        }

        public override CSharpSyntaxNode VisitIdentifierName(Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax node)
        {
            var identifier = SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier, node.GetAncestor<Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax>() != null));

            var qualifiedIdentifier = !node.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SimpleMemberAccessExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.QualifiedName, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NameColonEquals, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ImportsStatement, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NamespaceStatement, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NamedFieldInitializer)
                                      || node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression == node
                                      || node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.QualifiedNameSyntax qns && qns.Left == node
                ? QualifyNode(node, identifier) : identifier;

            var withArgList = AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
            var sym = GetSymbolInfoInDocument(node);
            if (sym != null && sym.Kind == SymbolKind.Local) {
                var vbMethodBlock = node.Ancestors().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax>().FirstOrDefault();
                if (vbMethodBlock != null &&
                    !node.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NameOfExpression) &&
                    node.Identifier.ValueText.Equals(GetMethodBlockBaseIdentifierForImplicitReturn(vbMethodBlock).ValueText, StringComparison.OrdinalIgnoreCase)) {
                    var retVar = GetRetVariableNameOrNull(vbMethodBlock);
                    if (retVar != null) {
                        return retVar;
                    }
                }
            }
            return withArgList;
        }

        public override CSharpSyntaxNode VisitQualifiedName(Microsoft.CodeAnalysis.VisualBasic.Syntax.QualifiedNameSyntax node)
        {
            var lhsSyntax = (NameSyntax)node.Left.Accept(TriviaConvertingVisitor);
            var rhsSyntax = (SimpleNameSyntax)node.Right.Accept(TriviaConvertingVisitor);

            Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax topLevelName = node;
            while (topLevelName.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax parentName)
            {
                topLevelName = parentName;
            }
            var partOfNamespaceDeclaration = topLevelName.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NamespaceStatement);
            var leftIsGlobal = node.Left.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GlobalName);

            ExpressionSyntax qualifiedName;
            if (partOfNamespaceDeclaration || !(lhsSyntax is SimpleNameSyntax sns)) {
                if (leftIsGlobal) return rhsSyntax;
                qualifiedName = lhsSyntax;
            } else {
                qualifiedName = QualifyNode(node.Left, sns);
            }

            return leftIsGlobal
                ? (CSharpSyntaxNode)SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)lhsSyntax, rhsSyntax)
                : SyntaxFactory.QualifiedName((NameSyntax) qualifiedName, rhsSyntax);
        }

        public override CSharpSyntaxNode VisitGenericName(Microsoft.CodeAnalysis.VisualBasic.Syntax.GenericNameSyntax node)
        {
            return SyntaxFactory.GenericName(ConvertIdentifier(node.Identifier), (TypeArgumentListSyntax)node.TypeArgumentList?.Accept(TriviaConvertingVisitor));
        }

        public override CSharpSyntaxNode VisitTypeArgumentList(Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeArgumentListSyntax node)
        {
            return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (TypeSyntax)a.Accept(TriviaConvertingVisitor))));
        }

        public static bool AllowsImplicitReturn(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax node)
        {
            return !IsIterator(node) && node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FunctionBlock, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GetAccessorBlock);
        }

        public static bool IsIterator(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax node)
        {
            return node.BlockStatement.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IteratorKeyword));
        }

        public IdentifierNameSyntax GetRetVariableNameOrNull(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax node)
        {
            if (!AllowsImplicitReturn(node)) return null;

            bool assignsToMethodNameVariable = false;

            if (!node.Statements.IsEmpty()) {
                string methodName = GetMethodBlockBaseIdentifierForImplicitReturn(node).ValueText;
                Func<ISymbol, bool> equalsMethodName = s => s.IsKind(SymbolKind.Local) && s.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase);
                var flow = ModelExtensions.AnalyzeDataFlow(_semanticModel, node.Statements.First(), node.Statements.Last());

                if (flow.Succeeded) {
                    assignsToMethodNameVariable = flow.ReadInside.Any(equalsMethodName) || flow.WrittenInside.Any(equalsMethodName);
                }
            }

            IdentifierNameSyntax csReturnVariable = null;

            if (assignsToMethodNameVariable)
            {
                // In VB, assigning to the method name implicitly creates a variable that is returned when the method exits
                var csReturnVariableName =
                    CommonConversions.ConvertIdentifier(GetMethodBlockBaseIdentifierForImplicitReturn(node)).ValueText + "Ret";
                csReturnVariable = SyntaxFactory.IdentifierName(csReturnVariableName);
            }

            return csReturnVariable;
        }

        public Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode node, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
        {
            var methodBodyVisitor = new MethodBodyExecutableStatementVisitor(node, _semanticModel, TriviaConvertingVisitor, CommonConversions, _withBlockTempVariableNames, _extraUsingDirectives, _additionalLocals, _methodsWithHandles, TriviaConvertingVisitor.TriviaConverter) {
                IsIterator = isIterator,
                ReturnVariable = csReturnVariable,
            };
            return methodBodyVisitor.CommentConvertingVisitor;
        }

        private CSharpSyntaxNode ConvertCastExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.CastExpressionSyntax node, ExpressionSyntax convertMethodOrNull = null)
        {
            var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);

            if (convertMethodOrNull != null)
            {
                return
                    SyntaxFactory.InvocationExpression(convertMethodOrNull,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(expressionSyntax)))
                    );
            }

            var castExpr = ValidSyntaxFactory.CastExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor), expressionSyntax);
            if (node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax)
            {
                return (ExpressionSyntax)SyntaxFactory.ParenthesizedExpression(castExpr);
            }
            return castExpr;
        }

        private ExpressionSyntax GetConvertMethodForKeywordOrNull(SyntaxNode type)
        {
            var convertedType = _semanticModel.GetTypeInfo(type).Type;
            return _convertMethodsLookupByReturnType.TryGetValue(convertedType, out var convertMethodName)
                ? SyntaxFactory.ParseExpression(convertMethodName) : null;
        }

        /// <remarks>https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion</remarks>
        private bool TryGetTypePromotedModuleSymbol(Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax node, out INamedTypeSymbol moduleSymbol)
        {
            if (ModelExtensions.GetSymbolInfo(_semanticModel, node.Expression).ExtractBestMatch() is INamespaceSymbol
                    expressionSymbol &&
                ModelExtensions.GetSymbolInfo(_semanticModel, node.Name).ExtractBestMatch()?.ContainingSymbol is INamedTypeSymbol
                    nameContainingSymbol &&
                nameContainingSymbol.ContainingSymbol.Equals(expressionSymbol)) {
                moduleSymbol = nameContainingSymbol;
                return true;
            }

            moduleSymbol = null;
            return false;
        }

        private static bool IsSubPartOfConditionalAccess(Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax node)
        {
            var firstPossiblyConditionalAncestor = node.Parent;
            while (firstPossiblyConditionalAncestor != null &&
                   firstPossiblyConditionalAncestor.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.InvocationExpression,
                       Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SimpleMemberAccessExpression))
            {
                firstPossiblyConditionalAncestor = firstPossiblyConditionalAncestor.Parent;
            }

            return firstPossiblyConditionalAncestor?.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConditionalAccessExpression) == true;
        }

        private IEnumerable<ArgumentSyntax> ConvertArguments(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax node)
        {
            ISymbol invocationSymbolForForcedNames = null;
            var argumentSyntaxs = node.Arguments.Select((a, i) =>
            {
                if (a.IsOmitted)
                {
                    invocationSymbolForForcedNames = GetInvocationSymbol(node.Parent);
                    if (invocationSymbolForForcedNames != null)
                    {
                        return null;
                    }

                    var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                        .WithTrailingTrivia(
                            SyntaxFactory.Comment("/* Conversion error: Set to default value for this argument */"));
                    return SyntaxFactory.Argument(nullLiteral);
                }

                var argumentSyntax = (ArgumentSyntax) a.Accept(TriviaConvertingVisitor);

                if (invocationSymbolForForcedNames != null)
                {
                    var elementAtOrDefault = invocationSymbolForForcedNames.GetParameters().ElementAt(i).Name;
                    return argumentSyntax.WithNameColon(SyntaxFactory.NameColon(elementAtOrDefault));
                }

                return argumentSyntax;
            }).Where(a => a != null);
            return argumentSyntaxs;
        }

        public ISymbol GetCsSymbolOrNull(ISymbol symbol)
        {
            // Construct throws an exception if ConstructedFrom differs from it, so let's use ConstructedFrom directly
            ISymbol symbolToFind = symbol is IMethodSymbol m ? m.ConstructedFrom : symbol;
            return SymbolFinder.FindSimilarSymbols(symbolToFind, _csCompilation).FirstOrDefault();
        }

        private bool NeedsVariableForArgument(Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax node)
        {
            bool isIdentifier = node.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
            bool isMemberAccess = node.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax;

            var symbolInfo = GetSymbolInfoInDocument(node.Expression);
            bool isProperty = symbolInfo != null && SymbolExtensions.IsKind(symbolInfo, SymbolKind.Property);
            bool isUsing = symbolInfo?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.Parent?.Parent?.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.UsingStatement) == true;

            var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, node.Expression);
            bool isTypeMismatch = typeInfo.Type == null || !typeInfo.Type.Equals(typeInfo.ConvertedType);

            return (!isIdentifier && !isMemberAccess) || isProperty || isTypeMismatch || isUsing;
        }

        private ISymbol GetInvocationSymbol(SyntaxNode invocation)
        {
            var symbol = invocation.TypeSwitch(
                (Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax e) => ModelExtensions.GetSymbolInfo(_semanticModel, e).ExtractBestMatch(),
                (Microsoft.CodeAnalysis.VisualBasic.Syntax.ObjectCreationExpressionSyntax e) => ModelExtensions.GetSymbolInfo(_semanticModel, e).ExtractBestMatch(),
                (Microsoft.CodeAnalysis.VisualBasic.Syntax.RaiseEventStatementSyntax e) => ModelExtensions.GetSymbolInfo(_semanticModel, e.Name).ExtractBestMatch(),
                _ => { throw new NotSupportedException(); }
            );
            return symbol;
        }

        private AttributeArgumentSyntax ToAttributeArgument(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax arg)
        {
            if (!(arg is Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax))
                throw new NotSupportedException();
            var a = (Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax)arg;
            var attr = SyntaxFactory.AttributeArgument((ExpressionSyntax)a.Expression.Accept(TriviaConvertingVisitor));
            if (a.IsNamed) {
                attr = attr.WithNameEquals(SyntaxFactory.NameEquals((IdentifierNameSyntax)a.NameColonEquals.Name.Accept(TriviaConvertingVisitor)));
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
            return operation is IPropertyReferenceOperation pro && pro.Arguments.Any() && Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.IsDefault(pro.Property);
        }

        private static bool IsArrayElementAccess(IOperation operation)
        {
            return operation != null && operation.Kind == OperationKind.ArrayElementReference;
        }

        /// <summary>
        /// Chances of having an unknown delegate stored as a field/local seem lower than having an unknown non-delegate type with an indexer stored.
        /// So for a standalone identifier err on the side of assuming it's an indexer.
        /// </summary>
        private static bool ProbablyNotAMethodCall(Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax node, ISymbol symbol, ITypeSymbol symbolReturnType)
        {
            return !(symbol is IMethodSymbol) && symbolReturnType.IsErrorType() && node.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax && node.ArgumentList.Arguments.Any();
        }

        private ArgumentListSyntax ConvertArgumentListOrEmpty(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax argumentListSyntax)
        {
            return (ArgumentListSyntax)argumentListSyntax?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList();
        }

        private bool TrySubstituteVisualBasicMethod(Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax node, out CSharpSyntaxNode cSharpSyntaxNode)
        {
            cSharpSyntaxNode = null;
            var symbol = ModelExtensions.GetSymbolInfo(_semanticModel, node.Expression).ExtractBestMatch();
            if (symbol?.Name == "ChrW" || symbol?.Name == "Chr") {
                cSharpSyntaxNode = ValidSyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("char"),
                    ConvertArguments(node.ArgumentList).Single().Expression);
            }

            return cSharpSyntaxNode != null;
        }

        private static CSharpSyntaxNode CreateLambdaExpression(ParameterListSyntax param, CSharpSyntaxNode body)
        {
            if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null)
                return SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
            return SyntaxFactory.ParenthesizedLambdaExpression(param, body);
        }

        private static SyntaxToken GetMethodBlockBaseIdentifierForImplicitReturn(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax vbMethodBlock)
        {
            if (vbMethodBlock.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax pb) {
                return pb.PropertyStatement.Identifier;
            } else if (vbMethodBlock is Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockSyntax mb) {
                return mb.SubOrFunctionStatement.Identifier;
            } else {
                return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.Token(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.EmptyToken);
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
                    !SymbolExtensions.IsConstructor(nodeSymbolInfo) /* Constructors are implicitly qualified with their type */) {
                    // Qualify with a type to handle VB's type promotion https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion
                    var qualification =
                        containingTypeSymbol.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart);
                    return Qualify(qualification, left);
                } else if (SymbolExtensions.IsNamespace(nodeSymbolInfo)) {
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
            var implicitCsQualifications = ((ITypeSymbol) typeContext).GetBaseTypesAndThis()
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