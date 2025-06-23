using System.IO;

internal partial class TestClass
{
    public void TestMethod(string dir)
    {
        Path.Combine(dir, "file.txt");
        var c = new System.Collections.ObjectModel.ObservableCollection<string>();
    }
}