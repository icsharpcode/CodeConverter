
internal partial class CastTest
{
    private int? Test(object input)
    {
        return (int?)input;
    }
    private decimal? Test2(object input)
    {
        return (decimal?)(double?)input;
    }
    private decimal? Test2(int input)
    {
        return (decimal?)(double?)input;
    }
}