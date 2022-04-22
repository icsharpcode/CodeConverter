using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Common;

internal static class AnnotationConstants
{
    public const string SelectedNodeAnnotationKind = "CodeConverter.SelectedNode";
    public const string AnnotatedNodeIsParentData = "CodeConverter.SelectedNode.IsAllChildrenOfThisNode";
    public const string ConversionErrorAnnotationKind = "CodeConverter.ConversionError";
    public const string SourceStartLineAnnotationKind = "CodeConverter.SourceStartLine";
    public const string SourceEndLineAnnotationKind = "CodeConverter.SourceEndLine";
    public const string LeadingTriviaAlreadyMappedAnnotation = nameof(CodeConverter) + "." + nameof(LeadingTriviaAlreadyMappedAnnotation);
    public const string TrailingTriviaAlreadyMappedAnnotation = nameof(CodeConverter) + "." + nameof(TrailingTriviaAlreadyMappedAnnotation);

    private static string AsString(LinePosition position)
    {
        return position.Line.ToString(CultureInfo.InvariantCulture) + ":" + position.Character.ToString(CultureInfo.InvariantCulture);
    }
    private static string FromString(LinePosition position)
    {
        return position.Line.ToString(CultureInfo.InvariantCulture) + ":" + position.Character.ToString(CultureInfo.InvariantCulture);
    }

    public static SyntaxAnnotation MarkLeadingTriviaAsMapped(int position)
    {
        return new SyntaxAnnotation(LeadingTriviaAlreadyMappedAnnotation, position.ToString(CultureInfo.InvariantCulture));
    }
    public static SyntaxAnnotation MarkTrailingTriviaAsMapped(int position)
    {
        return new SyntaxAnnotation(TrailingTriviaAlreadyMappedAnnotation, position.ToString(CultureInfo.InvariantCulture));
    }

    public static SyntaxAnnotation SourceStartLine(FileLinePositionSpan origLinespan)
    {
        return new SyntaxAnnotation(SourceStartLineAnnotationKind, origLinespan.StartLinePosition.Line.ToString(CultureInfo.InvariantCulture));
    }

    public static SyntaxAnnotation SourceEndLine(FileLinePositionSpan origLinespan)
    {
        return new SyntaxAnnotation(SourceEndLineAnnotationKind, origLinespan.EndLinePosition.Line.ToString(CultureInfo.InvariantCulture));
    }
}