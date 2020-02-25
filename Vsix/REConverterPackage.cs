using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    /// <summary>
    /// Implements the VS package exposed by this assembly.
    /// 
    /// This package will load when:
    /// * Visual Studio has been configured not to support UIContextRules and has a solution with a csproj or vbproj
    /// * Someone clicks one of the menu items
    /// * Someone opens the options page (it doesn't need to load in this case, but it seems to anyway)
    /// </summary>
    /// <remarks>
    /// Until the package is loaded, converting a multiple selection of projects won't work because there's no way to set a ProvideUIContextRule that covers that case
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ConverterOptionsPage),
        "Code Converter", "General", 0, 0, true)]
    [Guid(PackageGuidString)]
    //See https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-use-rule-based-ui-context-for-visual-studio-extensions?view=vs-2019#term-types
    [ProvideUIContextRule(ConvertableSolutionMenuVisibilityGuid, name: nameof(ConvertableSolutionMenuVisibilityGuid),
        expression: "HasVbproj | HasCsproj", termNames: new[] { "HasVbproj", "HasCsproj" },
        termValues: new[] { "SolutionHasProjectCapability:VB", "SolutionHasProjectCapability:CSharp" })]
    [ProvideUIContextRule(CsEditorMenuVisibilityGuid, name: nameof(CsEditorMenuVisibilityGuid),
        expression: "Cs", termNames: new[] { "Cs" },
        termValues: new[] { "ActiveEditorContentType:CSharp" })]
    [ProvideUIContextRule(VbEditorMenuVisibilityGuid, name: nameof(VbEditorMenuVisibilityGuid),
        expression: "Vb", termNames: new[] { "Vb" },
        termValues: new[] { "ActiveEditorContentType:Basic" })]
    [ProvideUIContextRule(CsFileMenuVisibilityGuid, name: nameof(CsFileMenuVisibilityGuid),
        expression: "DotCs", termNames: new[] { "DotCs" },
        termValues: new[] { "HierSingleSelectionName:.cs$"})]
    [ProvideUIContextRule(VbFileMenuVisibilityGuid, name: nameof(VbFileMenuVisibilityGuid),
        expression: "DotVb", termNames: new[] { "DotVb" },
        termValues: new[] { "HierSingleSelectionName:.vb$"})]
    [ProvideUIContextRule(CsProjMenuVisibilityGuid, name: nameof(CsProjMenuVisibilityGuid),
        expression: "Csproj", termNames: new[] { "Csproj" },
        termValues: new[] { "ActiveProjectCapability:CSharp" })]
    [ProvideUIContextRule(VbProjMenuVisibilityGuid, name: nameof(VbProjMenuVisibilityGuid),
        expression: "Vbproj", termNames: new[] { "Vbproj" },
        termValues: new[] { "ActiveProjectCapability:VB" })]
    [ProvideUIContextRule(CsSolutionMenuVisibilityGuid, name: nameof(CsSolutionMenuVisibilityGuid),
        expression: "HasCsproj", termNames: new[] { "HasCsproj" },
        termValues: new[] { "SolutionHasProjectCapability:CSharp" })]
    [ProvideUIContextRule(VbSolutionMenuVisibilityGuid, name: nameof(VbSolutionMenuVisibilityGuid),
        expression: "HasVbproj", termNames: new[] { "HasVbproj" },
        termValues: new[] { "SolutionHasProjectCapability:VB" })]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class REConverterPackage : AsyncPackage
    {
        /// <summary>
        /// ConvertCSToVBCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "60378c8b-d75c-4fb2-aa2b-58609d67f886";

        public const string CsEditorMenuVisibilityGuid = "64448be3-dcfe-467c-8659-408d672a9909";
        public const string VbEditorMenuVisibilityGuid = "8eb86734-0b20-4986-9f20-9ed22824d0e2";
        public const string CsFileMenuVisibilityGuid = "e32d529f-034b-4fe8-8e27-33a8ecf8f9ca";
        public const string VbFileMenuVisibilityGuid = "207ed41c-1bf3-4e92-ad4f-f910b461acfc";
        public const string CsProjMenuVisibilityGuid = "045a3ed1-4cb2-4c47-95be-0d99948e854f";
        public const string VbProjMenuVisibilityGuid = "11700acc-38d7-4fc1-88dd-9e316aa5d6d5";
        public const string CsSolutionMenuVisibilityGuid = "cbe34396-af03-49ab-8945-3611a641abf6";
        public const string VbSolutionMenuVisibilityGuid = "3332e9e5-019c-4e93-b75a-2499f6f1cec6";
        public const string ConvertableSolutionMenuVisibilityGuid = "8e7192d0-28b7-4fe7-8d84-82c1db98d459";

        internal Cancellation PackageCancellation { get; } = new Cancellation();

        /// <summary>
        /// Initializes a new instance of package class.
        /// </summary>
        public REConverterPackage()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadWithoutVersionForOurDependencies;
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        private Assembly LoadWithoutVersionForOurDependencies(object sender, ResolveEventArgs args)
        {
            var requestedAssemblyName = new AssemblyName(args.Name);
            if (requestedAssemblyName.Version != null && IsThisExtensionRequestingAssembly()) {
                return LoadAnyVersionOfAssembly(requestedAssemblyName);
            }
            return null;

        }

        private static Assembly LoadAnyVersionOfAssembly(AssemblyName assemblyName)
        {
            try {
                return Assembly.Load(new AssemblyName(assemblyName.Name){CultureName = assemblyName.CultureName});
            } catch (FileNotFoundException e) when (e.FileName.Contains("Microsoft.VisualStudio.LanguageServices") && ProbablyRequiresVsUpgrade) {
                MessageBox.Show(
                    "Code Converter cannot find `Microsoft.VisualStudio.LanguageServices`. Please upgrade Visual Studio to version 15.9.3 or above.\r\n\r\n" +
                    "If after upgrading you still see this error, attach your activity log %AppData%\\Microsoft\\VisualStudio\\<version>\\ActivityLog.xml to a GitHub issue at https://github.com/icsharpcode/CodeConverter \r\n\r\n" +
                    "You can press Ctrl + C to copy this message",
                    "Upgrade Visual Studio", MessageBoxButton.OK);
                return null;
            }
        }

        private bool IsThisExtensionRequestingAssembly()
        {
            return GetPossibleRequestingAssemblies().Contains(GetType().Assembly);
        }

        private IEnumerable<Assembly> GetPossibleRequestingAssemblies()
        {
            return new StackTrace().GetFrames().Select(f => f.GetMethod().DeclaringType?.Assembly)
                .SkipWhile(a => a == GetType().Assembly);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var oleMenuCommandService = await this.GetServiceAsync<IMenuCommandService, OleMenuCommandService>();
            var componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var visualStudioWorkspace = componentModel.GetService<VisualStudioWorkspace>();
            var codeConversion = await CodeConversion.CreateAsync(this, visualStudioWorkspace, this.GetDialogPageAsync<ConverterOptionsPage>);
            ConvertCSToVBCommand.Initialize(this, oleMenuCommandService, codeConversion);
            ConvertVBToCSCommand.Initialize(this, oleMenuCommandService, codeConversion);
            VisualStudioInteraction.Initialize(PackageCancellation);
            await TaskScheduler.Default;
            await base.InitializeAsync(cancellationToken, progress);
        }

        public static bool ProbablyRequiresVsUpgrade {
            get {
                var version = FullVsVersion;
                return version == null || version < new Version(15, 9, 3, 0);
            }
        }

        private static Version FullVsVersion {
            get {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");

                if (File.Exists(path)) {
                    var fvi = FileVersionInfo.GetVersionInfo(path);
                    return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart,
                        fvi.ProductPrivatePart);
                } else return null;
            }
        }

        internal OleMenuCommandWithBlockingStatus CreateCommand(Func<CancellationToken, Task> callbackAsync, CommandID menuCommandId)
        {
            return new OleMenuCommandWithBlockingStatus(JoinableTaskFactory, PackageCancellation, callbackAsync, menuCommandId);
        }

        protected override void Dispose(bool disposing)
        {
            PackageCancellation.Dispose();
            base.Dispose(disposing);
        }
    }
}
