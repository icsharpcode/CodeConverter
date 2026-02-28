using System;
using System.IO;
using System.Reflection;

namespace ICSharpCode.CodeConverter.Tests;

public static class TestConstants
{
    /// <summary>
    /// To recharacterize:
    ///  Set to true
    ///  Run all tests
    ///  Inspect changes in git
    ///  Set to false
    ///  Commit
    /// </summary>
    public static bool RecharacterizeByWritingExpectedOverActual => false;

    public static string GetTestDataDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var solutionDir = new FileInfo(new Uri(assembly.Location).LocalPath).Directory?.Parent?.Parent?.Parent ??
                          throw new InvalidOperationException(assembly.Location);
        return Path.Combine(solutionDir.FullName, "TestData");
    }
}