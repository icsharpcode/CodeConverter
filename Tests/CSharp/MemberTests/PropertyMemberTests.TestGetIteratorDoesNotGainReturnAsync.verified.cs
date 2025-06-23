using System.Collections.Generic;

public partial class VisualBasicClass
{
    public static IEnumerable<object[]> SomeObjects
    {
        get
        {
            yield return new object[3];
            yield return new object[3];
        }
    }
}