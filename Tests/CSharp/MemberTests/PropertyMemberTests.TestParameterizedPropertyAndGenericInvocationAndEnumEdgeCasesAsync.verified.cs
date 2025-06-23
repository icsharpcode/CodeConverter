using System.Linq;

public partial class ParameterizedPropertiesAndEnumTest
{
    public enum MyEnum
    {
        First
    }

    public string get_MyProp(int blah)
    {
        return blah.ToString();
    }
    public void set_MyProp(int blah, string value)
    {
    }


    public void ReturnWhatever(MyEnum m)
    {
        var enumerableThing = Enumerable.Empty<string>();
        switch (m)
        {
            case (MyEnum)(-1):
                {
                    return;
                }
            case MyEnum.First:
                {
                    return;
                }
            case (MyEnum)3:
                {
                    set_MyProp(4, enumerableThing.ToArray()[(int)m]);
                    return;
                }
        }
    }
}