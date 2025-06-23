Imports System
Imports System.Runtime.CompilerServices ' Removed since the extension attribute is removed

Public Module MyExtensions
    <Extension()>
    Public Sub Add(Of T)(ByRef arr As T(), item As T)
        Array.Resize(arr, arr.Length + 1)
        arr(arr.Length - 1) = item
    End Sub
End Module

Public Module UsagePoint
    Public Sub Main()
        Dim arr = New Integer() {1, 2, 3}
        arr.Add(4)
        System.Console.WriteLine(arr(3))
    End Sub
End Module