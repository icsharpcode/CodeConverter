
public partial class GenericComparison
{
    public void m<T>(T p)
    {
        if (p is null)
            return;
    }
}