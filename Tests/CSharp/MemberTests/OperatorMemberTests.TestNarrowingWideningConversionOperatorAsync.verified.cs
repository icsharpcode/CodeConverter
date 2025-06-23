
public partial class MyInt
{
    public static explicit operator MyInt(int i)
    {
        return new MyInt();
    }
    public static implicit operator int(MyInt myInt)
    {
        return 1;
    }
}