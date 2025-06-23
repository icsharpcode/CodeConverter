
internal partial class TestClass
{
    public sealed void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}
1 source compilation errors:
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
1 target compilation errors:
CS0238: 'TestClass.TestMethod<T, T2, T3>(out T, ref T2, T3)' cannot be sealed because it is not an override