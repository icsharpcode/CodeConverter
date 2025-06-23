using System;

public partial class MyType
{
    public static MyType operator ^(MyType left, MyType right)
    {
        throw new NotSupportedException("Not supported");
    }
}