using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var assembly = Assembly.Load(compiledIL);
            return GetTestNamesAndCallbacks(assembly);
        }

        public static Dictionary<string, Action> GetTestNamesAndCallbacks(Assembly assembly)
        {
            var factMethods = DiscoverFactMethods(assembly);
            return factMethods.ToDictionary(GetFullName, m => new Action(() => {
                var instance = Activator.CreateInstance(m.DeclaringType);
                m.Invoke(instance, null);
            }));
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
