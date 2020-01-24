using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax;
using BinaryExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
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

namespace ICSharpCode.CodeConverter.VB
{
    internal class CommonConversions
    {
        public SyntaxGenerator VbSyntaxGenerator { get; }
        private readonly CSharpSyntaxVisitor<VisualBasicSyntaxNode> _nodesVisitor;
        private readonly TriviaConverter _triviaConverter;
        private readonly SemanticModel _semanticModel;

        public CommonConversions(SemanticModel semanticModel, SyntaxGenerator vbSyntaxGenerator,
            CSharpSyntaxVisitor<VisualBasicSyntaxNode> nodesVisitor,
            TriviaConverter triviaConverter)
        {
            VbSyntaxGenerator = vbSyntaxGenerator;
            _semanticModel = semanticModel;
            _nodesVisitor = nodesVisitor;
            _triviaConverter = triviaConverter;
        }

        public SyntaxList<StatementSyntax> ConvertBody(BlockSyntax body,
            ArrowExpressionClauseSyntax expressionBody, MethodBodyExecutableStatementVisitor iteratorState = null)
        {
            if (body != null) {
                return ConvertStatements(body.Statements, iteratorState);
            }

            if (expressionBody != null) {
                var convertedBody = expressionBody.Expression.Accept(_nodesVisitor);
                if (convertedBody is ExpressionSyntax convertedBodyExpression) {
                    convertedBody = SyntaxFactory.ReturnStatement(convertedBodyExpression);
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

        private SyntaxList<StatementSyntax> ConvertStatement(Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax statement, CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> methodBodyVisitor)
        {
            var convertedStatements = statement.Accept(methodBodyVisitor);
            convertedStatements = InsertRequiredDeclarations(convertedStatements, statement);

            return convertedStatements;
        }

        private SyntaxList<StatementSyntax> InsertRequiredDeclarations(
            SyntaxList<StatementSyntax> convertedStatements, CSharpSyntaxNode originaNode)
        {
            var descendantNodes = originaNode.DescendantNodes().ToList();
            var declarationExpressions = descendantNodes.OfType<DeclarationExpressionSyntax>()
                .Where(e => !e.Parent.IsKind(CSSyntaxKind.ForEachVariableStatement)) //Handled inline for tuple loop
                .ToList();
            var isPatternExpressions = descendantNodes.OfType<IsPatternExpressionSyntax>().ToList();
            if (declarationExpressions.Any() || isPatternExpressions.Any()) {
                convertedStatements = convertedStatements.InsertRange(0, ConvertToDeclarationStatement(declarationExpressions, isPatternExpressions));
            }

            return convertedStatements;
        }

        public SyntaxList<StatementSyntax> InsertGeneratedClassMemberDeclarations(
            SyntaxList<StatementSyntax> convertedStatements, CSharpSyntaxNode originaNode)
        {
            var descendantNodes = originaNode.DescendantNodes().ToList();
            var propertyBlocks = descendantNodes.OfType<PropertyDeclarationSyntax>()
                .Where(e => e.AccessorList != null && e.AccessorList.Accessors.Any(a => a.Body == null && a.ExpressionBody == null && a.Modifiers.ContainsDeclaredVisibility()))
                .ToList();
            if (propertyBlocks.Any()) {
                convertedStatements = convertedStatements.Insert(0, ConvertToDeclarationStatement(propertyBlocks));
            }

            return convertedStatements;
        }

        private IEnumerable<StatementSyntax> ConvertToDeclarationStatement(List<DeclarationExpressionSyntax> des,
            List<IsPatternExpressionSyntax> isPatternExpressions)
        {
            IEnumerable<VariableDeclaratorSyntax> variableDeclaratorSyntaxs = des.Select(ConvertToVariableDeclarator)
                .Concat(isPatternExpressions.Select(ConvertToVariableDeclaratorOrNull).Where(x => x != null));
            var variableDeclaratorSyntaxes = variableDeclaratorSyntaxs.ToArray();
            return variableDeclaratorSyntaxes.Any() ? new StatementSyntax[]{CreateLocalDeclarationStatement(variableDeclaratorSyntaxes)} : Enumerable.Empty<StatementSyntax>();
        }

        private StatementSyntax ConvertToDeclarationStatement(List<PropertyDeclarationSyntax> propertyBlocks)
        {
            IEnumerable<VariableDeclaratorSyntax> variableDeclaratorSyntaxs = propertyBlocks.Select(ConvertToVariableDeclarator);
            return CreateLocalDeclarationStatement(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)), variableDeclaratorSyntaxs.ToArray());
        }

        public static StatementSyntax CreateLocalDeclarationStatement(params VariableDeclaratorSyntax[] variableDeclarators)
        {
            var syntaxTokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword));
            var declarators = SyntaxFactory.SeparatedList(variableDeclarators);
            return SyntaxFactory.LocalDeclarationStatement(syntaxTokenList, declarators);
        }

