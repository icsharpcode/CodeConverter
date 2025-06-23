using System;
using System.Collections.Generic;
using System.Linq;

public partial class Issue895
{
    private static void LinqWithGroup()
    {
        var numbers = new List<int>() { 1, 2, 3, 4, 4 };
        var duplicates = from x in numbers
                         group x by x into Group
                         where Group.Count() > 1
                         select Group;
        Console.WriteLine(duplicates.Count());
    }
}