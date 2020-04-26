using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using ICSharpCode.CodeConverter.Shared;
using System;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HoistedNodeState
    {
        public static SyntaxAnnotation Annotation = new SyntaxAnnotation("CodeconverterAdditionalLocal");

        private readonly Stack<List<IHoistedNode>> _hoistedNodesPerScope;

        public HoistedNodeState()
        {
            _hoistedNodesPerScope = new Stack<List<IHoistedNode>>();
        }

        public void PushScope()
        {
            _hoistedNodesPerScope.Push(new List<IHoistedNode>());
        }

        public void PopScope()
        {
            _hoistedNodesPerScope.Pop();
        }

        public void PopExpressionScope()
        {
            var statements = GetStatements();
            PopScope();
            foreach (var statement in statements) {
                Hoist(statement);
            }
        }

        public T Hoist<T>(T additionalLocal) where T: IHoistedNode
        {
            _hoistedNodesPerScope.Peek().Add(additionalLocal);
            return additionalLocal;
        }

        public IReadOnlyCollection<AdditionalDeclaration> GetDeclarations()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalDeclaration>().ToArray();
        }

        public IReadOnlyCollection<AdditionalAssignment> GetPostAssignments()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalAssignment>().ToArray();
        }

        public IReadOnlyCollection<HoistedStatement> GetStatements()
        {
            return _hoistedNodesPerScope.Peek().OfType<HoistedStatement>().ToArray();
        }

        public async Task<SyntaxList<CS.Syntax.StatementSyntax>> CreateLocals(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<CS.Syntax.StatementSyntax> csNodes, HashSet<string> generatedNames, SemanticModel semanticModel)
        {
            var preDeclarations = new List<CS.Syntax.StatementSyntax>();
            var postAssignments = new List<CS.Syntax.StatementSyntax>();

            var additionalDeclarationInfo = GetDeclarations();
            var newNames = additionalDeclarationInfo.ToDictionary(l => l.Id, l =>
                NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, vbNode, l.Prefix)
            );
            foreach (var additionalLocal in additionalDeclarationInfo) {
                var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Id],
                    additionalLocal.Initializer, additionalLocal.Type);
                preDeclarations.Add(CS.SyntaxFactory.LocalDeclarationStatement(decl));
            }

            foreach (var additionalAssignment in GetPostAssignments()) {
                var assign = CS.SyntaxFactory.AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, additionalAssignment.Expression, additionalAssignment.IdentifierName);
                postAssignments.Add(CS.SyntaxFactory.ExpressionStatement(assign));
            }

            var statementsWithUpdatedIds = AdditionalDeclaration.ReplaceNames(preDeclarations.Concat(csNodes).Concat(postAssignments), newNames);

            return CS.SyntaxFactory.List(statementsWithUpdatedIds);
        }
    }
}
