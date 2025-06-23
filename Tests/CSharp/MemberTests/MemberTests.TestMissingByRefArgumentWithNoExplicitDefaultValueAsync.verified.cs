using System.Runtime.InteropServices;

internal partial class MissingByRefArgumentWithNoExplicitDefaultValue
{
    public void S()
    {
        ByRefNoDefault();
        string argstr2 = default;
        OptionalByRefNoDefault(str2: ref argstr2);
        string argstr3 = "a";
        OptionalByRefWithDefault(str3: ref argstr3);
    }

    private void ByRefNoDefault(ref string str1)
    {
    }
    private void OptionalByRefNoDefault([Optional] ref string str2)
    {
    }
    private void OptionalByRefWithDefault([Optional][DefaultParameterValue("a")] ref string str3)
    {
    }
}
3 source compilation errors:
BC30455: Argument not specified for parameter 'str1' of 'Private Sub ByRefNoDefault(ByRef str1 As String)'.
BC30455: Argument not specified for parameter 'str2' of 'Private Sub OptionalByRefNoDefault(ByRef str2 As String)'.
BC30455: Argument not specified for parameter 'str3' of 'Private Sub OptionalByRefWithDefault(ByRef str3 As String)'.
1 target compilation errors:
CS7036: There is no argument given that corresponds to the required formal parameter 'str1' of 'MissingByRefArgumentWithNoExplicitDefaultValue.ByRefNoDefault(ref string)'
