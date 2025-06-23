
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool isFirst = false)
    {
        return FirstName + " " + LastName;
    }
    // This comment belongs to the set method
    internal void set_FullName(bool isFirst = false, string value = default)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(true, "hello2");
        set_FullName(value: "hello3");
        set_FullName(value: "hello4");
        return get_FullName();
    }
}