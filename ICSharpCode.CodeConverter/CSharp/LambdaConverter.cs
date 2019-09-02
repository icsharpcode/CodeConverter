using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class LambdaConverter
    {
        private readonly SemanticModel _semanticModel;

        public LambdaConverter(CommonConversions commonConversions, SemanticModel semanticModel)
        {
            CommonConversions = commonConversions;
            _semanticModel = semanticModel;
        }

        public CommonConversions CommonConversions { get; }

        public CSharpSyntaxNode Convert(VBSyntax.LambdaExpressionSyntax vbNode,
            ParameterListSyntax param, IReadOnlyCollection<StatementSyntax> convertedStatements)
        {
            BlockSyntax block = null;
            ExpressionSyntax expressionBody = null;
            ArrowExpressionClauseSyntax arrow = null;
            if (!convertedStatements.TryUnpackSingleStatement(out StatementSyntax singleStatement) ||
                !singleStatement.TryUnpackSingleExpressionFromStatement(out expressionBody)) {
                block = SyntaxFactory.Block(convertedStatements);
            } else {
                arrow = SyntaxFactory.ArrowExpressionClause(expressionBody);
            }

            var functionStatement = ConvertToFunctionDeclarationOrNull(vbNode, param, block, arrow);
            if (functionStatement != null) {
                return functionStatement;
            }

            var body = (CSharpSyntaxNode)block ?? expressionBody;
            if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null) {
                return SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
            }

            return SyntaxFactory.ParenthesizedLambdaExpression(param, body);
        }

        private CSharpSyntaxNode ConvertToFunctionDeclarationOrNull(VBSyntax.LambdaExpressionSyntax vbNode,
            ParameterListSyntax param, BlockSyntax block,
            ArrowExpressionClauseSyntax arrow)
        {
            if (!(_semanticModel.GetOperation(vbNode) is IAnonymousFunctionOperation operation)) {
                return null;
            }

            // Could do: See if we can improve upon returning "object" for pretty much everything (which is what the symbols say)
            // I believe that in general, special VB functions such as MultiplyObject are designed to work the same as integer when given two integers for example.
            // If all callers currently pass an integer, perhaps it'd be more idiomatic in C# to specify "int", than to have Operators
            var paramsWithTypes = operation.Symbol.Parameters.Select(p => CommonConversions.CsSyntaxGenerator.ParameterDeclaration(p));

            var paramListWithTypes = param.WithParameters(SyntaxFactory.SeparatedList(paramsWithTypes));
            var potentialAncestorDeclarationOperation = operation?.Parent?.Parent?.Parent;
            if (potentialAncestorDeclarationOperation is IFieldInitializerOperation fieldInit) {
                var fieldSymbol = fieldInit.InitializedFields.Single();
                if (fieldSymbol.GetResultantVisibility() != SymbolVisibility.Public && !fieldSymbol.Type.IsDelegateReferencableByName()) {
                    //Should do: Check no (other) write usages exist: SymbolFinder.FindReferencesAsync + checking if they're an assignment LHS or out parameter
                    return CreateMethodDeclaration(operation, fieldSymbol, block, arrow);
                }
            }

            var potentialDeclarationOperation = potentialAncestorDeclarationOperation?.Parent;
            if (potentialDeclarationOperation is IVariableDeclarationGroupOperation go) {
                potentialDeclarationOperation = go.Declarations.Single();
            }

            if (potentialDeclarationOperation is IVariableDeclarationOperation variableDeclaration) {
                var variableDeclaratorOperation = variableDeclaration.Declarators.Single();
                if (!variableDeclaratorOperation.Symbol.Type.IsDelegateReferencableByName()) {
                    //Should do: Check no (other) write usages exist: SymbolFinder.FindReferencesAsync + checking if they're an assignment LHS or out parameter
                    return CreateLocalFunction(operation, variableDeclaratorOperation, paramListWithTypes, block, arrow);
                }
            }

            return null;
        }

        private MethodDeclarationSyntax CreateMethodDeclaration(IAnonymousFunctionOperation operation,
            IFieldSymbol fieldSymbol,
            BlockSyntax block, ArrowExpressionClauseSyntax arrow)
        {
            MethodDeclarationSyntax methodDeclaration =
                (MethodDeclarationSyntax)CommonConversions.CsSyntaxGenerator.MethodDeclaration(operation.Symbol);
            MethodDeclarationSyntax methodDecl = methodDeclaration
                .WithIdentifier(SyntaxFactory.Identifier(fieldSymbol.Name))
                .WithBody(block).WithExpressionBody(arrow);
            return arrow == null
                ? methodDecl
                : methodDecl.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private LocalFunctionStatementSyntax CreateLocalFunction(IAnonymousFunctionOperation operation,
            IVariableDeclaratorOperation variableDeclaratorOperation,
            ParameterListSyntax param, BlockSyntax block,
            ArrowExpressionClauseSyntax arrow)
        {
            string symbolName = variableDeclaratorOperation.Symbol.Name;
            LocalFunctionStatementSyntax localFunctionStatementSyntax = SyntaxFactory.LocalFunctionStatement(
                SyntaxFactory.TokenList(),
                CommonConversions.GetTypeSyntax(operation.Symbol.ReturnType),
                SyntaxFactory.Identifier(symbolName), null, param,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), block, arrow,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            return localFunctionStatementSyntax;
        }
    }
}