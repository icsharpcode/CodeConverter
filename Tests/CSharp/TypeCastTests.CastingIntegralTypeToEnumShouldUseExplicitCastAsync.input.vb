
Enum TestEnum
    None = 0
End Enum
Enum TestEnum2
    None = 1
End Enum
Class Class1
    Private Sub TestIntegrals(b as Byte, s as Short, i as Integer, l as Long, e as TestEnum2)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
        res = CType(e, TestEnum)
    End Sub

    Private Sub TestNullableIntegrals(b as Byte?, s as Short?, i as Integer?, l as Long?, e as TestEnum2?)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
        res = CType(e, TestEnum)
    End Sub

    Private Sub TestUnsignedIntegrals(b as SByte, s as UShort, i as UInteger, l as ULong)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
    End Sub

    Private Sub TestNullableUnsignedIntegrals(b as SByte?, s as UShort?, i as UInteger?, l as ULong?)
        Dim res = CType(b, TestEnum)
        res = CType(s, TestEnum)
        res = CType(i, TestEnum)
        res = CType(l, TestEnum)
    End Sub
End Class