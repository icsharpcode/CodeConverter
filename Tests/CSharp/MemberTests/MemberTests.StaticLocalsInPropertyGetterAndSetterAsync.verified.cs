
internal partial class SurroundingClass
{
    private bool _Prop_b;
    private bool _Prop_b1;

    public string Prop
    {
        get
        {
            _Prop_b = true;
            return default;
        }

        set
        {
            _Prop_b1 = false;
        }
    }

}