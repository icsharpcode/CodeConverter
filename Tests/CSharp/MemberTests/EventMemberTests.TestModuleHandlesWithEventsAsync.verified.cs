
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal static partial class Module1
{
    private static MyEventClass EventClassInstance, EventClassInstance2;

    static Module1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public static void PrintTestMessage2()
    {
    }

    public static void PrintTestMessage3()
    {
    }
}