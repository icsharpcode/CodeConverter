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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class VBToCSConversion : ILanguageConversion
    {
        private Project _sourceVbProject;
        private CSharpCompilation _csharpViewOfVbSymbols;
        private Project _convertedCsProject;

        public string RootNamespace { get; set; }

        public async Task Initialize(Project project)
        {
            _sourceVbProject = project;
            var cSharpCompilationOptions = CSharpCompiler.CreateCompilationOptions();
            _convertedCsProject = project.ToProjectFromAnyOptions(cSharpCompilationOptions);
            _csharpViewOfVbSymbols = (CSharpCompilation) await project.CreateReferenceOnlyCompilationFromAnyOptionsAsync(cSharpCompilationOptions);
        }

        public async Task<Document> SingleFirstPass(Document document)
        {
            var converted = await VisualBasicConverter.ConvertCompilationTree(document, _csharpViewOfVbSymbols);
            var convertedDocument = _convertedCsProject.AddDocument(document.FilePath, converted);
            _convertedCsProject = convertedDocument.Project;
            return convertedDocument;
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

        public string PostTransformProjectFile(string xml)
        {
            xml = ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xml, "cs", "vb");

            if (!Regex.IsMatch(xml, @"<Reference\s+Include=""Microsoft.VisualBasic""\s*/>")) {
                xml = Regex.Replace(xml, @"(<Reference\s+Include=""System""\s*/>)", "<Reference Include=\"Microsoft.VisualBasic\" />\r\n    $1");
            }

            // TODO Find API to, or parse project file sections to remove "<DefineDebug>true</DefineDebug>" + "<DefineTrace>true</DefineTrace>"
            // Then add them to the define constants in the same section, or create one if necessary.

            var defineConstantsStart = xml.IndexOf("<DefineConstants>");
            var defineConstantsEnd = xml.IndexOf("</DefineConstants>");
            if (defineConstantsStart == -1 || defineConstantsEnd == -1)
                return xml;

            return xml.Substring(0, defineConstantsStart) +
                   xml.Substring(defineConstantsStart, defineConstantsEnd - defineConstantsStart).Replace(",", ";") +
                   xml.Substring(defineConstantsEnd);
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

        public async Task<SyntaxNode> SingleSecondPass(KeyValuePair<string, Document> cs)
        {
            var doc = cs.Value;
            var cSharpSyntaxNode = new CompilationErrorFixer((CSharpCompilation) await doc.Project.GetCompilationAsync(), (CSharpSyntaxTree) await doc.GetSyntaxTreeAsync()).Fix();
            var simplifiedDocument = doc.WithSyntaxRoot(cSharpSyntaxNode);
            _convertedCsProject = simplifiedDocument.Project;
            return await simplifiedDocument.GetSyntaxRootAsync();
        }

        public async Task<string> GetWarningsOrNull()
        {
            var sourceCompilation = await _sourceVbProject.GetCompilationAsync();
            var convertedCompilation = await _convertedCsProject.GetCompilationAsync();
            return CompilationWarnings.WarningsForCompilation(sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation( convertedCompilation, "target");
        }

        public SyntaxTree CreateTree(string text)
        {
            return CreateCompiler().CreateTree(text);
        }

        private VisualBasicCompiler CreateCompiler()
        {
            return new VisualBasicCompiler(RootNamespace);
        }

        public Document CreateProjectDocumentFromTree(Workspace workspace, SyntaxTree tree,
            IEnumerable<MetadataReference> references)
        {
            return VisualBasicCompiler.CreateCompilationOptions(RootNamespace).CreateProjectDocumentFromTree(workspace, tree, references);
        }
    }
}