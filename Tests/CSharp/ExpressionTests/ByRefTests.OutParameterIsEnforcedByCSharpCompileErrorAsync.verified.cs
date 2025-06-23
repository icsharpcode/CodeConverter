using System;

public partial class OutParameterIsEnforcedByCSharpCompileError
{
    public static void LogAndReset(out int arg)
    {
        Console.WriteLine(arg);
    }
}
2 target compilation errors:
CS0269: Use of unassigned out parameter 'arg'
CS0177: The out parameter 'arg' must be assigned to before control leaves the current method