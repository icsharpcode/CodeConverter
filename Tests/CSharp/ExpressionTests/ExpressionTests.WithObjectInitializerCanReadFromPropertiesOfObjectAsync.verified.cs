
public partial class SomeClass
{
    public string SomeProperty;
    static SomeClass initInstance()
    {
        var init = new SomeClass();
        return (init.SomeProperty = init.SomeProperty + nameof(init.SomeProperty), init).init; // Second line gets moved
    } // Third line gets moved

    public static SomeClass Instance = initInstance(); // First line gets moved
}