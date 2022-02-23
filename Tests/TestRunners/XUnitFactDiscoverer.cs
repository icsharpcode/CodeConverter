using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    /// <summary>
    /// Discover and return xUnit Facts/Theories in given assembly
    /// </summary>
    /// <remarks></remarks>
    public static class XUnitFactDiscoverer
    {
        public static IEnumerable<NamedTest> GetNamedFacts(Assembly assembly)
        {
            var factMethods = DiscoverMethods(assembly);
            return factMethods.SelectMany(m => {
                var isTheory = m.GetCustomAttribute<TheoryAttribute>();
                return isTheory != null ? GetTheories(m) : new[] { GetFact(m) };
            });
        }

        private static NamedTest GetFact(MethodInfo m)
        {
            return new NamedTest(GetFullName(m), () => {
                var instance = Activator.CreateInstance(m.DeclaringType);
                m.Invoke(instance, null);
                return Task.CompletedTask;
            });
        }

        private static IEnumerable<NamedTest> GetTheories(MethodInfo m)
        {
            var inlineData = m.GetCustomAttributes<InlineDataAttribute>();
            foreach (var data in inlineData.Select(t => t.GetData(m).Single()))
            {
                yield return new NamedTest(GetFullName(m, data), () => {
                    var instance = Activator.CreateInstance(m.DeclaringType);
                    m.Invoke(instance, data);
                    return Task.CompletedTask;
                });
            }
        }

        private static IEnumerable<MethodInfo> DiscoverMethods(Assembly assembly)
        {
            return assembly.GetTypes().SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(false).Any(a => a is FactAttribute fa && string.IsNullOrWhiteSpace(fa.Skip)));
        }

        private static string GetFullName(MethodInfo method) => method.DeclaringType.FullName + "." + method.Name;
        private static string GetFullName(MethodInfo method, object[] data) => GetFullName(method) + $"({string.Join(", ", GetArgumentsAsString(data))})";
        private static IEnumerable<string> GetArgumentsAsString(object[] data) => data.Select(t => t?.ToString() ?? "null");
    }
}
