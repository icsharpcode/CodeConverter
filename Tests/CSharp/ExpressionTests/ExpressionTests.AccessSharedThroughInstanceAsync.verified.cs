
public partial class A
{
    public static int x = 2;
    public void Test()
    {
        var tmp = this;
        int y = x;
        int z = x;
    }
}