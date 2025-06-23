using System;

[Flags()]
public enum FilePermissions : int
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8
}

public partial class MyTest
{
    public FilePermissions MyEnum = (FilePermissions)((int)FilePermissions.None + (int)FilePermissions.Create);
}