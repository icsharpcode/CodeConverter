using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;
using LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>
    /// Can be stateful, need a new one for each project
    /// </remarks>
    internal class VBToCSProjectContentsConverter
    {
        private readonly string _overrideRootNamespace;
        private Project _sourceVbProject;
        private CSharpCompilation _csharpViewOfVbSymbols;
        private Project _convertedCsProject;

        /// <summary>
        /// It's really hard to change simplifier options since everything is done on the Object hashcode of internal fields.
        /// I wanted to avoid saying "default" instead of "default(string)" because I don't want to force a later language version on people in such a common case.
        /// This will have that effect, but also has the possibility of failing to interpret code output by this converter.
        /// If this has such unintended effects in future, investigate the code that loads options from an editorconfig file
        /// </summary>
        private static readonly CSharpParseOptions DoNotAllowImplicitDefault = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7);

        private Project _csharpReferenceProject;

        public VBToCSProjectContentsConverter(string overrideRootNamespace)
        {
            _overrideRootNamespace = overrideRootNamespace;
        }

        public string RootNamespace => _overrideRootNamespace;

        public async Task<Project> InitializeSource(Project project)
        {
            var cSharpCompilationOptions = CSharpCompiler.CreateCompilationOptions();
            _convertedCsProject = project.ToProjectFromAnyOptions(cSharpCompilationOptions, DoNotAllowImplicitDefault);
            _csharpReferenceProject = project.CreateReferenceOnlyProjectFromAnyOptions(cSharpCompilationOptions);
            _csharpViewOfVbSymbols = (CSharpCompilation) await _csharpReferenceProject.GetCompilationAsync();

            project = await project.WithRenamedMergedMyNamespace();
            _sourceVbProject = project;
            return project;
        }

        public async Task<SyntaxNode> SingleFirstPass(Document document)
        {
            return await VisualBasicConverter.ConvertCompilationTree(document, _csharpViewOfVbSymbols, _csharpReferenceProject);
        }

        public async Task<(Project project, List<(string Path, DocumentId DocId, string[] Errors)> firstPassDocIds)>
            GetConvertedProject((string Path, SyntaxNode Node, string[] Errors)[] firstPassResults)
        {
            var (project, docIds) = _convertedCsProject.WithDocuments(firstPassResults);
            return (await project.RenameMergedNamespaces(), docIds);
        }

        public Document CreateProjectDocumentFromTree(Workspace workspace, SyntaxTree tree,
            IEnumerable<MetadataReference> references)
        {
            return VisualBasicCompiler.CreateCompilationOptions(_overrideRootNamespace)
                .CreateProjectDocumentFromTree(workspace, tree, references, VisualBasicParseOptions.Default,
                    ISymbolExtensions.ForcePartialTypesAssemblyName);
        }
    }
}