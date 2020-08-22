using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
using LambdaExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.LambdaExpressionSyntax;
using NameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax;
using ParameterListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterListSyntax;
using ParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;
using ReturnStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ReturnStatementSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax;
using YieldStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.YieldStatementSyntax;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CommonConversions
    {
        public SyntaxGenerator VbSyntaxGenerator { get; }
        private readonly CommentConvertingVisitorWrapper<VisualBasicSyntaxNode> _nodesVisitor;
        private readonly SemanticModel _semanticModel;

        public CommonConversions(SemanticModel semanticModel, SyntaxGenerator vbSyntaxGenerator,
            CommentConvertingVisitorWrapper<VisualBasicSyntaxNode> nodesVisitor)
        {
            VbSyntaxGenerator = vbSyntaxGenerator;
            _semanticModel = semanticModel;
            _nodesVisitor = nodesVisitor;
        }

        public SyntaxList<StatementSyntax> ConvertBody(CSS.BlockSyntax body,
            CSS.ArrowExpressionClauseSyntax expressionBody, bool hasReturnType, MethodBodyExecutableStatementVisitor iteratorState = null)
        {
            if (body != null) {
                return ConvertStatements(body.Statements, iteratorState);
            }

            if (expressionBody != null) {
                var convertedBody = expressionBody.Expression.Accept(_nodesVisitor);
                if (convertedBody is ExpressionSyntax convertedBodyExpression) {
                    convertedBody = hasReturnType ? (ExecutableStatementSyntax)SyntaxFactory.ReturnStatement(convertedBodyExpression)
                        : SyntaxFactory.ExpressionStatement(convertedBodyExpression);
                }

                return SyntaxFactory.SingletonList((StatementSyntax)convertedBody);
            }

            return SyntaxFactory.List<StatementSyntax>();
        }

        private SyntaxList<StatementSyntax> ConvertStatements(SyntaxList<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax> statements,
            MethodBodyExecutableStatementVisitor iteratorState = null)
        {
            var methodBodyVisitor = CreateMethodBodyVisitor(iteratorState);
            return SyntaxFactory.List(statements.SelectMany(s => ConvertStatement(s, methodBodyVisitor)));
        }

        private SyntaxList<StatementSyntax> ConvertStatement(Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax statement, CS.CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> methodBodyVisitor)
        {
            var convertedStatements = statement.Accept(methodBodyVisitor);
            convertedStatements = InsertRequiredDeclarations(convertedStatements, statement);

            return convertedStatements;
        }

        private SyntaxList<StatementSyntax> InsertRequiredDeclarations(
            SyntaxList<StatementSyntax> convertedStatements, CS.CSharpSyntaxNode originaNode)
        {
            var descendantNodes = originaNode.DescendantNodes().ToList();
            var declarationExpressions = descendantNodes.OfType<CSS.DeclarationExpressionSyntax>()
                .Where(e => !e.Parent.IsKind(CSSyntaxKind.ForEachVariableStatement)) //Handled inline for tuple loop
                .ToList();
            var isPatternExpressions = descendantNodes.OfType<CSS.IsPatternExpressionSyntax>().ToList();
            if (declarationExpressions.Any() || isPatternExpressions.Any()) {
                convertedStatements = convertedStatements.InsertRange(0, ConvertToDeclarationStatement(declarationExpressions, isPatternExpressions));
            }

            return convertedStatements;
        }

        public SyntaxList<StatementSyntax> InsertGeneratedClassMemberDeclarations(SyntaxList<StatementSyntax> convertedStatements, CSS.TypeDeclarationSyntax typeNode, bool isModule) {
            var propertyBlocks = typeNode.Members.OfType<CSS.PropertyDeclarationSyntax>()
                .Where(e => e.AccessorList != null && e.AccessorList.Accessors.Any(a => a.Body == null && a.ExpressionBody == null && a.Modifiers.ContainsDeclaredVisibility()))
                .ToList();
            return convertedStatements.InsertRange(0, ConvertToDeclarationStatement(propertyBlocks, isModule));
        }

        private IEnumerable<StatementSyntax> ConvertToDeclarationStatement(List<CSS.DeclarationExpressionSyntax> des,
            List<CSS.IsPatternExpressionSyntax> isPatternExpressions)
        {
            IEnumerable<VariableDeclaratorSyntax> variableDeclaratorSyntaxs = des.Select(ConvertToVariableDeclarator)
                .Concat(isPatternExpressions.Select(ConvertToVariableDeclaratorOrNull).Where(x => x != null));
            var variableDeclaratorSyntaxes = variableDeclaratorSyntaxs.ToArray();
            return variableDeclaratorSyntaxes.Any() ? new StatementSyntax[] { CreateLocalDeclarationStatement(variableDeclaratorSyntaxes) } : Enumerable.Empty<StatementSyntax>();
        }

        private IEnumerable<StatementSyntax> ConvertToDeclarationStatement(List<CSS.PropertyDeclarationSyntax> propertyBlocks, bool isModule)
        {
            var variableDeclaratorSyntaxs = propertyBlocks.GroupBy(x => x.Modifiers.Any(CSSyntaxKind.StaticKeyword)).ToList();
            var shared = variableDeclaratorSyntaxs.Where(x => x.Key).SelectMany(x => x.Select(ConvertToVariableDeclarator));
            var instance = variableDeclaratorSyntaxs.Where(x => !x.Key).SelectMany(x => x.Select(ConvertToVariableDeclarator));
            var privateTokens = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            var privateSharedTokens = !isModule ? privateTokens.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword)) : privateTokens;
            return new[] {
                CreateLocalDeclarationStatement(privateSharedTokens, shared.ToArray()),
                CreateLocalDeclarationStatement(privateTokens, instance.ToArray())
            }.Where(x => x != null);
        }

        public static StatementSyntax CreateLocalDeclarationStatement(params VariableDeclaratorSyntax[] variableDeclarators)
        {
            var syntaxTokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword));
            var declarators = SyntaxFactory.SeparatedList(variableDeclarators);
            return SyntaxFactory.LocalDeclarationStatement(syntaxTokenList, declarators);
        }

        public static StatementSyntax CreateLocalDeclarationStatement(SyntaxTokenList syntaxTokenList, params VariableDeclaratorSyntax[] DimVariableDeclarators)
        {
            if (DimVariableDeclarators.Length == 0)
                return null;
            var declarators = SyntaxFactory.SeparatedList(DimVariableDeclarators);
            return SyntaxFactory.LocalDeclarationStatement(syntaxTokenList, declarators);
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclarator(CSS.DeclarationExpressionSyntax des)
        {
            var id = ((IdentifierNameSyntax)des.Accept(_nodesVisitor)).Identifier;
            var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
            TypeSyntax typeSyntax;
            if (des.Type.IsVar) {
                var typeSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, des.Type).ExtractBestMatch<ITypeSymbol>();
                typeSyntax = typeSymbol?.ToVbSyntax(_semanticModel, des.Type);
            } else {
                typeSyntax = (TypeSyntax)des.Type.Accept(_nodesVisitor);
            }

            var simpleAsClauseSyntax = typeSyntax != null ? SyntaxFactory.SimpleAsClause(typeSyntax) : null; //Gracefully degrade when no type information available
            var equalsValueSyntax = SyntaxFactory.EqualsValue(SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression, SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
            return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclaratorOrNull(CSS.IsPatternExpressionSyntax node)
        {
            switch (node.Pattern) {
                case CSS.DeclarationPatternSyntax d: {
                        var id = ((IdentifierNameSyntax)d.Designation.Accept(_nodesVisitor)).Identifier;
                        var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
                        TypeSyntax right = (TypeSyntax)d.Type.Accept(_nodesVisitor);

                        var simpleAsClauseSyntax = SyntaxFactory.SimpleAsClause(right);
                        var equalsValueSyntax = SyntaxFactory.EqualsValue(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression,
                                SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
                        return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
                    }
                case CSS.ConstantPatternSyntax _:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node.Pattern), node.Pattern, null);
            }
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclarator(CSS.PropertyDeclarationSyntax des)
        {
            var id = GetVbPropertyBackingFieldName(des);
            var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
            TypeSyntax typeSyntax;
            if (des.Type.IsVar) {
                var typeSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, des.Type).ExtractBestMatch<ITypeSymbol>();
                typeSyntax = typeSymbol?.ToVbSyntax(_semanticModel, des.Type);
            } else {
                typeSyntax = (TypeSyntax)des.Type.Accept(_nodesVisitor);
            }

            var simpleAsClauseSyntax = typeSyntax != null ? SyntaxFactory.SimpleAsClause(typeSyntax) : null; //Gracefully degrade when no type information available
            EqualsValueSyntax equalsValueSyntax = null;
            return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
        }

        private CS.CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(MethodBodyExecutableStatementVisitor methodBodyExecutableStatementVisitor = null)
        {
            var visitor = methodBodyExecutableStatementVisitor ?? new MethodBodyExecutableStatementVisitor(_semanticModel, _nodesVisitor, this);
            return visitor.CommentConvertingVisitor;
        }

        public AccessorBlockSyntax ConvertAccessor(CSS.AccessorDeclarationSyntax node, out bool isIterator, bool isAutoImplementedProperty = false)
        {
            SyntaxKind blockKind;
            AccessorStatementSyntax stmt;
            EndBlockStatementSyntax endStmt;
            SyntaxList<StatementSyntax> body;
            isIterator = false;
            var accesorKind = CSharpExtensions.Kind(node);
            var isIteratorState = new MethodBodyExecutableStatementVisitor(_semanticModel, _nodesVisitor, this);
            body = ConvertBody(node.Body, node.ExpressionBody, accesorKind == CSSyntaxKind.GetAccessorDeclaration, isIteratorState);
            isIterator = isIteratorState.IsIterator;
            var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(_nodesVisitor)));
            var modifiers = ConvertModifiers(node.Modifiers, TokenContext.Local);
            var parent = (CSS.BasePropertyDeclarationSyntax)node.Parent.Parent;
            Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax valueParam;

            switch (accesorKind) {
                case CSSyntaxKind.GetAccessorDeclaration:
                    blockKind = SyntaxKind.GetAccessorBlock;
                    stmt = SyntaxFactory.GetAccessorStatement(attributes, modifiers, null);
                    endStmt = SyntaxFactory.EndGetStatement();
                    if (isAutoImplementedProperty) {
                        body = body.Count > 0 ? body :
                        SyntaxFactory.SingletonList((StatementSyntax)SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(GetVbPropertyBackingFieldName(parent))));
                    }
                    break;
                case CSSyntaxKind.SetAccessorDeclaration:
                    blockKind = SyntaxKind.SetAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor, false)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.SetAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndSetStatement();
                    if (isAutoImplementedProperty) {
                        body = body.Count > 0 ? body :
                        SyntaxFactory.SingletonList((StatementSyntax)SyntaxFactory.AssignmentStatement(SyntaxKind.SimpleAssignmentStatement,
                            SyntaxFactory.IdentifierName(GetVbPropertyBackingFieldName(parent)),
                            SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(SyntaxKind.SimpleAssignmentStatement)),
                            SyntaxFactory.IdentifierName("value")
                        ));
                    }
                    break;
                case CSSyntaxKind.AddAccessorDeclaration:
                    blockKind = SyntaxKind.AddHandlerAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor, false)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.AddHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndAddHandlerStatement();
                    break;
                case CSSyntaxKind.RemoveAccessorDeclaration:
                    blockKind = SyntaxKind.RemoveHandlerAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor, false)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.RemoveHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndRemoveHandlerStatement();
                    break;
                default:
                    throw new NotSupportedException();
            }
            return SyntaxFactory.AccessorBlock(blockKind, stmt, body, endStmt).WithCsSourceMappingFrom(node);
        }

        private static SyntaxToken GetVbPropertyBackingFieldName(CSS.BasePropertyDeclarationSyntax parent)
        {
            return Identifier("_" + ((CSS.PropertyDeclarationSyntax)parent).Identifier.Text);
        }

        public ExpressionSyntax ReduceArrayUpperBoundExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expr)
        {
            var constant = _semanticModel.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal((int)constant.Value - 1));

            return SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                (ExpressionSyntax)expr.Accept(_nodesVisitor), SyntaxFactory.Token(SyntaxKind.MinusToken), SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal(1)));
        }

        public LambdaExpressionSyntax ConvertLambdaExpression(CSS.AnonymousFunctionExpressionSyntax node, CS.CSharpSyntaxNode body, IEnumerable<ParameterSyntax> parameters, SyntaxTokenList modifiers)
        {
            var symbol = (IMethodSymbol)ModelExtensions.GetSymbolInfo(_semanticModel, node).Symbol;
            var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(p => (Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax)p.Accept(_nodesVisitor))));
            LambdaHeaderSyntax header;
            EndBlockStatementSyntax endBlock;
            SyntaxKind multiLineExpressionKind;
            SyntaxKind singleLineExpressionKind;
            bool isSub = symbol.ReturnsVoid;
            if (isSub) {
                header = SyntaxFactory.SubLambdaHeader(SyntaxFactory.List<AttributeListSyntax>(),
                    ConvertModifiers(modifiers, TokenContext.Local), parameterList, null);
                endBlock = SyntaxFactory.EndBlockStatement(SyntaxKind.EndSubStatement,
                    SyntaxFactory.Token(SyntaxKind.SubKeyword));
                multiLineExpressionKind = SyntaxKind.MultiLineSubLambdaExpression;
                singleLineExpressionKind = SyntaxKind.SingleLineSubLambdaExpression;
            } else {
                header = CreateFunctionHeader(modifiers, parameterList, out endBlock, out multiLineExpressionKind);
                singleLineExpressionKind = SyntaxKind.SingleLineFunctionLambdaExpression;
            }

            SyntaxList<StatementSyntax> statements;
            if (body is CSS.BlockSyntax block) {
                statements = ConvertStatements(block.Statements);

            } else if (body.Kind() == CSSyntaxKind.ThrowExpression) {
                var csThrowExpression = (CSS.ThrowExpressionSyntax)body;
                var vbThrowExpression = (ExpressionSyntax)csThrowExpression.Expression.Accept(_nodesVisitor);
                var vbThrowStatement = SyntaxFactory.ThrowStatement(SyntaxFactory.Token(SyntaxKind.ThrowKeyword), vbThrowExpression);

                return SyntaxFactory.MultiLineFunctionLambdaExpression(header,
                    SyntaxFactory.SingletonList<StatementSyntax>(vbThrowStatement), endBlock);
            } else {
                var stmt = GetStatementSyntax(body.Accept(_nodesVisitor),
                    expression => isSub ? (StatementSyntax)SyntaxFactory.ExpressionStatement(expression) : SyntaxFactory.ReturnStatement(expression));
                statements = InsertRequiredDeclarations(SyntaxFactory.SingletonList(stmt), body);
            }

            return CreateLambdaExpression(singleLineExpressionKind, multiLineExpressionKind, header, statements, endBlock);

        }

        private StatementSyntax GetStatementSyntax(VisualBasicSyntaxNode node, Func<ExpressionSyntax, StatementSyntax> create) {
            if (node is StatementSyntax syntax) return syntax;
            return create(node as ExpressionSyntax);
        }

        private static LambdaExpressionSyntax CreateLambdaExpression(SyntaxKind singleLineKind,
            SyntaxKind multiLineExpressionKind,
            LambdaHeaderSyntax header, SyntaxList<StatementSyntax> statements, EndBlockStatementSyntax endBlock)
        {
            if (statements.Count == 1 && TryGetNodeForeSingleLineLambdaExpression(singleLineKind, statements[0], out VisualBasicSyntaxNode singleNode)) {
                return SyntaxFactory.SingleLineLambdaExpression(singleLineKind, header, singleNode);
            }

            return SyntaxFactory.MultiLineLambdaExpression(multiLineExpressionKind, header, statements, endBlock);
        }

        private static bool TryGetNodeForeSingleLineLambdaExpression(SyntaxKind kind, StatementSyntax statement, out VisualBasicSyntaxNode singleNode)
        {
            switch (kind) {
                case SyntaxKind.SingleLineSubLambdaExpression when statement.DescendantNodesAndSelf().OfType<StatementSyntax>().Count() == 1:
                    singleNode = statement;
                    return true;
                case SyntaxKind.SingleLineFunctionLambdaExpression when UnpackExpressionFromStatement(statement, out var expression):
                    singleNode = expression;
                    return true;
                default:
                    singleNode = null;
                    return false;
            }
        }

        private static LambdaHeaderSyntax CreateFunctionHeader(SyntaxTokenList modifiers, ParameterListSyntax parameterList,
            out EndBlockStatementSyntax endBlock, out SyntaxKind multiLineExpressionKind)
        {
            LambdaHeaderSyntax header;
            header = SyntaxFactory.FunctionLambdaHeader(SyntaxFactory.List<AttributeListSyntax>(),
                ConvertModifiers(modifiers, TokenContext.Local), parameterList, null);
            endBlock = SyntaxFactory.EndBlockStatement(SyntaxKind.EndFunctionStatement,
                SyntaxFactory.Token(SyntaxKind.FunctionKeyword));
            multiLineExpressionKind = SyntaxKind.MultiLineFunctionLambdaExpression;
            return header;
        }

        private static bool UnpackExpressionFromStatement(StatementSyntax statementSyntax, out ExpressionSyntax expression)
        {
            if (statementSyntax is ReturnStatementSyntax returnStmt)
                expression = returnStmt.Expression;
            else if (statementSyntax is YieldStatementSyntax yieldStmt)
                expression = yieldStmt.Expression;
            else
                expression = null;
            return expression != null;
        }

        public void ConvertBaseList(CSS.BaseTypeDeclarationSyntax type, List<InheritsStatementSyntax> inherits, List<ImplementsStatementSyntax> implements)
        {
            TypeSyntax[] arr;
            switch (type.Kind()) {
                case CSSyntaxKind.ClassDeclaration:
                    var classOrInterface = type.BaseList?.Types.FirstOrDefault()?.Type;
                    if (classOrInterface == null) return;
                    var classOrInterfaceSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, classOrInterface).Symbol as ITypeSymbol;
                    if (classOrInterfaceSymbol?.IsInterfaceType() == true) {
                        arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                        if (arr.Length > 0)
                            implements.Add(SyntaxFactory.ImplementsStatement(arr));
                    } else {
                        inherits.Add(SyntaxFactory.InheritsStatement((TypeSyntax)classOrInterface.Accept(_nodesVisitor)));
                        arr = type.BaseList?.Types.Skip(1).Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                        if (arr.Length > 0)
                            implements.Add(SyntaxFactory.ImplementsStatement(arr));
                    }
                    break;
                case CSSyntaxKind.StructDeclaration:
                    arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                    if (arr?.Length > 0)
                        implements.Add(SyntaxFactory.ImplementsStatement(arr));
                    break;
                case CSSyntaxKind.InterfaceDeclaration:
                    arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                    if (arr?.Length > 0)
                        inherits.Add(SyntaxFactory.InheritsStatement(arr));
                    break;
            }
        }


        private static IEnumerable<SyntaxToken> ConvertModifiersCore(IReadOnlyCollection<SyntaxToken> modifiers, TokenContext context, bool isConstructor) {
            var needsExplicitVisibility = !(modifiers.Any(x => x.IsKind(CS.SyntaxKind.PartialKeyword)) && context == TokenContext.Global)
                && context != TokenContext.Local
                && context != TokenContext.MemberInInterface
                && context != TokenContext.MemberInProperty
                && !modifiers.Any(x => x.IsCsVisibility(true, isConstructor)); //TODO Don't always treat as variable or const, pass in more context to detect this
            var vbModifiers = modifiers
                .Where(m => !IgnoreInContext(m, context))
                .Select(x => ConvertModifier(x, context))
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList();
            if(needsExplicitVisibility)
                vbModifiers.Insert(vbModifiers.FirstOrDefault().IsKind(SyntaxKind.PartialKeyword) ? 1 : 0, CSharpDefaultVisibility(context));
            return vbModifiers;
        }

        private static bool IgnoreInContext(SyntaxToken m, TokenContext context)
        {
            switch (context) {
                case TokenContext.InterfaceOrModule:
                case TokenContext.MemberInModule:
                    return m.IsKind(CSSyntaxKind.StaticKeyword);
            }
            return false;
        }

        private static SyntaxToken CSharpDefaultVisibility(TokenContext context)
        {
            switch (context) {
                case TokenContext.Global:
                case TokenContext.InterfaceOrModule:
                    return SyntaxFactory.Token(SyntaxKind.FriendKeyword);
                case TokenContext.Local:
                case TokenContext.MemberInClass:
                case TokenContext.MemberInModule:
                case TokenContext.MemberInProperty:
                case TokenContext.MemberInStruct:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                case TokenContext.MemberInInterface:
                    return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        internal static SyntaxTokenList ConvertModifiers(IReadOnlyCollection<SyntaxToken> modifiers,
            TokenContext context = TokenContext.Global, bool isConstructor = false)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context, isConstructor));
        }

        private static SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = TokenContext.Global)
        {
            var token = CSharpExtensions.Kind(m).ConvertToken(context);
            return token == SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        internal SeparatedSyntaxList<VariableDeclaratorSyntax> RemodelVariableDeclaration(CSS.VariableDeclarationSyntax declaration)
        {
            var visualBasicSyntaxNode = declaration.Type.Accept(_nodesVisitor);
            var type = (TypeSyntax)visualBasicSyntaxNode;
            var declaratorsWithoutInitializers = new List<Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax>();
            var declarators = new List<VariableDeclaratorSyntax>();

            foreach (var v in declaration.Variables) {
                if (v.Initializer == null) {
                    declaratorsWithoutInitializers.Add(v);
                } else {
                    declarators.Add(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(ExtractIdentifier(v)),
                            declaration.Type.IsVar ? null : SyntaxFactory.SimpleAsClause(type),
                            SyntaxFactory.EqualsValue((ExpressionSyntax)ConvertTopLevelExpression(v.Initializer.Value))
                        )
                    );
                }
            }

            if (declaratorsWithoutInitializers.Count > 0) {
                declarators.Insert(0, SyntaxFactory.VariableDeclarator(SyntaxFactory.SeparatedList(declaratorsWithoutInitializers.Select(ExtractIdentifier)), SyntaxFactory.SimpleAsClause(type), null));
            }

            return SyntaxFactory.SeparatedList(declarators);
        }

        public VisualBasicSyntaxNode ConvertTopLevelExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax topLevelExpression)
        {
            return topLevelExpression.Accept(_nodesVisitor);
        }

        private ModifiedIdentifierSyntax ExtractIdentifier(Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax v)
        {
            return SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(v.Identifier));
        }

        public SyntaxToken ConvertIdentifier(SyntaxToken id, SourceTriviaMapKind sourceTriviaMapKind = SourceTriviaMapKind.All)
        {
            var parent = (CS.CSharpSyntaxNode)id.Parent;
            var idText = AdjustIfEventIdentifier(id.ValueText, parent);
            // Underscore is a special character in VB lexer which continues lines - not sure where to find the whole set of other similar tokens if any
            // Rather than a complicated contextual rename, just add an extra dash to all identifiers and hope this method is consistently used
            bool keywordRequiresEscaping = id.IsKind(CSSyntaxKind.IdentifierToken) && KeywordRequiresEscaping(id);
            switch (CSharpExtensions.Kind(id)) {
                case CSSyntaxKind.GlobalKeyword:
                    idText = "Global";
                    break;
                case CSSyntaxKind.ThisKeyword:
                    idText = "Item";
                    break;
            }
            var identifier = Identifier(idText, keywordRequiresEscaping);
            return id.SyntaxTree == _semanticModel.SyntaxTree && sourceTriviaMapKind != SourceTriviaMapKind.None ? identifier.WithSourceMappingFrom(id) : identifier;
        }

        private string AdjustIfEventIdentifier(string valueText, CS.CSharpSyntaxNode parent)
        {
            var symbol = GetSymbol(parent) as IEventSymbol;
            bool isEvent = symbol.IsKind(SymbolKind.Event);
            if (!isEvent) {
                return valueText;
            }

            var operation = _semanticModel.GetAncestorOperationOrNull<IEventReferenceOperation>(parent);
            if (operation == null || !operation.Event.Equals(symbol) || operation.Parent is IEventAssignmentOperation ||
                operation.Parent is IRaiseEventOperation || operation.Parent is IInvocationOperation ||
                operation.Parent is IConditionalAccessOperation cao && cao.WhenNotNull is IInvocationOperation) {
                return valueText;
            } else {
                return valueText + "Event";
            }
        }

        public static SyntaxToken Identifier(string idText, bool keywordRequiresEscaping = false)
        {
            if (idText.All(c => c == '_')) idText += "_";
            return keywordRequiresEscaping ? SyntaxFactory.BracketedIdentifier(idText) : SyntaxFactory.Identifier(idText);
        }

        private static bool KeywordRequiresEscaping(SyntaxToken id)
        {
            var keywordKind = SyntaxFacts.GetKeywordKind(id.ValueText);

            if (keywordKind == SyntaxKind.None) return false;
            if (SyntaxFacts.IsPredefinedType(keywordKind) || SyntaxFacts.IsReservedKeyword(keywordKind))
                return true;

            // List of the kinds that end in declaration and can have names attached
            return id.IsKind(CSSyntaxKind.CatchDeclaration,
                             CSSyntaxKind.ClassDeclaration,
                             CSSyntaxKind.DelegateDeclaration,
                             CSSyntaxKind.EnumDeclaration,
                             CSSyntaxKind.EnumMemberDeclaration,
                             CSSyntaxKind.EventDeclaration,
                             CSSyntaxKind.EventFieldDeclaration,
                             CSSyntaxKind.FieldDeclaration,
                             CSSyntaxKind.InterfaceDeclaration,
                             CSSyntaxKind.MethodDeclaration,
                             CSSyntaxKind.PropertyDeclaration,
                             CSSyntaxKind.NamespaceDeclaration,
                             CSSyntaxKind.StructDeclaration,
                             CSSyntaxKind.VariableDeclaration);
        }


        public ExpressionSyntax Literal(object o, string valueText = null) => GetLiteralExpression(o, valueText, VbSyntaxGenerator);

        internal static ExpressionSyntax GetLiteralExpression(object value, string valueText, SyntaxGenerator generator)
        {
            if (value is char)
                return (ExpressionSyntax)generator.LiteralExpression((char)value);

            if (value is string)
                return (ExpressionSyntax)generator.LiteralExpression((string)value);

            if (value == null)
                return (ExpressionSyntax)generator.NullLiteralExpression();

            if (value is bool)
                return (ExpressionSyntax)((bool)value ? generator.TrueLiteralExpression() : generator.FalseLiteralExpression());

            valueText = valueText != null ? ConvertNumericLiteralValueText(valueText) : value.ToString();

            if (value is byte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (byte)value));
            if (value is sbyte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (sbyte)value));
            if (value is short)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (short)value));
            if (value is ushort)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ushort)value));
            if (value is int)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (int)value));
            if (value is uint)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (uint)value));
            if (value is long)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (long)value));
            if (value is ulong)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ulong)value));

            if (value is float)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (float)value));
            if (value is double)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (double)value));
            if (value is decimal)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (decimal)value));

            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }


        /// <summary>
        ///  https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/data-types/type-characters
        /// </summary>
        private static string ConvertNumericLiteralValueText(string valueText)
        {
            var replacements = new Dictionary<string, string> {
                {"U", "UI"},
                {"UL", "UL"},
                {"M", "D"},
                {"F", "F"},
                {"D", "R"},
                {"L", "L"} // Normalizes casing
            };

            // Be careful not to replace only the "L" in "UL" for example
            var longestMatchingReplacement = replacements.Where(t => valueText.EndsWith(t.Key, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Key.Length).OrderByDescending(g => g.Key).FirstOrDefault()?.SingleOrDefault();

            if (longestMatchingReplacement != null) {
                valueText = valueText.ReplaceEnd(longestMatchingReplacement.Value);
            }

            if (valueText.Length <= 2) return valueText;

            if (valueText.StartsWith("0x")) {
                return "&H" + valueText.Substring(2).Replace("R", "D"); // Undo any accidental replacements that assumed this was a decimal;
            }

            if (valueText.StartsWith("0b")) {
                return "&B" + valueText.Substring(2);
            }

            return valueText;
        }

        public static string GetTupleName(CSS.ParenthesizedVariableDesignationSyntax node)
        {
            return String.Join("", node.Variables.Select((v, i) => {
                var sourceText1 = v.ToString();
                return i > 0 ? UppercaseFirstLetter(sourceText1) : sourceText1;
            }));
        }

        private static string UppercaseFirstLetter(string sourceText)
        {
            return sourceText.Substring(0, 1).ToUpper() + sourceText.Substring(1);
        }

        public bool IsEventHandlerIdentifier(CS.CSharpSyntaxNode syntax)
        {
            return GetSymbol(syntax).IsKind(SymbolKind.Event);
        }

        private bool IsEventReference(CS.CSharpSyntaxNode syntax)
        {
            var operation = _semanticModel.GetOperation(syntax.Parent);
            return operation is IEventReferenceOperation;
        }

        private ISymbol GetSymbol(CS.CSharpSyntaxNode syntax)
        {
            return syntax.SyntaxTree == _semanticModel.SyntaxTree
                ? _semanticModel.GetSymbolInfo(syntax).Symbol
                : null;
        }

        private ITypeSymbol GetTypeSymbol(CS.CSharpSyntaxNode syntax)
        {
            return syntax.SyntaxTree == _semanticModel.SyntaxTree
                ? _semanticModel.GetTypeInfo(syntax).Type
                : null;
        }

        public string GetFullyQualifiedName(INamespaceOrTypeSymbol symbol)
        {
            return GetFullyQualifiedNameSyntax(symbol).ToString();
        }

        public NameSyntax GetFullyQualifiedNameSyntax(INamespaceOrTypeSymbol symbol, bool allowGlobalPrefix = true)
        {
            switch (symbol) {
                case ITypeSymbol ts:
                    var nameSyntax = (NameSyntax)VbSyntaxGenerator.TypeExpression(ts);
                    if (allowGlobalPrefix)
                        return nameSyntax;
                    var globalNameNode = nameSyntax.DescendantNodes().OfType<GlobalNameSyntax>().FirstOrDefault();
                    if (globalNameNode != null)
                        nameSyntax = nameSyntax.ReplaceNodes((globalNameNode.Parent as QualifiedNameSyntax).Yield(), (orig, rewrite) => orig.Right);
                    return nameSyntax;
                case INamespaceSymbol ns:
                    return SyntaxFactory.ParseName(ns.GetFullMetadataName());
                default:
                    throw new NotImplementedException($"Fully qualified name for {symbol.GetType().FullName} not implemented");
            }
        }
    }
}
