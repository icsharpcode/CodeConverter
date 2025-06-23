
public partial class Issue876
{
    public int SomeProperty { get; set; }
    public int SomeProperty2 { get; set; }
    public int SomeProperty3 { get; set; }

    public T InlineAssignHelper<T>(ref T lhs, T rhs)
    {
        lhs = rhs;
        return lhs;
    }

    public void Main()
    {
        int localInlineAssignHelper() { int arglhs = SomeProperty3; var ret = InlineAssignHelper(ref arglhs, 1); SomeProperty3 = arglhs; return ret; }

        int localInlineAssignHelper1() { int arglhs1 = SomeProperty2; var ret = InlineAssignHelper(ref arglhs1, localInlineAssignHelper()); SomeProperty2 = arglhs1; return ret; }

        int arglhs = SomeProperty;
        int result = InlineAssignHelper(ref arglhs, localInlineAssignHelper1());
        SomeProperty = arglhs;
    }
}