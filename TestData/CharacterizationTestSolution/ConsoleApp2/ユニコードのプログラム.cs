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
            }
        }

    }
}
