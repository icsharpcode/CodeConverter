
public partial class Class1
{
    public Class1()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Foo()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Bar()
    {
        var i = default(int);
        string temp = string.Empty;
        i += 1;
    }

    public void Bar2()
    {
        var i = default(int);
        string temp = string.Empty;
        i = i + 1;
    }

    public void Bar3()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
    }

    public void Bar4()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
        i = 1;
    }

    public int State
    {
        get
        {
            var needsInitialization = default(int);
            int notUsed;
            int y = needsInitialization;
            return y;
        }
    }
}