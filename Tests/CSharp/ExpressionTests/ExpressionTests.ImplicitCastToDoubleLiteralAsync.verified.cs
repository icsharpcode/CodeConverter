
internal partial class DoubleLiteral
{
    private double Test(double myDouble)
    {
        return Test(2.37d) + Test(255d); // VB: D means decimal, C#: D means double
    }
}