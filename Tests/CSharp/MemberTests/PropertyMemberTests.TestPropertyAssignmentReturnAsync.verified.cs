using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public string Foo
    {
        get
        {
            string FooRet = default;
            FooRet = "";
            return FooRet;
        }
    }
    public string X
    {
        get
        {
            string XRet = default;
            XRet = 4.ToString();
            XRet = (Conversions.ToDouble(XRet) * 2d).ToString();
            string y = "random variable to check it isn't just using the value of the last statement";
            return XRet;
        }
    }
    public string _y;
    public string Y
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Y = "";
            }
            else
            {
                _y = "";
            }
        }
    }
}