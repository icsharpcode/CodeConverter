Class TestClass
    Private Sub TestMethod()
        ' Declare a single-dimension array of 5 numbers.
        Dim numbers1(4) As Integer

        ' Declare a single-dimension array and set its 4 values.
        Dim numbers2 = New Integer() {1, 2, 4, 8}

        ' Declare a 6 x 6 multidimensional array.
        Dim matrix1(5, 5) As Double

        ' Declare a 4 x 3 multidimensional array and set array element values.
        Dim matrix2 = New Integer(3, 2) {{1, 2, 3}, {2, 3, 4}, {3, 4, 5}, {4, 5, 6}}

        ' Combine rank specifiers with initializers of various kinds
        Dim rankSpecifiers(,) As Double = New Double(1,1) {{1.0, 2.0}, {3.0, 4.0}}
        Dim rankSpecifiers2(,) As Double = New Double(1,1) {}

        ' Declare a jagged array
        Dim sales()() As Double = New Double(11)() {}
    End Sub
End Class