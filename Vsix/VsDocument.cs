using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeConverter.VsExtension
{
    class VsDocument
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
                string itemPath = null;
                _hierarchy.GetMkDocument(_itemId, out itemPath);
                return itemPath;
            }
        }
    }
}