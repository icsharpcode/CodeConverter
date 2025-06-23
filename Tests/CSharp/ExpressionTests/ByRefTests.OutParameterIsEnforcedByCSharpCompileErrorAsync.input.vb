Imports System.Runtime.InteropServices ' Statement removed so comment removed too

Public Class OutParameterIsEnforcedByCSharpCompileError
    Shared Sub LogAndReset(<Out> ByRef arg As Integer)
        System.Console.WriteLine(arg)
    End Sub
End Class