
public partial interface IFoo
{
    void Save();
    int A { get; set; }
}

public partial interface IBar
{
    void OnSave();
    int B { get; set; }
}

public partial class Foo : IFoo, IBar
{

    public virtual void Save()
    {
    }

    void IFoo.Save() => Save();
    void IBar.OnSave() => Save();

    public virtual int A { get; set; }
    int IFoo.A { get => A; set => A = value; }
    int IBar.B { get => A; set => A = value; }

}