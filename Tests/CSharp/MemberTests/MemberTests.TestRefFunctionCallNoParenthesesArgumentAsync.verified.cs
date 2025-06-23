
internal partial class RefFunctionCallArgument
{
    public void S(ref object o)
    {
        object argo = GetI();
        S(ref argo);
    }
    public int GetI()
    {
        return default;
    }
}