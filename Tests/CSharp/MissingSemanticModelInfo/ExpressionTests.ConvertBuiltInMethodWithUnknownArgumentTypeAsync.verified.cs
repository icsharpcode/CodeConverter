
internal partial class A
{
    public void Test()
    {
        SomeUnknownType x = default;
        int y = 3;
        if (x == null || y == null)
        {

        }
    }
}
1 source compilation errors:
BC30002: Type 'SomeUnknownType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'SomeUnknownType' could not be found (are you missing a using directive or an assembly reference?)