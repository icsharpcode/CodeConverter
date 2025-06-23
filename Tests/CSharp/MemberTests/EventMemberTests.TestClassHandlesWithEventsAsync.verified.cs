
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal partial class Class1
{
    private static MyEventClass SharedEventClassInstance;
    private MyEventClass NonSharedEventClassInstance;

    static Class1()
    {
        SharedEventClassInstance = new MyEventClass();
        SharedEventClassInstance.TestEvent += PrintTestMessage2;
    }

    public Class1(int num)
    {
        NonSharedEventClassInstance = new MyEventClass(); // Comment moves to initialization in c# constructor
        NonSharedEventClassInstance.TestEvent += PrintTestMessage2;
        NonSharedEventClassInstance.TestEvent += PrintTestMessage3;
    }

    public Class1(object obj) : this(7)
    {
    }

    public static void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
    }

    public partial class NestedShouldNotGainConstructor
    {
    }
}

public partial class ShouldNotGainConstructor
{
}