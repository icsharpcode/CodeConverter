Imports System.Diagnostics
Imports System.Threading

Public Class AcmeClass
    Private Declare Sub SetForegroundWindow Lib "user32" (ByVal hwnd As Int32)

    Public Shared Sub Main()
        For Each proc In Process.GetProcesses().Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
            SetForegroundWindow(proc.MainWindowHandle.ToInt32())
            Thread.Sleep(1000)
        Next
    End Sub
End Class