using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class LambdaConverter
    {
        private readonly SemanticModel _semanticModel;
        private readonly Solution _solution;

        public LambdaConverter(CommonConversions commonConversions, SemanticModel semanticModel)
        {
            CommonConversions = commonConversions;
            _semanticModel = semanticModel;
            _solution = CommonConversions.Document.Project.Solution;
        }

        public CommonConversions CommonConversions { get; }

        public async Task<CSharpSyntaxNode> ConvertAsync(VBSyntax.LambdaExpressionSyntax vbNode,
            ParameterListSyntax param, IReadOnlyCollection<StatementSyntax> convertedStatements)
        {
            BlockSyntax block = null;
            ExpressionSyntax expressionBody = null;
            ArrowExpressionClauseSyntax arrow = null;
            if (!convertedStatements.TryUnpackSingleStatement(out StatementSyntax singleStatement)) {
                convertedStatements = convertedStatements.Select(l => l.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)).ToList();
                block = SyntaxFactory.Block(convertedStatements);
            } else if (singleStatement.TryUnpackSingleExpressionFromStatement(out expressionBody)) {
                arrow = SyntaxFactory.ArrowExpressionClause(expressionBody);
            } else {
                block = SyntaxFactory.Block(singleStatement);
            }

            var functionStatement = await ConvertToFunctionDeclarationOrNullAsync(vbNode, param, block, arrow);
            if (functionStatement != null) {
                return functionStatement;
            }

            var body = (CSharpSyntaxNode)block ?? expressionBody;

            LambdaExpressionSyntax lambda;
            if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null) {
                lambda = SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
            } else {
                lambda = SyntaxFactory.ParenthesizedLambdaExpression(param, body);
            }
            return lambda;
        }


        private async Task<CSharpSyntaxNode> ConvertToFunctionDeclarationOrNullAsync(VBSyntax.LambdaExpressionSyntax vbNode,
            ParameterListSyntax param, BlockSyntax block,
            ArrowExpressionClauseSyntax arrow)
        {
            if (!(_semanticModel.GetOperation(vbNode) is IAnonymousFunctionOperation anonFuncOp) || anonFuncOp.GetParentIgnoringConversions() is IDelegateCreationOperation dco && !dco.IsImplicit) {
                return null;
            }

            var potentialAncestorDeclarationOperation = anonFuncOp.GetParentIgnoringConversions().GetParentIgnoringConversions();
            // Could do: See if we can improve upon returning "object" for pretty much everything (which is what the symbols say)
            // I believe that in general, special VB functions such as MultiplyObject are designed to work the same as integer when given two integers for example.
            // If all callers currently pass an integer, perhaps it'd be more idiomatic in C# to specify "int", than to have Operators
            var paramsWithTypes = anonFuncOp.Symbol.Parameters.Select(p => CommonConversions.CsSyntaxGenerator.ParameterDeclaration(p));

            var paramListWithTypes = param.WithParameters(SyntaxFactory.SeparatedList(paramsWithTypes));
            if (potentialAncestorDeclarationOperation is IFieldInitializerOperation fieldInit) {
                var fieldSymbol = fieldInit.InitializedFields.Single();
                if (fieldSymbol.GetResultantVisibility() != SymbolVisibility.Public && !fieldSymbol.Type.IsDelegateReferencableByName() && await _solution.IsNeverWrittenAsync(fieldSymbol)) {
                    return CreateMethodDeclaration(anonFuncOp, fieldSymbol, block, arrow);
                }
            }

            if (potentialAncestorDeclarationOperation is IVariableInitializerOperation vio) {
                if (vio.GetParentIgnoringConversions().GetParentIgnoringConversions() is IVariableDeclarationGroupOperation go) {
                    potentialAncestorDeclarationOperation = go.Declarations.First(d => d.Syntax.FullSpan.Contains(vbNode.FullSpan));
                } else {
                    potentialAncestorDeclarationOperation = vio.Parent;
                }

                if (potentialAncestorDeclarationOperation is IVariableDeclarationOperation variableDeclaration) {
                    var variableDeclaratorOperation = variableDeclaration.Declarators.Single();
                    if (!variableDeclaratorOperation.Symbol.Type.IsDelegateReferencableByName() &&
                        await _solution.IsNeverWrittenAsync(variableDeclaratorOperation.Symbol)) {
                        //Should do: Check no (other) write usages exist: SymbolFinder.FindReferencesAsync + checking if they're an assignment LHS or out parameter
                        return CreateLocalFunction(anonFuncOp, variableDeclaratorOperation, paramListWithTypes, block,
                            arrow);
                    }
                }
            }

            return null;
        }

        private MethodDeclarationSyntax CreateMethodDeclaration(IAnonymousFunctionOperation operation,
            IFieldSymbol fieldSymbol,
            BlockSyntax block, ArrowExpressionClauseSyntax arrow)
        {
            MethodDeclarationSyntax methodDecl =
                ((MethodDeclarationSyntax)CommonConversions.CsSyntaxGenerator.MethodDeclaration(operation.Symbol))
                .WithIdentifier(SyntaxFactory.Identifier(fieldSymbol.Name))
                .WithBody(block).WithExpressionBody(arrow);

            if (operation.Symbol.IsAsync) methodDecl = methodDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

            if (arrow != null) methodDecl = methodDecl.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            return methodDecl;
        }

        private LocalFunctionStatementSyntax CreateLocalFunction(IAnonymousFunctionOperation operation,
            IVariableDeclaratorOperation variableDeclaratorOperation,
            ParameterListSyntax param, BlockSyntax block,
            ArrowExpressionClauseSyntax arrow)
        {
            string symbolName = variableDeclaratorOperation.Symbol.Name;
            LocalFunctionStatementSyntax localFunc = SyntaxFactory.LocalFunctionStatement(
                SyntaxFactory.TokenList(),
                CommonConversions.GetTypeSyntax(operation.Symbol.ReturnType),
                SyntaxFactory.Identifier(symbolName), null, param,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), block, arrow,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));


            if (operation.Symbol.IsAsync) localFunc = localFunc.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

            return localFunc;
        }
    }
}
