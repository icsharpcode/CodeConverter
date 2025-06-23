
internal partial class TestClass
{
    public static event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler(string a);
}