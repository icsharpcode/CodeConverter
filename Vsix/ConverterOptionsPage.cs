using System;
using System.ComponentModel;
using CodeConv.Shared.Util;
using Microsoft.VisualStudio.Shell;

namespace ICSharpCode.CodeConverter.VsExtension;

/// <summary>
/// Sample https://github.com/microsoft/VSExtensibility/blob/main/New_Extensibility_Model/Samples/SettingsSample/SettingDefinitions.cs
/// </summary>
internal sealed class ConverterOptionsPage : DialogPage
{
    private const string SettingsPageCategory = "Settings";
    private bool _bypassAssemblyLoadingErrors;

    [Category(SettingsPageCategory)]
    [DisplayName("Copy result to clipboard")]
    [Description("When a single document conversion finishs, copy the the result (if any) to the clipboard.")]
    public bool CopyResultToClipboardForSingleDocument { get; set; }

    [Category(SettingsPageCategory)]
    [DisplayName("Overwrite files without warning")]
    [Description("When a project is converted, the solution and any referencing project files need to be updated. Setting this option to true skips the dialog box usually displayed.")]
    public bool AlwaysOverwriteFiles { get; set; }

    [Category(SettingsPageCategory)]
    [DisplayName("Create backups")]
    [Description("When a project is converted, the solution and any referencing project files need to be updated. Setting this option to false skips creating '.bak' files for overwritten files.")]
    public bool CreateBackups { get; set; } = true;

    [Category(SettingsPageCategory)]
    [DisplayName("Comment and formatting timeout (minutes)")]
    [Description("Roslyn formatting can take a very long time for large files and has no progress updates. Set this to the maximum you're willing to wait without any indication of progress.")]
    public int FormattingTimeout { get; set; } = 15;

    [Category(SettingsPageCategory)]
    [DisplayName("Bypass assembly loading errors")]
    [Description("Attempts to workaround 'Could not load file or assembly' errors by loading any assembly version available.")]
    public bool BypassAssemblyLoadingErrors {
        get {
            return _bypassAssemblyLoadingErrors;
        }
        set {
            _bypassAssemblyLoadingErrors = value;
            AppDomain.CurrentDomain.UseVersionAgnosticAssemblyResolution(value);
        }
    }
}