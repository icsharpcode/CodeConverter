using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestGenericCast
{
    private static T GenericFunctionWithCTypeCast<T>()
    {
        const int result = 1;
        object resultObj = result;
        return Conversions.ToGenericParameter<T>(resultObj);
    }
    private static T GenericFunctionWithCast<T>()
    {
        const int result = 1;
        object resultObj = result;
        return Conversions.ToGenericParameter<T>(resultObj);
    }
    private static T GenericFunctionWithCastThatExistsInCsharp<T>() where T : TestGenericCast
    {
        return (T)new TestGenericCast();
    }
}