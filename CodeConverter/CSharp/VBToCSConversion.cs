using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;
using System.Threading;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>
    /// Can be used for multiple projects - does not persist state from one InitializeSource to the next
    /// </remarks>
    public class VBToCSConversion : ILanguageConversion
    {
        private const string UnresolvedNamespaceDiagnosticId = "CS0246";
        private const string FabricatedAssemblyName = ISymbolExtensions.ForcePartialTypesAssemblyName;
        private VBToCSProjectContentsConverter _vbToCsProjectContentsConverter;
        private IProgress<ConversionProgress> _progress;
        private CancellationToken _cancellationToken;

        public ConversionOptions ConversionOptions { get; set; }


        public async Task<IProjectContentsConverter> CreateProjectContentsConverterAsync(Project project, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            _progress = progress;
            _cancellationToken = cancellationToken;
            bool useProjectLevelWinformsAdjustments = project.AssemblyName != FabricatedAssemblyName;
            _vbToCsProjectContentsConverter = new VBToCSProjectContentsConverter(ConversionOptions, useProjectLevelWinformsAdjustments, progress, cancellationToken);
            await _vbToCsProjectContentsConverter.InitializeSourceAsync(project);
            return _vbToCsProjectContentsConverter;
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
            string rootNamespaceDot = _vbToCsProjectContentsConverter.RootNamespace;
            if (!string.IsNullOrEmpty(rootNamespaceDot)) rootNamespaceDot += ".";

            return new[] {
                ("\\\\Microsoft\\.VisualBasic\\.targets", "\\Microsoft.CSharp.targets"),
                ("\\.vb\"", ".cs\""),
                ("\\.vb<", ".cs<"),
                ("<\\s*Generator\\s*>\\s*VbMyResourcesResXFileCodeGenerator\\s*</\\s*Generator\\s*>", "<Generator>ResXFileCodeGenerator</Generator>"),
                ("(<\\s*CustomToolNamespace\\s*>)(.*</\\s*CustomToolNamespace\\s*>)", $"$1{rootNamespaceDot}$2"), // <CustomToolNamespace>My.Resources</CustomToolNamespace>
            };
        }

        public string PostTransformProjectFile(string xml)
        {
            xml = ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xml, "cs", "vb");

            if (!Regex.IsMatch(xml, @"<Reference\s+Include=""Microsoft.VisualBasic""\s*/>")) {
                xml = new Regex(@"(<ItemGroup>)(\s*)").Replace(xml, "$1$2<Reference Include=\"Microsoft.VisualBasic\" />$2", 1);
            }

            if (!Regex.IsMatch(xml, @"<\s*LangVersion\s*>")) {
                xml = new Regex(@"(\s*)(</\s*PropertyGroup\s*>)").Replace(xml, $"$1  <LangVersion>{_vbToCsProjectContentsConverter.LanguageVersion}</LangVersion>$1$2", 1);
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

        public async Task<Document> SingleSecondPassAsync(Document doc)
        {
            var simplifiedDocument = await doc.SimplifyStatementsAsync<CSSyntax.UsingDirectiveSyntax, CSSyntax.ExpressionSyntax>(UnresolvedNamespaceDiagnosticId, _cancellationToken);

            // Can't add a reference to Microsoft.VisualBasic if there's no project file, so hint to install the package
            if (_vbToCsProjectContentsConverter.SourceProject.AssemblyName == FabricatedAssemblyName) {
                var simpleRoot = await simplifiedDocument.GetSyntaxRootAsync();
                var vbUsings = simpleRoot.ChildNodes().OfType<CSSyntax.UsingDirectiveSyntax>().Where(u => u.Name.ToString().Contains("Microsoft.VisualBasic"));
                var commentedRoot = simpleRoot.ReplaceNodes(vbUsings, (_, r) => r.WithTrailingTrivia(CS.SyntaxFactory.Comment(" // Install-Package Microsoft.VisualBasic").Yield().Concat(r.GetTrailingTrivia())));
                simplifiedDocument = simplifiedDocument.WithSyntaxRoot(commentedRoot);
            }
            return simplifiedDocument;
        }

        public SyntaxTree CreateTree(string text)
        {
            return CreateCompiler().CreateTree(text);
        }

        private VisualBasicCompiler CreateCompiler()
        {
            return new VisualBasicCompiler(ConversionOptions.RootNamespaceOverride);
        }

        public async Task<Document> CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            var project = await CreateEmptyVbProjectAsync(references);
            return project.AddDocumentFromTree(tree);
        }

        private async Task<Project> CreateEmptyVbProjectAsync(IEnumerable<MetadataReference> references)
        {
            return await VisualBasicCompiler.CreateCompilationOptions(ConversionOptions.RootNamespaceOverride)
                            .CreateProjectAsync(references, VisualBasicParseOptions.Default, FabricatedAssemblyName);
        }
    }
}