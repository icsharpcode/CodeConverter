
public partial class Issue856
{
    public void Main()
    {
        var decimalTarget = default(decimal);
        double argresult = (double)decimalTarget;
        double.TryParse("123", out argresult);
        decimalTarget = (decimal)argresult;

        var longTarget = default(long);
        int argresult1 = (int)longTarget;
        int.TryParse("123", out argresult1);
        longTarget = argresult1;

        var intTarget = default(int);
        long argresult2 = intTarget;
        long.TryParse("123", out argresult2);
        intTarget = (int)argresult2;
    }

}