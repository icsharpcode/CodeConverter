Public Class WithRefReturnStructure
    Sub UseArr()
        Dim arr() As SomeStruct
        Dim s As String

        With arr(0)
            .P = s
            s = .P
        End With
    End Sub

    Sub UseRefReturn()
        Dim lst As CSharpRefReturn.RefReturnList(Of SomeStruct)
        Dim s As String

        With lst(0)
            .P = s
            s = .P
        End With

        With lst.RefProperty
            .P = s
            s = .P
        End With
    End Sub

    Structure SomeStruct
        Public Property P As String
    End Structure
End Class
