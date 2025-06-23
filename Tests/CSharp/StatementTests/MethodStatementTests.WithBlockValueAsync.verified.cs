
public partial class VisualBasicClass
{
    public void Stuff()
    {
        var str = default(SomeStruct);
        str.ArrField = new string[2];
        str.ArrProp = new string[3];
    }
}

public partial struct SomeStruct
{
    public string[] ArrField;
    public string[] ArrProp { get; set; }
}