
public partial interface IFoo
{
    int FriendProp { get; set; }
    void ProtectedSub();
    int PrivateFunc();
    void ProtectedInternalSub();
    void AbstractSub();
}

public partial interface IBar
{
    int FriendProp { get; set; }
    void ProtectedSub();
    int PrivateFunc();
    void ProtectedInternalSub();
    void AbstractSub();
}

public abstract partial class BaseFoo : IFoo, IBar
{

    internal virtual int FriendProp
    {
        get
        {
            return 5;
        }
        set
        {
        }
    }

    int IFoo.FriendProp { get => FriendProp; set => FriendProp = value; }
    int IBar.FriendProp { get => FriendProp; set => FriendProp = value; } // Comment moves because this line gets split

    protected void ProtectedSub()
    {
    }

    void IFoo.ProtectedSub() => ProtectedSub();
    void IBar.ProtectedSub() => ProtectedSub();

    private int PrivateFunc()
    {
        return default;
    }

    int IFoo.PrivateFunc() => PrivateFunc();
    int IBar.PrivateFunc() => PrivateFunc();

    protected internal virtual void ProtectedInternalSub()
    {
    }

    void IFoo.ProtectedInternalSub() => ProtectedInternalSub();
    void IBar.ProtectedInternalSub() => ProtectedInternalSub();

    protected abstract void AbstractSubRenamed();
    void IFoo.AbstractSub() => AbstractSubRenamed();
    void IBar.AbstractSub() => AbstractSubRenamed();
}

public partial class Foo : BaseFoo
{

    protected internal override void ProtectedInternalSub()
    {
    }

    protected override void AbstractSubRenamed()
    {
    }
}