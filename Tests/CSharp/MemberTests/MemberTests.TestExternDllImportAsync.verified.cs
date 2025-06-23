[DllImport("kernel32.dll", SetLastError = true)]
private static extern IntPtr OpenProcess(AccessMask dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

1 source compilation errors:
BC30002: Type 'AccessMask' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'AccessMask' could not be found (are you missing a using directive or an assembly reference?)