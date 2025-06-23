using System.Linq;

public partial class MoreParsing
{
    public void DoGet()
    {
        var anon = new { ANumber = 5 };
        var sameAnon = Identity(anon);
        var repeated = Enumerable.Repeat(anon, 5).ToList();
    }

    private TType Identity<TType>(TType tInstance)
    {
        return tInstance;
    }
}