
public partial interface IFoo
{
    void Save();
    int Prop { get; set; }
}

public abstract partial class BaseFoo
{
    protected virtual void OnSave()
    {
    }

    protected virtual int MyProp { get; set; } = 5;
}

public partial class Foo : BaseFoo, IFoo
{

    protected override void OnSave()
    {
    }

    void IFoo.Save() => OnSave();

    protected override int MyProp { get; set; } = 6;
    int IFoo.Prop { get => MyProp; set => MyProp = value; }

}