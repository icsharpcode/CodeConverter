using System.Runtime.InteropServices;

public partial class OptionalOutIssue882
{
    private void TestSub(out int a, [Optional] out int b)
    {
        a = 42;
        b = 23;
    }

    public void CallingFunc()
    {
        int a;
        int b;
        TestSub(a: out a, b: out b);
        int argb = 0;
        TestSub(a: out a, b: out argb);
    }
}