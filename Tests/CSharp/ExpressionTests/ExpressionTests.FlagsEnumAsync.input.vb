<Flags()> Public Enum FilePermissions As Integer
    None = 0
    Create = 1
    Read = 2
    Update = 4
    Delete = 8
End Enum
Public Class MyTest
    Public MyEnum As FilePermissions = FilePermissions.None + FilePermissions.Create
End Class