using System.Diagnostics;

public partial class Issue567
{
    private string[] arr;
    private string[,] arr2;

    public void DoSomething(ref string str)
    {
        str = "test";
    }

    public void Main()
    {
        DoSomething(ref arr[1]);
        Debug.Assert(arr[1] == "test");
        DoSomething(ref arr2[2, 2]);
        Debug.Assert(arr2[2, 2] == "test");
    }

}