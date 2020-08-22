using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    /// <summary>
    /// Discover and return xUnit Facts in given assemblies
    /// </summary>
    /// <remarks>Does not support any other xUnit attributes such as Theory</remarks>
    public static class XUnitFactDiscoverer
    {
        public static IEnumerable<NamedFact> GetNamedFacts(Assembly assembly)
        {
            var factMethods = DiscoverFactMethods(assembly);
            return factMethods.Select(m => new NamedFact(GetFullName(m), () => {
                var instance = Activator.CreateInstance(m.DeclaringType);
                m.Invoke(instance, null);
                return Task.CompletedTask;
            }));
        }

        private static IEnumerable<MethodInfo> DiscoverFactMethods(Assembly assembly)
        {
            return assembly.GetTypes().SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(false).Any(a => a is FactAttribute fa && string.IsNullOrWhiteSpace(fa.Skip)) &&
                           !m.GetCustomAttributes(false).Any(a => a is TheoryAttribute));
        }

        private static string GetFullName(MethodInfo method) => method.DeclaringType.FullName + "." + method.Name;
    }
}
