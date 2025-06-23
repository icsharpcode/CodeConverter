
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int MyClassDoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
    public virtual int DoFooRenamed(ref string str, int i) => MyClassDoFooRenamed(ref str, i); // Comment ends up out of order, but attached to correct method

    public int DoFooRenamedConsumer(ref string str, int i)
    {
        return MyClassDoFooRenamed(ref str, i);
    }
}