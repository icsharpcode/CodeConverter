using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ICSharpCode.CodeConverter.CSharp;
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
        return await _csToVbProjectContentsConverter.OptionalOperations.SimplifyStatementsAsync<ImportsStatementSyntax>(doc, UnresolvedNamespaceDiagnosticId);
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
            (@"\\([A-Za-z\.]*)\.CSharp\.targets", @"\$1.VisualBasic.targets"),
            (@"\.cs""", @".vb"""),
            (@"\.cs<", @".vb<")
        };
    }
    public string PostTransformProjectFile(string xml)
    {
        var xmlDoc = XDocument.Parse(xml);
        XNamespace xmlNs = xmlDoc.Root.GetDefaultNamespace();

        ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xmlDoc, xmlNs, "vb", "cs");
        AddInfer(xmlDoc, xmlNs);

        var propertyGroups = xmlDoc.Descendants(xmlNs + "PropertyGroup");
        foreach (XElement propertyGroup in propertyGroups) {
            TweakDefineConstants(propertyGroup, xmlNs);
            TweakOutputPath(propertyGroup, xmlNs);
        }

        return xmlDoc.Declaration != null ? xmlDoc.Declaration + Environment.NewLine + xmlDoc : xmlDoc.ToString();
    }

    private static void AddInfer(XDocument xmlDoc, XNamespace xmlNs)
    {
        var infer = xmlDoc.Descendants(xmlNs + "OptionInfer").FirstOrDefault();
        if (infer != null) {
            return;
        }

        var firstPropertyGroup = xmlDoc.Descendants(xmlNs + "PropertyGroup").FirstOrDefault();
        infer = new XElement(xmlNs + "OptionInfer", "On");
        firstPropertyGroup?.AddFirst(infer);
    }

    private static void TweakDefineConstants(XElement propertyGroup, XNamespace xmlNs)
    {
        var defineConstants = propertyGroup.Element(xmlNs + "DefineConstants");

        if (defineConstants != null) {
            // CS uses semi-colon as separator, VB uses comma
            var values = defineConstants.Value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            defineConstants.Value = string.Join(",", values);
        }
    }

    private static void TweakOutputPath(XElement propertyGroup, XNamespace xmlNs)
    {
        var outputPath = propertyGroup.Element(xmlNs + "OutputPath");
        var documentationFile = propertyGroup.Element(xmlNs + "DocumentationFile");

        // In CS, DocumentationFile is prepended by OutputPath, not in VB
        if (outputPath != null && documentationFile != null) {
            documentationFile.Value = documentationFile.Value.Replace(outputPath.Value, "");
        }
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