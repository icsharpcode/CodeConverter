
internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();
}

internal partial class Class1
{
    private MyEventClass MyEventClassInstance;

    public Class1()
    {
        MyEventClassInstance.TestEvent += EventClassInstance_TestEvent;
    }
    public void EventClassInstance_TestEvent()
    {
    }
}
