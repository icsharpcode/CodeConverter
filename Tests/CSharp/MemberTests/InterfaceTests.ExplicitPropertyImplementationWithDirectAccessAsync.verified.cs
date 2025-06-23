
public partial interface IFoo
{
    int ExplicitProp { get; set; }
    int ExplicitReadOnlyProp { get; }
}

public partial class Foo : IFoo
{

    public int ExplicitPropRenamed { get; set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed; set => ExplicitPropRenamed = value; }
    public int ExplicitRenamedReadOnlyProp { get; private set; }
    int IFoo.ExplicitReadOnlyProp { get => ExplicitRenamedReadOnlyProp; }

    private void Consumer()
    {
        ExplicitPropRenamed = 5;
        ExplicitRenamedReadOnlyProp = 10;
    }

}