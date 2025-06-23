using System.Collections;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        var x = new ArrayList();
        x.Add("a");

        var xs = new string[2];

        xs[0] = Conversions.ToString(x[0]);
    }
}
