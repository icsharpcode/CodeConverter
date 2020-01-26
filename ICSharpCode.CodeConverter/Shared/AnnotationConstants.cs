using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class AnnotationConstants
    {
        public const string SelectedNodeAnnotationKind = "CodeConverter.SelectedNode";
        public const string AnnotatedNodeIsParentData = "CodeConverter.SelectedNode.IsAllChildrenOfThisNode";
        public const string ConversionErrorAnnotationKind = "CodeConverter.ConversionError";
        public const string WithinOriginalLineAnnotationKind = "CodeConverter.WithinOriginalLine";

        public static SyntaxAnnotation OriginalLineAnnotation(FileLinePositionSpan origLinespan)
        {
            return new SyntaxAnnotation(WithinOriginalLineAnnotationKind, origLinespan.StartLinePosition.Line.ToString());
        }
    }
}