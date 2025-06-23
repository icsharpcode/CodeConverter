
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

public partial class Class1
{
    private MyEventClass EventClassInstance, EventClassInstance2;

    public Class1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public Class1(int num)
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass(); // Comment moves to initialization in c# constructor
        EventClassInstance.TestEvent += PrintTestMessage2;
        EventClassInstance.TestEvent += PrintTestMessage3;
        EventClassInstance2.TestEvent += PrintTestMessage2;
    }

    public Class1(object obj) : this()
    {
    }
}

public partial class Class1
{
    public void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
    }
}