        public static StatementSyntax CreateLocalDeclarationStatement(SyntaxTokenList syntaxTokenList, params VariableDeclaratorSyntax[] DimVariableDeclarators)
        {
            var declarators = SyntaxFactory.SeparatedList(DimVariableDeclarators);
            return SyntaxFactory.LocalDeclarationStatement(syntaxTokenList, declarators);
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclarator(DeclarationExpressionSyntax des)
        {
            var id = ((IdentifierNameSyntax)des.Accept(_nodesVisitor)).Identifier;
            var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
            TypeSyntax typeSyntax;
            if (des.Type.IsVar) {
                var typeSymbol = (ITypeSymbol)ModelExtensions.GetSymbolInfo(_semanticModel, des.Type).ExtractBestMatch();
                typeSyntax = typeSymbol?.ToVbSyntax(_semanticModel, des.Type);
            } else {
                typeSyntax = (TypeSyntax)des.Type.Accept(_nodesVisitor);
            }

            var simpleAsClauseSyntax = typeSyntax != null ? SyntaxFactory.SimpleAsClause(typeSyntax) : null; //Gracefully degrade when no type information available
            var equalsValueSyntax = SyntaxFactory.EqualsValue(SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression, SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
            return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node)
        {
            switch (node.Pattern) {
                case DeclarationPatternSyntax d: {
                    var id = ((IdentifierNameSyntax)d.Designation.Accept(_nodesVisitor)).Identifier;
                    var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
                    TypeSyntax right = (TypeSyntax)d.Type.Accept(_nodesVisitor);

                    var simpleAsClauseSyntax = SyntaxFactory.SimpleAsClause(right);
                    var equalsValueSyntax = SyntaxFactory.EqualsValue(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression,
                            SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
                    return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
                }
                case ConstantPatternSyntax _:
                    return null;
                default:
                 throw new ArgumentOutOfRangeException(nameof(node.Pattern), node.Pattern, null);
            }
        }

        private VariableDeclaratorSyntax ConvertToVariableDeclarator(PropertyDeclarationSyntax des)
        {
            var id = GetVbPropertyBackingFieldName(des);
            var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
            TypeSyntax typeSyntax;
            if (des.Type.IsVar) {
                var typeSymbol = (ITypeSymbol)ModelExtensions.GetSymbolInfo(_semanticModel, des.Type).ExtractBestMatch();
                typeSyntax = typeSymbol?.ToVbSyntax(_semanticModel, des.Type);
            } else {
                typeSyntax = (TypeSyntax)des.Type.Accept(_nodesVisitor);
            }

            var simpleAsClauseSyntax = typeSyntax != null ? SyntaxFactory.SimpleAsClause(typeSyntax) : null; //Gracefully degrade when no type information available
            EqualsValueSyntax equalsValueSyntax = null;
            return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
        }

        private CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(MethodBodyExecutableStatementVisitor methodBodyExecutableStatementVisitor = null)
        {
            var visitor = methodBodyExecutableStatementVisitor ?? new MethodBodyExecutableStatementVisitor(_semanticModel, _nodesVisitor, _triviaConverter, this);
            return visitor.CommentConvertingVisitor;
        }

        public AccessorBlockSyntax ConvertAccessor(AccessorDeclarationSyntax node, out bool isIterator, bool isAutoImplementedProperty = false)
        {
            SyntaxKind blockKind;
            AccessorStatementSyntax stmt;
            EndBlockStatementSyntax endStmt;
            SyntaxList<StatementSyntax> body;
            isIterator = false;
            var isIteratorState = new MethodBodyExecutableStatementVisitor(_semanticModel, _nodesVisitor, _triviaConverter, this);
            body = ConvertBody(node.Body, node.ExpressionBody, isIteratorState);
            isIterator = isIteratorState.IsIterator;
            var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(_nodesVisitor)));
            var modifiers = ConvertModifiers(node.Modifiers, TokenContext.Local);
            var parent = (BasePropertyDeclarationSyntax)node.Parent.Parent;
            Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax valueParam;

            switch (CSharpExtensions.Kind(node)) {
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
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.SetAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndSetStatement();
                    if (isAutoImplementedProperty) {
                        body = body.Count > 0 ? body :
                        SyntaxFactory.SingletonList((StatementSyntax)SyntaxFactory.AssignmentStatement(SyntaxKind.SimpleAssignmentStatement, SyntaxFactory.IdentifierName(GetVbPropertyBackingFieldName(parent)), SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(SyntaxKind.SimpleAssignmentStatement)), SyntaxFactory.IdentifierName("value")));
                    }
                    break;
                case CSSyntaxKind.AddAccessorDeclaration:
                    blockKind = SyntaxKind.AddHandlerAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.AddHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndAddHandlerStatement();
                    break;
                case CSSyntaxKind.RemoveAccessorDeclaration:
                    blockKind = SyntaxKind.RemoveHandlerAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.RemoveHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndRemoveHandlerStatement();
                    break;
                default:
                    throw new NotSupportedException();
            }
            return SyntaxFactory.AccessorBlock(blockKind, stmt, body, endStmt);
        }

