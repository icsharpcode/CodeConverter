using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace ICSharpCode.CodeConverter.VsExtension
{
    internal sealed class ConverterOptionsPage : DialogPage
    {
        [Category("Settings")]
        [DisplayName("Copy result to clipboard")]
        [Description("When a single document conversion finishs, copy the the result (if any) to the clipboard.")]
        public bool CopyResultToClipboardForSingleDocument { get; set; }

        [Category("Settings")]
        [DisplayName("Overwrite files without warning")]
        [Description("When a project is converted, the solution and any referencing project files need to be updated. Setting this option to true skips the dialog box usually displayed.")]
        public bool AlwaysOverwriteFiles { get; set; }

        [Category("Settings")]
        [DisplayName("Create backups")]
        [Description("When a project is converted, the solution and any referencing project files need to be updated. Setting this option to false skips creating '.bak' files for overwritten files.")]
        public bool CreateBackups{ get; set; } = true;
    }

}