namespace ICSharpCode.CodeConverter.Common;

internal static class PathConverter
{
    public static string TogglePathExtension(string filePath)
    {
        var originalExtension = Path.GetExtension(filePath);
        return Path.ChangeExtension(filePath, GetConvertedExtension(originalExtension));
    }

    private static string GetConvertedExtension(string originalExtension)
    {
        switch (originalExtension) {
            case ".csproj":
                return ".vbproj";
            case ".vbproj":
                return ".csproj";
            case ".cs":
                return ".vb";
            case ".vb": //https://github.com/dotnet/roslyn/blob/91571a3bb038e05e7bf2ab87510273a1017faed0/src/VisualStudio/VisualBasic/Impl/LanguageService/VisualBasicPackage.vb#L45-L52
            case ".bas":
            case ".cls":
            case ".ctl":
            case ".dob":
            case ".dsr":
            case ".frm":
            case ".pag":
                return ".cs";
            default:
                return originalExtension;
        }
    }

    public static string GetRelativePath(string relativeTo, string path)
    {
        var uri = new Uri(relativeTo);
        var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        return rel;
    }
}