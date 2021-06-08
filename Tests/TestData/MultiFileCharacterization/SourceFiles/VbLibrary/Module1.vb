Imports System
Imports VbNetStandardLib

Module Module1
    Private dict As New Dictionary(Of Integer, Integer)

    Private Sub UseOutParameterInModule()
        Dim x
        dict.TryGetValue(1, x)
    End Sub

    Sub Main()
        Console.Write(AClass.NestedEnum.First)

        Dim interfaceInstance As AnInterface = New AnInterfaceImplementation
        Dim classInstance As New AnInterfaceImplementation
        Console.WriteLine(interfaceInstance.AnInterfaceProperty)
        Console.WriteLine(classInstance.APropertyWithDifferentName)

        interfaceInstance.AnInterfaceMethod()
        classInstance.AMethodWithDifferentName()
    End Sub

End Module
