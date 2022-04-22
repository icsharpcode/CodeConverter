Imports System
Imports System.Linq
Imports Xunit

Public Class NullableRelationalOperatorsTests

    Private Property CallsA As Integer
    Private _a As Integer?
    Private Property A As Integer?
        Get
            CallsA += 1
            Return _a
        End Get
        Set
            _a = Value
        End Set
    End Property

    Private Property CallsB As Integer
    Private _b As Integer?
    Private Property B As Integer?
        Get
            CallsB += 1
            Return _b
        End Get
        Set
            _b = Value
        End Set
    End Property

    Private Property CallsIntProp As Integer
    Private _intProp As Integer
    Private Property IntProp As Integer
        Get
            CallsIntProp += 1
            Return _intProp
        End Get
        Set
            _intProp = Value
        End Set
    End Property

    <Theory>
    <InlineData(Nothing, Nothing, Nothing, 21505)>
    <InlineData(Nothing, 1, Nothing, 21505)>
    <InlineData(1, Nothing, Nothing, 21505)>
    <InlineData(1, 1, True, 5187)>
    <InlineData(1, 0, False, 21505)>
    <InlineData(0, 1, False, 21505)>
    Public Sub CallCheckOnNullableComparison(a As Integer?, b As Integer?, result As Boolean?, expectedCheck as Integer)
        Dim check As Integer = 1
        Me.A = a
        Me.B = b
        
        Dim actual1 = a = b
        Dim actual2 = Me.A = Me.B
        Dim actual3 = a = Me.B
        Dim actual4 = Me.A = b

        If a = b Then check *= 3 Else check *= 5
        If Me.A = Me.B Then check *= 7 Else check *= 11
        If a = Me.B Then check *= 13 Else check *= 17
        If Me.A = b Then check *= 19 Else check *= 23

        Assert.Equal(result, actual1)
        Assert.Equal(result, actual2)
        Assert.Equal(result, actual3)
        Assert.Equal(result, actual4)

        Assert.Equal(expectedCheck, check)
        Assert.Equal(4, Me.CallsA)
        Assert.Equal(4, Me.CallsB)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, Nothing, 21505)>
    <InlineData(1, 1, True, 5187)>
    <InlineData(1, 0, False, 21505)>
    <InlineData(0, 1, False, 21505)>
    Public Sub CallCheckOnNullableAndNormalComparison(a As Integer?, b As Integer, result As Boolean?, expectedCheck as Integer)
        Dim check As Integer = 1
        Me.A = a
        Me.IntProp = b
        
        Dim actual1 = a = b
        Dim actual2 = Me.A = b
        Dim actual3 = a = Me.IntProp
        Dim actual4 = Me.A = Me.IntProp

        If a = b Then check *= 3 Else check *= 5
        If Me.A = b Then check *= 7 Else check *= 11
        If a = Me.IntProp Then check *= 13 Else check *= 17
        If Me.A = Me.IntProp Then check *= 19 Else check *= 23

        Assert.Equal(result, actual1)
        Assert.Equal(result, actual2)
        Assert.Equal(result, actual3)
        Assert.Equal(result, actual4)

        Assert.Equal(expectedCheck, check)
        Assert.Equal(4, Me.CallsA)
        Assert.Equal(4, Me.CallsIntProp)
    End Sub

    <Theory>
    <InlineData(1, Nothing, Nothing, 21505)>
    <InlineData(1, 1, True, 5187)>
    <InlineData(1, 0, False, 21505)>
    <InlineData(0, 1, False, 21505)>
    Public Sub CallCheckOnNormalAndNullableComparison(a As Integer, b As Integer?, result As Boolean?, expectedCheck as Integer)
        Dim check As Integer = 1
        Me.IntProp = a
        Me.B = b
        
        Dim actual1 = a = b
        Dim actual2 = Me.IntProp = b
        Dim actual3 = a = Me.B
        Dim actual4 = Me.IntProp = Me.B

        If a = b Then check *= 3 Else check *= 5
        If Me.IntProp = b Then check *= 7 Else check *= 11
        If a = Me.B Then check *= 13 Else check *= 17
        If Me.IntProp = Me.B Then check *= 19 Else check *= 23

        Assert.Equal(result, actual1)
        Assert.Equal(result, actual2)
        Assert.Equal(result, actual3)
        Assert.Equal(result, actual4)

        Assert.Equal(expectedCheck, check)
        Assert.Equal(4, Me.CallsB)
        Assert.Equal(4, Me.CallsIntProp)
    End Sub

    <Fact>
    Public Sub TestNullableComparisonWithReferenceType()
        Dim x As New ReferenceType
        Dim nullableInt As Integer? = 5
        Dim isEqual = x = nullableInt
        Dim isEqual2 = nullableInt = x
        Dim isNotEqual = x <> nullableInt
        Dim isNotEqual2 = nullableInt <> x
        
        Dim check = isEqual AndAlso isEqual2 AndAlso Not isNotEqual AndAlso Not isNotEqual2

        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableComparisonWithValueType()
        Dim x As New ValueType
        Dim nullableInt As Integer? = 5
        Dim isEqual = x = nullableInt
        Dim isEqual2 = nullableInt = x
        Dim isNotEqual = x <> nullableInt
        Dim isNotEqual2 = nullableInt <> x
        
        Dim check = isEqual AndAlso isEqual2 AndAlso Not isNotEqual AndAlso Not isNotEqual2

        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableComparisonWithNullableValueType()
        Dim x As New ValueType?
        Dim y As New ValueType?
        Dim isEqual = x = y
        Dim isEqual2 = y = x
        Dim isNotEqual = x <> y
        Dim isNotEqual2 = y <> x
        
        Dim check = isEqual Is Nothing AndAlso isEqual2 Is Nothing AndAlso isNotEqual Is Nothing AndAlso isNotEqual2 Is Nothing AndAlso 
                    Not isEqual.HasValue AndAlso Not isEqual2.HasValue AndAlso Not isNotEqual.HasValue AndAlso Not isNotEqual2.HasValue 

        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableEqualityProducesNullableBoolean()
        Dim x As Integer? = Nothing
        Dim isEqual = x = 5
        Dim isEqual2 = 5 = x
        Dim isNotEqual = x <> 5
        Dim isNotEqual2 = 5 <> x
        
        Dim check = isEqual Is Nothing AndAlso isEqual2 Is Nothing AndAlso
                    isNotEqual Is Nothing AndAlso isNotEqual2 Is Nothing AndAlso
                    Not isEqual.HasValue AndAlso Not isEqual2.HasValue AndAlso Not isNotEqual.HasValue AndAlso Not isNotEqual2.HasValue

        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableWithLessOrGreaterProducesNullableBoolean()
        Dim x As Integer? = Nothing
        Dim isLess = x < 5
        Dim isLess2 = 5 < x
        Dim isGreater = x > 5
        Dim isGreater2 = 5 > x
        
        Dim check = isLess Is Nothing AndAlso isLess2 Is Nothing AndAlso
                    isGreater Is Nothing AndAlso isGreater2 Is Nothing AndAlso
                    Not isLess.HasValue AndAlso Not isLess2.HasValue AndAlso Not isGreater.HasValue AndAlso Not isGreater2.HasValue

        Assert.True(check)
    End Sub
    
    
    <Fact>
    Public Sub TestNullableWithLessGreaterOrEqualProducesNullableBoolean()
        Dim x As Integer? = Nothing
        Dim isLess = x <= 5
        Dim isLess2 = 5 <= x
        Dim isGreater = x >= 5
        Dim isGreater2 = 5 >= x
        
        Dim check = isLess Is Nothing AndAlso isLess2 Is Nothing AndAlso
                    isGreater Is Nothing AndAlso isGreater2 Is Nothing AndAlso
                    Not isLess.HasValue AndAlso Not isLess2.HasValue AndAlso Not isGreater.HasValue AndAlso Not isGreater2.HasValue

        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableEqualityProducesTrueOrFalse()
        Dim x As Integer? = 5
        Dim isEqual = x = 5
        Dim isEqual2 = 5 = x
        Dim isNotEqual = x <> 5
        Dim isNotEqual2 = 5 <> x
        
        Dim check = isEqual AndAlso isEqual2 AndAlso Not isNotEqual AndAlso Not isNotEqual2 AndAlso
                    isEqual.HasValue AndAlso isEqual2.HasValue AndAlso isNotEqual.HasValue AndAlso isNotEqual2.HasValue
        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableWithLessOrGreaterProducesTrueOrFalse()
        Dim x As Integer? = 5
        Dim isLess = x < 6
        Dim isLess2 = 6 < x
        Dim isGreater = x > 6
        Dim isGreater2 = 6 > x
        
        Dim check = isLess AndAlso Not isLess2 AndAlso Not isGreater AndAlso isGreater2 AndAlso
                    isLess.HasValue AndAlso isLess2.HasValue AndAlso isGreater.HasValue AndAlso isGreater2.HasValue
        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableWithLessGreaterOrEqualProducesTrueOrFalse()
        Dim x As Integer? = 5
        Dim isLess = x <= 5
        Dim isLess2 = 5 <= x
        Dim isGreater = x >= 5
        Dim isGreater2 = 5 >= x
        
        Dim check = isLess AndAlso isLess2 AndAlso isGreater AndAlso isGreater2 AndAlso
                    isLess.HasValue AndAlso isLess2.HasValue AndAlso isGreater.HasValue AndAlso isGreater2.HasValue
        Assert.True(check)
    End Sub

    <Fact>
    Public Sub TestNullableInCondition()
        Assert.True(NullableInIfCondition(Nothing))
        Assert.True(NullableInConditionalOperator(Nothing))
        Assert.True(NullableInConditionalOperator(Nothing))
    End Sub

    Private Function NullableInIfCondition(x As Integer?) As Boolean
        If x <> 10 AndAlso x >= 10 Then Return False
        Return True
    End Function

    Private Function NullableInConditionalOperator(x As Integer?) As Boolean
        Return If(x <> 10 AndAlso x <= 10, False, True)
    End Function

    Private Function ComplexStatementInConditionalOperator(x As Integer?) As Boolean
        Return If((x <> 10 OrElse x > 5 And x = x) AndAlso (x <= 10 AndAlso (x > 0 OrElse x < 0)), False, True)
    End Function

    Public Class ReferenceType
        Public Shared Operator =(left as ReferenceType, right as Integer?) as Boolean
            Return True
        End Operator

        Public Shared Operator <>(left as ReferenceType, right as Integer?) as Boolean
            Return False
        End Operator

        Public Shared Operator =(left as Integer?, right as ReferenceType) as Boolean
            Return True
        End Operator

        Public Shared Operator <>(left as Integer?, right as ReferenceType) as Boolean
            Return False
        End Operator
    End Class

    Public Structure ValueType
        Public Shared Operator =(left as ValueType, right as ValueType) as Boolean
            Return True
        End Operator

        Public Shared Operator <>(left as ValueType, right as ValueType) as Boolean
            Return False
        End Operator

        Public Shared Operator =(left as ValueType, right as Integer?) as Boolean
            Return True
        End Operator

        Public Shared Operator <>(left as ValueType, right as Integer?) as Boolean
            Return False
        End Operator

        Public Shared Operator =(left as Integer?, right as ValueType) as Boolean
            Return True
        End Operator

        Public Shared Operator <>(left as Integer?, right as ValueType) as Boolean
            Return False
        End Operator
    End Structure
End Class
