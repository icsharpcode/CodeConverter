using System;
using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    public record NamedTest(string Name, Func<Task> Execute)
    {
        public override string ToString() => Name;
    }
}