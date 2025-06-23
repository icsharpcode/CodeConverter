
internal partial class TestClass
{
    public int TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        return 0;
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method