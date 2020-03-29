using System;
using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    public class NamedFact
    {
        public NamedFact(string name, Func<Task> execute)
        {
            Execute = execute;
            Name = name;
        }

        public Func<Task> Execute { get; }
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
