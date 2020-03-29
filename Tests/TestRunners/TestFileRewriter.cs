using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    public static class TestFileRewriter
    {
        private static readonly Lazy<Dictionary<string, string>> FileContents = new Lazy<Dictionary<string, string>>(GetTestFileContents);

        private static Dictionary<string, string> GetTestFileContents()
        {
            return Directory.GetFiles(GetTestSourceDirectoryPath(), "*Tests.cs", SearchOption.AllDirectories)
                .ToDictionary(f => f, s => File.ReadAllText(s, Encoding.UTF8));
        }

        private static string GetTestSourceDirectoryPath()
        {
            string assemblyDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);
            string testSourceDirectoryPath = Path.Combine(assemblyDir, @"..\..\");
            return testSourceDirectoryPath;
        }

        public static void UpdateFiles(string expected, string actual)
        {
            lock (FileContents) {
                foreach (var fileContent in FileContents.Value) {
                    var newFc = fileContent.Value.Replace(expected.Replace("\"", "\"\""), actual.Replace("\"", "\"\""));
                    if (fileContent.Value != newFc) {
                        FileContents.Value[fileContent.Key] = newFc;
                        File.WriteAllText(fileContent.Key, newFc, Encoding.UTF8);
                        return;
                    }
                }
            }
        }
    }
}
