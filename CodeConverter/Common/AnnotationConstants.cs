using System.Globalization;

namespace ICSharpCode.CodeConverter.Common;

internal static class AnnotationConstants
{
    public const string SelectedNodeAnnotationKind = "CodeConverter.SelectedNode";
    public const string AnnotatedNodeIsParentData = "CodeConverter.SelectedNode.IsAllChildrenOfThisNode";
    public const string ConversionErrorAnnotationKind = "CodeConverter.ConversionError";
    public const string SourceStartLineAnnotationKind = "CodeConverter.SourceStartLine";
    public const string SourceEndLineAnnotationKind = "CodeConverter.SourceEndLine";

    public static SyntaxAnnotation SourceStartLine(FileLinePositionSpan origLinespan)
    {
        return new SyntaxAnnotation(SourceStartLineAnnotationKind, origLinespan.StartLinePosition.Line.ToString(CultureInfo.InvariantCulture));
    }

    public static SyntaxAnnotation SourceEndLine(FileLinePositionSpan origLinespan)
    {
        return new SyntaxAnnotation(SourceEndLineAnnotationKind, origLinespan.EndLinePosition.Line.ToString(CultureInfo.InvariantCulture));
    }
}