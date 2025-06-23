
public partial class Class1
{
    public void Foo()
    {
        Bar(default);
    }

    private SomeClass Bar(SomeClass x)
    {
        return x;
    }

}
1 source compilation errors:
BC30002: Type 'SomeClass' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'SomeClass' could not be found (are you missing a using directive or an assembly reference?)