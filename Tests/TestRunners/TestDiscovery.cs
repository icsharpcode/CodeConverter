using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Engine;
using NUnit.Engine.Services;
using Xunit;

namespace CodeConverter.Tests.TestRunners
{
    /// <summary>
    /// Discover and return xUnit tests in given assemblies.
    /// </summary>
    public static class TestDiscovery
    {
        public static Dictionary<string, Action> GetTestNamesAndCallbacks(byte[] compiledIL)
        {
            string tempAssembly = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
            try {
                File.WriteAllBytes(tempAssembly, compiledIL);
                // https://github.com/nunit/nunit3-tdnet-adapter/issues/9#issuecomment-239300093
                using (var testEngine = TestEngineActivator.CreateInstance()) {
                    var testRunner = testEngine.GetRunner(new TestPackage(tempAssembly));
                    var tests = testRunner.Explore(TestFilter.Empty);

                    var assembly = Assembly.Load(compiledIL);
                    var factMethods = DiscoverFactMethods(assembly);
                    return factMethods.ToDictionary(GetFullName, m => new Action(() => {
                        var instance = Activator.CreateInstance(m.DeclaringType);
                        m.Invoke(instance, null);
                    }));
                }
            } finally {
                File.Delete(tempAssembly);
            }

        }

        private static IEnumerable<MethodInfo> DiscoverFactMethods(Assembly assembly)
        {
            return assembly.GetTypes().SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(false).Any(a => a is FactAttribute) &&
                           !m.GetCustomAttributes(false).Any(a => a is TheoryAttribute));
        }

        private static string GetFullName(MethodInfo method) => method.DeclaringType.FullName + "." + method.Name;
    }
}
