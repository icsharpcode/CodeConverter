using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public void M(string[] OldWords, string[] NewWords, string HTMLCode)
    {
        for (int i = 0, loopTo = Conversions.ToInteger(i < OldWords.Length - 1); i <= loopTo; i++)
            HTMLCode = HTMLCode.Replace(OldWords[i], NewWords[i]);
    }
}