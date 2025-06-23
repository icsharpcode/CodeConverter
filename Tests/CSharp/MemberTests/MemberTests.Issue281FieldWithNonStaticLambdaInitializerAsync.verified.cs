using System;
using System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Issue281
{
    private Delegate lambda = new ErrorEventHandler((a, b) => Strings.Len(0));
    private Delegate nonShared;

    public Issue281()
    {
        nonShared = new ErrorEventHandler(OnError);
    }

    public void OnError(object s, ErrorEventArgs e)
    {
    }
}