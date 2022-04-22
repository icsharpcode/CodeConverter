namespace ICSharpCode.CodeConverter.Common;


public readonly record struct WipFileConversion<TWip>(string SourcePath, string TargetPath, TWip Wip, string[] Errors)
{
    internal WipFileConversion(string sourcePath, TWip wip, string[] errors) : this(sourcePath, PathConverter.TogglePathExtension(sourcePath), wip, errors)
    {
    }

    internal WipFileConversion<T> With<T>(T wip, string[] errors = null)
    {
        return new WipFileConversion<T>(SourcePath, TargetPath, wip, errors ?? Errors);
    }
}