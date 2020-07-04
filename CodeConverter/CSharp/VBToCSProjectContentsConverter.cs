using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using LangVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using System;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>
    /// Can be stateful, need a new one for each project
    /// </remarks>
    internal class VBToCSProjectContentsConverter : IProjectContentsConverter
    {
        private readonly ConversionOptions _conversionOptions;
        private readonly bool _useProjectLevelWinformsAdjustments;
        private CSharpCompilation _csharpViewOfVbSymbols;
        private Dictionary<string, string> _designerToResxRelativePath;
        private Project _convertedCsProject;

        private Project _csharpReferenceProject;
        private readonly IProgress<ConversionProgress> _progress;
        private readonly CancellationToken _cancellationToken;

        public VBToCSProjectContentsConverter(ConversionOptions conversionOptions, bool useProjectLevelWinformsAdjustments, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            _conversionOptions = conversionOptions;
            this._useProjectLevelWinformsAdjustments = useProjectLevelWinformsAdjustments;
            _progress = progress;
            _cancellationToken = cancellationToken;
        }

        public string RootNamespace => _conversionOptions.RootNamespaceOverride ??
                                       ((VisualBasicCompilationOptions)SourceProject.CompilationOptions).RootNamespace;

        public async Task InitializeSourceAsync(Project project)
        {
            project = await ClashingMemberRenamer.RenameClashingSymbolsAsync(project);
            var cSharpCompilationOptions = CSharpCompiler.CreateCompilationOptions();
            _convertedCsProject = project.ToProjectFromAnyOptions(cSharpCompilationOptions, CSharpCompiler.ParseOptions);
            _csharpReferenceProject = project.CreateReferenceOnlyProjectFromAnyOptions(cSharpCompilationOptions, CSharpCompiler.ParseOptions);
            _csharpViewOfVbSymbols = (CSharpCompilation)await _csharpReferenceProject.GetCompilationAsync(_cancellationToken);
            _designerToResxRelativePath = project.ReadVbEmbeddedResources().ToDictionary(r => r.LastGenOutput, r => r.RelativePath);
            SourceProject = await WithProjectLevelWinformsAdjustmentsAsync(project);
        }

        private async Task<Project> WithProjectLevelWinformsAdjustmentsAsync(Project project)
        {
            if (!_useProjectLevelWinformsAdjustments) return project;
            return await project.WithAdditionalDocs(_designerToResxRelativePath.Values)
                                    .WithRenamedMergedMyNamespaceAsync(_cancellationToken);
        }

        public string LanguageVersion { get { return LangVersion.Latest.ToDisplayString(); } }

        public Project SourceProject { get; private set; }

        public async Task<SyntaxNode> SingleFirstPassAsync(Document document)
        {
            return await VisualBasicConverter.ConvertCompilationTreeAsync(document, _csharpViewOfVbSymbols, _csharpReferenceProject, _cancellationToken);
        }

        public async Task<(Project project, List<WipFileConversion<DocumentId>> firstPassDocIds)> GetConvertedProjectAsync(WipFileConversion<SyntaxNode>[] firstPassResults)
        {
            var projDirPath = SourceProject.GetDirectoryPath();
            var (project, docIds) = _convertedCsProject.WithDocuments(firstPassResults.Select(r => r.WithTargetPath(GetTargetPath(projDirPath, r))).ToArray());
            if (_useProjectLevelWinformsAdjustments) project = await project.RenameMergedNamespacesAsync(_progress, _cancellationToken);
            return (project, docIds);
        }

        private string GetTargetPath(string projDirPath, WipFileConversion<SyntaxNode> r)
        {
            return _designerToResxRelativePath.ContainsKey(GetPathRelativeToProject(projDirPath, r.SourcePath)) ? Path.Combine(projDirPath, Path.GetFileName(r.TargetPath)) : null;
        }

        private static string GetPathRelativeToProject(string projDirPath, string p)
        {
            return p.Replace(projDirPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public async IAsyncEnumerable<ConversionResult> GetAdditionalConversionResults(IReadOnlyCollection<TextDocument> additionalDocumentsToConvert, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string projDirPath = SourceProject.GetDirectoryPath();
            foreach (var doc in additionalDocumentsToConvert) {
                string newPath = Path.Combine(projDirPath, Path.GetFileName(doc.FilePath));
                if (newPath != doc.FilePath) {
                    string newText = RebaseResxPaths(projDirPath, Path.GetDirectoryName(doc.FilePath), (await doc.GetTextAsync(cancellationToken)).ToString());
                    yield return new ConversionResult(newText) {
                        SourcePathOrNull = doc.FilePath,
                        TargetPathOrNull = newPath
                    };
                }
            }
        }

        private string RebaseResxPaths(string projDirPath, string resxDirPath, string originalResx)
        {
            var xml = XDocument.Parse(originalResx);
            var xmlNs = xml.Root.GetDefaultNamespace();
            var fileRefValues = xml.Descendants(xmlNs + "data")
                .Where(a => a.Attribute("type")?.Value == "System.Resources.ResXFileRef, System.Windows.Forms")
                .Select(d => d.Element(xmlNs + "value"));
            foreach (var fileRefValue in fileRefValues) {
                var origValueParts = fileRefValue.Value.Split(';');
                string newRelativePath = GetPathRelativeToProject(projDirPath, Path.GetFullPath(Path.Combine(resxDirPath, origValueParts[0])));
                fileRefValue.Value = string.Join(";", newRelativePath.Yield().Concat(origValueParts.Skip(1)));
            }
            return xml.Declaration.ToString() + Environment.NewLine + xml.ToString();
        }
    }
}