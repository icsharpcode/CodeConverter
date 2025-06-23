Enum MyEnum
    AMember
End Enum

Class TestClass
    Private Sub TestMethod(v as String)
        Dim b As MyEnum = MyEnum.Parse(GetType(MyEnum), v)
        b = MyEnum.Parse(GetType(MyEnum), v)
    End Sub
End Class