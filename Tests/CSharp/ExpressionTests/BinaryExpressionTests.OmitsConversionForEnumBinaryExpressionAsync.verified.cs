
internal enum RankEnum : sbyte
{
    First = 1,
    Second = 2
}

public partial class TestClass
{
    public void TestMethod()
    {
        var eEnum = RankEnum.Second;
        bool enumEnumEquality = eEnum == RankEnum.First;
    }
}