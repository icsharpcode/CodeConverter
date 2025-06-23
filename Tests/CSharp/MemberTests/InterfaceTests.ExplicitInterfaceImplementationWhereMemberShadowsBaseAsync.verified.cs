
public partial interface IFoo
{
    void Save();
    int Prop { get; set; }
}

public abstract partial class BaseFoo
{
    public virtual void OnSave()
    {
    }

    public virtual int MyProp { get; set; } = 5;
}

public partial class Foo : BaseFoo, IFoo
{

    public new void OnSave()
    {
    }

    void IFoo.Save() => OnSave();

    public new int MyProp { get; set; } = 6;
    int IFoo.Prop { get => MyProp; set => MyProp = value; }

}