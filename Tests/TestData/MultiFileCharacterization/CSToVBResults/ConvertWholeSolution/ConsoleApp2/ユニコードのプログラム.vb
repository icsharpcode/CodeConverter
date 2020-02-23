Imports System
Imports System.Diagnostics

Namespace ConsoleApp2
    Public Class ユニコードのプログラム
        Public Shared Sub Main()
            For Each process In Process.GetProcesses().Where(Function(p) String.IsNullOrEmpty(p.MainWindowTitle)).Take(1)
                ' Here's a comment
                Console.WriteLine(-1)
                Dim class1 = New CSharpNetStandardLib.Class1()
                class1.MethodOnlyDifferingInTypeParameterCount()
                class1.MethodOnlyDifferingInTypeParameterCount(Of Object)()
                class1.MethodOnlyDifferingInTypeParameterCount(Of Object, String)()
            Next
        End Sub
    End Class
End Namespace
