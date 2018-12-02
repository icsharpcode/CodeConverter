using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CodeConverter.Tests.TestRunners
{
    /// <summary>
    /// Discover and return xUnit tests in given assemblies.
    /// </summary>
    public class TestDiscovery : XunitTestFramework//XunitTestFrameworkDiscoverer
    {
        public static Dictionary<string, Action> GetTestNamesAndCallbacks(byte[] compiledIL)
        {
            var assembly = Assembly.Load(compiledIL);
            return GetTestNamesAndCallbacks(assembly);
        }

        public static Dictionary<string, Action> GetTestNamesAndCallbacks(Assembly assembly)
        {
            List<object> obj = new List<object>();
            new TestDiscovery(new LambdaMessageSink(m => obj.Add(m))).GetDiscoverer(new ReflectionAssemblyInfo(assembly)).Find(false, new LambdaMessageSink(m => obj.Add(m)), new TestDiscoveryOptions());
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
        

        private class LambdaMessageSink : IMessageSink
        {
            private readonly Action<IMessageSinkMessage> _action;

            public LambdaMessageSink(Action<IMessageSinkMessage> action)
            {
                _action = action;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                //if (message is DiscoveryCompleteMessage)
                _action(message);
                return true;
            }
        }

        private class TestDiscoveryOptions : ITestFrameworkDiscoveryOptions
        {
            private readonly Dictionary<string, object> _options = new Dictionary<string, object>();

            public TValue GetValue<TValue>(string name)
            {
                return  _options.TryGetValue(name, out var option) ? (TValue)option : default(TValue);
            }

            public void SetValue<TValue>(string name, TValue value)
            {
                _options[name] = value;
            }
        }

        public TestDiscovery(IMessageSink messageSink) : base(messageSink)
        {
        }
    }
}
