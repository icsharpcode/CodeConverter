using System.Collections.Generic;
using System.Diagnostics;

public partial class ClassWithProperties
{
    public string Property1 { get; set; }
}

public partial class VisualBasicClass
{
    public VisualBasicClass()
    {
        var x = new Dictionary<string, string>();
        var y = new ClassWithProperties();

        bool localTryGetValue() { string argvalue = y.Property1; var ret = x.TryGetValue("x", out argvalue); y.Property1 = argvalue; return ret; }

        if (localTryGetValue())
        {
            Debug.Print(y.Property1);
        }
    }
}