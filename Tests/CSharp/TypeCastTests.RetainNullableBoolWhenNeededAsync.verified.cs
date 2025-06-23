
internal partial class Class1
{
    public bool F(System.Net.IPAddress a)
    {
        return ((a?.ScopeId) is { } arg1 ? arg1 == 0 : (bool?)null) ?? true;
    }
}