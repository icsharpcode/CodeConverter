
internal partial class TestClass
{
    public int? Bar(string value)
    {
        int result;
        if (int.TryParse(value, out result))
        {
            return result;
        }
        else
        {
            return default;
        }
    }
}