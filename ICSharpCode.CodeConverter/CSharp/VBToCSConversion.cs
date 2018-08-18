using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class VBToCSConversion : ILanguageConversion
    {
        private Compilation _sourceCompilation;
        private readonly List<SyntaxTree> _secondPassResults = new List<SyntaxTree>();
        private CSharpCompilation _convertedCompilation;


        public void Initialize(Compilation convertedCompilation)
        {
            _convertedCompilation = (CSharpCompilation) convertedCompilation;
        }

        public SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            _sourceCompilation = sourceCompilation;
            var converted = VisualBasicConverter.ConvertCompilationTree((VisualBasicCompilation)sourceCompilation, (VisualBasicSyntaxTree)tree);
            var convertedTree = SyntaxFactory.SyntaxTree(converted);
            _convertedCompilation = _convertedCompilation.AddSyntaxTrees(convertedTree);
            return convertedTree;
        }

        public SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
            bool surroundedByMethod)
        {
            return surroundedByMethod
                ? descendantNodes.OfType<VBSyntax.MethodBlockBaseSyntax>().First<SyntaxNode>()
                : descendantNodes.OfType<VBSyntax.TypeBlockSyntax>().First<SyntaxNode>();
        }

        public IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings()
        {
            return ProjectTypeGuids.VbToCsTypeGuids;
        }

        public IEnumerable<(string, string)> GetProjectFileReplacementRegexes()
        {
            return new[] {
                ("\\\\Microsoft.VisualBasic.targets", "\\Microsoft.CSharp.targets"),
                (".vb\"", ".cs\""),
                (".vb<", ".cs<")
            };
        }

        public string TargetLanguage { get; } = LanguageNames.CSharp;

        public bool CanBeContainedByMethod(SyntaxNode node)
        {
            return node is VBSyntax.IncompleteMemberSyntax ||
                   !(node is VBSyntax.DeclarationStatementSyntax) ||
                   CouldBeFieldOrLocalVariableDeclaration(node) ||
                   IsNonTypeEndBlock(node);
        }

        private static bool CouldBeFieldOrLocalVariableDeclaration(SyntaxNode node)
        {
            return node is VBSyntax.FieldDeclarationSyntax f && f.Modifiers.All(m => m.IsKind(SyntaxKind.DimKeyword));
        }

        private static bool IsNonTypeEndBlock(SyntaxNode node)
        {
            return node is VBSyntax.EndBlockStatementSyntax ebs && 
                   !ebs.BlockKeyword.IsKind(SyntaxKind.ClassKeyword, SyntaxKind.StructureKeyword, SyntaxKind.InterfaceKeyword, SyntaxKind.ModuleKeyword);
        }

        public bool MustBeContainedByClass(SyntaxNode node)
        {
            return node is VBSyntax.MethodBlockBaseSyntax || node is VBSyntax.MethodBaseSyntax ||
                   node is VBSyntax.FieldDeclarationSyntax || node is VBSyntax.PropertyBlockSyntax ||
                   node is VBSyntax.EventBlockSyntax;
        }

        public string WithSurroundingMethod(string text)
        {
            return $@"Sub SurroundingSub()
{text}
End Sub";
        }

        public string WithSurroundingClass(string text)
        {
            return $@"Class SurroundingClass
{text}
End Class";
        }

        public List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode)
        {
            var children = annotatedNode.ChildNodes().ToList();
            if (children.Count > 1) {
                switch (annotatedNode) {
                    case CSSyntax.MethodDeclarationSyntax _:
                        return annotatedNode.ChildNodes().OfType<CSSyntax.BlockSyntax>().ToList<SyntaxNode>();
                    case CSSyntax.BaseTypeSyntax _:
                        return annotatedNode.ChildNodes().OfType<CSSyntax.BlockSyntax>().ToList<SyntaxNode>();
                }
            }
            return children;
        }

        public SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            var cSharpSyntaxNode = new CompilationErrorFixer(_convertedCompilation, (CSharpSyntaxTree)cs.Value).Fix();
            _secondPassResults.Add(cSharpSyntaxNode.SyntaxTree);
            return cSharpSyntaxNode;
        }

        public string GetWarningsOrNull()
        {
            return CompilationWarnings.WarningsForCompilation(_sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(_convertedCompilation, "target");
        }

        public SyntaxTree CreateTree(string text)
        {
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            var compilation = CreateVisualBasicCompilation(references);
            return compilation.AddSyntaxTrees(tree);
        }

        public static VisualBasicCompilation CreateVisualBasicCompilation(IEnumerable<MetadataReference> references)
        {
            var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithGlobalImports(GlobalImport.Parse(
                    "System",
                    "System.Collections.Generic",
                    "System.Diagnostics",
                    "System.Globalization",
                    "System.IO",
                    "System.Linq",
                    "System.Reflection",
                    "System.Runtime.CompilerServices",
                    "System.Security",
                    "System.Text",
                    "System.Threading.Tasks",
                    "Microsoft.VisualBasic"));
            var compilation = VisualBasicCompilation.Create("Conversion", references: references)
                .WithOptions(compilationOptions);
            return compilation;
        }
    }
}