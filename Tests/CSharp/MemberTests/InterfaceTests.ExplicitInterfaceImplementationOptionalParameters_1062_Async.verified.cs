
public partial interface InterfaceWithOptionalParameters
{
    void S(int i = 0);
}

public partial class ImplInterfaceWithOptionalParameters : InterfaceWithOptionalParameters
{
    public void InterfaceWithOptionalParameters_S(int i = 0)
    {
    }

    void InterfaceWithOptionalParameters.S(int i = 0) => InterfaceWithOptionalParameters_S(i);
}
