namespace ICSharpCode.CodeConverter.Shared;

internal struct WipFileConversion
{
    public static WipFileConversion<TWip> Create<TWip>(string path, TWip wip, string[] errors)
    {
        return new WipFileConversion<TWip>(path, wip, errors);
    }
}

public readonly struct WipFileConversion<TWip>
{
    public string SourcePath { get; }
    public string TargetPath { get; }
    public TWip Wip { get; }
    public string[] Errors { get; }

    private WipFileConversion(string sourcePath, string targetPath, TWip wip, string[] errors)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath ?? (sourcePath != null ? PathConverter.TogglePathExtension(sourcePath) : null);
        Wip = wip;
        Errors = errors;
    }

    internal WipFileConversion(string sourcePath, TWip wip, string[] errors) : this(sourcePath, null, wip, errors)
    {
    }

    public WipFileConversion<TWip> WithTargetPath(string targetPathOrNull)
    {
        return new WipFileConversion<TWip>(SourcePath, targetPathOrNull, Wip, Errors);
    }

    internal WipFileConversion<T> With<T>(T wip, string[] errors = null)
    {
        return new WipFileConversion<T>(SourcePath, TargetPath, wip, errors ?? Errors);
    }

    public override bool Equals(object obj)
    {
        return obj is WipFileConversion<TWip> other &&
               SourcePath == other.SourcePath &&
               EqualityComparer<TWip>.Default.Equals(Wip, other.Wip) &&
               EqualityComparer<string[]>.Default.Equals(Errors, other.Errors);
    }

    public override int GetHashCode()
    {
        return SourcePath.GetHashCode();
    }

    public void Deconstruct(out string path, out TWip node, out string[] errors)
    {
        path = SourcePath;
        node = Wip;
        errors = Errors;
    }

    public static implicit operator (string Path, TWip Node, string[] Errors)(WipFileConversion<TWip> value)
    {
        return (value.SourcePath, value.Wip, value.Errors);
    }

    public static implicit operator WipFileConversion<TWip>((string Path, TWip Wip, string[] Errors) value)
    {
        return new WipFileConversion<TWip>(value.Path, value.Wip, value.Errors);
    }
}