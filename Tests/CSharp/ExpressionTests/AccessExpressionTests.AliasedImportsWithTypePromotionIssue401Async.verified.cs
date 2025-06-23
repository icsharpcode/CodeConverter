using System;
using System.IO;
using SIO = System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using VB = Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Test
{
    private string aliased = VB.Strings.Left("SomeText", 1);
    private string aliasedAgain = VB.Strings.Left("SomeText", 1);
    private Delegate aliased2 = new SIO.ErrorEventHandler(OnError);

    // Make use of the non-aliased imports, but ensure there's a name clash that requires the aliases in the above case
    private string Tr = nameof(TextReader);
    private string Strings = nameof(AppWinStyle);

    public partial class ErrorEventHandler
    {
    }

    public static void OnError(object s, ErrorEventArgs e)
    {
    }
}
1 target compilation errors:
CS8082: Sub-expression cannot be used in an argument to nameof.