namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// VB label names are case insensitive, so they're compared in a normalized form.
/// </summary>
internal static class LabelName
{
    public static string Normalize(string label) => label.ToLowerInvariant();

    public static string Of(VBSyntax.LabelStatementSyntax label) => Normalize(label.LabelToken.Text);
    public static string Of(VBSyntax.LabelSyntax label) => Normalize(label.LabelToken.Text);
}
