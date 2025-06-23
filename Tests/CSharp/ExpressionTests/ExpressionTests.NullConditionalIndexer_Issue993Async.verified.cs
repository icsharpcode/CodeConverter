
public partial class VisualBasicClass
{

    private bool TestMethod(object[] testArray)
    {
        return !string.IsNullOrWhiteSpace(testArray?[0]?.ToString());
    }

}