using System.Diagnostics;
using System.Reflection;

namespace ICSharpCode.CodeConverter.Util;

public static class CodeConverterVersion
{
    private static string _versionInfo;
    public static string GetVersion() => _versionInfo ??= GetFileVersion();
    private static string GetFileVersion()
    {
        var assembly = Assembly.GetAssembly(typeof(CodeConverter));
        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fvi.FileVersion;
    }
}