
internal partial class RefConstArgument
{
    private const string a = "a";
    public void S()
    {
        const string b = "b";
        object args = a;
        MO(ref args);
        string args1 = b;
        MS(ref args1);
    }
    public void MO(ref object s)
    {
    }
    public void MS(ref string s)
    {
    }
}