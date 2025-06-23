Class DoubleLiteral
    Private Function Test(myDouble As Double) As Double
        Return Test(2.37D) + Test(&HFFUL) 'VB: D means decimal, C#: D means double
    End Function
End Class