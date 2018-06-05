using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using CSSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CSToVBConversion : ILanguageConversion
    {
        private readonly List<SyntaxTree> _firstPassResults = new List<SyntaxTree>();
        private Compilation _sourceCompilation;

        private VisualBasicCompilation CreateCompilation(List<SyntaxTree> vbTrees)
        {
            var references = _sourceCompilation.References.Select(ReferenceConverter.ConvertReference);
            return VisualBasicCompilation.Create("Conversion", vbTrees, references,
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            _sourceCompilation = sourceCompilation;
            var converted = CSharpConverter.ConvertCompilationTree((CSharpCompilation)sourceCompilation, (CSharpSyntaxTree)tree);
            var convertedTree = VBSyntaxFactory.SyntaxTree(converted);
            _firstPassResults.Add(convertedTree);
            return convertedTree;
        }

        public SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
            bool surroundedWithMethod)
        {
            return surroundedWithMethod
                ? descendantNodes.OfType<CSSyntax.MethodDeclarationSyntax>().First<SyntaxNode>()
                : descendantNodes.OfType<CSSyntax.BaseTypeDeclarationSyntax>().First<SyntaxNode>();
        }

        public IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings()
        {
            return ProjectTypeGuids.VbToCsTypeGuids.Select((vbCs, i) => (vbCs.Item2, vbCs.Item1)).ToArray();
        }

        public IEnumerable<(string, string)> GetProjectFileReplacementRegexes()
        {
            return new[] {
                ("\\\\Microsoft.CSharp.targets", "\\Microsoft.VisualBasic.targets"),
                (".cs\"", ".vb\""),
                (".cs<", ".vb<")
            };
        }

        public bool CanBeContainedByMethod(SyntaxNode node)
        {
            return node is CSSyntax.IncompleteMemberSyntax || 
                   node is CSSyntax.StatementSyntax || 
                   CouldBeFieldOrLocalVariableDeclaration(node); ;
        }

        public bool MustBeContainedByClass(SyntaxNode node)
        {
            return node is CSSyntax.BaseMethodDeclarationSyntax || node is CSSyntax.BaseFieldDeclarationSyntax ||
                   node is CSSyntax.BasePropertyDeclarationSyntax;
        }

        private static bool CouldBeFieldOrLocalVariableDeclaration(SyntaxNode node)
        {
            return node is CSSyntax.FieldDeclarationSyntax f && f.Modifiers.All(m => m.IsKind(SyntaxKind.TypeVarKeyword));
        }

        public string WithSurroundingMethod(string text)
        {
            return $@"void SurroundingSub()
{{
{text}
}}";
        }

        public string WithSurroundingClass(string text)
        {
            return $@"class SurroundingClass
{{
{text}
}}";
        }

        public List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode)
        {
            var children = annotatedNode.ChildNodes().ToList();
            if (children.Count > 1) {
                switch (annotatedNode) {
                    case VBSyntax.TypeBlockSyntax typeBlock:
                        return typeBlock.Members.ToList<SyntaxNode>();
                    case VBSyntax.MethodBlockBaseSyntax methodBlock:
                        return methodBlock.Statements.ToList<SyntaxNode>();
                }
            }
            return children;
        }

        public SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            return cs.Value.GetRoot();
        }

        public string GetWarningsOrNull()
        {
            var finalCompilation = CreateCompilation(_firstPassResults);
            return CompilationWarnings.WarningsForCompilation(_sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(finalCompilation, "target");
        }

        public SyntaxTree CreateTree(string text)
        {
            return CSSyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return CSharpCompilation.Create("Conversion", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}