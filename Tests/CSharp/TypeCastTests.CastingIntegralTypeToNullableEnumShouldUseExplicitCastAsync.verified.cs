
internal enum TestEnum
{
    None = 0
}

internal enum TestEnum2
{
    None = 1
}

internal partial class Class1
{
    private void TestIntegrals(byte b, short s, int i, long l, TestEnum2 e)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
        res = (TestEnum?)e;
    }

    private void TestNullableIntegrals(byte? b, short? s, int? i, long? l, TestEnum2? e)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
        res = (TestEnum?)e;
    }

    private void TestUnsignedIntegrals(sbyte b, ushort s, uint i, ulong l)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
    }

    private void TestNullableUnsignedIntegrals(sbyte? b, ushort? s, uint? i, ulong? l)
    {
        TestEnum? res = (TestEnum?)b;
        res = (TestEnum?)s;
        res = (TestEnum?)i;
        res = (TestEnum?)l;
    }
}
