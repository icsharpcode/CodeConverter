Public Enum TheType
    Tree
End Enum

Public Class MoreParsing
    Sub DoGet()
        Dim anon = New With {
            .TheType = GetEnumValues(Of TheType)
        }
    End Sub

    Private Function GetEnumValues(Of TEnum)() As IDictionary(Of Integer, String)
        Return System.Enum.GetValues(GetType(TEnum)).Cast(Of TEnum).
            ToDictionary(Function(enumValue) DirectCast(DirectCast(enumValue, Object), Integer),
                         Function(enumValue) enumValue.ToString())
    End Function
End Class