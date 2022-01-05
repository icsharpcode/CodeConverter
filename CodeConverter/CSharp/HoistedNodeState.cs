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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HoistedNodeState
    {
        public static SyntaxAnnotation AdditionalLocalAnnotation = new SyntaxAnnotation("CodeConverter.AdditionalLocal");
                
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
            var statements = GetParameterlessFunctions();
            PopScope();
            foreach (var statement in statements) {
                Hoist(statement);
            }
        }

        public T Hoist<T>(T additionalLocal) where T : IHoistedNode
        {
            _hoistedNodesPerScope.Peek().Add(additionalLocal);
            return additionalLocal;
        }

        public T HoistToTopLevel<T>(T additionalField) where T : IHoistedNode
        {
            _hoistedNodesPerScope.Last().Add(additionalField);
            return additionalField;
        }

        public IReadOnlyCollection<AdditionalDeclaration> GetDeclarations()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalDeclaration>().ToArray();
        }

        public IReadOnlyCollection<AdditionalAssignment> GetPostAssignments()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalAssignment>().ToArray();
        }

        public IReadOnlyCollection<HoistedParameterlessFunction> GetParameterlessFunctions()
        {
            return _hoistedNodesPerScope.Peek().OfType<HoistedParameterlessFunction>().ToArray();
        }

        public IReadOnlyCollection<HoistedFieldFromVbStaticVariable> GetFields()
        {
            return _hoistedNodesPerScope.Peek().OfType<HoistedFieldFromVbStaticVariable>().ToArray();
        }

        public SyntaxList<StatementSyntax> CreateStatements(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<StatementSyntax> statements, HashSet<string> generatedNames, SemanticModel semanticModel)
        {
            var localFunctions = GetParameterlessFunctions(); 
            var newNames = localFunctions.ToDictionary(f => f.Id, f =>
                NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, vbNode, f.Prefix)
            );
            statements = ReplaceNames(statements, newNames);
            var functions = localFunctions.Select(f => f.AsLocalFunction(newNames[f.Id]));
            return SyntaxFactory.List(functions.Concat(statements));
        }

        public async Task<SyntaxList<StatementSyntax>> CreateLocalsAsync(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<StatementSyntax> csNodes, HashSet<string> generatedNames, SemanticModel semanticModel)
        {
            var preDeclarations = new List<StatementSyntax>();
            var postAssignments = new List<StatementSyntax>();

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
                var assign = CS.SyntaxFactory.AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, additionalAssignment.LeftHandSide, additionalAssignment.RightHandSide);
                postAssignments.Add(CS.SyntaxFactory.ExpressionStatement(assign));
            }

            var statementsWithUpdatedIds = ReplaceNames(preDeclarations.Concat(csNodes).Concat(postAssignments), newNames);

            return CS.SyntaxFactory.List(statementsWithUpdatedIds);
        }


        public async Task<SyntaxList<MemberDeclarationSyntax>> CreateVbStaticFieldsAsync(VBasic.VisualBasicSyntaxNode vbNode, IEnumerable<MemberDeclarationSyntax> csNodes, HashSet<string> generatedNames, SemanticModel semanticModel)
        {
            var declarations = new List<FieldDeclarationSyntax>();

            var fieldInfo = GetFields();
            var newNames = fieldInfo.ToDictionary(f => f.OriginalVariableName, f =>
                NameGenerator.GetUniqueVariableNameInScope(semanticModel, generatedNames, vbNode, f.FieldName)
            );
            foreach (var field in fieldInfo) {
                var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[field.OriginalVariableName],
                    field.Initializer, field.Type);
                var modifiers = new List<SyntaxToken> { CS.SyntaxFactory.Token(SyntaxKind.PrivateKeyword) };
                if (field.IsStatic) {
                    modifiers.Add(CS.SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                }
                declarations.Add(CS.SyntaxFactory.FieldDeclaration(CS.SyntaxFactory.List<AttributeListSyntax>(), CS.SyntaxFactory.TokenList(modifiers), decl));
            }

            var statementsWithUpdatedIds = ReplaceNames(declarations.Concat(csNodes), newNames);

            return CS.SyntaxFactory.List(statementsWithUpdatedIds);
        }

        public static IEnumerable<T> ReplaceNames<T>(IEnumerable<T> csNodes, Dictionary<string, string> newNames) where T : SyntaxNode
        {
            csNodes = csNodes.Select(csNode => ReplaceNames(csNode, newNames)).ToList();
            return csNodes;
        }

        public static T ReplaceNames<T>(T csNode, Dictionary<string, string> newNames) where T : SyntaxNode
        {
            return csNode.ReplaceNodes(csNode.DescendantNodes().OfType<IdentifierNameSyntax>(), (_, idns) => {
                if (newNames.TryGetValue(idns.Identifier.ValueText, out var newName)) {
                    return idns.WithoutAnnotations(AdditionalLocalAnnotation).WithIdentifier(CS.SyntaxFactory.Identifier(newName));
                }
                return idns;
            });
        }
    }
}
