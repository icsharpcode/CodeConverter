using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.VB
{
    /// <remarks>
    /// Can be stateful, need a new one for each project
    /// </remarks>
    internal class CSToVBProjectContentsConverter
    {
        private readonly VisualBasicCompilationOptions _vbCompilationOptions;
        private readonly VisualBasicParseOptions _vbParseOptions;
        private Project _sourceCsProject;
        private Project _convertedVbProject;
        private VisualBasicCompilation _vbViewOfCsSymbols;
        private Project _vbReferenceProject;

        public CSToVBProjectContentsConverter(string rootNamespace, VisualBasicCompilationOptions vbCompilationOptions, VisualBasicParseOptions vbParseOptions)
        {
            _vbCompilationOptions = vbCompilationOptions?.WithRootNamespace(rootNamespace) ?? VisualBasicCompiler.CreateCompilationOptions(rootNamespace);
            _vbParseOptions = vbParseOptions ?? VisualBasicParseOptions.Default;
        }

        public string LanguageVersion { get { return _vbParseOptions.LanguageVersion.ToDisplayString(); } }


        public async Task<Project> InitializeSourceAsync(Project project)
        {
            _sourceCsProject = project;
            _convertedVbProject = project.ToProjectFromAnyOptions(_vbCompilationOptions, _vbParseOptions);
            _vbReferenceProject = project.CreateReferenceOnlyProjectFromAnyOptions(_vbCompilationOptions);
            _vbViewOfCsSymbols = (VisualBasicCompilation)await _vbReferenceProject.GetCompilationAsync();
            return project;
        }

        public async Task<SyntaxNode> SingleFirstPass(Document document)
        {
            return await CSharpConverter.ConvertCompilationTree(document, _vbViewOfCsSymbols, _vbReferenceProject);
        }

        public async Task<(Project project, List<(string Path, DocumentId DocId, string[] Errors)> firstPassDocIds)>
            GetConvertedProject((string Path, SyntaxNode Node, string[] Errors)[] firstPassResults)
        {
            return _convertedVbProject.WithDocuments(firstPassResults);
        }
    }
}