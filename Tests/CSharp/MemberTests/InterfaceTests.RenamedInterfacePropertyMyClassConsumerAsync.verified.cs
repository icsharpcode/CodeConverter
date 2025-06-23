using System;

public partial interface IFoo
{
    int DoFoo { get; }
    int DoBar { set; }
}

public partial class Foo : IFoo
{

    public int MyClassDoFooRenamed
    {
        get
        {
            return 4;
        }
    }

    int IFoo.DoFoo { get => DoFooRenamed; }

    public virtual int DoFooRenamed  // Comment ends up out of order, but attached to correct method
    {
        get
        {
            return MyClassDoFooRenamed;
        }
    }

    public int MyClassDoBarRenamed
    {
        set
        {
            throw new Exception();
        }
    }

    int IFoo.DoBar { set => DoBarRenamed = value; }

    public virtual int DoBarRenamed  // Comment ends up out of order, but attached to correct method
    {
        set
        {
            MyClassDoBarRenamed = value;
        }
    }

    public void DoFooRenamedConsumer()
    {
        MyClassDoBarRenamed = MyClassDoFooRenamed;
    }
}