using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock = new StringBuilder();
            withBlock.Capacity = 20;
            withBlock?.Append(0);
        }
    }
}