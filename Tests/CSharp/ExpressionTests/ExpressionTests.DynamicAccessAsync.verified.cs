
public partial class TestDynamicUsage
{
    public int Prop { get; set; }

    public void S()
    {
        object o;
        o = new TestDynamicUsage();
        ((dynamic)o).Prop = 1; // Must not cast to object here
    }
}