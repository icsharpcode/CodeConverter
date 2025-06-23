using System.Collections.Generic;
using System.Linq;

internal partial class C
{
    public string MyString { get; set; }
}

public static partial class Module1
{
    public static void Main()
    {
        var list = new List<C>();
        var result = from f in list
                     group f by f.MyString into @group
                     orderby MyString
                     select @group;
    }
}
1 target compilation errors:
CS0103: The name 'MyString' does not exist in the current context