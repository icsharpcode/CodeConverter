using System.Threading.Tasks;

public partial class ConversionTest8
{
    private int x = 5;

    public ConversionTest8()
    {
        ClassVariable1 = new ParallelOptions() { MaxDegreeOfParallelism = x };

        // Constructor Comment 1
        bool constructorVar1 = true;

        // Constructor Comment 2
        bool constructorVar2 = true;

    }

    #region Region1
    private void Method1()
    {
    }
    #endregion
    #region Region2
    // Class Comment 3
    private readonly ParallelOptions ClassVariable1;
    #endregion
}