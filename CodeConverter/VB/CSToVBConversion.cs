using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.VB;

/// <remarks>
/// Can be used for multiple projects - InitializeSource resets state
/// </remarks>
public class CSToVBConversion : ILanguageConversion
{
    private const string UnresolvedNamespaceDiagnosticId = "BC40056";

    private CSToVBProjectContentsConverter _csToVbProjectContentsConverter;
    public ConversionOptions ConversionOptions { get; set; }
    private CancellationToken _cancellationToken;

    public async Task<IProjectContentsConverter> CreateProjectContentsConverterAsync(Project project,
        IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _csToVbProjectContentsConverter = new CSToVBProjectContentsConverter(ConversionOptions, progress, cancellationToken);
        await _csToVbProjectContentsConverter.InitializeSourceAsync(project);
        return _csToVbProjectContentsConverter;
    }
    public async Task<Document> SingleSecondPassAsync(Document doc)
    {
        return await doc.SimplifyStatementsAsync<ImportsStatementSyntax>(UnresolvedNamespaceDiagnosticId, _cancellationToken);
    }

    public SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
        bool surroundedWithMethod)
    {
        return surroundedWithMethod
            ? descendantNodes.OfType<CSSyntax.MethodDeclarationSyntax>().First<SyntaxNode>()
            : descendantNodes.OfType<CSSyntax.BaseTypeDeclarationSyntax>().First<SyntaxNode>();
    }

    public IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings()
    {
        return ProjectTypeGuids.VbToCsTypeGuids.Select((vbCs, _) => (vbCs.Item2, vbCs.Item1)).ToArray();
    }

    public IEnumerable<(string, string)> GetProjectFileReplacementRegexes()
    {
        return new[] {
            ("\\\\Microsoft\\.CSharp\\.targets", "\\Microsoft.VisualBasic.targets"),
            ("\\.cs\"", ".vb\""),
            ("\\.cs<", ".vb<")
        };
    }
    public string PostTransformProjectFile(string xml)
    {
        xml = ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xml, "vb", "cs");
        xml = TweakDefineConstantsSeparator(xml);
        xml = AddInfer(xml);
        xml = TweakOutputPaths(xml);
        return xml;
    }

    private static string AddInfer(string xml)
    {
        if (xml.IndexOf("<OptionInfer>", StringComparison.Ordinal) > -1) return xml;

        string propertygroup = "<PropertyGroup>";
        var startOfFirstPropertyGroup = xml.IndexOf(propertygroup, StringComparison.Ordinal);
        if (startOfFirstPropertyGroup == -1) return xml;

        int endOfFirstPropertyGroupStartTag = startOfFirstPropertyGroup + propertygroup.Length;
        return xml.Substring(0, endOfFirstPropertyGroupStartTag) + Environment.NewLine + "    <OptionInfer>On</OptionInfer>" +
               xml.Substring(endOfFirstPropertyGroupStartTag);
    }

    private static string TweakDefineConstantsSeparator(string s)
    {
        var startTag = "<DefineConstants>";
        var endTag = "</DefineConstants>";
        var defineConstantsStart = s.IndexOf(startTag, StringComparison.Ordinal);
        var defineConstantsEnd = s.IndexOf(endTag, StringComparison.Ordinal);
        if (defineConstantsStart == -1 || defineConstantsEnd == -1)
            return s;

        return s.Substring(0, defineConstantsStart) +
               s.Substring(defineConstantsStart, defineConstantsEnd - defineConstantsStart).Replace(";", ",") +
               s.Substring(defineConstantsEnd);
    }

    private static string TweakOutputPaths(string s)
    {
        var startTag = "<PropertyGroup";
        var endTag = "</PropertyGroup>";
        var prevGroupEnd = 0;
        var propertyGroupStart = s.IndexOf(startTag, StringComparison.Ordinal);
        var propertyGroupEnd = s.IndexOf(endTag, StringComparison.Ordinal);

        if (propertyGroupStart == -1 || propertyGroupEnd == -1) {
            return s;
        }
            
        var sb = new StringBuilder();
        while (propertyGroupStart != -1 && propertyGroupEnd != -1) {
            sb.Append(s, prevGroupEnd, propertyGroupStart - prevGroupEnd);

            var curSegment = s.Substring(propertyGroupStart, propertyGroupEnd - propertyGroupStart);
            curSegment = TweakOutputPath(curSegment);
            sb.Append(curSegment);
            prevGroupEnd = propertyGroupEnd;
            propertyGroupStart = s.IndexOf(startTag, propertyGroupEnd, StringComparison.Ordinal);
            propertyGroupEnd = s.IndexOf(endTag, prevGroupEnd + 1, StringComparison.Ordinal);
        }

        sb.Append(s, prevGroupEnd, s.Length - prevGroupEnd);

        return sb.ToString();
    }

    private static string TweakOutputPath(string s)
    {
        var startPathTag = "<OutputPath>";
        var endPathTag = "</OutputPath>";
        var pathStart = s.IndexOf(startPathTag, StringComparison.Ordinal);
        var pathEnd = s.IndexOf(endPathTag, StringComparison.Ordinal);

        if (pathStart == -1 || pathEnd == -1)
            return s;
        var filePath = s.Substring(pathStart + startPathTag.Length,
            pathEnd - (pathStart + startPathTag.Length));
            
        var startFileTag = "<DocumentationFile";
        var endFileTag = "</DocumentationFile>";
        var fileTagStart = s.IndexOf(startFileTag, StringComparison.Ordinal);
        var fileTagEnd = s.IndexOf(endFileTag, StringComparison.Ordinal);

        if (fileTagStart == -1 || fileTagEnd == -1)
            return s;

        return s.Substring(0, fileTagStart) +
               s.Substring(fileTagStart, fileTagEnd - fileTagStart).Replace(filePath, "") +
               s.Substring(fileTagEnd);
    }

    public string TargetLanguage { get; } = LanguageNames.VisualBasic;

    public bool CanBeContainedByMethod(SyntaxNode node)
    {
        return node is CSSyntax.IncompleteMemberSyntax ||
               node is CSSyntax.StatementSyntax and not CSSyntax.LocalFunctionStatementSyntax ||
               node.ContainsSkippedText ||
               node.IsMissing ||
               ParsedAsFieldButCouldBeLocalVariableDeclaration(node);
    }

    public bool MustBeContainedByClass(SyntaxNode node)
    {
        return node is CSSyntax.BaseMethodDeclarationSyntax || node is CSSyntax.BaseFieldDeclarationSyntax ||
               node is CSSyntax.BasePropertyDeclarationSyntax || node is CSSyntax.LocalFunctionStatementSyntax;
    }

    private static bool ParsedAsFieldButCouldBeLocalVariableDeclaration(SyntaxNode node)
    {
        return node is CSSyntax.FieldDeclarationSyntax f && f.Modifiers.All(m => m.IsKind(SyntaxKind.TypeVarKeyword));
    }

    public string WithSurroundingMethod(string text)
    {
        return $@"void SurroundingSub()
{{
{text}
}}";
    }

    public string WithSurroundingClass(string text)
    {
        var modifier = text.Contains("abstract ") ? "abstract " : "";
        return $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

{modifier}class SurroundingClass
{{
{text}
}}";
    }

    public List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode)
    {
        var children = annotatedNode.ChildNodes().ToList();
        if (children.Count > 1) {
            switch (annotatedNode) {
                case VBSyntax.TypeBlockSyntax typeBlock:
                    return typeBlock.Members.ToList<SyntaxNode>();
                case VBSyntax.MethodBlockBaseSyntax methodBlock:
                    return methodBlock.Statements.ToList<SyntaxNode>();
            }
        }
        return children;
    }

    public SyntaxTree CreateTree(string text)
    {
        return new CSharpCompiler().CreateTree(text);
    }

    public async Task<Document> CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references)
    {
        return (await CSharpCompiler.CreateCompilationOptions().CreateProjectAsync(references, CSharpParseOptions.Default))
            .AddDocumentFromTree(tree);
    }
}