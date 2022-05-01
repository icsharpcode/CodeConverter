namespace ICSharpCode.CodeConverter.Util.FromRoslyn;

internal static class ConversionExtensions
{
    public static bool IsKind(this VBasic.Conversion conversion, VbConversionKind kind)
    {
        if (!Enum.TryParse<VbConversionKind>(conversion.ToString(), ignoreCase: true, out var conversionKind)) {
            throw new ArgumentException($"Invalid vb conversion kind {conversion}", nameof(conversion));
        }

        return (conversionKind & kind) != 0;
    }
}