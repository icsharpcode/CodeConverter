using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace RefactoringEssentials.VsExtension
{
    class VsDocument
    {
        private readonly IVsProject hierarchy;
        private readonly uint itemId;
        public Guid ProjectGuid { get; }

        public VsDocument(IVsProject hierarchy, Guid projectGuid, uint itemId)
        {
            this.hierarchy = hierarchy;
            ProjectGuid = projectGuid;
            this.itemId = itemId;
        }

        public string ItemPath {
            get {
                string itemPath = null;
                hierarchy.GetMkDocument(itemId, out itemPath);
                return itemPath;
            }
        }
    }
}