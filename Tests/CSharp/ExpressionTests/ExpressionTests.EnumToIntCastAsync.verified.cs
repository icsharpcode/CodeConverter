
public partial class MyTest
{
    public enum TestEnum : int
    {
        Test1 = 0,
        Test2 = 1
    }

    public void Main()
    {
        var EnumVariable = TestEnum.Test1;
        int t1 = (int)EnumVariable;
    }
}
