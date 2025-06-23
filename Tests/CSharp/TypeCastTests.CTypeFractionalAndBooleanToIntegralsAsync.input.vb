
Enum TestEnum
    None = 1
End Enum

Class Class1
    Private Sub Test(b as Boolean, f as Single, d as Double, m as Decimal)
        Dim i = CType(b, Integer)
        i = CType(f, Integer)
        i = CType(d, Integer)
        i = CType(m, Integer)

        Dim ui = CType(b, UInteger)
        ui = CType(f, UInteger)
        ui = CType(d, UInteger)
        ui = CType(m, UInteger)

        Dim s = CType(b, Short)
        s = CType(f, Short)
        s = CType(d, Short)
        s = CType(m, Short)

        Dim l = CType(b, Long)
        l = CType(f, Long)
        l = CType(d, Long)
        l = CType(m, Long)

        Dim byt = CType(b, Byte)
        byt = CType(f, Byte)
        byt = CType(d, Byte)
        byt = CType(m, Byte)

        Dim e = CType(b, TestEnum)
        e = CType(f, TestEnum)
        e = CType(d, TestEnum)
        e = CType(m, TestEnum)
    End Sub

    Private Sub TestNullable(b as Boolean?, f as Single?, d as Double?, m as Decimal?)
        Dim i = CType(b, Integer)
        i = CType(f, Integer)
        i = CType(d, Integer)
        i = CType(m, Integer)

        Dim ui = CType(b, UInteger)
        ui = CType(f, UInteger)
        ui = CType(d, UInteger)
        ui = CType(m, UInteger)

        Dim s = CType(b, Short)
        s = CType(f, Short)
        s = CType(d, Short)
        s = CType(m, Short)

        Dim l = CType(b, Long)
        l = CType(f, Long)
        l = CType(d, Long)
        l = CType(m, Long)

        Dim byt = CType(b, Byte)
        byt = CType(f, Byte)
        byt = CType(d, Byte)
        byt = CType(m, Byte)

        Dim e = CType(b, TestEnum)
        e = CType(f, TestEnum)
        e = CType(d, TestEnum)
        e = CType(m, TestEnum)
    End Sub
End Class