
public partial class Class1
{
    public float get_SomeProp(int index)
    {
        return 1.5f;
    }
    public void set_SomeProp(int index, float value)
    {
    }

    public void Foo()
    {
        decimal someDecimal = 123.0m;
        set_SomeProp(123, (float)someDecimal);
    }
}