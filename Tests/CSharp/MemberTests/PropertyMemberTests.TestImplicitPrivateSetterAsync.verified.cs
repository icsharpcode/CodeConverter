
public partial class SomeClass
{
    public int SomeValue { get; private set; }

    public void SetValue(int value1, int value2)
    {
        SomeValue = value1 + value2;
    }
}