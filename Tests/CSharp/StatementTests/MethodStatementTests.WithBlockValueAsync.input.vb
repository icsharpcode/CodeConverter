Public Class VisualBasicClass
    Public Sub Stuff()
        Dim str As SomeStruct
        With Str
            ReDim .ArrField(1)
            ReDim .ArrProp(2)
        End With
    End Sub
End Class

Public Structure SomeStruct
    Public ArrField As String()
    Public Property ArrProp As String()
End Structure