Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Imports Xunit
Imports <xmlns="http://example.com/default">
Imports <xmlns:t="http://example.com/test">

Public Class XmlAttributeAxisTests
    <Fact>
    Sub SingleElementAttributeIsAString()
        Dim element = New XElement("item", New XAttribute("id", "first & ""quoted"""))
        Dim value = element.@id
        Assert.IsType(Of String)(value)
        Assert.Equal("first & ""quoted""", value)
        Assert.Equal("FIRST & ""QUOTED""", value.ToUpperInvariant())
    End Sub

    <Fact>
    Sub MissingAndEmptyAttributesAreDistinct()
        Dim element = <item id=""/>
        Assert.Equal("", element.@id)
        Assert.Null(element.@missing)
    End Sub

    <Fact>
    Sub SequenceReadsOnlyTheFirstElement()
        Dim elements As IEnumerable(Of XElement) = {<item id="first"/>, <item id="second"/>}
        Assert.Equal("first", elements.@id)
    End Sub

    <Fact>
    Sub SequenceDoesNotSearchLaterElementsForAttribute()
        Dim elements As IEnumerable(Of XElement) = {<item/>, <item id="second"/>}
        Assert.Null(elements.@id)
    End Sub

    <Fact>
    Sub EmptySequenceReturnsNothing()
        Dim elements As IEnumerable(Of XElement) = Enumerable.Empty(Of XElement)()
        Assert.Null(elements.@id)
    End Sub

    <Fact>
    Sub NullFirstElementThrows()
        Dim elements As IEnumerable(Of XElement) = {Nothing, <item id="second"/>}
        Assert.Throws(Of NullReferenceException)(Function() elements.@id)
    End Sub

    <Fact>
    Sub NullReceiversThrow()
        Dim element As XElement = Nothing
        Dim elements As IEnumerable(Of XElement) = Nothing
        Assert.Throws(Of NullReferenceException)(Function() element.@id)
        Assert.Throws(Of NullReferenceException)(Function() elements.@id)
    End Sub

    <Fact>
    Sub AttributeNamespacesAreIndependentOfDefaultElementNamespace()
        Dim element = <item id="plain" t:id="qualified" t:number-type="integer" xml:lang="en"/>
        Assert.Equal("plain", element.@id)
        Assert.Equal("qualified", element.@t:id)
        Assert.Equal("en", element.@xml:lang)
        Assert.Equal("plain", element.@<id>)
        Assert.Equal("qualified", element.@<t:id>)
        Assert.Equal("qualified", {element}.@t:id)
        Assert.Equal("integer", element.@<t:number-type>)
    End Sub

    <Fact>
    Sub ConditionalAttributeAccess()
        Dim element As XElement = Nothing
        Dim elements As IEnumerable(Of XElement) = Nothing
        Assert.Null(element?.@id)
        Assert.Null(elements?.@id)
        element = <item id="present"/>
        elements = {element}
        Assert.Equal("present", element?.@id)
        Assert.Equal("present", elements?.@id)
    End Sub

    <Fact>
    Sub AttributeAccessInWithBlock()
        With <item id="present"/>
            Assert.Equal("present", .@id)
        End With
    End Sub

    <Fact>
    Sub AttributeAccessAfterConditionalChildAxis()
        Dim root = <root><item id="first"/><item id="second"/></root>
        Assert.Equal("first", root?.<item>.@id)
        root = Nothing
        Assert.Null(root?.<item>.@id)
    End Sub

    <Fact>
    Sub AttributeAccessAfterConditionalIndex()
        Dim elements As XElement() = {<item id="first"/>}
        Assert.Equal("first", elements?(0).@id)
        elements = Nothing
        Assert.Null(elements?(0).@id)
    End Sub

    <Fact>
    Sub WithAttributeInsideConditionalLambda()
        Dim values As IEnumerable(Of Integer) = {1}
        With <item id="present"/>
            Assert.Equal("present", values?.Select(Function(i) .@id).Single())
        End With
    End Sub

    <Fact>
    Sub QueryableReceiverUsesEnumerableSemantics()
        Dim elements As IQueryable(Of XElement) = {<item id="first"/>}.AsQueryable()
        Assert.Equal("first", elements.@id)
        Assert.Equal("first", elements?.@id)
    End Sub

    <Fact>
    Sub MissingAttributeDoesNotShortCircuitFollowingMemberAccess()
        Dim element = <item/>
        Assert.Throws(Of NullReferenceException)(Function() element.@id.Length)
        Assert.Throws(Of NullReferenceException)(Function() element?.@id.Length)
        element = Nothing
        Assert.Null(element?.@id.Length)
    End Sub

    <Fact>
    Sub ConditionalAttributeFollowedByVoidMethod()
        Dim element = <item id="abc"/>
        Dim chars(2) As Char
        element?.@id.CopyTo(0, chars, 0, 3)
        element?.@id.ToUpperInvariant()
        Assert.Equal("abc", New String(chars))
        element = Nothing
        element?.@id.CopyTo(0, chars, 0, 3)
        Assert.Equal("abc", New String(chars))
    End Sub

    <Fact>
    Sub ConditionalIntermediateSequenceAndNestedConditionalAccess()
        Dim holder = New ElementHolder()
        Assert.Throws(Of NullReferenceException)(Function() holder?.Items.@id)
        Assert.Null(holder?.Items?.@id)
        holder.Items = {<item id="present"/>}
        Assert.Equal("present", holder?.Items.@id)
        Assert.Equal("present", holder?.Items?.@id)
        holder = Nothing
        Assert.Null(holder?.Items.@id)
        Assert.Null(holder?.Items?.@id)
    End Sub

    <Fact>
    Sub SequenceIgnoresCustomAsEnumerableMethod()
        Dim elements = New ElementList() From {<item id="first"/>}
        Assert.Equal("first", elements.@id)
        Assert.Equal("first", elements?.@id)
    End Sub

    <Fact>
    Sub ElementIgnoresShadowedAttributeMethod()
        Dim element = New DerivedElement()
        Assert.Equal("first", element.@id)
        Assert.Equal("first", element?.@id)
        Dim elements As IEnumerable(Of DerivedElement) = {element}
        Assert.Equal("first", elements.@id)
    End Sub

    <Fact>
    Sub ReceiverIsEvaluatedOnce()
        Dim calls = 0
        Dim getElement As Func(Of XElement) = Function()
                                                 calls += 1
                                                 Return <item id="present"/>
                                             End Function
        Assert.Equal("present", getElement().@id)
        Assert.Equal(1, calls)
        Assert.Equal("present", getElement()?.@id)
        Assert.Equal(2, calls)
        Dim getElements As Func(Of IEnumerable(Of XElement)) = Function()
                                                                 calls += 1
                                                                 Return {<item id="present"/>}
                                                             End Function
        Assert.Equal("present", getElements().@id)
        Assert.Equal(3, calls)
        Assert.Equal("present", getElements()?.@id)
        Assert.Equal(4, calls)
    End Sub

    <Fact>
    Sub SequenceDoesNotAdvancePastFirstElement()
        Assert.Equal("first", FirstElementOnly().@id)
    End Sub

    Private Iterator Function FirstElementOnly() As IEnumerable(Of XElement)
        Yield <item id="first"/>
        Throw New InvalidOperationException("The second element must not be read.")
    End Function

    Private Class ElementHolder
        Public Property Items As IEnumerable(Of XElement)
    End Class

    Private Class ElementList
        Inherits List(Of XElement)

        Public Function AsEnumerable() As IEnumerable(Of XElement)
            Throw New InvalidOperationException("Attribute access must enumerate the receiver directly.")
        End Function
    End Class

    Private Class DerivedElement
        Inherits XElement

        Public Sub New()
            MyBase.New("item", New XAttribute("id", "first"))
        End Sub

        Public Shadows Function Attribute(name As XName) As XAttribute
            Throw New InvalidOperationException("Attribute access must use XElement.Attribute.")
        End Function
    End Class
End Class
