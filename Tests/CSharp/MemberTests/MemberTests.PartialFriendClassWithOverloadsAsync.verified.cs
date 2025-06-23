
internal abstract partial class TestClass1
{
    public static void CreateStatic()
    {
    }

    public void CreateInstance()
    {
    }

    public void CreateInstance(object o)
    {
    }

    public abstract void CreateAbstractInstance();

    public virtual void CreateVirtualInstance()
    {
    }
}

internal partial class TestClass2 : TestClass1
{
    public new static void CreateStatic()
    {
    }

    public new void CreateInstance()
    {
    }

    public override void CreateAbstractInstance()
    {
    }

    public void CreateVirtualInstance(object o)
    {
    }
}