using System;
using System.Diagnostics;
using System.Linq;

namespace ConsoleApp2
{
    public class ユニコードのプログラム
    {
        public static void Main()
        {
            foreach (var process in Process.GetProcesses().Where(p => String.IsNullOrEmpty(p.MainWindowTitle)).Take(1))
            {
                // Here's a comment
                Console.WriteLine(-1);
                var class1 = new CSharpNetStandardLib.Class1();
                class1.MethodOnlyDifferingInTypeParameterCount();
                class1.MethodOnlyDifferingInTypeParameterCount<object>();
                class1.MethodOnlyDifferingInTypeParameterCount<object, string>();
            }
        }

    }
}
