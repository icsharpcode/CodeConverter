
public abstract partial class A
{
    public int MyClassF1(int x)
    {
        return 1;
    }

    public virtual int F1(int x) => MyClassF1(x); // Comment ends up out of order, but attached to correct method
    public abstract int F2();
    public void TestMethod()
    {
        int w = MyClassF1(1);
        int x = F1(2);
        int y = F2();
        int z = F2();
    }
}
1 source compilation errors:
BC30614: 'MustOverride' method 'Public MustOverride Function F2() As Integer' cannot be called with 'MyClass'.