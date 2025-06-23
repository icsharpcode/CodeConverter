using System;

internal partial class TestClass45
{
    private event EventHandler backingField;

    public event EventHandler MyEvent
    {
        add
        {
            backingField += value;
        }
        remove
        {
            backingField -= value;
        }
    } // RaiseEvent moves outside this block
    void OnMyEvent(object sender, EventArgs e)
    {
        Console.WriteLine("Event Raised");
    }

    public void RaiseCustomEvent()
    {
        OnMyEvent(this, EventArgs.Empty);
    }
}