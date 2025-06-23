Imports System

Public Module MyExtensions
    public sub NewColumn(type As Type , Optional strV1 As String = nothing, Optional code As String = "code", Optional argInt as Integer = 1)
    End sub

    public Sub CallNewColumn()
        NewColumn(GetType(MyExtensions))
        NewColumn(Nothing, , "otherCode")
        NewColumn(Nothing, "fred")
        NewColumn(Nothing, , argInt:=2)
    End Sub
End Module