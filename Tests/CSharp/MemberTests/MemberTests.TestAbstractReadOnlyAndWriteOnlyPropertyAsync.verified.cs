
internal abstract partial class TestClass
{
    public abstract string ReadOnlyProp { get; }
    public abstract string WriteOnlyProp { set; }
}

internal partial class ChildClass : TestClass
{

    public override string ReadOnlyProp { get; }
    public override string WriteOnlyProp
    {
        set
        {
        }
    }
}