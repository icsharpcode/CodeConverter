using System.Diagnostics;

public partial class C
{
    public void S()
    {
        int i = 0;
        Modify(ref i);
        Debug.Assert(i == 1);
        int argi = i;
        Modify(ref argi);
        Debug.Assert(i == 1);
    }

    public void Modify(ref int i)
    {
        i = i + 1;
    }
}