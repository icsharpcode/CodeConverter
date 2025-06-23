
internal partial class TestClass
{
    public System.Some.UnknownType SomeProperty { get; set; }
    private void TestMethod()
    {
        int num = 0;
        var value = SomeProperty[num];
        value = SomeProperty[0];
    }
}
2 source compilation errors:
BC30002: Type 'System.Some.UnknownType' is not defined.
BC32016: 'Public Property SomeProperty As System.Some.UnknownType' has no parameters and its return type cannot be indexed.
1 target compilation errors:
CS0234: The type or namespace name 'Some' does not exist in the namespace 'System' (are you missing an assembly reference?)