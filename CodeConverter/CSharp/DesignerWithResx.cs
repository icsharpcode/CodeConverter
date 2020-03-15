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

        public static DesignerWithResx TryCreate(string path)
        {
            if (path != null && path.EndsWith(".Designer.vb")) {
                var sourceFile = new FileInfo(path);
                if (sourceFile.Directory.Name == "My Project") {
                    string projectDir = sourceFile.Directory.Parent.FullName;
                    string resxFilename = sourceFile.Name.Replace(".Designer.vb", ".resx");
                    string oldResxPath = System.IO.Path.Combine(projectDir, sourceFile.Directory.Name, resxFilename);
                    if (File.Exists(oldResxPath)) {
                        string newDesignerPath = PathConverter.TogglePathExtension(System.IO.Path.Combine(projectDir, sourceFile.Name));
                        string newResxPath = System.IO.Path.Combine(sourceFile.Directory.Parent.FullName, resxFilename);
                        return new DesignerWithResx(path, newDesignerPath, oldResxPath, newResxPath);
                    }
                }
            }
            return null;
        }
    }
}