using System;
using System.Collections.Generic;
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
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ConverterOptionsPage),
        "Code Converter", "General", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class REConverterPackage : AsyncPackage
    {
        public VisualStudioWorkspace VsWorkspace {
            get {
                var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
                return componentModel.GetService<VisualStudioWorkspace>();
            }
        }

        /// <summary>
        /// ConvertCSToVBCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "60378c8b-d75c-4fb2-aa2b-58609d67f886";

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

        public async Task<ConverterOptionsPage> GetOptionsAsync()
        {
            return await this.GetDialogPageAsync<ConverterOptionsPage>();
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await Task.WhenAll(ConvertCSToVBCommand.InitializeAsync(this),
                ConvertVBToCSCommand.InitializeAsync(this));
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
    }
}
