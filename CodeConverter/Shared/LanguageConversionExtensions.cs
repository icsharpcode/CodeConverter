using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class LanguageConversionExtensions {
        public static SyntaxTree MakeFullCompilationUnit(this ILanguageConversion languageConversion, string code, out TextSpan? textSpan) {
            var tree= languageConversion.CreateTree(code);
            var root = tree.GetRoot();
            textSpan = null;
            var rootChildren = root.ChildNodes().ToList();
            var requiresSurroundingClass = rootChildren.Any(languageConversion.MustBeContainedByClass);
            var requiresSurroundingMethod = rootChildren.All(languageConversion.CanBeContainedByMethod);

            if (requiresSurroundingMethod || requiresSurroundingClass) {
                var text = root.GetText().ToString();
                if (requiresSurroundingMethod) text = languageConversion.WithSurroundingMethod(text);
                text = languageConversion.WithSurroundingClass(text);

                var fullCompilationUnit = languageConversion.CreateTree(text).GetRoot();

                var selectedNode = languageConversion.GetSurroundedNode(fullCompilationUnit.DescendantNodes(), requiresSurroundingMethod);
                tree = fullCompilationUnit.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind, AnnotationConstants.AnnotatedNodeIsParentData);
                textSpan = selectedNode.Span;
            }

            return tree;
        }
    }
}