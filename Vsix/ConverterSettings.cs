using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility;

namespace ICSharpCode.CodeConverter.VsExtension;

[DataContract]
public class ConverterSettings
{
    [DataMember]
    public bool CopyResultToClipboardForSingleDocument { get; set; }

    [DataMember]
    public bool AlwaysOverwriteFiles { get; set; }

    [DataMember]
    public bool CreateBackups { get; set; } = true;

    [DataMember]
    public int FormattingTimeoutMinutes { get; set; } = 15;

    [DataMember]
    public bool BypassAssemblyLoadingErrors { get; set; }
}