        private static SyntaxToken GetVbPropertyBackingFieldName(BasePropertyDeclarationSyntax parent)
        {
            return Identifier("_" + ((PropertyDeclarationSyntax)parent).Identifier.Text);
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

        public LambdaExpressionSyntax ConvertLambdaExpression(AnonymousFunctionExpressionSyntax node, CSharpSyntaxNode body, IEnumerable<ParameterSyntax> parameters, SyntaxTokenList modifiers)
        {
            var symbol = (IMethodSymbol) ModelExtensions.GetSymbolInfo(_semanticModel, node).Symbol;
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
            if (body is BlockSyntax block) {
                statements = ConvertStatements(block.Statements);

            } else if (body.Kind() == CSSyntaxKind.ThrowExpression) {
                var csThrowExpression = (ThrowExpressionSyntax)body;
                var vbThrowExpression = (ExpressionSyntax)csThrowExpression.Expression.Accept(_nodesVisitor);
                var vbThrowStatement = SyntaxFactory.ThrowStatement(SyntaxFactory.Token(SyntaxKind.ThrowKeyword), vbThrowExpression);

                return SyntaxFactory.MultiLineFunctionLambdaExpression(header,
                    SyntaxFactory.SingletonList<StatementSyntax>(vbThrowStatement), endBlock);
            } else {
                var expressionSyntax = (ExpressionSyntax)body.Accept(_nodesVisitor);
                var stmt = isSub ? (StatementSyntax) SyntaxFactory.ExpressionStatement(expressionSyntax) : SyntaxFactory.ReturnStatement(expressionSyntax);
                statements = InsertRequiredDeclarations(SyntaxFactory.SingletonList(stmt), body);
            }

            return CreateLambdaExpression(singleLineExpressionKind, multiLineExpressionKind, header, statements, endBlock);

        }

        private static LambdaExpressionSyntax CreateLambdaExpression(SyntaxKind singleLineKind,
            SyntaxKind multiLineExpressionKind,
            LambdaHeaderSyntax header, SyntaxList<StatementSyntax> statements, EndBlockStatementSyntax endBlock)
        {
            if (statements.Count == 1  && TryGetNodeForeSingleLineLambdaExpression(singleLineKind, statements[0], out VisualBasicSyntaxNode singleNode)) {
                return SyntaxFactory.SingleLineLambdaExpression(singleLineKind, header, singleNode);
            }

            return SyntaxFactory.MultiLineLambdaExpression(multiLineExpressionKind, header, statements, endBlock);
        }

        private static bool TryGetNodeForeSingleLineLambdaExpression(SyntaxKind kind, StatementSyntax statement, out VisualBasicSyntaxNode singleNode) {
            switch (kind) {
                case SyntaxKind.SingleLineSubLambdaExpression when !(statement is MultiLineIfBlockSyntax):
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

        public void ConvertBaseList(BaseTypeDeclarationSyntax type, List<InheritsStatementSyntax> inherits, List<ImplementsStatementSyntax> implements)
        {
            TypeSyntax[] arr;
            switch (type.Kind()) {
                case CSSyntaxKind.ClassDeclaration:
                    var classOrInterface = type.BaseList?.Types.FirstOrDefault()?.Type;
                    if (classOrInterface == null) return;
                    var classOrInterfaceSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, classOrInterface).Symbol;
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


        private static IEnumerable<SyntaxToken> ConvertModifiersCore(IReadOnlyCollection<SyntaxToken> modifiers,
            TokenContext context, bool isConstructor)
        {
            if (context != TokenContext.Local && context != TokenContext.MemberInInterface && context != TokenContext.MemberInProperty) {
                bool visibility = false;
                foreach (var token in modifiers) {
                    if (token.IsCsVisibility(true, isConstructor)) { //TODO Don't always treat as variable or const, pass in more context to detect this
                        visibility = true;
                        break;
                    }
                }
                if (!visibility)
                    yield return CSharpDefaultVisibility(context);
            }
            foreach (var token in modifiers.Where(m => !IgnoreInContext(m, context))) {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
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

        internal SeparatedSyntaxList<VariableDeclaratorSyntax> RemodelVariableDeclaration(VariableDeclarationSyntax declaration)
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

        public SyntaxToken ConvertIdentifier(SyntaxToken id)
        {
            CSharpSyntaxNode parent = (CSharpSyntaxNode) id.Parent;
            var idText = AdjustIfEventIdentifier(id, parent);
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
            return Identifier(idText, keywordRequiresEscaping);
        }

        private string AdjustIfEventIdentifier(SyntaxToken id, CSharpSyntaxNode parent)
        {
            var symbol = GetSymbol(parent) as IEventSymbol;
            bool isKind = symbol.IsKind(SymbolKind.Event);
            if (!isKind) {
                return id.ValueText;
            }

            var operation = _semanticModel.GetAncestorOperationOrNull<IEventReferenceOperation>(parent);
            if (operation == null || !operation.Event.Equals(symbol) || operation.Parent is IEventAssignmentOperation ||
                operation.Parent is IRaiseEventOperation || operation.Parent is IInvocationOperation ||
                operation.Parent is IConditionalAccessOperation cao && cao.WhenNotNull is IInvocationOperation) {
                return id.ValueText;
            } else {
                return id.ValueText + "Event";
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


        public static ExpressionSyntax Literal(object o, string valueText = null) => GetLiteralExpression(o, valueText);

        internal static ExpressionSyntax GetLiteralExpression(object value, string valueText = null)
        {
            if (value is char)
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));

            if (value is string)
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal((string)value));

            if (value == null)
                return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword));

            if (value is bool)
                return (bool)value ? SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword)) : SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword));


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

