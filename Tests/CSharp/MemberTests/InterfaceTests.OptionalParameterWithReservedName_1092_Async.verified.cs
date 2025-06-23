
public partial class WithOptionalParameters
{
    public void S1(object a = null, string @default = "")
    {
    }

    public void S()
    {
        S1(@default: "a");
    }
}
