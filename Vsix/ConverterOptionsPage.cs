using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Settings;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;

namespace ICSharpCode.CodeConverter.VsExtension;

/// <summary>
/// Sample https://github.com/microsoft/VSExtensibility/blob/main/New_Extensibility_Model/Samples/SettingsSample/SettingDefinitions.cs
/// </summary>
internal static class ConverterSettings
{
    [VisualStudioContribution]
    public static SettingCategory ConverterSettingsCategory { get; } = new("converterSettings", "Settings") {
        Description = "Settings for the Converter extension.",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean CopyResultToClipboardForSingleDocument { get; } = new("copyResultToClipboardForSingleDocument", "Copy result to clipboard", ConverterSettingsCategory, defaultValue: false) {
        Description = "When a single document conversion finishes, copy the result (if any) to the clipboard.",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean AlwaysOverwriteFiles { get; } = new("alwaysOverwriteFiles", "Overwrite files without warning", ConverterSettingsCategory, defaultValue: false) {
        Description = "When a project is converted, the solution and any referencing project files need to be updated. Setting this option to true skips the dialog box usually displayed.",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean CreateBackups { get; } = new("createBackups", "Create backups", ConverterSettingsCategory, defaultValue: true) {
        Description = "When a project is converted, the solution and any referencing project files need to be updated. Setting this option to false skips creating '.bak' files for overwritten files.",
    };

    [VisualStudioContribution]
    internal static Setting.Integer FormattingTimeout { get; } = new("formattingTimeout", "Comment and formatting timeout (minutes)", ConverterSettingsCategory, defaultValue: 15) {
        Description = "Roslyn formatting can take a very long time for large files and has no progress updates. Set this to the maximum you're willing to wait without any indication of progress.",
        Minimum = 1,
    };
}

namespace ICSharpCode.CodeConverter.VsExtension
{
}