        public static string GetTupleName(ParenthesizedVariableDesignationSyntax node)
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

        public bool IsEventHandlerIdentifier(CSharpSyntaxNode syntax)
        {
            return GetSymbol(syntax).IsKind(SymbolKind.Event);
        }

        private bool IsEventReference(CSharpSyntaxNode syntax)
        {
            var operation = _semanticModel.GetOperation(syntax.Parent);
            return operation is IEventReferenceOperation;
        }

        private ISymbol GetSymbol(CSharpSyntaxNode syntax)
        {
            return syntax.SyntaxTree == _semanticModel.SyntaxTree
                ? _semanticModel.GetSymbolInfo(syntax).Symbol
                : null;
        }

        private ITypeSymbol GetTypeSymbol(CSharpSyntaxNode syntax)
        {
            return syntax.SyntaxTree == _semanticModel.SyntaxTree
                ? _semanticModel.GetTypeInfo(syntax).Type
                : null;
        }

        public string GetFullyQualifiedName(INamespaceOrTypeSymbol symbol)
        {
            return GetFullyQualifiedNameSyntax(symbol).ToString();
        }

        public NameSyntax GetFullyQualifiedNameSyntax(INamespaceOrTypeSymbol symbol)
        {
            switch (symbol)
            {
                case ITypeSymbol ts:
                    return (NameSyntax) VbSyntaxGenerator.TypeExpression(ts);
                case INamespaceSymbol ns:
                    return SyntaxFactory.ParseName(ns.GetFullMetadataName());
                default:
                    throw new NotImplementedException($"Fully qualified name for {symbol.GetType().FullName} not implemented");
            }
        }
    }
}
