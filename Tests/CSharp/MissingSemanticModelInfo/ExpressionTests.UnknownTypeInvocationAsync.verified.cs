
internal partial class TestClass
{
    private System.SomeUnknownType DefaultDate { get; set; }
    private void TestMethod()
    {
        var a = DefaultDate(1, 2, 3).Blawer(1, 2, 3);
    }
}
2 source compilation errors:
BC30002: Type 'System.SomeUnknownType' is not defined.
BC32016: 'Private Property DefaultDate As System.SomeUnknownType' has no parameters and its return type cannot be indexed.
2 target compilation errors:
CS0234: The type or namespace name 'SomeUnknownType' does not exist in the namespace 'System' (are you missing an assembly reference?)
CS1955: Non-invocable member 'TestClass.DefaultDate' cannot be used like a method.