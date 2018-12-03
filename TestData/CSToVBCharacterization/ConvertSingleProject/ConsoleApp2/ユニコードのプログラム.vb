Imports System
Imports System.Diagnostics
Imports System.Linq

Namespace ConsoleApp2
    Public Class ユニコードのプログラム
        Public Shared Sub Main()
            For Each process In Process.GetProcesses().Where(Function(p) String.IsNullOrEmpty(p.MainWindowTitle)).Take(1)
                Console.WriteLine(-1)
            Next
        End Sub
    End Class
End Namespace
