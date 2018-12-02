Imports System
Imports System.Linq
Imports NUnit.Framework

Module Program

    Public Class ExpressionTests

        <Test>
        Public Sub TestFloatingPointDivision()
            Dim x = 7 / 2
            Assert.That(x, [Is].EqualTo(3.5))
        End Sub

        <Test>
        Public Sub TestIntegerDivision()
            Dim x = 7 \ 2
            Assert.That(x, [Is].EqualTo(3))
        End Sub

        ' Message: Error compiling target: CodeConverter.Tests.Compilation.CompilationException: Compilation failed:
        ' (29,21) Error CS0019 : Operator '/' cannot be applied to operands of type 'decimal' and 'double'
        ' https://github.com/icsharpcode/CodeConverter/issues/202
        '<Fact>
        'Public Sub TestDecimalDivision()
        '   Dim x = 7D / 2D
        '   Assert.Equal(x, 3.5D)
        'End Sub

    End Class

End Module
