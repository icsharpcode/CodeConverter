using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
using LambdaExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.LambdaExpressionSyntax;
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
        private readonly CSharpSyntaxVisitor<VisualBasicSyntaxNode> _nodesVisitor;
        private readonly TriviaConverter _triviaConverter;
        private readonly SemanticModel _semanticModel;

        public CommonConversions(SemanticModel semanticModel, CSharpSyntaxVisitor<VisualBasicSyntaxNode> nodesVisitor,
            TriviaConverter triviaConverter)
        {
            _semanticModel = semanticModel;
            _nodesVisitor = nodesVisitor;
            _triviaConverter = triviaConverter;
        }

        public SyntaxList<StatementSyntax> ConvertBody(BlockSyntax body,
            ArrowExpressionClauseSyntax expressionBody, MethodBodyVisitor iteratorState = null)
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
            MethodBodyVisitor iteratorState = null)
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
            var declarationExpressions = descendantNodes.OfType<DeclarationExpressionSyntax>().ToList();
            var isPatternExpressions = descendantNodes.OfType<IsPatternExpressionSyntax>().ToList();
            if (declarationExpressions.Any() || isPatternExpressions.Any()) {
                convertedStatements = convertedStatements.Insert(0, ConvertToDeclarationStatement(declarationExpressions, isPatternExpressions));
            }

            return convertedStatements;
        }

        private StatementSyntax ConvertToDeclarationStatement(List<DeclarationExpressionSyntax> des,
            List<IsPatternExpressionSyntax> isPatternExpressions)
        {
            var declarators = SyntaxFactory.SeparatedList(des.Select(ConvertToVariableDeclarator)
                .Concat(isPatternExpressions.Select(ConvertToVariableDeclarator)));
            return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword)), declarators);
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
        private VariableDeclaratorSyntax ConvertToVariableDeclarator(IsPatternExpressionSyntax node)
        {
            return node.Pattern.TypeSwitch(
                (DeclarationPatternSyntax d) => {
                    var id = ((IdentifierNameSyntax)d.Designation.Accept(_nodesVisitor)).Identifier;
                    var ids = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(id));
                    TypeSyntax right = (TypeSyntax)d.Type.Accept(_nodesVisitor);

                    var simpleAsClauseSyntax = SyntaxFactory.SimpleAsClause(right);
                    var equalsValueSyntax = SyntaxFactory.EqualsValue(SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression, SyntaxFactory.Token(SyntaxKind.NothingKeyword)));
                    return SyntaxFactory.VariableDeclarator(ids, simpleAsClauseSyntax, equalsValueSyntax);
                },
                p => throw new ArgumentOutOfRangeException(nameof(p), p, null));
        }

        private CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(MethodBodyVisitor methodBodyVisitor = null)
        {
            var visitor = methodBodyVisitor ?? new MethodBodyVisitor(_semanticModel, _nodesVisitor, _triviaConverter, this);
            return visitor.CommentConvertingVisitor;
        }

        public AccessorBlockSyntax ConvertAccessor(AccessorDeclarationSyntax node, out bool isIterator)
        {
            SyntaxKind blockKind;
            AccessorStatementSyntax stmt;
            EndBlockStatementSyntax endStmt;
            SyntaxList<StatementSyntax> body;
            isIterator = false;
            var isIteratorState = new MethodBodyVisitor(_semanticModel, _nodesVisitor, _triviaConverter, this);
            body = ConvertBody(node.Body, node.ExpressionBody, isIteratorState);
            isIterator = isIteratorState.IsIterator;
            var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(_nodesVisitor)));
            var modifiers = ConvertModifiers(node.Modifiers, TokenContext.Local);
            var parent = (BasePropertyDeclarationSyntax)node.Parent.Parent;
            Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax valueParam;

            switch (CSharpExtensions.Kind(node)) {
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetAccessorDeclaration:
                    blockKind = SyntaxKind.GetAccessorBlock;
                    stmt = SyntaxFactory.GetAccessorStatement(attributes, modifiers, null);
                    endStmt = SyntaxFactory.EndGetStatement();
                    break;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SetAccessorDeclaration:
                    blockKind = SyntaxKind.SetAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.SetAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndSetStatement();
                    break;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddAccessorDeclaration:
                    blockKind = SyntaxKind.AddHandlerAccessorBlock;
                    valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                        .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(_nodesVisitor)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                    stmt = SyntaxFactory.AddHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                    endStmt = SyntaxFactory.EndAddHandlerStatement();
                    break;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.RemoveAccessorDeclaration:
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
            var symbol = ModelExtensions.GetSymbolInfo(_semanticModel, node).Symbol as IMethodSymbol;
            var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(p => (Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax)p.Accept(_nodesVisitor))));
            LambdaHeaderSyntax header;
            EndBlockStatementSyntax endBlock;
            SyntaxKind multiLineExpressionKind;
            SyntaxKind singleLineExpressionKind;
            if (symbol.ReturnsVoid) {
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

            } else if (body.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.ThrowExpression) {
                var csThrowExpression = (ThrowExpressionSyntax)body;
                var vbThrowExpression = (ExpressionSyntax)csThrowExpression.Expression.Accept(_nodesVisitor);
                var vbThrowStatement = SyntaxFactory.ThrowStatement(SyntaxFactory.Token(SyntaxKind.ThrowKeyword), vbThrowExpression);

                return SyntaxFactory.MultiLineFunctionLambdaExpression(header, SyntaxFactory.SingletonList<StatementSyntax>(vbThrowStatement), endBlock);
            } else {
                statements = InsertRequiredDeclarations(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ReturnStatement((ExpressionSyntax)body.Accept(_nodesVisitor))),
                    body);
            }

            if (statements.Count == 1 && UnpackExpressionFromStatement(statements[0], out var expression)) {
                return SyntaxFactory.SingleLineLambdaExpression(singleLineExpressionKind, header, expression);
            }

            return SyntaxFactory.MultiLineLambdaExpression(multiLineExpressionKind, header, statements, endBlock);
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

        private bool UnpackExpressionFromStatement(StatementSyntax statementSyntax, out ExpressionSyntax expression)
        {
            if (statementSyntax is ReturnStatementSyntax)
                expression = ((ReturnStatementSyntax)statementSyntax).Expression;
            else if (statementSyntax is YieldStatementSyntax)
                expression = ((YieldStatementSyntax)statementSyntax).Expression;
            else
                expression = null;
            return expression != null;
        }

        public void ConvertBaseList(BaseTypeDeclarationSyntax type, List<InheritsStatementSyntax> inherits, List<ImplementsStatementSyntax> implements)
        {
            TypeSyntax[] arr;
            switch (type.Kind()) {
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration:
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
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.StructDeclaration:
                    arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                    if (arr?.Length > 0)
                        implements.Add(SyntaxFactory.ImplementsStatement(arr));
                    break;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.InterfaceDeclaration:
                    arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(_nodesVisitor)).ToArray();
                    if (arr?.Length > 0)
                        inherits.Add(SyntaxFactory.InheritsStatement(arr));
                    break;
            }
        }


        private static IEnumerable<SyntaxToken> ConvertModifiersCore(IReadOnlyCollection<SyntaxToken> modifiers, TokenContext context)
        {
            if (context != TokenContext.Local && context != TokenContext.MemberInInterface) {
                bool visibility = false;
                foreach (var token in modifiers) {
                    if (token.IsCsVisibility(true)) { //TODO Don't treat const as visibility, pass in more context to detect this
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
                    return m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
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
                case TokenContext.MemberInStruct:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                case TokenContext.MemberInInterface:
                    return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        internal static SyntaxTokenList ConvertModifiers(IReadOnlyCollection<SyntaxToken> modifiers, TokenContext context = TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context));
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

        private static ModifiedIdentifierSyntax ExtractIdentifier(Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax v)
        {
            return SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(v.Identifier));
        }

        public static SyntaxToken ConvertIdentifier(SyntaxToken id)
        {
            var idText = id.ValueText;
            // Underscore is a special character in VB lexer which continues lines - not sure where to find the whole set of other similar tokens if any
            // Rather than a complicated contextual rename, just add an extra dash to all identifiers and hope this method is consistently used
            if (idText.All(c => c == '_')) idText += "_";
            
            return KeywordRequiresEscaping(id) ? SyntaxFactory.Identifier($"[{idText}]") : SyntaxFactory.Identifier(idText);
        }

        private static bool KeywordRequiresEscaping(SyntaxToken id)
        {
            var keywordKind = SyntaxFacts.GetKeywordKind(id.ValueText);

            if (keywordKind == SyntaxKind.None) return false;
            if (!SyntaxFacts.IsPredefinedType(keywordKind)) return true;

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
    }
}