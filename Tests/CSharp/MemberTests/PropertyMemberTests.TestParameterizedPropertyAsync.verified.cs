
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool lastNameFirst, bool isFirst)
    {
        if (lastNameFirst)
        {
            return LastName + " " + FirstName;
        }
        else
        {
            return FirstName + " " + LastName;
        }
    }
    // This comment belongs to the set method
    internal void set_FullName(bool lastNameFirst, bool isFirst, string value)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(false, true, "hello");
        return get_FullName(false, true);
    }
}