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
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class VBToCSConversion : ILanguageConversion
    {
        private Compilation _sourceCompilation;
        private readonly List<SyntaxTree> _secondPassResults = new List<SyntaxTree>();
        private CSharpCompilation _convertedCompilation;
        private Project _project;
        private Project _convertedProject;
        private CSharpCompilation _csharpViewOfVbSymbols;

        public string RootNamespace { get; set; }


        public async Task Initialize(Compilation convertedCompilation, Project project)
        {
            _project = project;
            _convertedCompilation = (CSharpCompilation) convertedCompilation;
            if (project != null) {

                var projectInfo = CreateProjectInfo(project, project.Id, project.Name, CSharpCompiler.CreateCompilationOptions(), project.ProjectReferences);
                var convertedSolution = project.Solution.RemoveProject(project.Id).AddProject(projectInfo);
                _convertedProject = convertedSolution.GetProject(project.Id);
                _csharpViewOfVbSymbols = await GetCSharpCompilationReferencingProject(project);
            }
        }

        private async Task<CSharpCompilation> GetCSharpCompilationReferencingProject(Project project)
        {
            var baseOptions = CSharpCompiler.CreateCompilationOptions();
            var options = WithMetadataImportOptionsAll(baseOptions);
            var viewerId = ProjectId.CreateNewId();
            var projectReferences = project.ProjectReferences.Concat(new[] {new ProjectReference(project.Id)});
            var viewerProjectInfo = CreateProjectInfo(project, viewerId, project.Name + viewerId, options,
                projectReferences);
            var csharpViewOfVbProject = project.Solution.AddProject(viewerProjectInfo).GetProject(viewerId);
            return (CSharpCompilation) await csharpViewOfVbProject.GetCompilationAsync();
        }

        /// <summary>
        /// This method becomes public in CodeAnalysis 3.1 and hence we can be confident it won't disappear.
        /// Need to use reflection for now until that version is widely enough deployed as taking a dependency would mean everyone needs latest VS version.
        /// </summary>
        private CSharpCompilationOptions WithMetadataImportOptionsAll(CSharpCompilationOptions baseOptions)
        {
            Type optionsType = baseOptions.GetType();
            var withMetadataImportOptions = optionsType
                .GetMethod("WithMetadataImportOptions", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var options =
                (CSharpCompilationOptions) withMetadataImportOptions.Invoke(baseOptions,
                    new object[] {(byte)2 /*MetadataImportOptions.All*/});
            return options;
        }

        private static ProjectInfo CreateProjectInfo(Project project, ProjectId projectId, string projectName, CSharpCompilationOptions cSharpCompilationOptions, IEnumerable<ProjectReference> projectProjectReferences)
        {
            return ProjectInfo.Create(projectId, project.Version, projectName, project.AssemblyName,
                LanguageNames.CSharp, null, project.OutputFilePath,
                cSharpCompilationOptions,
                CSharpParseOptions.Default, new DocumentInfo[0], projectProjectReferences,
                project.MetadataReferences, project.AnalyzerReferences);
        }

        public Document SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            _sourceCompilation = sourceCompilation;
            var converted = VisualBasicConverter.ConvertCompilationTree((VisualBasicCompilation)sourceCompilation, _csharpViewOfVbSymbols, (VisualBasicSyntaxTree)tree);
            var convertedTree = SyntaxFactory.SyntaxTree(converted);
            _convertedCompilation = _convertedCompilation.AddSyntaxTrees(convertedTree);
            var convertedDocument = _convertedProject.AddDocument(tree.FilePath, converted);
            _convertedProject = convertedDocument.Project;
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
            var doc = cs.Value.WithSyntaxRoot((await cs.Value.GetSyntaxRootAsync()).WithAdditionalAnnotations(Simplifier.Annotation));
            var cSharpSyntaxNode = new CompilationErrorFixer((CSharpCompilation) await doc.Project.GetCompilationAsync(), (CSharpSyntaxTree) await doc.GetSyntaxTreeAsync()).Fix();
            var simplifiedDocument = await Simplifier.ReduceAsync(doc.WithSyntaxRoot(cSharpSyntaxNode));
            _convertedProject = simplifiedDocument.Project;

            _secondPassResults.Add(await simplifiedDocument.GetSyntaxTreeAsync());
            return cSharpSyntaxNode;
        }

        public string GetWarningsOrNull()
        {
            return CompilationWarnings.WarningsForCompilation(_sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(_convertedCompilation, "target");
        }

        public SyntaxTree CreateTree(string text)
        {
            return CreateCompiler().CreateTree(text);
        }

        private VisualBasicCompiler CreateCompiler()
        {
            return new VisualBasicCompiler(RootNamespace);
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return CreateCompiler().CreateCompilationFromTree(tree, references);
        }
    }
}