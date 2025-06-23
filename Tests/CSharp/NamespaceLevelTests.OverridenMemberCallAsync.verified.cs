using System;

internal static partial class Module1
{
    public partial class BaseImpl
    {
        protected virtual string GetImplName()
        {
            return nameof(BaseImpl);
        }
    }

    /// <summary>
    /// The fact that this class doesn't contain a definition for GetImplName is crucial to the repro
    /// </summary>
    public partial class ErrorSite : BaseImpl
    {
        public object PublicGetImplName()
        {
            // This must not be qualified with MyBase since the method is overridable
            return GetImplName();
        }
    }

    public partial class OverrideImpl : ErrorSite
    {
        protected override string GetImplName()
        {
            return nameof(OverrideImpl);
        }
    }

    public static void Main()
    {
        var c = new OverrideImpl();
        Console.WriteLine(c.PublicGetImplName());
    }
}