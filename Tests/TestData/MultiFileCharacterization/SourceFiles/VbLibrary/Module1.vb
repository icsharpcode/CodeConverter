Imports System

Module Module1
    Private dict As New Dictionary(Of Integer, Integer)

    Private Sub UseOutParameterInModule()
        Dim x
        dict.TryGetValue(1, x)
    End Sub

    Sub Main()
        Console.Write(AClass.NestedEnum.First)
    End Sub

End Module
