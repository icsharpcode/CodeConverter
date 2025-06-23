
public enum TestState
{
    one,
    two
}

public partial class test
{
    private TestState _state;
    public TestState State
    {
        get
        {
            return _state;
        }
        set
        {
            if (!_state.Equals(value))
            {
                _state = value;
            }
        }
    }
}