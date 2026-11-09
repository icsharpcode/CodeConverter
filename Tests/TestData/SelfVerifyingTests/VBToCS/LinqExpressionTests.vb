Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Xunit

Public Class LinqExpressionTests

    Public Class Entity
        Public Property P1 As String
        Public Property P2 As String
        Public Property Value As Decimal
        Public Sub New(p1 As String, p2 As String, value As Decimal)
            Me.P1 = p1
            Me.P2 = p2
            Me.Value = value
        End Sub
    End Class

    Private Shared ReadOnly Entities As Entity() = {
        New Entity("a", "x", 1D), New Entity("a", "y", 2D), New Entity("a", "x", 3D),
        New Entity("b", "x", 4D), New Entity("b", "y", 5D), New Entity("c", "z", 6D)
    }

    Private Shared Function Show(Of T)(items As IEnumerable(Of T)) As String
        Return String.Join("|", items)
    End Function

    Private _evaluations As Integer

    Private Function Evaluate(Of T)(value As T) As T
        _evaluations += 1
        Return value
    End Function

    <Fact>
    Sub TestWhereAfterGroup()
        Dim numbers = New List(Of Integer) From {1, 2, 3, 4, 4}
        Dim duplicates = From x In numbers
                         Group By x Into Group
                         Where Group.Count > 1
        Assert.Equal(1, duplicates.Count)
        Assert.Equal(4, duplicates.Single().x)
    End Sub

    <Fact>
    Sub TestSelectThenWhere()
        Dim data = From d In {1.2, 2.2, 3.4, 5.6}
                   Select i = CType(Math.Floor(d), Integer)
                   Where i Mod 2 = 1
        Assert.Equal("1|3|5", Show(data))
    End Sub

    <Fact>
    Sub TestSelectThenOrderBy()
        Dim fields = (From v In {DayOfWeek.Tuesday, DayOfWeek.Monday} Select Value = v, Name = [Enum].GetName(GetType(DayOfWeek), v) Order By Value Ascending).ToArray
        Assert.Equal("{ Value = Monday, Name = Monday }|{ Value = Tuesday, Name = Tuesday }", Show(fields))
    End Sub

    <Fact>
    Sub TestSelectReusingRangeVariableName()
        Dim data = From x In {1, 2, 3}
                   Select x = x * 2
                   Where x > 2
        Assert.Equal("4|6", Show(data))
    End Sub

    <Fact>
    Sub TestImplicitSelectOfAllRangeVariables()
        Dim data = From x In {1, 2}, y In {"a", "b"}
                   Let z = x.ToString() & y
                   Where z <> "1a"
        Assert.Equal("{ x = 1, y = b, z = 1b }|{ x = 2, y = a, z = 2a }|{ x = 2, y = b, z = 2b }", Show(data))
    End Sub

    <Fact>
    Sub TestDistinctAfterSelectOfMultipleVariables()
        Dim data = From x In {1, 2, 3, 4, 5, 6, 7}
                   Select a = x Mod 2, b = x Mod 3
                   Distinct
                   Where a > 0
                   Select b
        Assert.Equal("1|0|2", Show(data))
    End Sub

    <Fact>
    Sub TestTakeWhileAfterSelectOfMultipleVariables()
        Dim data = From x In {1, 2, 3}
                   Select x, y = x * 2
                   Take While y < 6
        Assert.Equal("{ x = 1, y = 2 }|{ x = 2, y = 4 }", Show(data))
    End Sub

    <Fact>
    Sub TestPartitionsThenWhere()
        Dim data = From x In {1, 2, 3, 4, 5, 6}
                   Skip 1
                   Skip While x < 3
                   Take While x < 6
                   Take 2
                   Where x > 3
        Assert.Equal("4", Show(data))
    End Sub

    <Fact>
    Sub TestJoinOnComputedKey()
        Dim data = From current In {2001, 2002, 2003}
                   Join previous In {2000, 2001} On current - 1 Equals previous
                   Select current
        Assert.Equal("2001|2002", Show(data))
    End Sub

    <Fact>
    Sub TestJoinWrittenInnerFirst()
        Dim data = From current In {2001, 2002, 2003}
                   Join previous In {2000, 2001} On previous Equals current - 1
                   Select current, previous
        Assert.Equal("{ current = 2001, previous = 2000 }|{ current = 2002, previous = 2001 }", Show(data))
    End Sub

    <Fact>
    Sub TestJoinOnMultipleConditions()
        Dim data = From e In Entities
                   Join f In Entities On e.P1 Equals f.P1 And e.P2 Equals f.P2
                   Where e.Value < f.Value
                   Select e.Value, Other = f.Value
        Assert.Equal("{ Value = 1, Other = 3 }", Show(data))
    End Sub

    <Fact>
    Sub TestNestedJoin()
        Dim data = From x In {1, 2, 3}
                   Join y In {2, 3, 4}
                       Join z In {3, 4} On z Equals y
                   On x Equals y
                   Select x, y, z
        Assert.Equal("{ x = 3, y = 3, z = 3 }", Show(data))
    End Sub

    <Fact>
    Sub TestGroupJoinIntoGroupThenFromDefaultIfEmpty()
        Dim data = From p In {"a", "b", "d"}
                   Group Join e In Entities On e.P1 Equals p Into Group
                   From e In Group.DefaultIfEmpty()
                   Order By p Descending
                   Select p & If(e Is Nothing, "-", e.Value.ToString())
                   Distinct
        Assert.Equal("d-|b4|b5|a1|a2|a3", Show(data))
    End Sub

    <Fact>
    Sub TestGroupJoinWithoutSelect()
        Dim data = (From p In {"a", "d"}
                    Group Join e In Entities On e.P1 Equals p Into Matches = Group
                    From m In Matches.DefaultIfEmpty).ToList
        Assert.Equal("a31|a32|a33|d0", Show(data.Select(Function(d) d.p & d.Matches.Count() & If(d.m Is Nothing, "", d.m.Value.ToString()))))
    End Sub

    <Fact>
    Sub TestGroupJoinAggregates()
        Dim data = From p In {"a", "b", "d"}
                   Group Join e In Entities On p Equals e.P1
                   Into Total = Sum(e.Value), Count()
                   Where Count > 0
        Assert.Equal("{ p = a, Total = 6, Count = 3 }|{ p = b, Total = 9, Count = 2 }", Show(data))
    End Sub

    <Fact>
    Sub TestGroupByMultipleKeysIntoAggregate()
        Dim queryMax = From item In Entities
                       Where item.P1 <> "c"
                       Group By item.P1, item.P2
                       Into ValueMax = Max(item.Value)
                       Select New With {
                           P1, P2, ValueMax
                       }
        Assert.Equal("{ P1 = a, P2 = x, ValueMax = 3 }|{ P1 = a, P2 = y, ValueMax = 2 }|{ P1 = b, P2 = x, ValueMax = 4 }|{ P1 = b, P2 = y, ValueMax = 5 }", Show(queryMax))
    End Sub

    <Fact>
    Sub TestGroupByAggregateUsedInWhereAndSelect()
        Dim tSummary = From t In Entities
                       Group By t.P1 Into Summary = Sum(t.Value)
                       Where Summary > 6
                       Select P1, Summary
        Assert.Equal("{ P1 = b, Summary = 9 }", Show(tSummary))
    End Sub

    <Fact>
    Sub TestOrderByGroupKey()
        Dim result = From f In Entities
                     Group f By f.P2 Into Group
                     Order By P2 Descending
        Assert.Equal("z1|y2|x3", Show(result.Select(Function(r) r.P2 & r.Group.Count())))
    End Sub

    <Fact>
    Sub TestGroupByMultipleKeysIntoNamedGroup()
        Dim result As IEnumerable(Of Decimal) = From e In Entities
                                                Group e By e.P1, e.P2 Into g1 = Group
                                                Select g1.First().Value
        Assert.Equal("1|2|4|5|6", Show(result))
    End Sub

    <Fact>
    Sub TestGroupItemsByKeyIntoAggregates()
        Dim result = From item In Entities
                     Group v = item.Value, item.P1 By k = item.P2 Into s = Sum(v), Group, Count()
                     Select k, s, Count, Firsts = String.Join("", Group.Select(Function(g) g.P1))
        Assert.Equal("{ k = x, s = 8, Count = 3, Firsts = aab }|{ k = y, s = 7, Count = 2, Firsts = ab }|{ k = z, s = 6, Count = 1, Firsts = c }", Show(result))
    End Sub

    <Fact>
    Sub TestGroupByCompoundElement()
        Dim q = From x In {1, 2}
                From y In {3, 4}
                Group By k = x Mod 2 Into Total = Sum(x * y)
        Assert.Equal("{ k = 1, Total = 7 }|{ k = 0, Total = 14 }", Show(q))
    End Sub

    <Fact>
    Sub TestGroupByWithoutGroupAggregation()
        Dim q = From x In {1, 2, 2}
                Group By x Into Count()
        Assert.Equal("{ x = 1, Count = 1 }|{ x = 2, Count = 2 }", Show(q))
    End Sub

    <Fact>
    Sub TestAggregateWithSingleAggregation()
        Dim total = Aggregate e In Entities Where e.P1 = "a" Into Sum(e.Value)
        Assert.Equal(6D, total)
    End Sub

    <Fact>
    Sub TestAggregateWithMultipleAggregationsEvaluatesSourceOnce()
        Dim agg = Aggregate x In Evaluate({1, 2, 3}) Where x > 1 Into s = Sum(x), c = Count()
        Assert.Equal("{ s = 5, c = 2 }", agg.ToString())
        Assert.Equal(1, _evaluations)
    End Sub

    <Fact>
    Sub TestAggregateClauseWithinQuery()
        Dim q = From x In {1, 2, 3}
                Aggregate y In {1, 2, 3} Where y > x Into Sum(y), Count()
                Where Count > 0
        Assert.Equal("{ x = 1, Sum = 5, Count = 2 }|{ x = 2, Sum = 3, Count = 1 }", Show(q))
    End Sub

    <Fact>
    Sub TestLetUsedTwiceIsEvaluatedOncePerElement()
        Dim q = (From x In {1, 2, 3}
                 Let y = Evaluate(x * 2)
                 Where y > 2
                 Select y).ToList()
        Assert.Equal("4|6", Show(q))
        Assert.Equal(3, _evaluations)
    End Sub

    <Fact>
    Sub TestNestedQueryReferencingOuterRangeVariable()
        Dim q = From x In {1, 2, 3}
                Select x, Smaller = (From y In {1, 2, 3} Where y < x Select y).Count()
                Where Smaller > 0
        Assert.Equal("{ x = 2, Smaller = 1 }|{ x = 3, Smaller = 2 }", Show(q))
    End Sub

    <Fact>
    Sub TestTypedFromConvertsElements()
        Dim objects = New Object() {1, 2}
        Dim q = From x As Integer In objects
                Select x + 1
        Assert.Equal("2|3", Show(q))
    End Sub

    <Fact>
    Sub TestTypedFromWithWideningConversion()
        Dim q = From x As Long In {1, 2}
                Select x * Integer.MaxValue
        Assert.Equal("2147483647|4294967294", Show(q))
    End Sub

    <Fact>
    Sub TestFromNonGenericCollection()
        Dim list As New System.Collections.ArrayList From {1, "a"}
        Dim q = From x In list
                Select s = x.ToString()
        Assert.Equal("1|a", Show(q))
    End Sub

    <Fact>
    Sub TestAggregateReusingNameOfDroppedVariable()
        Dim q = From x In {1, 2} Select y = x Aggregate x In {10, 20} Into Sum(x + y)
        Assert.Equal("{ y = 1, Sum = 32 }|{ y = 2, Sum = 34 }", Show(q))
    End Sub

    <Fact>
    Sub TestGroupJoinReusingNameOfDroppedVariable()
        Dim q = From x In {1, 2} Select y = x Group Join x In {1, 1, 2} On y Equals x Into s = Sum(x + y)
        Assert.Equal("{ y = 1, s = 4 }|{ y = 2, s = 4 }", Show(q))
    End Sub

    <Fact>
    Sub TestNestedQueryReusingNameOfDroppedVariable()
        Dim q = From x In {1, 3} Select y = x Select Inner = (From x In {1, 2, 3, 4} Skip While x < y).Count()
        Assert.Equal("4|2", Show(q))
    End Sub

    <Fact>
    Sub TestLambdaParameterReusingNameOfDroppedVariable()
        Dim ys = {2, 3}
        Dim q = From x In {1, 5} Select y = x Where ys.Any(Function(x) x > y)
        Assert.Equal("1", Show(q))
    End Sub

    <Fact>
    Sub TestLambdaParameterNamedLikeGeneratedElementParameter()
        Dim zs = {1, 2}
        Dim q = From a In {1, 2}, b In {3} Distinct Where zs.Any(Function(x) x = a)
        Assert.Equal("{ a = 1, b = 3 }|{ a = 2, b = 3 }", Show(q))
    End Sub

    <Fact>
    Sub TestQueryInLambdaWithParameterNamedLikeGeneratedName()
        Dim f = Function(x As Integer) (From a In {1, 2}, b In {3} Distinct Where a > x).Count()
        Assert.Equal(1, f(1))
    End Sub

    <Fact>
    Sub TestTypedJoinVariable()
        Dim objs = New Object() {New Entity("a", "x", 1D)}
        Dim q = From x In {"a", "b"} Join e As Entity In objs On x Equals e.P1 Select e.Value
        Assert.Equal("1", Show(q))
    End Sub

    <Fact>
    Sub TestNestedJoinWithConditionOnBothJoinedVariables()
        Dim q = From x In {2, 4, 6}
                Join y In {1, 2, 3}
                    Join z In {1, 2, 3} On z Equals y
                On x Equals y + z
                Select x, y, z
        Assert.Equal("{ x = 2, y = 1, z = 1 }|{ x = 4, y = 2, z = 2 }|{ x = 6, y = 3, z = 3 }", Show(q))
    End Sub

    <Fact>
    Sub TestJoinNestedWithinGroupJoin()
        Dim q = From x In {1, 2, 3}
                Group Join y In {1, 1, 2}
                    Join z In {1, 2, 2} On z Equals y
                On x Equals y
                Into Group, Total = Sum(y + z)
        Assert.Equal("1:2:4|2:2:8|3:0:0", Show(q.Select(Function(r) r.x & ":" & r.Group.Count() & ":" & r.Total)))
    End Sub

    <Fact>
    Sub TestGroupJoinNestedWithinJoin()
        Dim q = From x In {1, 2, 3}
                Join y In {1, 2, 3}
                    Group Join z In {1, 1, 3} On z Equals y Into c = Count()
                On x Equals y
                Where c > 0
        Assert.Equal("{ x = 1, y = 1, c = 2 }|{ x = 3, y = 3, c = 1 }", Show(q))
    End Sub

    <Fact>
    Sub TestTypedFromOverNonGenericCollectionConvertsValues()
        Dim list As New System.Collections.ArrayList From {1, "2"}
        Dim q = From x As Integer In list Select x + 1
        Assert.Equal("2|3", Show(q))
    End Sub

    <Fact>
    Sub TestValueReferencedBeforeDistinct()
        Dim q = From x In {1, 1, 2, 3}
                Let y = Evaluate(x * 2)
                Where y > 2
                Distinct
        Assert.Equal("{ x = 2, y = 4 }|{ x = 3, y = 6 }", Show(q))
        Assert.Equal(4, _evaluations)
    End Sub

    <Fact>
    Sub TestNestedJoinConditionSkippingAJoinedVariable()
        Dim q = From x In {1, 2, 3}
                Join y In {2, 3, 4}
                    Join z In {1, 2, 3} On z Equals y
                On x Equals z
                Select x, y, z
        Assert.Equal("{ x = 2, y = 2, z = 2 }|{ x = 3, y = 3, z = 3 }", Show(q))
    End Sub

    <Fact>
    Sub TestGroupByFollowedByMethodCallThenMoreClauses()
        Dim q = From x In {1, 2, 3, 4, 5}
                Group By k = x Mod 2 Into Count()
                Distinct
                Where Count > 2
        Assert.Equal("{ k = 1, Count = 3 }", Show(q))
    End Sub

    <Fact>
    Sub TestTwoGroupBys()
        Dim q = From x In {1, 2, 3, 4, 5, 6, 7}
                Group By k = x Mod 4 Into Count()
                Group By Count Into Keys = Group
        Assert.Equal("2:3|1:1", Show(q.Select(Function(g) g.Count & ":" & g.Keys.Count())))
    End Sub

    <Fact>
    Sub TestNestedJoinConditionOnlyInvalidOnceBothVariablesAreInScope()
        Dim q = From x In {2, 4}
                Join y In {1, 2}
                    Join z In {1, 2} On z Equals y
                On x Equals y + y And x Equals y + z
                Select x, y
        Assert.Equal("{ x = 2, y = 1 }|{ x = 4, y = 2 }", Show(q))
    End Sub

    <Fact>
    Sub TestDroppedVariableNotCapturedByLambdaLocal()
        Dim zs = {1}
        Dim q = (From x In {3} Select y = x Select zs.Select(Function(z)
                                                            Dim x = z * 10
                                                            Return x + y
                                                        End Function).Single()).Single()
        Assert.Equal(13, q)
    End Sub

    Private ReadOnly _field As Integer = 7

    <Fact>
    Sub TestSubstitutedMemberNotCapturedByLaterRangeVariable()
        Dim q = (From x In {3} Select v = _field, w = x Let _field = w * 5 Select v).Single()
        Assert.Equal(7, q)
    End Sub

    <Fact>
    Sub TestJoinOnCompositeKeysOfDifferentTypes()
        Dim ys = New Integer?() {1, 2}
        Dim q = From x In {2, 3} Join y In ys On x Equals y And x Equals y Select x
        Assert.Equal("2", Show(q))
    End Sub

    <Fact>
    Sub TestSubstitutedConditionalInInterpolation()
        Dim q = From x In {3, 1} Let y = If(x > 1, "a", "b") Select $"{y}"
        Assert.Equal("a|b", Show(q))
    End Sub

    <Fact>
    Sub TestSubstitutedArrayCreationIndexed()
        Dim q = From x In {3, 1} Let a = New Integer(x) {} Select a(0)
        Assert.Equal("0|0", Show(q))
    End Sub

    <Fact>
    Sub TestLetWithDeclaredType()
        Dim q = From x In {12, 3} Let s As String = x Select s.Length
        Assert.Equal("2|1", Show(q))
    End Sub

    <Fact>
    Sub TestLetNothing()
        Dim q = From x In {1} Let y = Nothing Select x, y
        Assert.Equal("{ x = 1, y =  }", Show(q))
    End Sub

    <Fact>
    Sub TestWhereNullableBoolean()
        Dim q = From b In New Boolean?() {True, Nothing, False} Where b
        Assert.Equal(1, q.Count())
    End Sub

    <Fact>
    Sub TestQueryOfOnlyFromIsNotTheSourceItself()
        Dim list As New List(Of Integer) From {1, 2}
        Dim q = From x In list
        q = q.Where(Function(y) y > 1)
        Assert.Equal("2", Show(q))
    End Sub

    Public Class Bag
        Public Function AsEnumerable() As IEnumerable(Of Integer)
            Return {1, 2}
        End Function
    End Class

    <Fact>
    Sub TestConditionalSourceWithImplicitAsEnumerable()
        Dim flag = True
        Dim q = From i In If(flag, New Bag(), Nothing) Select i
        Assert.Equal("1|2", Show(q))
    End Sub

    <Fact>
    Sub TestNestedAggregate()
        Dim xs = {1, 2, 3}
        Dim total = Aggregate x In xs Aggregate y In xs Where y > x Into c = Count() Into Sum(c)
        Assert.Equal(3, total)
    End Sub

    <Fact>
    Sub TestLetWithDeclaredDelegateType()
        Dim xs = {1, 2, 3}
        Dim q = From x In {1} Let f As Predicate(Of Integer) = Function(a) a > x Select Array.FindAll(xs, f).Length
        Assert.Equal("2", Show(q))
    End Sub

    <Fact>
    Sub TestNameOfGroupKey()
        Dim q = From x In {1, 2} Group By k = x Mod 2 Into Count() Select NameOf(k)
        Assert.Equal("k|k", Show(q))
    End Sub
    Private Shared Function BoxAll(ints As Integer()) As IEnumerable(Of Object)
        Return From x As Object In ints
    End Function

    <Fact>
    Sub TestFromAsObjectBoxes()
        Assert.Equal("1|2", Show(BoxAll({1, 2})))
    End Sub

    Private Shared Function ToNullables(ints As Integer()) As IEnumerable(Of Integer?)
        Return From x In ints Let o As Integer? = x Select o
    End Function

    <Fact>
    Sub TestLetAsNullable()
        Assert.Equal("1|2", Show(ToNullables({1, 2})))
    End Sub

    Private Function Five() As Integer
        Return 5
    End Function

    <Fact>
    Sub TestLetAddressOf()
        Dim q = From x In {1} Let f As Func(Of Integer) = AddressOf Five Where f() > 0 Select f
        Assert.Equal(5, q.Single()())
    End Sub
    <Fact>
    Sub TestPartitionCountsConvertedToInteger()
        Dim n As Long = 1
        Dim q = From x In {1, 2, 3} Skip n Take 1.5 Select x
        Assert.Equal("2|3", Show(q))
    End Sub

    <Fact>
    Sub TestFromNullableOfWiderType()
        Dim q = From x As Long? In New Integer() {1, 2} Select x
        Assert.Equal("1|2", Show(q))
    End Sub

    <Fact>
    Sub TestAggregateFunctionInDifferentCase()
        Dim total = Aggregate x In {1, 2} Into t = sum(x)
        Assert.Equal(3, total)
    End Sub

    Public Class Person
        Public Property Id As Integer
        Public Property Name As String
        Public Sub New(id As Integer, name As String)
            Me.Id = id
            Me.Name = name
        End Sub
    End Class

    <Fact>
    Sub TestGroupByKeysNamedDifferentlyFromTheirMembers()
        Dim people = {New Person(1, "a"), New Person(2, "b")}
        Dim pairs = {Tuple.Create(1, 2), Tuple.Create(1, 2), Tuple.Create(2, 1)}
        Dim q = From p In pairs
                Join agent In people On agent.Id Equals p.Item1
                Join insured In people On insured.Id Equals p.Item2
                Group By Agent_Name = agent.Name, Insured_Name = insured.Name Into Group1 = Group
                Select Agent_Name = Agent_Name, Insured_Name = Insured_Name, Count = Group1.Count()
        Assert.Equal("{ Agent_Name = a, Insured_Name = b, Count = 2 }|{ Agent_Name = b, Insured_Name = a, Count = 1 }", Show(q))
    End Sub

    Public Class BaseValue
        Public Property Key As Integer
    End Class

    Public Class DerivedValue
        Inherits BaseValue
    End Class

    Private Shared Function ToDictionary(values As IEnumerable(Of DerivedValue)) As Dictionary(Of Integer, DerivedValue)
        Return values.Distinct(EqualityComparer(Of BaseValue).Default).ToDictionary(Function(value) value.Key)
    End Function

    <Fact>
    Sub TestGenericMethodInferenceWithBaseTypeComparer()
        Dim value = New DerivedValue With {.Key = 1}
        Dim result = ToDictionary({value, value})
        Assert.Same(value, result(1))
    End Sub

End Class
