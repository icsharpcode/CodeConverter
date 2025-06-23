
internal partial class SurroundingClass
{
    private bool _Prop_bGet;
    private bool _Prop_bSet;

    public string get_Prop(int i)
    {
        _Prop_bGet = false;
        return default;
    }

    public void set_Prop(int i, string value)
    {
        _Prop_bSet = false;
    }

}