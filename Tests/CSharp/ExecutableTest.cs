using System;

namespace CodeConverter.Tests.CSharp
{
    public class ExecutableTest
    {
        public ExecutableTest(string name, Action execute)
        {
            Execute = execute;
            Name = name;
        }

        public Action Execute { get; }
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}