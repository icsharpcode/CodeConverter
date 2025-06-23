
public partial class TestEventWithNoType
{
    public event OnCakeChangeEventHandler OnCakeChange;

    public delegate void OnCakeChangeEventHandler();

    public void RaisingFlour()
    {
        OnCakeChange?.Invoke();
    }
}