using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock2 = new StringBuilder();
            int withBlock = 3;
            {
                var withBlock3 = new StringBuilder();
                int withBlock1 = 4;
                withBlock3.Capacity = withBlock1;
            }

            withBlock2.Length = withBlock;
        }
    }
}