
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}