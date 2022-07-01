Imports System
Imports System.Linq
Imports Xunit

Public Class VariableScopeTests

    <Fact>
    Sub TestDeclarationInsideForLoop()
        For i = 1 To 2
            Dim b As Boolean
            If i = 1 Then
                Assert.False(b)
            Else
                Assert.True(b)
            End If

            b = True
            Assert.True(b)
        Next
    End Sub

    <Fact>
    Sub TestDeclarationsInsideNestedLoops()
        Dim results As New List(Of String)
        Dim i = 1
        Do
            Dim b As Integer
            b += 1
            results.Add("b=" & b)
            For j = 1 To 3
                Dim c As Integer
                c += 1
                results.Add("c1=" & c)
            Next
            For j = 1 To 3
                Dim c As Integer
                c += 1
                results.Add("c2=" & c)
            Next
            Dim k = 1
            Do While k <= 3
                Dim c As Integer
                c += 1
                results.Add("c3=" & c)
                k += 1
            Loop
            i += 1
        Loop Until i > 3
        Assert.Equal(
            "b=1, c1=1, c1=2, c1=3, c2=1, c2=2, c2=3, c3=1, c3=2, c3=3, " &
            "b=2, c1=4, c1=5, c1=6, c2=4, c2=5, c2=6, c3=4, c3=5, c3=6, " &
            "b=3, c1=7, c1=8, c1=9, c2=7, c2=8, c2=9, c3=7, c3=8, c3=9",
            String.Join(", ", results))
    End Sub

    <Fact>
    Sub TestMultipleVariablesDefinedInOneDeclarationStatement()
        For i = 1 To 2
            Dim a = True, b As Boolean, c As Integer? = Nothing, d = 4, e As New Integer(), f As Integer?
            a = Not a
            b = Not b
            c = If(Not c.HasValue, 0, c + 1)
            d += 1
            e += 1
            f = If(Not f.HasValue, 0, f + 1)

            If i = 2 Then
                Assert.Equal(False, a)
                Assert.Equal(False, b)
                Assert.Equal(0, c)
                Assert.Equal(5, d)
                Assert.Equal(1, e)
                Assert.Equal(1, f)
            End If
        Next
    End Sub


End Class
