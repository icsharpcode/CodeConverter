
namespace TestNamespace
{
    public static partial class TestModule
    {
        public static void ModuleFunction()
        {
        }
    }
}

internal partial class TestClass
{
    public void TestMethod(string dir)
    {
        TestNamespace.TestModule.ModuleFunction();
    }
}