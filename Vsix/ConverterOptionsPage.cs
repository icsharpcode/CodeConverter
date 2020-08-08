using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace ICSharpCode.CodeConverter.VsExtension
{
    internal sealed class ConverterOptionsPage : DialogPage
    {
        private const string SettingsPageCategory = "Settings";

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
        public bool CreateBackups{ get; set; } = true;

        [Category(SettingsPageCategory)]
        [DisplayName("Comment and formatting timeout (minutes)")]
        [Description("Positioning comments correctly, and formatting and tidying up the result can take a very long time for large files. Set this to how many minutes you're willing to wait.")]
        public int FormattingTimeout{ get; set; } = 15;
    }

}