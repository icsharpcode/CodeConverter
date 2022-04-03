using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ICSharpCode.CodeConverter.VsExtension;

internal class VsDocument
{
    private readonly IVsProject _hierarchy;
    private readonly uint _itemId;
    public Guid ProjectGuid { get; }

    public VsDocument(IVsProject hierarchy, Guid projectGuid, uint itemId)
    {
        this._hierarchy = hierarchy;
        ProjectGuid = projectGuid;
        this._itemId = itemId;
    }

    public string ItemPath {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();
            _hierarchy.GetMkDocument(_itemId, out string itemPath);
            return itemPath;
        }
    }
}