using System.Text;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;

namespace ICSharpCode.CodeConverter.CSharp;

/// <remarks>
/// Can be used for multiple projects - does not persist state from one InitializeSource to the next
/// </remarks>
public class VBToCSConversion : ILanguageConversion
{
    private const string UnresolvedNamespaceDiagnosticId = "CS0246";
    private const string FabricatedAssemblyName = ISymbolExtensions.ForcePartialTypesAssemblyName;
    private VBToCSProjectContentsConverter _vbToCsProjectContentsConverter;
    private CancellationToken _cancellationToken;

    public ConversionOptions ConversionOptions { get; set; }

    public async Task<IProjectContentsConverter> CreateProjectContentsConverterAsync(Project project,
        IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        bool useProjectLevelWinformsAdjustments = project.AssemblyName != FabricatedAssemblyName;
        _vbToCsProjectContentsConverter = new VBToCSProjectContentsConverter(ConversionOptions, useProjectLevelWinformsAdjustments, progress, cancellationToken);
        await _vbToCsProjectContentsConverter.InitializeSourceAsync(project);
        return _vbToCsProjectContentsConverter;
    }

    public SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
        bool surroundedWithMethod)
    {
        return surroundedWithMethod
            ? descendantNodes.OfType<VBSyntax.MethodBlockBaseSyntax>().First<SyntaxNode>()
            : descendantNodes.OfType<VBSyntax.TypeBlockSyntax>().First<SyntaxNode>();
    }

    public IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings()
    {
        return ProjectTypeGuids.VbToCsTypeGuids;
    }

    public IEnumerable<(string, string)> GetProjectFileReplacementRegexes()
    {
        string rootNamespaceDot = _vbToCsProjectContentsConverter.RootNamespace;
        if (!string.IsNullOrEmpty(rootNamespaceDot)) rootNamespaceDot += ".";

        return new[] {
            (@"\\([A-Za-z\.]*)\.VisualBasic\.targets", @"\$1.CSharp.targets"),
            (@"\.vb""", @".cs"""),
            (@"\.vb<", @".cs<"),
            (@"<\s*Generator\s*>\s*VbMyResourcesResXFileCodeGenerator\s*</\s*Generator\s*>", @"<Generator>ResXFileCodeGenerator</Generator>"),
            (@"(<\s*CustomToolNamespace\s*>)(.*</\s*CustomToolNamespace\s*>)", @$"$1{rootNamespaceDot}$2"), // <CustomToolNamespace>My.Resources</CustomToolNamespace>
        };
    }

    public string PostTransformProjectFile(string xml)
    {
        var xmlDoc = XDocument.Parse(xml);
        XNamespace xmlNs = xmlDoc.Root.GetDefaultNamespace();

        ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xmlDoc, xmlNs, "cs", "vb");
        AddVisualBasicReference(xmlDoc, xmlNs);
        AddLangVersion(xmlDoc, xmlNs);

        var propertyGroups = xmlDoc.Descendants(xmlNs + "PropertyGroup");
        foreach (XElement propertyGroup in propertyGroups) {
            TweakDefineConstants(propertyGroup, xmlNs);
            TweakOutputPath(propertyGroup, xmlNs);
        }

        return xmlDoc.Declaration != null ? xmlDoc.Declaration + Environment.NewLine + xmlDoc : xmlDoc.ToString();
    }

    private static void AddVisualBasicReference(XDocument xmlDoc, XNamespace xmlNs)
    {
        var reference = xmlDoc.Descendants(xmlNs + "Reference").FirstOrDefault(e => e.Attribute("Include")?.Value == "Microsoft.VisualBasic");
        if (reference != null) {
            return;
        }

        var firstItemGroup = xmlDoc.Descendants(xmlNs + "ItemGroup").FirstOrDefault();
        reference = new XElement(xmlNs + "Reference");
        reference.SetAttributeValue("Include", "Microsoft.VisualBasic");
        firstItemGroup?.AddFirst(reference);
    }

    private void AddLangVersion(XDocument xmlDoc, XNamespace xmlNs)
    {
        var langVersion = xmlDoc.Descendants(xmlNs + "LangVersion").FirstOrDefault();
        if (langVersion != null) {
            return;
        }

        var firstPropertyGroup = xmlDoc.Descendants(xmlNs + "PropertyGroup").FirstOrDefault();
        langVersion = new XElement(xmlNs + "LangVersion", _vbToCsProjectContentsConverter.LanguageVersion);
        firstPropertyGroup?.Add(langVersion);
    }

    private static void TweakDefineConstants(XElement propertyGroup, XNamespace xmlNs)
    {
        var defineConstants = propertyGroup.Element(xmlNs + "DefineConstants");

        var defineDebug = propertyGroup.Element(xmlNs + "DefineDebug");
        bool shouldDefineDebug = defineDebug?.Value == "true";
        defineDebug?.Remove();

        var defineTrace = propertyGroup.Element(xmlNs + "DefineTrace");
        bool shouldDefineTrace = defineTrace?.Value == "true";
        defineTrace?.Remove();

        if (defineConstants == null && (shouldDefineDebug || shouldDefineTrace)) {
            defineConstants = new XElement(xmlNs + "DefineConstants", "");
            propertyGroup.Add(defineConstants);
        }

        if (defineConstants != null) {
            // CS uses semi-colon as separator, VB uses comma
            var values = defineConstants.Value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // DefineDebug and DefineTrace are ignored in CS
            if (shouldDefineDebug && !values.Contains("DEBUG")) {
                values.Insert(0, "DEBUG");
            }
            if (shouldDefineTrace && !values.Contains("TRACE")) {
                values.Insert(0, "TRACE");
            }

            defineConstants.Value = string.Join(";", values);
        }
    }

    private static void TweakOutputPath(XElement propertyGroup, XNamespace xmlNs)
    {
        var outputPath = propertyGroup.Element(xmlNs + "OutputPath");
        var documentationFile = propertyGroup.Element(xmlNs + "DocumentationFile");

        // In CS, DocumentationFile is prepended by OutputPath, not in VB
        if (outputPath != null && documentationFile != null) {
            documentationFile.Value = outputPath.Value + documentationFile.Value;
        }
    }

    public string TargetLanguage { get; } = LanguageNames.CSharp;

    public bool CanBeContainedByMethod(SyntaxNode node)
    {
        return node is VBSyntax.IncompleteMemberSyntax ||
               !(node is VBSyntax.DeclarationStatementSyntax) ||
               node.ContainsSkippedText ||
               node.IsMissing ||
               CouldBeFieldOrLocalVariableDeclaration(node) ||
               IsNonTypeEndBlock(node);
    }

    private static bool CouldBeFieldOrLocalVariableDeclaration(SyntaxNode node)
    {
        return node is VBSyntax.FieldDeclarationSyntax f && f.Modifiers.All(m => m.IsKind(SyntaxKind.DimKeyword));
    }

    private static bool IsNonTypeEndBlock(SyntaxNode node)
    {
        return node is VBSyntax.EndBlockStatementSyntax ebs &&
               !ebs.BlockKeyword.IsKind(SyntaxKind.ClassKeyword, SyntaxKind.StructureKeyword) && !ebs.BlockKeyword.IsKind(SyntaxKind.InterfaceKeyword, SyntaxKind.ModuleKeyword);
    }

    public bool MustBeContainedByClass(SyntaxNode node)
    {
        return node is VBSyntax.MethodBlockBaseSyntax || node is VBSyntax.MethodBaseSyntax ||
               node is VBSyntax.FieldDeclarationSyntax || node is VBSyntax.PropertyBlockSyntax ||
               node is VBSyntax.EventBlockSyntax;
    }

    public string WithSurroundingMethod(string text)
    {
        return $@"Sub SurroundingSub()
{text}
End Sub";
    }

    public string WithSurroundingClass(string text)
    {
        var modifier = text.Contains("MustOverride ") ? "MustInherit " : "";
        return $@"{modifier}Class SurroundingClass
{text}
End Class";
    }

    public List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode)
    {
        var children = annotatedNode.ChildNodes().ToList();
        if (children.Count > 1) {
            switch (annotatedNode) {
                case CSSyntax.MethodDeclarationSyntax _:
                    return annotatedNode.ChildNodes().OfType<CSSyntax.BlockSyntax>().ToList<SyntaxNode>();
                case CSSyntax.BaseTypeSyntax _:
                    return annotatedNode.ChildNodes().OfType<CSSyntax.BlockSyntax>().ToList<SyntaxNode>();
            }
        }
        return children;
    }

    public async Task<Document> SingleSecondPassAsync(Document doc)
    {
        var simplifiedDocument = await _vbToCsProjectContentsConverter.OptionalOperations.SimplifyStatementsAsync<UsingDirectiveSyntax>(doc, UnresolvedNamespaceDiagnosticId);

        // Can't add a reference to Microsoft.VisualBasic if there's no project file, so hint to install the package
        if (_vbToCsProjectContentsConverter.SourceProject.AssemblyName == FabricatedAssemblyName) {
            var simpleRoot = await simplifiedDocument.GetSyntaxRootAsync();
            var vbUsings = simpleRoot.ChildNodes().OfType<CSSyntax.UsingDirectiveSyntax>().Where(u => u.Name.ToString().Contains("Microsoft.VisualBasic"));
            var commentedRoot = simpleRoot.ReplaceNodes(vbUsings, (_, r) => r.WithTrailingTrivia(CS.SyntaxFactory.Comment(" // Install-Package Microsoft.VisualBasic").Yield().Concat(r.GetTrailingTrivia())));
            simplifiedDocument = simplifiedDocument.WithSyntaxRoot(commentedRoot);
        }
        return simplifiedDocument;
    }

    public SyntaxTree CreateTree(string text)
    {
        return CreateCompiler().CreateTree(text);
    }

    private VisualBasicCompiler CreateCompiler()
    {
        return new VisualBasicCompiler(ConversionOptions.RootNamespaceOverride);
    }

    public async Task<Document> CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references)
    {
        var project = await CreateEmptyVbProjectAsync(references);
        return project.AddDocumentFromTree(tree);
    }

    private async Task<Project> CreateEmptyVbProjectAsync(IEnumerable<MetadataReference> references)
    {
        return await VisualBasicCompiler.CreateCompilationOptions(ConversionOptions.RootNamespaceOverride)
            .CreateProjectAsync(references, VisualBasicParseOptions.Default, FabricatedAssemblyName);
    }
}