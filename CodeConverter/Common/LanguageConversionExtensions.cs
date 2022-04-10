using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Common;

internal static class LanguageConversionExtensions {
    public static SyntaxTree MakeFullCompilationUnit(this ILanguageConversion languageConversion, string code, out TextSpan? textSpan) {
        var tree= languageConversion.CreateTree(code);
        var root = tree.GetRoot();
        textSpan = null;
        
        var rootChildren = root.ChildNodes()
            //https://github.com/icsharpcode/CodeConverter/issues/825
            .Select(c => c is GlobalStatementSyntax {Statement: var s} ? s : c)
            .ToList();
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