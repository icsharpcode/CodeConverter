
internal static partial class Extensions
{
    public static void TestExtension(this NestingClass.ExtendedClass extendedClass)
    {
    }
}

internal partial class NestingClass
{
    public partial class ExtendedClass
    {
        public void TestExtensionConsumer()
        {
            this.TestExtension();
        }
    }
}