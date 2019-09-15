using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CSToVBConversion : ILanguageConversion
    {
        private Project _sourceCsProject;
        private Project _convertedVbProject;
        private VisualBasicCompilation _vbViewOfCsSymbols;
        private VisualBasicParseOptions _visualBasicParseOptions;
        private Project _vbReferenceProject;
        public string RootNamespace { get; set; }

        public async Task Initialize(Project project)
        {
            _sourceCsProject = project;
            var cSharpCompilationOptions = VisualBasicCompiler.CreateCompilationOptions(RootNamespace);
            _visualBasicParseOptions = VisualBasicParseOptions.Default;
            _convertedVbProject = project.ToProjectFromAnyOptions(cSharpCompilationOptions, _visualBasicParseOptions);
            _vbReferenceProject = project.CreateReferenceOnlyProjectFromAnyOptionsAsync(cSharpCompilationOptions);
            _vbViewOfCsSymbols = (VisualBasicCompilation)await _vbReferenceProject.GetCompilationAsync();
        }

        public async Task<Document> SingleFirstPass(Document document)
        {
            var convertedTree = await CSharpConverter.ConvertCompilationTree(document, _vbViewOfCsSymbols, _vbReferenceProject);
            var convertedDocument = _convertedVbProject.AddDocument(document.FilePath, convertedTree);
            _convertedVbProject = convertedDocument.Project;
            return convertedDocument;
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
                ("\\\\Microsoft\\.CSharp\\.targets", "\\Microsoft.VisualBasic.targets"),
                ("\\.cs\"", ".vb\""),
                ("\\.cs<", ".vb<")
            };
        }
        public string PostTransformProjectFile(string xml)
        {
            xml = ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xml, "vb", "cs");
            xml = TweakDefineConstantsSeparator(xml);
            xml = AddInfer(xml);
            return xml;
        }

        private string AddInfer(string xml)
        {
            if (xml.IndexOf("<OptionInfer>") > -1) return xml;

            string propertygroup = "<PropertyGroup>";
            var startOfFirstPropertyGroup = xml.IndexOf(propertygroup);
            if (startOfFirstPropertyGroup == -1) return xml;

            int endOfFirstPropertyGroupStartTag = startOfFirstPropertyGroup + propertygroup.Length;
            return xml.Substring(0, endOfFirstPropertyGroupStartTag) + Environment.NewLine + "    <OptionInfer>On</OptionInfer>" +
                   xml.Substring(endOfFirstPropertyGroupStartTag);
        }

        private static string TweakDefineConstantsSeparator(string s)
        {
            var startTag = "<DefineConstants>";
            var endTag = "</DefineConstants>";
            var defineConstantsStart = s.IndexOf(startTag);
            var defineConstantsEnd = s.IndexOf(endTag);
            if (defineConstantsStart == -1 || defineConstantsEnd == -1)
                return s;

            return s.Substring(0, defineConstantsStart) +
                   s.Substring(defineConstantsStart, defineConstantsEnd - defineConstantsStart).Replace(";", ",") +
                   s.Substring(defineConstantsEnd);
        }

        public string TargetLanguage { get; } = LanguageNames.VisualBasic;

        public bool CanBeContainedByMethod(SyntaxNode node)
        {
            return node is CSSyntax.IncompleteMemberSyntax || 
                   node is CSSyntax.StatementSyntax || 
                   node.ContainsSkippedText ||
                   node.IsMissing ||
                   ParsedAsFieldButCouldBeLocalVariableDeclaration(node); ;
        }

        public bool MustBeContainedByClass(SyntaxNode node)
        {
            return node is CSSyntax.BaseMethodDeclarationSyntax || node is CSSyntax.BaseFieldDeclarationSyntax ||
                   node is CSSyntax.BasePropertyDeclarationSyntax;
        }

        private static bool ParsedAsFieldButCouldBeLocalVariableDeclaration(SyntaxNode node)
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
            var modifier = text.Contains("abstract ") ? "abstract " : "";
            return $@"{modifier}class SurroundingClass
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

        public Task<SyntaxNode> SingleSecondPass(Document doc)
        {
            return doc.GetSyntaxRootAsync();
        }

        public async Task<string> GetWarningsOrNull()
        {
            var sourceCompilation = await _sourceCsProject.GetCompilationAsync();
            var convertedCompilation = await _convertedVbProject.GetCompilationAsync();
            return CompilationWarnings.WarningsForCompilation(sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(convertedCompilation, "target");
        }

        public SyntaxTree CreateTree(string text)
        {
            return new CSharpCompiler().CreateTree(text);
        }

        public Document CreateProjectDocumentFromTree(Workspace workspace, SyntaxTree tree,
            IEnumerable<MetadataReference> references)
        {
            return CSharpCompiler.CreateCompilationOptions().CreateProjectDocumentFromTree(workspace, tree, references, CSharpParseOptions.Default);
        }
    }
}