Public Class SomeClass
    Public SomeProperty As String
    Public Shared Instance As SomeClass = New SomeClass() With { ' First line gets moved
             .SomeProperty = .SomeProperty + NameOf(.SomeProperty) ' Second line gets moved
        } ' Third line gets moved
End Class