using System;

public partial class EnumTests
{
    private enum RankEnum : sbyte
    {
        First = 1,
        Second = 2
    }

    public void TestEnumConcat()
    {
        Console.Write(RankEnum.First + RankEnum.Second);
    }
}
1 target compilation errors:
CS0019: Operator '+' cannot be applied to operands of type 'EnumTests.RankEnum' and 'EnumTests.RankEnum'