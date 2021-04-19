using System;

namespace CSharpNetStandardLib
{
    public class Class1
    {
        public void MethodOnlyDifferingInTypeParameterCount()
        {
        }
        public void MethodOnlyDifferingInTypeParameterCount<T>()
        {
        }
        public void MethodOnlyDifferingInTypeParameterCount<T1, T2>()
        {
        }
    }
}
