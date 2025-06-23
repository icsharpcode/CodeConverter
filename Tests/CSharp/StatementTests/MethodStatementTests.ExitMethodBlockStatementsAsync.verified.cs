
internal partial class TestClass
{
    private object FuncReturningNull()
    {
        int zeroLambda(object y) => default;
        return default;
    }

    private int FuncReturningZero()
    {
        object nullLambda(object y) => default;
        return default;
    }

    private int FuncReturningAssignedValue()
    {
        int FuncReturningAssignedValueRet = default;
        void aSub(object y) { return; };
        FuncReturningAssignedValueRet = 3;
        return FuncReturningAssignedValueRet;
    }
}