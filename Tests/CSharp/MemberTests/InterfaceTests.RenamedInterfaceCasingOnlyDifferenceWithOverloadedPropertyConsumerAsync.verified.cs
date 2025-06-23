
public partial interface IUserContext
{
    string GroupID { get; }
}

public partial interface IFoo
{
    string ConnectedGroupId { get; }
}

public abstract partial class BaseFoo : IUserContext
{

    protected internal string ConnectedGroupID { get; private set; }
    string IUserContext.GroupID { get => ConnectedGroupID; }

}

public partial class Foo : BaseFoo, IFoo
{

    protected internal new string ConnectedGroupID
    {
        get
        {
            return "" ?? base.ConnectedGroupID;
        }
    }

    string IFoo.ConnectedGroupId { get => ConnectedGroupID; } // Comment moves because this line gets split

    private string Consumer()
    {
        var foo = new Foo();
        IFoo ifoo = foo;
        BaseFoo baseFoo = foo;
        IUserContext iUserContext = foo;
        return foo.ConnectedGroupID + foo.ConnectedGroupID + ifoo.ConnectedGroupId + ifoo.ConnectedGroupId + baseFoo.ConnectedGroupID + baseFoo.ConnectedGroupID + iUserContext.GroupID + iUserContext.GroupID;
    }

}
