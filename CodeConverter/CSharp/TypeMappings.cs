using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class TypeMappings
    {
        public static readonly IReadOnlyDictionary<string, string> VbToCs = new Dictionary<string, string>
        {
            { "Microsoft.VisualBasic.MsgBoxResult", "System.Windows.Forms.DialogResult" }
        };
    }
}
