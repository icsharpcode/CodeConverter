using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class VBToCSConversion : ILanguageConversion
    {
        private Compilation _sourceCompilation;
        private readonly List<SyntaxTree> _secondPassResults = new List<SyntaxTree>();
        private CSharpCompilation _convertedCompilation;

        public string RootNamespace { get; set; }


        public void Initialize(Compilation convertedCompilation)
        {
            _convertedCompilation = (CSharpCompilation) convertedCompilation;
        }

        public SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            _sourceCompilation = sourceCompilation;
            var converted = VisualBasicConverter.ConvertCompilationTree((VisualBasicCompilation)sourceCompilation, _convertedCompilation, (VisualBasicSyntaxTree)tree);
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
                ("\\\\Microsoft\\.VisualBasic\\.targets", "\\Microsoft.CSharp.targets"),
                ("\\.vb\"", ".cs\""),
                ("\\.vb<", ".cs<")
            };
        }

        public string PostTransformProjectFile(string s)
        {
            s = ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(s, "cs", "vb");

            if (!Regex.IsMatch(s, @"<Reference\s+Include=""Microsoft.VisualBasic""\s*/>")) {
                s = Regex.Replace(s, @"(<Reference\s+Include=""System""\s*/>)", "<Reference Include=\"Microsoft.VisualBasic\" />\r\n    $1");
            }

            // TODO Find API to, or parse project file sections to remove "<DefineDebug>true</DefineDebug>" + "<DefineTrace>true</DefineTrace>"
            // Then add them to the define constants in the same section, or create one if necessary.

            var defineConstantsStart = s.IndexOf("<DefineConstants>");
            var defineConstantsEnd = s.IndexOf("</DefineConstants>");
            if (defineConstantsStart == -1 || defineConstantsEnd == -1)
                return s;

            return s.Substring(0, defineConstantsStart) +
                   s.Substring(defineConstantsStart, defineConstantsEnd - defineConstantsStart).Replace(",", ";") +
                   s.Substring(defineConstantsEnd);
        }

        public string TargetLanguage { get; } = LanguageNames.CSharp;
        
        public bool CanBeContainedByMethod(SyntaxNode node)
        {
            return node is VBSyntax.IncompleteMemberSyntax ||
                   !(node is VBSyntax.DeclarationStatementSyntax) ||
                   node.ContainsSkippedText ||
                   node.IsMissing ||
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
            var modifier = text.Contains("MustOverride ") ? "MustInherit " : "";
            return $@"{modifier}Class SurroundingClass
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
            return new VisualBasicCompiler(RootNamespace).CreateTree(text);
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return new VisualBasicCompiler(RootNamespace).CreateCompilationFromTree(tree, references);
        }
    }
}