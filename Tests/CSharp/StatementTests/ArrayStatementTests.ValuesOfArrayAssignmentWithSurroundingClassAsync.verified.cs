
internal partial class SurroundingClass
{
    public string[] Arr;
}

internal partial class UseClass
{
    public void DoStuff()
    {
        var surrounding = new SurroundingClass();
        surrounding.Arr[1] = "bla";
    }
}