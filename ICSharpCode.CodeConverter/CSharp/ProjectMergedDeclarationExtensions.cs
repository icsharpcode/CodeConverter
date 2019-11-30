using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

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
        public static async Task<Project> WithRenamedMergedMyNamespace(this Project vbProject)
        {
            string name = "MyNamespace";
            var projectDir = Path.Combine(vbProject.GetDirectoryPath() ?? vbProject.AssemblyName, "My Project");

            var compilation = await vbProject.GetCompilationAsync();
            string embeddedSourceText = (await GetAllEmbeddedSourceText(compilation));
            string generatedSourceText = (await GetDynamicallyGeneratedSourceText(compilation));

            vbProject = WithRenamespacedDocument(name + ".Static", vbProject, embeddedSourceText, projectDir);
            vbProject = WithRenamespacedDocument(name + ".Dynamic", vbProject, generatedSourceText, projectDir);

            return vbProject;
        }

        private static Project WithRenamespacedDocument(string baseName, Project vbProject, string sourceText, string myProjectDirPath)
        {
            if (string.IsNullOrWhiteSpace(sourceText)) return vbProject;
            return vbProject.AddDocument(baseName, sourceText.Renamespace(), filePath: Path.Combine(myProjectDirPath, baseName + ".Designer.vb")).Project;
        }

        private static async Task<string> GetAllEmbeddedSourceText(Compilation compilation)
        {
            var roots = await compilation.SourceModule.GlobalNamespace.Locations.
                Where(l => !l.IsInSource).Select(CachedReflectedDelegates.GetEmbeddedSyntaxTree)
                .SelectAsync(t => t.GetTextAsync());
            var renamespacesRootTexts =
                roots.Select(r => r.ToString());
            var combined = string.Join(Environment.NewLine, renamespacesRootTexts);
            return combined;
        }

        private static async Task<string> GetDynamicallyGeneratedSourceText(Compilation compilation)
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
                .Replace("Namespace My", $"Namespace {Constants.MergedMyNamespace}");
        }

        public static async Task<Project> RenameMergedNamespaces(this Project project)
        {
            project = await RenamePrefix(project, Constants.MergedMyNamespace, "My", SymbolFilter.Namespace);
            project = await RenamePrefix(project, Constants.MergedMsVbNamespace, "VisualBasic", SymbolFilter.Namespace);
            project = await RenamePrefix(project, Constants.MergedMyMemberPrefix, "", SymbolFilter.Member);
            return project;
        }

        private static async Task<Project> RenamePrefix(Project project, string oldNamePrefix, string newNamePrefix, SymbolFilter symbolFilter)
        {
            for (var symbolToRename = await GetFirstSymbolStartingWith(project, oldNamePrefix, symbolFilter); symbolToRename != null; symbolToRename = await GetFirstSymbolStartingWith(project, oldNamePrefix, symbolFilter))
            {
                var renamedSolution = await Renamer.RenameSymbolAsync(project.Solution, symbolToRename, symbolToRename.Name.Replace(oldNamePrefix, newNamePrefix), default(OptionSet));
                project = renamedSolution.GetProject(project.Id);
            }
            return project;
        }

        private static async Task<ISymbol> GetFirstSymbolStartingWith(Project project, string symbolPrefix, SymbolFilter symbolFilter)
        {
            var compilation = await project.GetCompilationAsync();
            return compilation.GetSymbolsWithName(s => s.StartsWith(symbolPrefix), symbolFilter).FirstOrDefault();
        }
    }
}
