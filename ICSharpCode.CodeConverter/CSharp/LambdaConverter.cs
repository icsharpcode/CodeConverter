using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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

        public CSharpSyntaxNode Convert(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode vbNode,
            ParameterListSyntax param, IReadOnlyCollection<StatementSyntax> convertedStatements)
        {
            BlockSyntax block = null;
            ExpressionSyntax expressionBody = null;
            ArrowExpressionClauseSyntax arrow = null;
            if (!convertedStatements.TryUnpackSingleStatement(out var singleStatement) ||
                !singleStatement.TryUnpackSingleExpressionFromStatement(out expressionBody)) {
                block = SyntaxFactory.Block(convertedStatements);
            } else {
                arrow = SyntaxFactory.ArrowExpressionClause(expressionBody);
            }

            if (TryConvertToFunctionDeclaration(vbNode, param, block, arrow, out CSharpSyntaxNode functionStatement)) {
                return functionStatement;
            }

            var body = (CSharpSyntaxNode) block ?? expressionBody;
            if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null)
                return SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
            return SyntaxFactory.ParenthesizedLambdaExpression(param, body);
        }

        private bool TryConvertToFunctionDeclaration(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode vbNode, ParameterListSyntax param, BlockSyntax block,
            ArrowExpressionClauseSyntax arrow, out CSharpSyntaxNode localFunctionStatement)
        {
            var operation = _semanticModel.GetOperation(vbNode) as IAnonymousFunctionOperation;
            var potentialAncestorDeclarationOperation = operation?.Parent?.Parent?.Parent;
            if (potentialAncestorDeclarationOperation is IFieldInitializerOperation fieldInit) {
                localFunctionStatement = CreateMethodDeclaration(operation, fieldInit.InitializedFields.Single(), block, arrow);
                return true;
            }

            var potentialDeclarationOperation = potentialAncestorDeclarationOperation?.Parent;
            if (potentialDeclarationOperation is IVariableDeclarationGroupOperation go)
            {
                potentialDeclarationOperation = go.Declarations.Single();
            }

            if (potentialDeclarationOperation is IVariableDeclarationOperation variableDeclaration)
            {
                var variableDeclaratorOperation = variableDeclaration.Declarators.Single();
                localFunctionStatement = CreateLocalFunction(operation, variableDeclaratorOperation, param, block, arrow);
                return true;
            }

            localFunctionStatement = null;
            return false;
        }

        private MethodDeclarationSyntax CreateMethodDeclaration(IAnonymousFunctionOperation operation,
            IFieldSymbol fieldSymbol,
            BlockSyntax block, ArrowExpressionClauseSyntax arrow)
        {
            var methodDeclaration =
                (MethodDeclarationSyntax) CommonConversions.CsSyntaxGenerator.MethodDeclaration(operation.Symbol);
            var methodDecl = methodDeclaration
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
            var localFunctionStatementSyntax = SyntaxFactory.LocalFunctionStatement(SyntaxFactory.TokenList(),
                CommonConversions.GetTypeSyntax(operation.Symbol.ReturnType),
                SyntaxFactory.Identifier(symbolName), null, param,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), block, arrow,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            return localFunctionStatementSyntax;
        }
    }
}