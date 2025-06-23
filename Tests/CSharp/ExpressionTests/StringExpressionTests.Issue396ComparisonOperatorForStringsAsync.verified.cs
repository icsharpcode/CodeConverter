using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue396ComparisonOperatorForStringsAsync
{
    private object str = 1.ToString();
    private object b;

    public Issue396ComparisonOperatorForStringsAsync()
    {
        b = Operators.ConditionalCompareObjectGreater(str, "", false);
    }
}