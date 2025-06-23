
public abstract partial class A
{
    public int MyClassP1 { get; set; } = 1;

    public virtual int P1
    {
        get
        {
            return MyClassP1;
        }

        set
        {
            MyClassP1 = value;
        }
    }
    public abstract int P2 { get; set; }
    public void TestMethod()
    {
        int w = MyClassP1;
        int x = P1;
        int y = P2;
        int z = P2;
    }
}
1 source compilation errors:
BC30614: 'MustOverride' method 'Public MustOverride Property P2 As Integer' cannot be called with 'MyClass'.