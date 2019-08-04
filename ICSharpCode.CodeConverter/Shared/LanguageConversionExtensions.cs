using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class LanguageConversionExtensions {
        public static SyntaxTree MakeFullCompilationUnit(this ILanguageConversion languageConversion, string code) {
            var tree= languageConversion.CreateTree(code);
            var root = tree.GetRoot();
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
            }

            return tree;
        }
    }
}