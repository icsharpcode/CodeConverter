using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class ProjectTypeGuids
    {
        public static readonly IReadOnlyCollection<(string, string)> VbToCsTypeGuids = new [] {
            ("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"), // Framework project
            ("{778DAE3C-4631-46EA-AA77-85C1314464D9}", "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"), // Net standard project
            ("{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}", "{20D4826A-C6FA-45DB-90F4-C717570B9F32}"), // Legacy (2003) Smart Device
            ("{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}", "{4D628B5B-2FBC-4AA6-8C16-197242AEB884}"), // Smart Device
            ("{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}", "{14822709-B5A1-4724-98CA-57A101D1B079}"), // Workflow
            ("{593B0543-81F6-4436-BA1E-4747859CAAE2}", "{EC05E597-79D4-47f3-ADA0-324C4F7C7484}"), // SharePoint
            ("{DB03555F-0C8B-43BE-9FF9-57896B3C5E56}", "{C089C8C0-30E0-4E22-80C0-CE093F111A43}") // Store App Windows Phone 8.1 Silverlight
        };
    }
}
