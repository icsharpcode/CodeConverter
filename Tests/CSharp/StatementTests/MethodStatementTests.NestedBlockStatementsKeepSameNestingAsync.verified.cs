
internal partial class TestClass
{
    public static int FindTextInCol(string w, int pTitleRow, int startCol, string needle)
    {

        for (int c = startCol, loopTo = w.Length; c <= loopTo; c++)
        {
            if (string.IsNullOrEmpty(needle))
            {
                if (string.IsNullOrWhiteSpace(w[c].ToString()))
                {
                    return c;
                }
            }
            else if ((w[c].ToString() ?? "") == (needle ?? ""))
            {
                return c;
            }
        }
        return -1;
    }
}