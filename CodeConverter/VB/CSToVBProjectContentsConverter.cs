using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Threading;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ICSharpCode.CodeConverter.VB
{
    /// <remarks>
    /// Can be stateful, need a new one for each project
    /// </remarks>
    internal class CSToVBProjectContentsConverter : IProjectContentsConverter
    {
        private readonly VisualBasicCompilationOptions _vbCompilationOptions;
        private readonly VisualBasicParseOptions _vbParseOptions;
        private Project _sourceCsProject;
        private Project _convertedVbProject;
        private VisualBasicCompilation _vbViewOfCsSymbols;
        private Project _vbReferenceProject;
        private readonly IProgress<ConversionProgress> _progress;
        private readonly CancellationToken _cancellationToken;

        public CSToVBProjectContentsConverter(ConversionOptions conversionOptions, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            _progress = progress;
            _cancellationToken = cancellationToken;
            var vbCompilationOptions =
                (VisualBasicCompilationOptions)conversionOptions.TargetCompilationOptionsOverride ??
                VisualBasicCompiler.CreateCompilationOptions(conversionOptions.RootNamespaceOverride);

            if (conversionOptions.RootNamespaceOverride != null) {
                vbCompilationOptions = vbCompilationOptions.WithRootNamespace(conversionOptions.RootNamespaceOverride);
            }

            _vbCompilationOptions = vbCompilationOptions;
            _vbParseOptions = VisualBasicCompiler.ParseOptions;
            RootNamespace = conversionOptions.RootNamespaceOverride;
        }

        public string RootNamespace { get; }
        public Project SourceProject { get; private set; }

        public string LanguageVersion { get { return _vbParseOptions.LanguageVersion.ToDisplayString(); } }


        public async Task InitializeSourceAsync(Project project)
        {
            // TODO: Don't throw away solution-wide effects - write them to referencing files, and use in conversion of any other projects being converted at the same time.
            project = await ClashingMemberRenamer.RenameClashingSymbolsAsync(project);
            _sourceCsProject = project;
            _convertedVbProject = project.ToProjectFromAnyOptions(_vbCompilationOptions, _vbParseOptions);
            _vbReferenceProject = project.CreateReferenceOnlyProjectFromAnyOptions(_vbCompilationOptions, _vbParseOptions);
            _vbViewOfCsSymbols = (VisualBasicCompilation)await _vbReferenceProject.GetCompilationAsync(_cancellationToken);
            SourceProject = project;
        }

        public async Task<SyntaxNode> SingleFirstPassAsync(Document document)
        {
            return await CSharpConverter.ConvertCompilationTreeAsync(document, _vbViewOfCsSymbols, _vbReferenceProject, _cancellationToken);
        }

        public async Task<(Project project, List<WipFileConversion<DocumentId>> firstPassDocIds)>
            GetConvertedProjectAsync(WipFileConversion<SyntaxNode>[] firstPassResults)
        {
            return _convertedVbProject.WithDocuments(firstPassResults);
        }

        public async IAsyncEnumerable<ConversionResult> GetAdditionalConversionResults(IReadOnlyCollection<TextDocument> additionalDocumentsToConvert, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
    }
}