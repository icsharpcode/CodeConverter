using ICSharpCode.CodeConverter.Shared;
using System.IO;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal sealed class DesignerWithResx
    {
        public string SourceDesignerPath {get;}
        public string TargetDesignerPath { get; }
        public string SourceResxPath { get; }
        public string TargetResxPath { get; }

        private DesignerWithResx(string path, string newDesignerPath, string oldResxPath, string newResxPath)
        {
            this.SourceDesignerPath = path;
            this.TargetDesignerPath = newDesignerPath;
            this.SourceResxPath = oldResxPath;
            this.TargetResxPath = newResxPath;
        }

        public static DesignerWithResx TryCreate(string projectDir, string sourcePathOrNull)
        {
            if (sourcePathOrNull != null && sourcePathOrNull.EndsWith(".Designer.vb")) {
                var sourceFile = new FileInfo(sourcePathOrNull);
                if (sourceFile.Directory.FullName != projectDir) {
                    string resxFilename = sourceFile.Name.Replace(".Designer.vb", ".resx");
                    string oldResxPath = System.IO.Path.Combine(sourceFile.Directory.FullName, resxFilename);
                    if (File.Exists(oldResxPath)) {
                        string newDesignerPath = PathConverter.TogglePathExtension(sourcePathOrNull);
                        string newResxPath = System.IO.Path.Combine(projectDir, resxFilename);
                        return new DesignerWithResx(sourcePathOrNull, newDesignerPath, oldResxPath, newResxPath);
                    }
                }
            }
            return null;
        }
    }
}