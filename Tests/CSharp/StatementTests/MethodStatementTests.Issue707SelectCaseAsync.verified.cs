using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Issue707SelectCaseAsyncClass
{
    private bool? Exists(char? sort)
    {
        switch (Strings.LCase(Conversions.ToString(sort.Value) + "") ?? "")
        {
            case var @case when @case == "":
            case var case1 when case1 == "":
                {
                    return false;
                }

            default:
                {
                    return true;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code