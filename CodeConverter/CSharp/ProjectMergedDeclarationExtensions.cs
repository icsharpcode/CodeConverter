using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Allows transforming embedded/merged declarations into real documents. i.e. the VB My namespace
    /// </summary>
    /// <remarks>
    /// Rather than renaming the declarations, it may make more sense to change the parse options to eliminate the original one using the internal: WithSuppressEmbeddedDeclarations.
    /// </remarks>
    internal static class ProjectMergedDeclarationExtensions
    {
        public static Project WithAdditionalDocs(this Project vbProject, IEnumerable<string> relativePaths)
        {
            string projDir = vbProject.GetDirectoryPath();
            foreach (var fullPath in relativePaths.SelectMany(relativePath => GetAllLocaleResxFilePaths(projDir, relativePath))) {
                vbProject = vbProject.AddAdditionalDocument(Path.GetFileName(fullPath), SourceText.From(File.ReadAllText(fullPath)), filePath: fullPath).Project;
            }
            return vbProject;
        }

        private static IEnumerable<string> GetAllLocaleResxFilePaths(string projDir, string relativePath)
        {
            var filename = Path.GetFileName(relativePath);
            string fullDir = Path.Combine(projDir, Path.GetDirectoryName(relativePath));
            string otherLocaleSearchPattern = Path.ChangeExtension(filename, ".*.resx");
            var resxPaths = Directory.EnumerateFiles(fullDir, filename).Concat(Directory.EnumerateFiles(fullDir, otherLocaleSearchPattern));
            return resxPaths;
        }

        public static IEnumerable<(string RelativePath, string LastGenOutput)> ReadVbEmbeddedResources(this Project vbProject)
        {
            if (vbProject.FilePath == null || !File.Exists(vbProject.FilePath)) yield break;
            var projXml = XDocument.Load(vbProject.FilePath);
            var xmlNs = projXml.Root.GetDefaultNamespace();
            foreach (var resx in projXml.Descendants().Where(IsStandaloneGeneratedResource)) {
                string relativePath = GetIncludeOrUpdateAttribute(resx).Value;
                string lastGenOutput = resx.Element(xmlNs + "LastGenOutput").Value;
                yield return (relativePath, Path.Combine(Path.GetDirectoryName(relativePath), lastGenOutput));
            }
        }

        private static bool IsStandaloneGeneratedResource(XElement t)
        {
            return t.Name.LocalName == "EmbeddedResource" &&
                GetIncludeOrUpdateAttribute(t)?.Value?.EndsWith(".resx") == true &&
                t.Element(t.GetDefaultNamespace() + "Generator")?.Value?.EndsWith("ResXFileCodeGenerator") == true;
        }

        private static XAttribute GetIncludeOrUpdateAttribute(XElement t)
        {
            return t.Attribute("Include") ?? t.Attribute("Update");
        }

        public static async Task<Project> WithRenamedMergedMyNamespaceAsync(this Project vbProject, CancellationToken cancellationToken)
        {
            string name = "MyNamespace";
            var projectDir = Path.Combine(vbProject.GetDirectoryPath(), "My Project");

            var compilation = await vbProject.GetCompilationAsync(cancellationToken);
            var embeddedSourceTexts = await GetAllEmbeddedSourceText(compilation).Select((r, i) => (Text: r, Suffix: $".Static.{i+1}")).ToArrayAsync();
            var generatedSourceTexts = (Text: await GetDynamicallyGeneratedSourceTextAsync(compilation), Suffix: ".Dynamic").Yield();

            foreach (var (text, suffix) in embeddedSourceTexts.Concat(generatedSourceTexts)) {
                vbProject = WithRenamespacedDocument(name + suffix, vbProject, text, projectDir);
            }

            return vbProject;
        }

        private static Project WithRenamespacedDocument(string baseName, Project vbProject, string sourceText, string myProjectDirPath)
        {
            if (string.IsNullOrWhiteSpace(sourceText)) return vbProject;
            return vbProject.AddDocument(baseName, sourceText.Renamespace(), filePath: Path.Combine(myProjectDirPath, baseName + ".Designer.vb")).Project;
        }

        private static async IAsyncEnumerable<string> GetAllEmbeddedSourceText(Compilation compilation)
        {
            var roots = await compilation.SourceModule.GlobalNamespace.Locations.
                Where(l => !l.IsInSource).Select(CachedReflectedDelegates.GetEmbeddedSyntaxTree)
                .SelectAsync(t => t.GetTextAsync());
            foreach (var r in roots) yield return r.ToString();
        }

        private static async Task<string> GetDynamicallyGeneratedSourceTextAsync(Compilation compilation)
        {
            var myNamespace = (compilation.RootNamespace() + ".My").TrimStart('.'); //Root namespace can be empty
            var myProject = compilation.GetTypeByMetadataName($"{myNamespace}.MyProject");
            var myForms = GetVbTextForProperties(myProject, "MyForms");
            var myWebServices = GetVbTextForProperties(myProject, "MyWebservices");
            if (string.IsNullOrWhiteSpace(myForms) && string.IsNullOrWhiteSpace(myWebServices)) return "";

            return $@"Imports System
Imports System.ComponentModel
Imports System.Diagnostics

Namespace My
    Public Partial Module MyProject
{myForms}

{myWebServices}
    End Module
End Namespace";
        }

        private static string GetVbTextForProperties(INamedTypeSymbol myProject, string propertyContainerClassName)
        {
            var containerType = myProject?.GetMembers(propertyContainerClassName).OfType<ITypeSymbol>().FirstOrDefault();
            var propertiesToReplicate = containerType?.GetMembers().Where(m => m.IsKind(SymbolKind.Property)).ToArray();
            if (propertiesToReplicate?.Any() != true) return "";
            var vbTextForProperties = propertiesToReplicate.Select(s => {
                var fieldName = $"{Constants.MergedMyMemberPrefix}m_{s.Name}";
                return $@"
            <EditorBrowsable(EditorBrowsableState.Never)>
            Public {fieldName} As {s.Name}

            Public Property {Constants.MergedMyMemberPrefix}{s.Name} As {s.Name}
                <DebuggerHidden>
                Get
                    {fieldName} = Create__Instance__(Of {s.Name})({fieldName})
                    Return {fieldName}
                End Get
                <DebuggerHidden>
                Set(ByVal value As {s.Name})
                    If value Is {fieldName} Then Return
                    If value IsNot Nothing Then Throw New ArgumentException(""Property can only be set to Nothing"")
                    Me.Dispose__Instance__(Of {s.Name})({fieldName})
                End Set
            End Property
";
            });
            string propertiesWithoutContainer = string.Join(Environment.NewLine, vbTextForProperties);
            return $@"        Friend Partial Class {propertyContainerClassName}
{propertiesWithoutContainer}
        End Class";
        }

        private static string Renamespace(this string sourceText)
        {
            return sourceText
                .Replace("Namespace Global.Microsoft.VisualBasic", $"Namespace Global.Microsoft.{Constants.MergedMsVbNamespace}")
                .Replace("Global.Microsoft.VisualBasic.Embedded", $"Global.Microsoft.{Constants.MergedMsVbNamespace}.Embedded")
                .Replace("Namespace My", $"Namespace {Constants.MergedMyNamespace}");
        }

        public static async Task<Project> RenameMergedNamespacesAsync(this Project project, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            project = await RenamePrefixAsync(project, Constants.MergedMyNamespace, "My", SymbolFilter.Namespace, progress, cancellationToken);
            project = await RenamePrefixAsync(project, Constants.MergedMsVbNamespace, "VisualBasic", SymbolFilter.Namespace, progress, cancellationToken);
            project = await RenamePrefixAsync(project, Constants.MergedMyMemberPrefix, "", SymbolFilter.Member, progress, cancellationToken);
            return project;
        }

        private static async Task<Project> RenamePrefixAsync(Project project, string oldNamePrefix,
            string newNamePrefix, SymbolFilter symbolFilter, IProgress<ConversionProgress> progress,
            CancellationToken cancellationToken)
        {
            int toSkip = 0;
            for (var symbolToRename = await GetElementToRename(project); symbolToRename != null; symbolToRename = await GetElementToRename(project, toSkip)) {
                string newName = symbolToRename.Name.Replace(oldNamePrefix, newNamePrefix);
                try {
                    var renamedSolution = await Renamer.RenameSymbolAsync(project.Solution, symbolToRename, newName, project.Solution.Workspace.Options, cancellationToken);
                    project = renamedSolution.GetProject(project.Id);
                } catch (Exception e) {
                    toSkip++;
                    progress.Report(new ConversionProgress($"ERROR: Failed to rename {symbolToRename} to {newName}, please do so manually. Exception details:\r\n{e}"));
                }
            }
            return project;

            async Task<ISymbol> GetElementToRename(Project lProject, int lToSkip = 0)
            {
                return (await GetSymbolStartingWithAsync(lProject, oldNamePrefix, symbolFilter, cancellationToken)).ElementAtOrDefault(lToSkip);
            }
        }

        private static async Task<IEnumerable<ISymbol>> GetSymbolStartingWithAsync(Project project, string symbolPrefix, SymbolFilter symbolFilter, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            return compilation.GetSymbolsWithName(s => s.StartsWith(symbolPrefix), symbolFilter);
        }
    }
}
