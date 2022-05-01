using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using Microsoft.CodeAnalysis.VisualBasic;
using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Declaration nodes, and nodes only used directly in that declaration (i.e. never within an expression)
/// e.g. Class, Enum, TypeConstraint
/// </summary>
/// <remarks>The split between this and the <see cref="ExpressionNodeVisitor"/> is purely organizational and serves no real runtime purpose.</remarks>
internal class DeclarationNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
{
    private static readonly Type DllImportType = typeof(DllImportAttribute);
    private static readonly Type CharSetType = typeof(CharSet);
    private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(CSSyntaxKind.SemicolonToken);
    private readonly SyntaxGenerator _csSyntaxGenerator;
    private readonly ILookup<ITypeSymbol, ITypeSymbol> _typeToInheritors;
    private readonly Compilation _vbCompilation;
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _generatedNames = new();
    private readonly Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]> _additionalDeclarations = new();
    private readonly TypeContext _typeContext = new();
    private uint _failedMemberConversionMarkerCount;
    private readonly HashSet<string> _extraUsingDirectives = new();
    private readonly XmlImportContext _xmlImportContext;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private HashSet<string> _accessedThroughMyClass;
    public CommentConvertingVisitorWrapper TriviaConvertingDeclarationVisitor { get; }
    private readonly CommentConvertingVisitorWrapper _triviaConvertingExpressionVisitor;
    private string _topAncestorNamespace;

    private CommonConversions CommonConversions { get; }
    private Func<VisualBasicSyntaxNode, bool, IdentifierNameSyntax, Task<VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>>> _createMethodBodyVisitorAsync { get; }

    internal PerScopeState AdditionalLocals => _typeContext.PerScopeState;

    public DeclarationNodeVisitor(Document document, Compilation compilation, SemanticModel semanticModel,
        CSharpCompilation csCompilation, SyntaxGenerator csSyntaxGenerator, ILookup<ITypeSymbol, ITypeSymbol> typeToInheritors)
    {
        _vbCompilation = compilation;
        _semanticModel = semanticModel;
        _csSyntaxGenerator = csSyntaxGenerator;
        _typeToInheritors = typeToInheritors;
        _xmlImportContext = new XmlImportContext(document);
        _visualBasicEqualityComparison = new VisualBasicEqualityComparison(_semanticModel, _extraUsingDirectives);
        TriviaConvertingDeclarationVisitor = new CommentConvertingVisitorWrapper(this, _semanticModel.SyntaxTree);
        var expressionEvaluator = new ExpressionEvaluator(semanticModel, _visualBasicEqualityComparison);
        var nullableExpressionsConverter = new VisualBasicNullableExpressionsConverter();
        var typeConversionAnalyzer = new TypeConversionAnalyzer(semanticModel, csCompilation, _extraUsingDirectives, _csSyntaxGenerator, expressionEvaluator, nullableExpressionsConverter);
        CommonConversions = new CommonConversions(document, semanticModel, typeConversionAnalyzer, csSyntaxGenerator, compilation, csCompilation, _typeContext, _visualBasicEqualityComparison);
        var expressionNodeVisitor = new ExpressionNodeVisitor(semanticModel, _visualBasicEqualityComparison, _typeContext, CommonConversions, _extraUsingDirectives, _xmlImportContext, nullableExpressionsConverter);
        _triviaConvertingExpressionVisitor = expressionNodeVisitor.TriviaConvertingExpressionVisitor;
        _createMethodBodyVisitorAsync = expressionNodeVisitor.CreateMethodBodyVisitorAsync;
        CommonConversions.TriviaConvertingExpressionVisitor = _triviaConvertingExpressionVisitor;
    }

    public async Task<VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>> CreateMethodBodyVisitorAsync(VBasic.VisualBasicSyntaxNode node, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {
        return await _createMethodBodyVisitorAsync(node, isIterator, csReturnVariable);
    }

    public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }

    public override async Task<CSharpSyntaxNode> VisitCompilationUnit(VBSyntax.CompilationUnitSyntax node)
    {
        var options = (VBasic.VisualBasicCompilationOptions)_semanticModel.Compilation.Options;
        var importsClauses = options.GlobalImports.Select(gi => gi.Clause).Concat(node.Imports.SelectMany(imp => imp.ImportsClauses)).ToList();

        _topAncestorNamespace = node.Members.Any(m => !IsNamespaceDeclaration(m)) ? options.RootNamespace : null;
        var fileOptionCompareValue = node.Options.Where(x => x.NameKeyword.IsKind(VBasic.SyntaxKind.CompareKeyword)).LastOrDefault()?.ValueKeyword;
        _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive = fileOptionCompareValue?.IsKind(VBasic.SyntaxKind.TextKeyword) ?? options.OptionCompareText;

        var attributes = SyntaxFactory.List(await node.Attributes.SelectMany(a => a.AttributeLists).SelectManyAsync(CommonConversions.ConvertAttributeAsync));

        var xmlImportHelperClassDeclarationOrNull = (await _xmlImportContext.HandleImportsAsync(importsClauses, x => x.AcceptAsync<FieldDeclarationSyntax>(TriviaConvertingDeclarationVisitor))).GenerateHelper();
            
        var sourceAndConverted = await node.Members
            .Where(m => !(m is VBSyntax.OptionStatementSyntax))
            .SelectAsync(async m => (Source: m, Converted: await ConvertMemberAsync(m)));


        var convertedMembers = string.IsNullOrEmpty(options.RootNamespace)
            ? sourceAndConverted.Select(sd => sd.Converted)
            : PrependRootNamespace(sourceAndConverted, SyntaxFactory.IdentifierName(options.RootNamespace));
            
        var usings = await importsClauses
            .SelectAsync(async c => await c.AcceptAsync<UsingDirectiveSyntax>(TriviaConvertingDeclarationVisitor));
        var usingDirectiveSyntax = usings
            .Concat(_extraUsingDirectives.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))))
            .OrderByDescending(IsSystemUsing).ThenBy(u => u.Name.ToString().Replace("global::", "")).ThenByDescending(HasSourceMapAnnotations)
            .GroupBy(u => (Name: u.Name.ToString(), Alias: u.Alias))
            .Select(g => g.First())
            .Concat(xmlImportHelperClassDeclarationOrNull.YieldNotNull().Select(_ => SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals(XmlImportContext.HelperClassShortIdentifierName), _xmlImportContext.HelperClassUniqueIdentifierName)));

        return SyntaxFactory.CompilationUnit(
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List(usingDirectiveSyntax),
            attributes,
            SyntaxFactory.List(xmlImportHelperClassDeclarationOrNull.YieldNotNull().Concat(convertedMembers))
        );
    }

    private static bool IsSystemUsing(UsingDirectiveSyntax u)
    {
        return u.Name.ToString().StartsWith("System", StringComparison.InvariantCulture) || u.Name.ToString().StartsWith("global::System", StringComparison.InvariantCulture);
    }

    private static bool HasSourceMapAnnotations(UsingDirectiveSyntax c)
    {
        return c.HasAnnotations(new[] { AnnotationConstants.SourceStartLineAnnotationKind, AnnotationConstants.SourceEndLineAnnotationKind });
    }

    private IReadOnlyCollection<MemberDeclarationSyntax> PrependRootNamespace(
        IReadOnlyCollection<(VBSyntax.StatementSyntax VbNode, MemberDeclarationSyntax CsNode)> membersConversions,
        IdentifierNameSyntax rootNamespaceIdentifier)
    {

        if (_topAncestorNamespace != null) {
            var csMembers = membersConversions.ToLookup(c => ShouldBeNestedInRootNamespace(c.VbNode, rootNamespaceIdentifier.Identifier.Text), c => c.CsNode);
            var nestedMembers = csMembers[true].Select<MemberDeclarationSyntax, SyntaxNode>(x => x);
            var newNamespaceDecl = (MemberDeclarationSyntax) _csSyntaxGenerator.NamespaceDeclaration(rootNamespaceIdentifier.Identifier.Text, nestedMembers);
            return csMembers[false].Concat(new[] { newNamespaceDecl }).ToArray();
        }
        return membersConversions.Select(n => n.CsNode).ToArray();
    }

    private bool ShouldBeNestedInRootNamespace(VBSyntax.StatementSyntax vbStatement, string rootNamespace)
    {
        return (_semanticModel.GetDeclaredSymbol(vbStatement)?.ToDisplayString()).StartsWith(rootNamespace, StringComparison.InvariantCulture) == true;
    }

    private static bool IsNamespaceDeclaration(VBSyntax.StatementSyntax m)
    {
        return m is VBSyntax.NamespaceBlockSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitSimpleImportsClause(VBSyntax.SimpleImportsClauseSyntax node)
    {
        var nameEqualsSyntax = node.Alias == null
            ? null
            : SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Alias.Identifier)));

        var name = await node.Name.AcceptAsync<NameSyntax>(_triviaConvertingExpressionVisitor);

        //Add static keyword for class and module imports
        var classification = await CommonConversions.GetClassificationLastTokenAsync(node);
        var staticToken = SyntaxFactory.Token(CSSyntaxKind.StaticKeyword);
        var staticClassifications = new List<string>
        {
            ClassificationTypeNames.ClassName,
            ClassificationTypeNames.ModuleName
        };

        var usingDirective = staticClassifications.Contains(classification)
            ? SyntaxFactory.UsingDirective(staticToken, nameEqualsSyntax, name)
            : SyntaxFactory.UsingDirective(nameEqualsSyntax, name);

        return usingDirective;
    }

    public override async Task<CSharpSyntaxNode> VisitNamespaceBlock(VBSyntax.NamespaceBlockSyntax node)
    {
        var members = (await node.Members.SelectAsync(ConvertMemberAsync)).Where(m => m != null);
        var sym = ModelExtensions.GetDeclaredSymbol(_semanticModel, node);
        string namespaceToDeclare = await WithDeclarationNameCasingAsync(node, sym);
        var parentNamespaceSyntax = node.GetAncestor<VBSyntax.NamespaceBlockSyntax>();
        var parentNamespaceDecl = parentNamespaceSyntax != null ? ModelExtensions.GetDeclaredSymbol(_semanticModel, parentNamespaceSyntax) : null;
        var parentNamespaceFullName = parentNamespaceDecl?.ToDisplayString() ?? _topAncestorNamespace;
        if (parentNamespaceFullName != null && namespaceToDeclare.StartsWith(parentNamespaceFullName + ".", StringComparison.InvariantCulture))
            namespaceToDeclare = namespaceToDeclare.Substring(parentNamespaceFullName.Length + 1);

        var cSharpSyntaxNode = (CSharpSyntaxNode) _csSyntaxGenerator.NamespaceDeclaration(namespaceToDeclare, SyntaxFactory.List(members));
        return cSharpSyntaxNode;
    }

    /// <summary>
    /// Semantic model merges the symbols, but the compiled form retains multiple namespaces, which (when referenced from C#) need to keep the correct casing.
    /// <seealso cref="CommonConversions.WithDeclarationNameCasing(TypeSyntax, ITypeSymbol)"/>
    /// <seealso cref="CommonConversions.WithDeclarationName(SyntaxToken, ISymbol, string)"/>
    /// </summary>
    private async Task<string> WithDeclarationNameCasingAsync(VBSyntax.NamespaceBlockSyntax node, ISymbol sym)
    {
        var sourceName = (await node.NamespaceStatement.Name.AcceptAsync<CSharpSyntaxNode>(_triviaConvertingExpressionVisitor)).ToString();
        var namespaceToDeclare = sym?.ToDisplayString() ?? sourceName;
        int lastIndex = namespaceToDeclare.LastIndexOf(sourceName, StringComparison.OrdinalIgnoreCase);
        if (lastIndex >= 0 && lastIndex + sourceName.Length == namespaceToDeclare.Length)
        {
            namespaceToDeclare = namespaceToDeclare.Substring(0, lastIndex) + sourceName;
        }

        return namespaceToDeclare;
    }

    private async Task<IEnumerable<MemberDeclarationSyntax>> ConvertMembersAsync(VBSyntax.TypeBlockSyntax parentType)
    {
        var members = parentType.Members;

        var namedTypeSymbol = _semanticModel.GetDeclaredSymbol(parentType);
        var additionalInitializers = new AdditionalInitializers(parentType, namedTypeSymbol, _vbCompilation);
        var methodsWithHandles = await GetMethodWithHandlesAsync(parentType, additionalInitializers.DesignerGeneratedInitializeComponentOrNull);

        if (methodsWithHandles.AnySynchronizedPropertiesGenerated()) _extraUsingDirectives.Add("System.Runtime.CompilerServices");//For MethodImplOptions.Synchronized
            
        _typeContext.Push(methodsWithHandles, additionalInitializers);
        try {
            var membersFromBase = additionalInitializers.IsBestPartToAddTypeInit ? methodsWithHandles.GetDeclarationsForHandlingBaseMembers() : Array.Empty<MemberDeclarationSyntax>();
            var convertedMembers = await members.SelectManyAsync(async member => {
                _typeContext.PerScopeState.PushScope();
                try
                {
                    return (await _typeContext.PerScopeState.CreateVbStaticFieldsAsync(
                            parentType, namedTypeSymbol, (await ConvertMemberAsync(member)).Yield(), _generatedNames, _semanticModel)
                        ).Concat(GetAdditionalDeclarations(member));
                }
                finally
                {
                    _typeContext.PerScopeState.PopScope();
                }
            });
            return WithAdditionalMembers(membersFromBase.Concat(convertedMembers)).ToArray();//Ensure evaluated before popping type context
        } finally {
            _typeContext.Pop();
        }

        IEnumerable<MemberDeclarationSyntax> WithAdditionalMembers(IEnumerable<MemberDeclarationSyntax> convertedMembers)
        {
            var otherPartsOfType = GetAllPartsOfType(namedTypeSymbol).ToArray();
            var constructorFieldInitializersFromOtherParts = otherPartsOfType
                .Where(t => (!Equals(t.Type.SyntaxTree.FilePath, _semanticModel.SyntaxTree.FilePath) ||
                             !t.Type.Span.Equals(parentType.Span)))
                .SelectMany(r => GetFieldsIdentifiersWithInitializer(r.Type, r.SemanticModel));
            additionalInitializers.AdditionalInstanceInitializers.AddRange(constructorFieldInitializersFromOtherParts);
            if (additionalInitializers.DesignerGeneratedInitializeComponentOrNull is {}) {
                // Constructor event handlers not required since they'll be inside InitializeComponent - see other use of IsDesignerGeneratedTypeWithInitializeComponent
                if (additionalInitializers.IsBestPartToAddTypeInit) convertedMembers = convertedMembers.Concat(methodsWithHandles.CreateDelegatingMethodsRequiredByInitializeComponent());
                additionalInitializers.AdditionalInstanceInitializers.AddRange(CommonConversions.WinformsConversions.GetConstructorReassignments(otherPartsOfType));
            } else {
                additionalInitializers.AdditionalInstanceInitializers.AddRange(methodsWithHandles.GetConstructorEventHandlers());
            }

            return additionalInitializers.WithAdditionalInitializers(convertedMembers.ToList(),
                CommonConversions.ConvertIdentifier(parentType.BlockStatement.Identifier, sourceTriviaMapKind: SourceTriviaMapKind.None));
        }
    }

    private IEnumerable<(VBSyntax.TypeBlockSyntax Type, SemanticModel SemanticModel)> GetAllPartsOfType(ITypeSymbol parentType)
    {
        return parentType.DeclaringSyntaxReferences
            .Select(t => t.GetSyntax().Parent as VBSyntax.TypeBlockSyntax)
            .Where(t => t != null)
            .Select(t => (Type: t, SemanticModel: _vbCompilation.GetSemanticModel(t.SyntaxTree)));
    }

    private IEnumerable<Assignment> GetFieldsIdentifiersWithInitializer(VBSyntax.TypeBlockSyntax tbs, SemanticModel semanticModel)
    {
        return tbs.Members.OfType<VBSyntax.FieldDeclarationSyntax>()
            .SelectMany(f => f.Declarators.SelectMany(d => d.Names.Select(n => (n, d.Initializer))))
            .Where(f => !semanticModel.IsDefinitelyStatic(f.n, f.Initializer?.Value))
            .Select(CreateInitializer);
    }

    private Assignment CreateInitializer((VBSyntax.ModifiedIdentifierSyntax n, VBSyntax.EqualsValueSyntax Initializer) f)
    {
        var csId = CommonConversions.ConvertIdentifier(f.n.Identifier);
        string initializerFunctionName = CommonConversions.GetInitialValueFunctionName(f.n);
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(initializerFunctionName)), SyntaxFactory.ArgumentList());
        return new Assignment(SyntaxFactory.IdentifierName(csId), CSSyntaxKind.SimpleAssignmentExpression, invocation);
    }

    private MemberDeclarationSyntax[] GetAdditionalDeclarations(VBSyntax.StatementSyntax member)
    {
        if (_additionalDeclarations.TryGetValue(member, out var additionalStatements))
        {
            _additionalDeclarations.Remove(member);
            return additionalStatements;
        }

        return Array.Empty<MemberDeclarationSyntax>();
    }

    /// <summary>
    /// In case of error, creates a dummy class to attach the error comment to.
    /// This is because:
    /// * Empty statements are invalid in many contexts in C#.
    /// * There may be no previous node to attach to.
    /// * Attaching to a parent would result in the code being out of order from where it was originally.
    /// </summary>
    private async Task<MemberDeclarationSyntax> ConvertMemberAsync(VBSyntax.StatementSyntax member)
    {
        try {
            return await member.AcceptAsync<MemberDeclarationSyntax>(TriviaConvertingDeclarationVisitor);
        } catch (Exception e) {
            return CreateErrorMember(member, e);
        }

        MemberDeclarationSyntax CreateErrorMember(VBSyntax.StatementSyntax memberCausingError, Exception e)
        {
            var dummyClass
                = SyntaxFactory.ClassDeclaration("_failedMemberConversionMarker" + ++_failedMemberConversionMarkerCount);
            return dummyClass.WithCsTrailingErrorComment(memberCausingError, e);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitClassBlock(VBSyntax.ClassBlockSyntax node)
    {
        _accessedThroughMyClass = GetMyClassAccessedNames(node);
        var classStatement = node.ClassStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(classStatement.AttributeLists);
        var (parameters, constraints) = await SplitTypeParametersAsync(classStatement.TypeParameterList);
        var convertedIdentifier = CommonConversions.ConvertIdentifier(classStatement.Identifier);

        return SyntaxFactory.ClassDeclaration(
            attributes, ConvertTypeBlockModifiers(classStatement, TokenContext.Global),
            convertedIdentifier,
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            SyntaxFactory.List(await ConvertMembersAsync(node))
        );
    }

    private async Task<BaseListSyntax> ConvertInheritsAndImplementsAsync(SyntaxList<VBSyntax.InheritsStatementSyntax> inherits, SyntaxList<VBSyntax.ImplementsStatementSyntax> implements)
    {
        if (inherits.Count + implements.Count == 0)
            return null;
        var baseTypes = new List<BaseTypeSyntax>();
        foreach (var t in inherits.SelectMany(c => c.Types).Concat(implements.SelectMany(c => c.Types)))
            baseTypes.Add(SyntaxFactory.SimpleBaseType(await t.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor)));
        return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes));
    }

    public override async Task<CSharpSyntaxNode> VisitModuleBlock(VBSyntax.ModuleBlockSyntax node)
    {
        var stmt = node.ModuleStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(stmt.AttributeLists);
        var members = SyntaxFactory.List(await ConvertMembersAsync(node));
        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return SyntaxFactory.ClassDeclaration(
            attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule, CSSyntaxKind.StaticKeyword),
            CommonConversions.ConvertIdentifier(stmt.Identifier),
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            members
        );
    }

    public override async Task<CSharpSyntaxNode> VisitStructureBlock(VBSyntax.StructureBlockSyntax node)
    {
        var stmt = node.StructureStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(stmt.AttributeLists);
        var members = SyntaxFactory.List(await ConvertMembersAsync(node));

        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return SyntaxFactory.StructDeclaration(
            attributes, ConvertTypeBlockModifiers(stmt, TokenContext.Global), CommonConversions.ConvertIdentifier(stmt.Identifier),
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            members
        );
    }

    public override async Task<CSharpSyntaxNode> VisitInterfaceBlock(VBSyntax.InterfaceBlockSyntax node)
    {
        var stmt = node.InterfaceStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(stmt.AttributeLists);
        var members = SyntaxFactory.List(await ConvertMembersAsync(node));

        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return SyntaxFactory.InterfaceDeclaration(
            attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule), CommonConversions.ConvertIdentifier(stmt.Identifier),
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            members
        );
    }

    private SyntaxTokenList ConvertTypeBlockModifiers(VBSyntax.TypeStatementSyntax stmt,
        TokenContext interfaceOrModule, params Microsoft.CodeAnalysis.CSharp.SyntaxKind[] extraModifiers)
    {
        if (IsPartialType(stmt) && !HasPartialKeyword(stmt.Modifiers)) {
            extraModifiers = extraModifiers.Concat(new[] { CSSyntaxKind.PartialKeyword})
                .ToArray();
        }

        return CommonConversions.ConvertModifiers(stmt, stmt.Modifiers, interfaceOrModule, extraCsModifierKinds: extraModifiers);
    }

    private static bool HasPartialKeyword(SyntaxTokenList modifiers)
    {
        return modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.PartialKeyword));
    }

    private bool IsPartialType(VBSyntax.DeclarationStatementSyntax stmt)
    {
        return _semanticModel.GetDeclaredSymbol(stmt).IsPartialClassDefinition();
    }

    public override async Task<CSharpSyntaxNode> VisitEnumBlock(VBSyntax.EnumBlockSyntax node)
    {
        var stmt = node.EnumStatement;
        // we can cast to SimpleAsClause because other types make no sense as enum-type.
        var asClause = (VBSyntax.SimpleAsClauseSyntax)stmt.UnderlyingType;
        var attributes = await stmt.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        BaseListSyntax baseList = null;
        if (asClause != null) {
            baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(await asClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor))));
            if (asClause.AttributeLists.Count > 0) {
                var attributeLists = await asClause.AttributeLists.SelectManyAsync(l => CommonConversions.ConvertAttributeAsync(l));
                attributes = attributes.Concat(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(CSSyntaxKind.ReturnKeyword)),
                        SyntaxFactory.SeparatedList(attributeLists.SelectMany(a => a.Attributes)))
                ).ToArray();
            }
        }
        var members = SyntaxFactory.SeparatedList(await node.Members.SelectAsync(async m => await m.AcceptAsync<EnumMemberDeclarationSyntax>(TriviaConvertingDeclarationVisitor)));
        return SyntaxFactory.EnumDeclaration(
            SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(stmt, stmt.Modifiers), CommonConversions.ConvertIdentifier(stmt.Identifier),
            baseList,
            members
        );
    }

    public override async Task<CSharpSyntaxNode> VisitEnumMemberDeclaration(VBSyntax.EnumMemberDeclarationSyntax node)
    {
        var attributes = await CommonConversions.ConvertAttributesAsync(node.AttributeLists);
        return SyntaxFactory.EnumMemberDeclaration(
            attributes, CommonConversions.ConvertIdentifier(node.Identifier),
            await node.Initializer.AcceptAsync<EqualsValueClauseSyntax>(_triviaConvertingExpressionVisitor)
        );
    }

    public override async Task<CSharpSyntaxNode> VisitDelegateStatement(VBSyntax.DelegateStatementSyntax node)
    {
        var attributes = await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);

        var (typeParameters, constraints) = await SplitTypeParametersAsync(node.TypeParameterList);

        TypeSyntax returnType;
        var asClause = node.AsClause;
        if (asClause == null) {
            returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword));
        } else {
            returnType = await asClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor);
            if (asClause.AttributeLists.Count > 0) {
                var attributeListSyntaxs = await asClause.AttributeLists.SelectManyAsync(l => CommonConversions.ConvertAttributeAsync(l));
                attributes = attributes.Concat(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(CSSyntaxKind.ReturnKeyword)),
                        SyntaxFactory.SeparatedList(attributeListSyntaxs.SelectMany(a => a.Attributes)))
                ).ToArray();
            }
        }

        return SyntaxFactory.DelegateDeclaration(
            SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(node, node.Modifiers),
            returnType, CommonConversions.ConvertIdentifier(node.Identifier),
            typeParameters,
            await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor),
            constraints
        );
    }

    public override async Task<CSharpSyntaxNode> VisitFieldDeclaration(VBSyntax.FieldDeclarationSyntax node)
    {
        AdditionalLocals.PushScope();
        List<MemberDeclarationSyntax> declarations;
        try {
            declarations = await GetMemberDeclarationsAsync(node);
        } finally {
            AdditionalLocals.PopScope();
        }
        _additionalDeclarations.Add(node, declarations.Skip(1).ToArray());
        return declarations.First();
    }

    private async Task<List<MemberDeclarationSyntax>> GetMemberDeclarationsAsync(VBSyntax.FieldDeclarationSyntax node)
    {
        var attributes = (await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync)).ToList();
        var convertableModifiers =
            node.Modifiers.Where(m => !SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
        var isWithEvents = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
        var convertedModifiers =
            CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], convertableModifiers.ToList(), GetMemberContext(node));
        var declarations = new List<MemberDeclarationSyntax>(node.Declarators.Count);

        foreach (var declarator in node.Declarators)
        {
            var splitDeclarations = await CommonConversions.SplitVariableDeclarationsAsync(declarator, preferExplicitType: true);
            declarations.AddRange(CreateMemberDeclarations(splitDeclarations.Variables, isWithEvents, convertedModifiers, attributes));
            declarations.AddRange(splitDeclarations.Methods.Cast<MemberDeclarationSyntax>());
        }

        return declarations;
    }

    private IEnumerable<MemberDeclarationSyntax> CreateMemberDeclarations(IReadOnlyCollection<(VariableDeclarationSyntax Decl, ITypeSymbol Type)> splitDeclarationVariables,
        bool isWithEvents, SyntaxTokenList convertedModifiers, List<AttributeListSyntax> attributes)
    {

        foreach (var (decl, type) in splitDeclarationVariables)
        {
            var thisFieldModifiers = convertedModifiers;
            if (type?.SpecialType == SpecialType.System_DateTime) {
                var index = thisFieldModifiers.IndexOf(CSSyntaxKind.ConstKeyword);
                if (index >= 0) {
                    thisFieldModifiers = thisFieldModifiers.Replace(thisFieldModifiers[index], SyntaxFactory.Token(CSSyntaxKind.StaticKeyword));
                }
            }

            if (isWithEvents) {
                var fieldDecls = CreateWithEventsMembers(thisFieldModifiers, attributes, decl);
                foreach (var f in fieldDecls) yield return f;
            } else {
                foreach (var method in CreateExtraMethodMembers()) yield return method;

                if (AdditionalLocals.GetDeclarations().Any()) {
                    foreach (var additionalDecl in CreateAdditionalLocalMembers(thisFieldModifiers, attributes, decl)) {
                        yield return additionalDecl;
                    }
                } else {
                    yield return SyntaxFactory.FieldDeclaration(SyntaxFactory.List(attributes), thisFieldModifiers, decl);
                }

            }
        }
    }

    private IEnumerable<MemberDeclarationSyntax> CreateWithEventsMembers(SyntaxTokenList convertedModifiers, List<AttributeListSyntax> attributes, VariableDeclarationSyntax decl)
    {
        _extraUsingDirectives.Add("System.Runtime.CompilerServices");
        var initializers = decl.Variables
            .Where(a => a.Initializer != null)
            .ToDictionary(v => v.Identifier.Text, v => v.Initializer);
        var fieldDecl = decl.RemoveNodes(initializers.Values, SyntaxRemoveOptions.KeepNoTrivia);
        var initializerState = _typeContext.Initializers;
        var initializerCollection = convertedModifiers.Any(m => m.IsKind(CSSyntaxKind.StaticKeyword))
            ? initializerState.AdditionalStaticInitializers
            : initializerState.AdditionalInstanceInitializers;
        foreach (var initializer in initializers) {
            initializerCollection.Add(new Assignment(SyntaxFactory.IdentifierName(initializer.Key), CSSyntaxKind.SimpleAssignmentExpression, initializer.Value.Value));
        }

        var fieldDecls = _typeContext.HandledEventsAnalysis.GetDeclarationsForFieldBackedProperty(fieldDecl,
            convertedModifiers, SyntaxFactory.List(attributes));
        return fieldDecls;
    }

    private IEnumerable<MemberDeclarationSyntax> CreateAdditionalLocalMembers(SyntaxTokenList convertedModifiers, List<AttributeListSyntax> attributes, VariableDeclarationSyntax decl)
    {
        if (decl.Variables.Count > 1) {
            // Currently no way to tell which _additionalLocals would apply to which initializer
            throw new NotImplementedException(
                "Fields with multiple declarations and initializers with ByRef parameters not currently supported");
        }

        var v = decl.Variables.First();
        var invocations = v.Initializer.Value.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
        if (invocations.Length > 1) {
            throw new NotImplementedException(
                "Field initializers with nested method calls not currently supported");
        }

        var invocationExpressionSyntax = invocations.First();
        var methodName = invocationExpressionSyntax.Expression
            .ChildNodes().OfType<SimpleNameSyntax>().Last();
        var newMethodName = $"{methodName.Identifier.ValueText}_{v.Identifier.ValueText}";
        var declarationInfo = AdditionalLocals.GetDeclarations();

        var localVars = declarationInfo
            .Select(al => CommonConversions.CreateLocalVariableDeclarationAndAssignment(al.Prefix, al.Initializer))
            .ToArray<StatementSyntax>();

        // This should probably use a unique name like in MethodBodyVisitor - a collision is far less likely here
        var newNames = declarationInfo.ToDictionary(l => l.Id, l => l.Prefix);
        var newInitializer = PerScopeState.ReplaceNames(v.Initializer.Value, newNames);

        var body = SyntaxFactory.Block(localVars.Concat(SyntaxFactory.ReturnStatement(newInitializer).Yield()));
        // Method calls in initializers must be static in C# - Supporting this is #281
        var methodDecl = ValidSyntaxFactory.CreateParameterlessMethod(newMethodName, decl.Type, body);
        yield return methodDecl;

        var newVar =
            v.WithInitializer(SyntaxFactory.EqualsValueClause(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(newMethodName))));
        var newVarDecl =
            SyntaxFactory.VariableDeclaration(decl.Type, SyntaxFactory.SingletonSeparatedList(newVar));

        yield return SyntaxFactory.FieldDeclaration(SyntaxFactory.List(attributes), convertedModifiers, newVarDecl);
    }

    private IReadOnlyCollection<MemberDeclarationSyntax> CreateExtraMethodMembers()
    {
        var methodsInfos = AdditionalLocals.GetParameterlessFunctions();

        // In theory this should use a unique name like in MethodBodyVisitor
        // Unfortunately we can't change the name for these since the file containing the constructor of a partial class tries to call the method with exactly this name.
        // See GetInitialValueFunctionName
        var newMethodNames = methodsInfos.ToDictionary(l => l.Id, l => l.Prefix);
        for (int i = 0; i < _typeContext.Initializers.AdditionalInstanceInitializers.Count; i++) {
            var (a, b, initializer, _) = _typeContext.Initializers.AdditionalInstanceInitializers[i];
            _typeContext.Initializers.AdditionalInstanceInitializers[i] = new Assignment(a, b, PerScopeState.ReplaceNames(initializer, newMethodNames));
        }

        return methodsInfos
            .Select(al => al.AsInstanceMethod(newMethodNames[al.Id]))
            .ToArray();
    }

    private async Task<HandledEventsAnalysis> GetMethodWithHandlesAsync(VBSyntax.TypeBlockSyntax parentType, IMethodSymbol designerGeneratedInitializeComponentOrNull)
    {
        if (parentType == null || _semanticModel.GetDeclaredSymbol((SyntaxNode)parentType) is not INamedTypeSymbol containingType) {
            return new HandledEventsAnalysis(CommonConversions, null, Array.Empty<(HandledEventsAnalysis.EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] HandledMethods)>());
        }
        return await HandledEventsAnalyzer.AnalyzeAsync(CommonConversions, containingType, designerGeneratedInitializeComponentOrNull, _typeToInheritors);
    }

    public override async Task<CSharpSyntaxNode> VisitPropertyStatement(VBSyntax.PropertyStatementSyntax node)
    {
        var attributes = SyntaxFactory.List(await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync));
        var isReadonly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.ReadOnlyKeyword));
        var isWriteOnly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WriteOnlyKeyword));
        var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
        var modifiers = CommonConversions.ConvertModifiers(node, convertibleModifiers.ToList(), GetMemberContext(node));
        var isIndexer = CommonConversions.IsDefaultIndexer(node);
        var propSymbol = ModelExtensions.GetDeclaredSymbol(_semanticModel, node) as IPropertySymbol;
        var accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, propSymbol);

        var directlyConvertedCsIdentifier = CommonConversions.CsEscapedIdentifier(node.Identifier.Value as string);
        var additionalDeclarations = new List<MemberDeclarationSyntax>();

        var hasExplicitInterfaceImplementation = IsNonPublicInterfaceImplementation(propSymbol) || IsRenamedInterfaceMember(propSymbol, directlyConvertedCsIdentifier, propSymbol.ExplicitInterfaceImplementations);
        var additionalInterfaceImplements = propSymbol.ExplicitInterfaceImplementations;
        directlyConvertedCsIdentifier = hasExplicitInterfaceImplementation ? directlyConvertedCsIdentifier : CommonConversions.ConvertIdentifier(node.Identifier);

        var explicitInterfaceModifiers = modifiers.RemoveWhere(m => m.IsCsMemberVisibility() || m.IsKind(CSSyntaxKind.VirtualKeyword, CSSyntaxKind.AbstractKeyword, CSSyntaxKind.OverrideKeyword, CSSyntaxKind.NewKeyword));
        var shouldConvertToMethods = ShouldConvertAsParameterizedProperty(node);
        var (initializer, vbType) = await GetVbReturnTypeAsync(node);

        var rawType = await vbType.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor)
                      ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.ObjectKeyword));

        AccessorListSyntax accessors;
        if (node.Parent is VBSyntax.PropertyBlockSyntax propertyBlock) {
            if (shouldConvertToMethods) {
                if (accessedThroughMyClass) {
                    // Would need to create a delegating implementation to implement this
                    throw new NotImplementedException("MyClass indexing not implemented");
                }
                var methodDeclarationSyntaxs = await propertyBlock.Accessors.SelectAsync(async a =>
                    await a.AcceptAsync<MethodDeclarationSyntax>(TriviaConvertingDeclarationVisitor, a == propertyBlock.Accessors.First() ? SourceTriviaMapKind.All : SourceTriviaMapKind.None));
                var accessorMethods = methodDeclarationSyntaxs.Select(WithMergedModifiers).ToArray();

                if (hasExplicitInterfaceImplementation) {
                    accessorMethods
                        .Zip(propertyBlock.Accessors, Tuple.Create)
                        .Do(x => {
                            var (method, accessor) = x;
                            AddRemainingInterfaceDeclarations(method, attributes, explicitInterfaceModifiers, additionalInterfaceImplements, additionalDeclarations, accessor.Kind());
                        });
                }
                    
                _additionalDeclarations.Add(propertyBlock, accessorMethods.Skip(1).Concat(additionalDeclarations).ToArray());

                return accessorMethods[0];
            }

            var convertedAccessors = await propertyBlock.Accessors.SelectAsync(async a => 
                await a.AcceptAsync<AccessorDeclarationSyntax>(TriviaConvertingDeclarationVisitor));
            accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(convertedAccessors));

        } else if (shouldConvertToMethods && propSymbol.ContainingType.IsInterfaceType()) {
            var methodDeclarationSyntaxs = new List<MemberDeclarationSyntax>();

            if (propSymbol.GetMethod != null) {
                methodDeclarationSyntaxs.Add(await CreateMethodDeclarationSyntaxAsync(node.ParameterList, GetMethodId(node.Identifier.Text), false));
            }

            if (propSymbol.SetMethod != null) {
                var setMethod = await CreateMethodDeclarationSyntaxAsync(node.ParameterList, SetMethodId(node.Identifier.Text), true);
                setMethod = AddValueSetParameter(propSymbol, setMethod, rawType, hasExplicitInterfaceImplementation);
                methodDeclarationSyntaxs.Add(setMethod);
            }

            _additionalDeclarations.Add(node, methodDeclarationSyntaxs.Skip(1).ToArray());

            return methodDeclarationSyntaxs[0];
        } else {
            bool allowPrivateAccessorForDirectAccess = node.Modifiers.All(m => !m.IsKind(VBasic.SyntaxKind.MustOverrideKeyword, VBasic.SyntaxKind.OverridesKeyword)) && 
                                                       node.GetAncestor<VBSyntax.InterfaceBlockSyntax>() == null;
            accessors = ConvertSimpleAccessors(isWriteOnly, isReadonly, allowPrivateAccessorForDirectAccess, propSymbol.DeclaredAccessibility);
        }

        if (isIndexer) {
            if (accessedThroughMyClass) {
                // Not sure if this is possible
                throw new NotImplementedException("MyClass indexing not implemented");
            }

            var parameters = await node.ParameterList.Parameters.SelectAsync(async p => await p.AcceptAsync<ParameterSyntax>(_triviaConvertingExpressionVisitor));
            var parameterList = SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameters));
            return SyntaxFactory.IndexerDeclaration(
                SyntaxFactory.List(attributes),
                modifiers,
                rawType,
                null,
                parameterList,
                accessors
            );
        }

        if (hasExplicitInterfaceImplementation) {

            var delegatingAccessorList = GetDelegatingAccessorList(directlyConvertedCsIdentifier, accessors);
            foreach (var additionalInterface in additionalInterfaceImplements) {
                var explicitInterfaceAccessors = new SyntaxList<AccessorDeclarationSyntax>();
                if (additionalInterface.IsReadOnly)
                    explicitInterfaceAccessors = explicitInterfaceAccessors.Add(delegatingAccessorList.Single(t => t.IsKind(CSSyntaxKind.GetAccessorDeclaration)));
                else if (additionalInterface.IsWriteOnly)
                    explicitInterfaceAccessors = explicitInterfaceAccessors.Add(delegatingAccessorList.Single(t => t.IsKind(CSSyntaxKind.SetAccessorDeclaration)));
                else
                    explicitInterfaceAccessors = delegatingAccessorList;

                var interfaceDeclParams = new PropertyDeclarationParameters(attributes, explicitInterfaceModifiers, rawType, SyntaxFactory.AccessorList(explicitInterfaceAccessors));
                AddInterfaceMemberDeclarations(additionalInterface, additionalDeclarations, interfaceDeclParams);
            }
        }

        if (accessedThroughMyClass) {

            var realModifiers = modifiers.RemoveWhere(m => m.IsKind(CSSyntaxKind.PrivateKeyword));
            string csIdentifierName = AddRealPropertyDelegatingToMyClassVersion(additionalDeclarations, directlyConvertedCsIdentifier, attributes, realModifiers, rawType, isReadonly, isWriteOnly);
            modifiers = modifiers.Remove(modifiers.Single(m => m.IsKind(CSSyntaxKind.VirtualKeyword)));
            directlyConvertedCsIdentifier = SyntaxFactory.Identifier(csIdentifierName);
        }

        if (additionalDeclarations.Any()) {
            var declNode = (VBSyntax.StatementSyntax)node.FirstAncestorOrSelf<VBSyntax.PropertyBlockSyntax>() ?? node;
            _additionalDeclarations.Add(declNode, additionalDeclarations.ToArray());
        }

        var semicolonToken = SyntaxFactory.Token(initializer == null ? CSSyntaxKind.None : CSSyntaxKind.SemicolonToken);
        return SyntaxFactory.PropertyDeclaration(
            attributes,
            modifiers,
            rawType,
            explicitInterfaceSpecifier: null,
            directlyConvertedCsIdentifier, 
            accessors,
            null,
            initializer,
            semicolonToken);

        MethodDeclarationSyntax WithMergedModifiers(MethodDeclarationSyntax member)
        {
            SyntaxTokenList originalModifiers = member.GetModifiers();
            var hasVisibility = originalModifiers.Any(m => m.IsCsMemberVisibility());
            var modifiersToAdd = hasVisibility ? modifiers.Where(m => !m.IsCsMemberVisibility()) : modifiers;
            var newModifiers = SyntaxFactory.TokenList(originalModifiers.Concat(modifiersToAdd));
            return member.WithModifiers(newModifiers);
        }

        async Task<MethodDeclarationSyntax> CreateMethodDeclarationSyntaxAsync(VBSyntax.ParameterListSyntax containingPropParameterList, string methodId, bool voidReturn)
        {
            var parameterListSyntax = await containingPropParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor);
            var methodModifiers = SyntaxFactory.TokenList(modifiers.Where(m => !m.IsCsVisibility(false, false)));
            MethodDeclarationSyntax methodDeclarationSyntax = SyntaxFactory.MethodDeclaration(attributes, methodModifiers,
                    voidReturn ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword)) : rawType,
                    null,
                    SyntaxFactory.Identifier(methodId), null,
                    parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), null, null)
                .WithSemicolonToken(SyntaxFactory.Token(CSSyntaxKind.SemicolonToken));
            return methodDeclarationSyntax;
        }
    }

    private void AddRemainingInterfaceDeclarations(MethodDeclarationSyntax method, SyntaxList<AttributeListSyntax> attributes,
        SyntaxTokenList filteredModifiers, IEnumerable<IPropertySymbol> additionalInterfaceImplements,
        ICollection<MemberDeclarationSyntax> additionalDeclarations, VBasic.SyntaxKind accessorKind)
    {
        var clause = GetDelegatingClause(method.Identifier, method.ParameterList, false);

        additionalInterfaceImplements.Do(interfaceImplement => {
            var isGetterMethodForParametrizedProperty = accessorKind == VBasic.SyntaxKind.GetAccessorBlock;

            if (interfaceImplement.IsReadOnly && !isGetterMethodForParametrizedProperty)
                return;
            if (interfaceImplement.IsWriteOnly && isGetterMethodForParametrizedProperty)
                return;

            var identifier = SyntaxFactory.Identifier(isGetterMethodForParametrizedProperty ? 
                GetMethodId(interfaceImplement.Name) : 
                SetMethodId(interfaceImplement.Name));
            var interfaceMethodDeclParams = new MethodDeclarationParameters(attributes, filteredModifiers,
                method.ReturnType, method.TypeParameterList, MakeOptionalParametersRequired(method.ParameterList), method.ConstraintClauses, clause, identifier);

            AddInterfaceMemberDeclarations(interfaceImplement, additionalDeclarations, interfaceMethodDeclParams);
        });
    }

    private async Task<(EqualsValueClauseSyntax Initializer, VBSyntax.TypeSyntax VbType)> GetVbReturnTypeAsync(VBSyntax.PropertyStatementSyntax node)
    {
        var initializer = await node.Initializer.AcceptAsync<EqualsValueClauseSyntax>(_triviaConvertingExpressionVisitor);
        VBSyntax.TypeSyntax vbType;
        switch (node.AsClause)
        {
            case VBSyntax.SimpleAsClauseSyntax c:
                vbType = c.Type;
                break;
            case VBSyntax.AsNewClauseSyntax c:
                initializer = SyntaxFactory.EqualsValueClause(
                    await c.NewExpression.AcceptAsync<ExpressionSyntax>(_triviaConvertingExpressionVisitor));
                vbType = VBasic.SyntaxExtensions.Type(c.NewExpression);
                break;
            case null:
                vbType = null;
                break;
            default:
                throw new NotImplementedException($"{node.AsClause.GetType().FullName} not implemented!");
        }

        return (initializer, vbType);
    }

    private static SyntaxList<AccessorDeclarationSyntax> GetDelegatingAccessorList(SyntaxToken csIdentifier, AccessorListSyntax accessors)
    {
        var getArrowClause = GetDelegatingClause(csIdentifier, null, false);
        var setArrowClause = GetDelegatingClause(csIdentifier, null, true);

        var getSetDict = new Dictionary<CSSyntaxKind, ArrowExpressionClauseSyntax> {
            {CSSyntaxKind.GetAccessorDeclaration, getArrowClause},
            {CSSyntaxKind.SetAccessorDeclaration, setArrowClause}
        };

        var delegatingAccessors = accessors.Accessors.Select(a => {
            var attributes = a.AttributeLists;
            var modifiers = a.Modifiers;

            var delegatingAccessor = SyntaxFactory.AccessorDeclaration(a.Kind(),
                attributes, modifiers, getSetDict[a.Kind()]).WithSemicolonToken(SemicolonToken);

            return delegatingAccessor;
        });

        return new SyntaxList<AccessorDeclarationSyntax>(delegatingAccessors);
    }

    private static string AddRealPropertyDelegatingToMyClassVersion(List<MemberDeclarationSyntax> additionalDeclarations, SyntaxToken csIdentifier,
        SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers, TypeSyntax rawType, bool readOnly, bool writeOnly)
    {
        var csIdentifierName = "MyClass" + csIdentifier.ValueText;
        ExpressionSyntax thisDotIdentifier = GetSimpleMemberAccess(SyntaxFactory.Identifier(csIdentifierName));

        var accessors = SyntaxFactory.List(Array.Empty<AccessorDeclarationSyntax>());
        if (readOnly || !writeOnly) {
            var getReturn = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(thisDotIdentifier));
            var getAccessor = SyntaxFactory.AccessorDeclaration(CSSyntaxKind.GetAccessorDeclaration, getReturn);
            accessors = accessors.Add(getAccessor);
        }

        if (writeOnly || !readOnly) {
            var setValue = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(CSSyntaxKind.SimpleAssignmentExpression, thisDotIdentifier,
                    SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier("value")))));
            var setAccessor = SyntaxFactory.AccessorDeclaration(CSSyntaxKind.SetAccessorDeclaration, setValue);
            accessors = accessors.Add(setAccessor);
        }

        var realAccessors = SyntaxFactory.AccessorList(accessors);
        var realDecl = SyntaxFactory.PropertyDeclaration(
            attributes,
            modifiers,
            rawType,
            null,
            csIdentifier, realAccessors,
            null,
            null,
            SyntaxFactory.Token(CSSyntaxKind.None));

        additionalDeclarations.Add(realDecl);
        return csIdentifierName;
    }

    private static AccessorListSyntax ConvertSimpleAccessors(bool isWriteOnly, bool isReadonly,
        bool allowPrivateAccessorForDirectAccess, Accessibility declaredAccessibility)
    {
        var getAccessor = SyntaxFactory.AccessorDeclaration(CSSyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(SemicolonToken);
        var setAccessor = SyntaxFactory.AccessorDeclaration(CSSyntaxKind.SetAccessorDeclaration)
            .WithSemicolonToken(SemicolonToken);

        if (isWriteOnly && declaredAccessibility != Accessibility.Private) {
            getAccessor = getAccessor.AddModifiers(SyntaxFactory.Token(CSSyntaxKind.PrivateKeyword));
        }

        if (isReadonly && declaredAccessibility != Accessibility.Private) {
            setAccessor = setAccessor.AddModifiers(SyntaxFactory.Token(CSSyntaxKind.PrivateKeyword));
        }

        // this could be improved by looking if there is actually a direct access somewhere
        // if not we could skip generating private property accessor
        var isReadOnlyInterface = !allowPrivateAccessorForDirectAccess && isReadonly;
        var isWriteOnlyInterface = !allowPrivateAccessorForDirectAccess && isWriteOnly;

        if (isReadOnlyInterface)
            return SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getAccessor }));
        if (isWriteOnlyInterface)
            return SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { setAccessor }));

        return SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getAccessor, setAccessor }));
    }

    public override async Task<CSharpSyntaxNode> VisitPropertyBlock(VBSyntax.PropertyBlockSyntax node)
    {
        return await node.PropertyStatement.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.SubNodesOnly);
    }

    public override async Task<CSharpSyntaxNode> VisitAccessorBlock(VBSyntax.AccessorBlockSyntax node)
    {
        CSSyntaxKind blockKind;
        bool isIterator = node.IsIterator();
        var ancestoryPropertyBlock = node.GetAncestor<VBSyntax.PropertyBlockSyntax>();
        var containingPropertyStmt = ancestoryPropertyBlock?.PropertyStatement;
        var csReturnVariableOrNull = CommonConversions.GetRetVariableNameOrNull(node);
        var convertedStatements = await ConvertStatementsAsync(node.Statements, await CreateMethodBodyVisitorAsync(node, isIterator));
        var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);
        var attributes = await CommonConversions.ConvertAttributesAsync(node.AccessorStatement.AttributeLists);
        var modifiers = CommonConversions.ConvertModifiers(node, node.AccessorStatement.Modifiers, TokenContext.Local);
        var declaredPropSymbol = containingPropertyStmt != null ? _semanticModel.GetDeclaredSymbol(containingPropertyStmt) : null;

        string potentialMethodId;
        var sourceMap = ancestoryPropertyBlock?.Accessors.FirstOrDefault() == node ? SourceTriviaMapKind.All : SourceTriviaMapKind.None;
        var returnType = containingPropertyStmt?.AsClause is VBSyntax.SimpleAsClauseSyntax asClause ?
            await asClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor, sourceMap) :
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword));

        switch (node.Kind()) {
            case VBasic.SyntaxKind.GetAccessorBlock:
                blockKind = CSSyntaxKind.GetAccessorDeclaration;
                potentialMethodId = GetMethodId(containingPropertyStmt.Identifier.Text);

                if (ShouldConvertAsParameterizedProperty(containingPropertyStmt)) {
                    var method = await CreateMethodDeclarationSyntax(containingPropertyStmt.ParameterList, false);
                    return method;
                }
                break;
            case VBasic.SyntaxKind.SetAccessorBlock:
                blockKind = CSSyntaxKind.SetAccessorDeclaration;
                potentialMethodId = SetMethodId(containingPropertyStmt.Identifier.Text);

                if (ShouldConvertAsParameterizedProperty(containingPropertyStmt)) {
                    var setMethod = await CreateMethodDeclarationSyntax(containingPropertyStmt.ParameterList, true);
                    return AddValueSetParameter(declaredPropSymbol, setMethod, returnType, false);
                }
                break;
            case VBasic.SyntaxKind.AddHandlerAccessorBlock:
                blockKind = CSSyntaxKind.AddAccessorDeclaration;
                break;
            case VBasic.SyntaxKind.RemoveHandlerAccessorBlock:
                blockKind = CSSyntaxKind.RemoveAccessorDeclaration;
                break;
            case VBasic.SyntaxKind.RaiseEventAccessorBlock:
                var eventStatement = ((VBSyntax.EventBlockSyntax)node.Parent).EventStatement;
                var eventName = CommonConversions.ConvertIdentifier(eventStatement.Identifier).ValueText;
                potentialMethodId = $"On{eventName}";
                return await CreateMethodDeclarationSyntax(node.AccessorStatement.ParameterList, true);
            default:
                throw new NotSupportedException(node.Kind().ToString());
        }

        return SyntaxFactory.AccessorDeclaration(blockKind, attributes, modifiers, body);

        async Task<MethodDeclarationSyntax> CreateMethodDeclarationSyntax(VBSyntax.ParameterListSyntax containingPropParameterList, bool voidReturn)
        {
            var parameterListSyntax = await containingPropParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor, sourceMap);

            MethodDeclarationSyntax methodDeclarationSyntax = SyntaxFactory.MethodDeclaration(attributes, modifiers,
                voidReturn ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword)) : returnType,
                explicitInterfaceSpecifier: null,
                SyntaxFactory.Identifier(potentialMethodId), null,
                parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), body, null);
            return methodDeclarationSyntax;
        }
    }

    private static MethodDeclarationSyntax AddValueSetParameter(IPropertySymbol declaredPropSymbol,
        MethodDeclarationSyntax setMethod, TypeSyntax returnType, bool hasExplicitInterfaceImplementation)
    {
        var valueParam = SyntaxFactory.Parameter(CommonConversions.CsEscapedIdentifier("value")).WithType(returnType);
        if ((declaredPropSymbol?.Parameters.Any(p => p.IsOptional) ?? false) && !hasExplicitInterfaceImplementation) valueParam = valueParam.WithDefault(SyntaxFactory.EqualsValueClause(ValidSyntaxFactory.DefaultExpression));
        return setMethod.AddParameterListParameters(valueParam);
    }

    private static string SetMethodId(string methodName) => $"set_{methodName}";

    private static string GetMethodId(string methodName) => $"get_{methodName}";

    private static bool ShouldConvertAsParameterizedProperty(VBSyntax.PropertyStatementSyntax propStmt)
    {
        return propStmt.ParameterList?.Parameters.Any() == true
               && !CommonConversions.IsDefaultIndexer(propStmt);
    }

    public override async Task<CSharpSyntaxNode> VisitAccessorStatement(VBSyntax.AccessorStatementSyntax node)
    {
        return SyntaxFactory.AccessorDeclaration(node.Kind().ConvertToken(), null);
    }

    public override async Task<CSharpSyntaxNode> VisitMethodBlock(VBSyntax.MethodBlockSyntax node)
    {
        var methodBlock = await node.SubOrFunctionStatement.AcceptAsync<BaseMethodDeclarationSyntax>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.SubNodesOnly);

        var declaredSymbol = ModelExtensions.GetDeclaredSymbol(_semanticModel, node);
        if (!declaredSymbol.CanHaveMethodBody()) {
            return methodBlock;
        }
        var csReturnVariableOrNull = CommonConversions.GetRetVariableNameOrNull(node);
        var visualBasicSyntaxVisitor = await CreateMethodBodyVisitorAsync(node, node.IsIterator(), csReturnVariableOrNull);
        var convertedStatements = await ConvertStatementsAsync(node.Statements, visualBasicSyntaxVisitor);

        //  Just class events - for property events, see other use of IsDesignerGeneratedTypeWithInitializeComponent
        if (node.SubOrFunctionStatement.Identifier.Text == "InitializeComponent" && node.SubOrFunctionStatement.IsKind(VBasic.SyntaxKind.SubStatement) && declaredSymbol.ContainingType.GetDesignerGeneratedInitializeComponentOrNull(_vbCompilation) != null) {
            var firstResumeLayout = convertedStatements.Statements.FirstOrDefault(IsThisResumeLayoutInvocation) ?? convertedStatements.Statements.Last();
            convertedStatements = convertedStatements.InsertNodesBefore(firstResumeLayout, _typeContext.HandledEventsAnalysis.GetInitializeComponentClassEventHandlers());
        }

        var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);

        return methodBlock.WithBody(body);
    }

    private static bool IsThisResumeLayoutInvocation(StatementSyntax s)
    {
        return s is ExpressionStatementSyntax ess && ess.Expression is InvocationExpressionSyntax ies && ies.Expression.ToString().Equals("this.ResumeLayout", StringComparison.Ordinal);
    }

    private BlockSyntax WithImplicitReturnStatements(VBSyntax.MethodBlockBaseSyntax node, BlockSyntax convertedStatements,
        IdentifierNameSyntax csReturnVariableOrNull)
    {
        if (!node.MustReturn()) return convertedStatements;
        if (_semanticModel.GetDeclaredSymbol(node) is { } ms && ms.ReturnsVoidOrAsyncTask()) {
            return convertedStatements;
        }


        var preBodyStatements = new List<StatementSyntax>();
        var postBodyStatements = new List<StatementSyntax>();

        var functionSym = ModelExtensions.GetDeclaredSymbol(_semanticModel, node);
        if (functionSym != null) {
            var returnType = CommonConversions.GetTypeSyntax(functionSym.GetReturnType());

            if (csReturnVariableOrNull != null) {
                var retDeclaration = CommonConversions.CreateVariableDeclarationAndAssignment(
                    csReturnVariableOrNull.Identifier.ValueText, SyntaxFactory.DefaultExpression(returnType),
                    returnType);
                preBodyStatements.Add(SyntaxFactory.LocalDeclarationStatement(retDeclaration));
            }

            ControlFlowAnalysis controlFlowAnalysis = null;
            if (!node.Statements.IsEmpty())
                controlFlowAnalysis =
                    ModelExtensions.AnalyzeControlFlow(_semanticModel, node.Statements.First(), node.Statements.Last());

            bool mayNeedReturn = controlFlowAnalysis?.EndPointIsReachable != false;
            if (mayNeedReturn) {
                var csReturnExpression = csReturnVariableOrNull ??
                                         (ExpressionSyntax)SyntaxFactory.DefaultExpression(returnType);
                postBodyStatements.Add(SyntaxFactory.ReturnStatement(csReturnExpression));
            }
        }

        var statements = preBodyStatements
            .Concat(convertedStatements.Statements)
            .Concat(postBodyStatements);

        return SyntaxFactory.Block(statements);
    }

    private static async Task<BlockSyntax> ConvertStatementsAsync(SyntaxList<VBSyntax.StatementSyntax> statements, VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> methodBodyVisitor)
    {
        return SyntaxFactory.Block(await statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor)));
    }

    private bool IsAccessedThroughMyClass(SyntaxNode node, SyntaxToken identifier, ISymbol symbolOrNull)
    {
        bool accessedThroughMyClass = false;
        if (symbolOrNull != null && symbolOrNull.IsVirtual && !symbolOrNull.IsAbstract) {
            var classBlock = node.Ancestors().OfType<VBSyntax.ClassBlockSyntax>().FirstOrDefault();
            if (classBlock != null) {
                accessedThroughMyClass = _accessedThroughMyClass.Contains(identifier.Text);
            }
        }

        return accessedThroughMyClass;
    }

    private static HashSet<string> GetMyClassAccessedNames(VBSyntax.ClassBlockSyntax classBlock)
    {
        var memberAccesses = classBlock.DescendantNodes().OfType<VBSyntax.MemberAccessExpressionSyntax>();
        var accessedTextNames = new HashSet<string>(memberAccesses
            .Where(mae => mae.Expression is VBSyntax.MyClassExpressionSyntax)
            .Select(mae => mae.Name.Identifier.Text), StringComparer.OrdinalIgnoreCase);
        return accessedTextNames;
    }

    public override async Task<CSharpSyntaxNode> VisitMethodStatement(VBSyntax.MethodStatementSyntax node)
    {
        var attributes = await CommonConversions.ConvertAttributesAsync(node.AttributeLists);
        bool hasBody = node.Parent is VBSyntax.MethodBlockBaseSyntax;

        if ("Finalize".Equals(node.Identifier.ValueText, StringComparison.OrdinalIgnoreCase) && 
            node.Modifiers.Any(m => VBasic.VisualBasicExtensions.Kind(m) == VBasic.SyntaxKind.OverridesKeyword)) 
        {
            var declaration = SyntaxFactory.
                DestructorDeclaration(CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier)).
                WithAttributeLists(attributes);
            return hasBody ? declaration : declaration.WithSemicolonToken(SemicolonToken);
        } 
            
        var tokenContext = GetMemberContext(node);
        var declaredSymbol = (IMethodSymbol)ModelExtensions.GetDeclaredSymbol(_semanticModel, node);
        var extraCsModifierKinds = declaredSymbol?.IsExtern == true ? new[] { CSSyntaxKind.ExternKeyword } : Array.Empty<CSSyntaxKind>();
        var convertedModifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, tokenContext, extraCsModifierKinds: extraCsModifierKinds);

        bool accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, declaredSymbol);

        if (declaredSymbol.IsPartialMethodImplementation() || declaredSymbol.IsPartialMethodDefinition()) 
        {
            var privateModifier = convertedModifiers.SingleOrDefault(m => m.IsKind(CSSyntaxKind.PrivateKeyword));
            if (privateModifier != default) {
                convertedModifiers = convertedModifiers.Remove(privateModifier);
            }
            if (!HasPartialKeyword(node.Modifiers)) {
                convertedModifiers = convertedModifiers.Add(SyntaxFactory.Token(CSSyntaxKind.PartialKeyword));
            }
        }
        var (typeParameters, constraints) = await SplitTypeParametersAsync(node.TypeParameterList);

        var returnType = (declaredSymbol != null ? CommonConversions.GetTypeSyntax(declaredSymbol.ReturnType) :
            await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword)));

        var directlyConvertedCsIdentifier = CommonConversions.CsEscapedIdentifier(node.Identifier.Value as string);
        var parameterList = await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ParameterList();
        var additionalDeclarations = new List<MemberDeclarationSyntax>();

        var hasExplicitInterfaceImplementation = IsNonPublicInterfaceImplementation(declaredSymbol) || IsRenamedInterfaceMember(declaredSymbol, directlyConvertedCsIdentifier, declaredSymbol.ExplicitInterfaceImplementations);
        directlyConvertedCsIdentifier = hasExplicitInterfaceImplementation ? directlyConvertedCsIdentifier : CommonConversions.ConvertIdentifier(node.Identifier);

        if (hasExplicitInterfaceImplementation) {

            var requiredParameterList = MakeOptionalParametersRequired(parameterList);
            var delegatingClause = GetDelegatingClause(directlyConvertedCsIdentifier, requiredParameterList, false);
            var explicitInterfaceModifiers = convertedModifiers.RemoveWhere(m => m.IsCsMemberVisibility() || m.IsKind(CSSyntaxKind.VirtualKeyword, CSSyntaxKind.AbstractKeyword, CSSyntaxKind.OverrideKeyword, CSSyntaxKind.NewKeyword));

            var interfaceDeclParams = new MethodDeclarationParameters(attributes, explicitInterfaceModifiers, returnType, typeParameters, requiredParameterList, constraints, delegatingClause);
            AddInterfaceMemberDeclarations(declaredSymbol.ExplicitInterfaceImplementations, additionalDeclarations, interfaceDeclParams);
        }

        // If the method is virtual, and there is a MyClass.SomeMethod() call,
        // we need to emit a non-virtual method for it to call
        if (accessedThroughMyClass) {
            var identifierName = "MyClass" + directlyConvertedCsIdentifier.ValueText;
            var arrowClause = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(identifierName), CreateDelegatingArgList(parameterList)));
            var declModifiers = convertedModifiers;

            var originalNameDecl = SyntaxFactory.MethodDeclaration(
                attributes,
                declModifiers,
                returnType,
                null,
                directlyConvertedCsIdentifier,
                typeParameters,
                parameterList,
                constraints,
                null,
                arrowClause,
                SyntaxFactory.Token(CSSyntaxKind.SemicolonToken)
            );

            additionalDeclarations.Add(originalNameDecl);
            convertedModifiers = convertedModifiers.Remove(convertedModifiers.Single(m => m.IsKind(CSSyntaxKind.VirtualKeyword)));
            directlyConvertedCsIdentifier = SyntaxFactory.Identifier(identifierName);
        }

        if (additionalDeclarations.Any()) {
            var declNode = (VBSyntax.StatementSyntax)node.FirstAncestorOrSelf<VBSyntax.MethodBlockBaseSyntax>() ?? node;
            _additionalDeclarations.Add(declNode, additionalDeclarations.ToArray());
        }

        var decl = SyntaxFactory.MethodDeclaration(
            attributes,
            convertedModifiers,
            returnType,
            null,
            directlyConvertedCsIdentifier,
            typeParameters,
            parameterList,
            constraints,
            null,//Body added by surrounding method block if appropriate
            null
        );

        return hasBody && declaredSymbol.CanHaveMethodBody() ? decl : decl.WithSemicolonToken(SemicolonToken);
    }

    private void AddInterfaceMemberDeclarations(ISymbol interfaceImplement,
        ICollection<MemberDeclarationSyntax> additionalDeclarations,
        DeclarationParameters declParams)
    {
        var semicolonToken = SyntaxFactory.Token(CSSyntaxKind.SemicolonToken);
        Func<ExplicitInterfaceSpecifierSyntax, SyntaxToken, MemberDeclarationSyntax>
            declDelegate = declParams switch {
                MethodDeclarationParameters methodParams => (explintfspec, identifier)
                    => SyntaxFactory.MethodDeclaration(methodParams.Attributes, methodParams.Modifiers,
                        methodParams.ReturnType, explintfspec, identifier
                        , methodParams.TypeParameters, methodParams.ParameterList, methodParams.Constraints, null,
                        methodParams.ArrowClause, semicolonToken).WithoutSourceMapping(),

                PropertyDeclarationParameters propertyParams => (explintfspec, identifier)
                    => SyntaxFactory.PropertyDeclaration(propertyParams.Attributes, propertyParams.Modifiers,
                        propertyParams.ReturnType, explintfspec, identifier, propertyParams.Accessors,
                        null, null).NormalizeWhitespace(),

                _ => throw new ArgumentOutOfRangeException(nameof(declParams), declParams, null)
            };

        AddMemberDeclaration(additionalDeclarations, interfaceImplement, declParams.Identifier, declDelegate);
    }

    private void AddInterfaceMemberDeclarations(IEnumerable<ISymbol> additionalInterfaceImplements,
        ICollection<MemberDeclarationSyntax> additionalDeclarations,
        DeclarationParameters declParams)
    {
        additionalInterfaceImplements.Do(interfaceImplement => AddInterfaceMemberDeclarations(interfaceImplement, additionalDeclarations, declParams));
    }

    private void AddMemberDeclaration(ICollection<MemberDeclarationSyntax> additionalDeclarations,
        ISymbol interfaceImplement, SyntaxToken identifier, Func<ExplicitInterfaceSpecifierSyntax, SyntaxToken, MemberDeclarationSyntax> declDelegate)
    {
        var explicitInterfaceName = CommonConversions.GetFullyQualifiedNameSyntax(interfaceImplement.ContainingType);
        var newExplicitInterfaceSpecifier = SyntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceName);
        var interfaceImplIdentifier = identifier == default
            ? SyntaxFactory.Identifier(interfaceImplement.Name)
            : identifier;

        var declaration = declDelegate.Invoke(newExplicitInterfaceSpecifier, interfaceImplIdentifier);
        additionalDeclarations.Add(declaration);
    }


    private static ParameterListSyntax MakeOptionalParametersRequired(ParameterListSyntax parameterList)
    {
        if (parameterList == null) return null;

        var nonOptionalParameters = parameterList.Parameters.Select(ConvertOptionalParameter);

        var separatedSyntaxList = SyntaxFactory.SeparatedList(nonOptionalParameters);
        var newParameterList = parameterList.WithParameters(separatedSyntaxList);
        return newParameterList;
    }

    private static ParameterSyntax ConvertOptionalParameter(ParameterSyntax parameter)
    {
        var optionalAttributes = new List<string> { nameof(OptionalAttribute).GetAttributeIdentifier(),
            nameof(DefaultParameterValueAttribute).GetAttributeIdentifier() };

        var attrListsToRemove = parameter.AttributeLists.SingleOrDefault(aList => aList.Attributes
            .All(a =>
            {
                string attrIdentifier = string.Empty;

                if (a.Name is IdentifierNameSyntax identifierNameSyntax) {
                    attrIdentifier = identifierNameSyntax.Identifier.Text;
                } else if (a.Name is AliasQualifiedNameSyntax aliasQualifiedNameSyntax) {
                    attrIdentifier = aliasQualifiedNameSyntax.Alias.Identifier.Text;
                }

                return optionalAttributes.Contains(attrIdentifier);
            }));

        var newAttrLists = parameter.AttributeLists.Remove(attrListsToRemove);
        return parameter.WithDefault(null).WithAttributeLists(newAttrLists);
    }

    private static ArrowExpressionClauseSyntax GetDelegatingClause(SyntaxToken csIdentifier,
        ParameterListSyntax parameterList, bool isSetAccessor)
    {
        if (parameterList != null && isSetAccessor)
            throw new InvalidOperationException("Parameterized setters shouldn't have a delegating clause. " +
                                                $"\r\nInvalid arguments: {nameof(isSetAccessor)} = {true}," +
                                                $" {nameof(parameterList)} has {parameterList.Parameters.Count} parameters");

        var simpleMemberAccess = GetSimpleMemberAccess(csIdentifier);

        var expression = parameterList != null
            ? (ExpressionSyntax)SyntaxFactory.InvocationExpression(simpleMemberAccess, CreateDelegatingArgList(parameterList))
            : simpleMemberAccess;

        var arrowClauseExpression = isSetAccessor
            ? SyntaxFactory.AssignmentExpression(CSSyntaxKind.SimpleAssignmentExpression, simpleMemberAccess,
                SyntaxFactory.IdentifierName("value"))
            : expression;

        var arrowClause = SyntaxFactory.ArrowExpressionClause(arrowClauseExpression);
        return arrowClause;
    }

    private static MemberAccessExpressionSyntax GetSimpleMemberAccess(SyntaxToken csIdentifier)
    {
        var simpleMemberAccess = SyntaxFactory.MemberAccessExpression(
            CSSyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
            SyntaxFactory.Token(CSSyntaxKind.DotToken), SyntaxFactory.IdentifierName(csIdentifier));

        return simpleMemberAccess;
    }

    private static bool IsNonPublicInterfaceImplementation(ISymbol declaredSymbol)
    {
        return declaredSymbol switch {
            IMethodSymbol methodSymbol => methodSymbol.DeclaredAccessibility != Accessibility.Public &&
                                          methodSymbol.ExplicitInterfaceImplementations.Any(),
            IPropertySymbol propertySymbol => propertySymbol.DeclaredAccessibility != Accessibility.Public &&
                                              propertySymbol.ExplicitInterfaceImplementations.Any(),
            _ => throw new ArgumentOutOfRangeException(nameof(declaredSymbol))
        };
    }

    private static bool IsRenamedInterfaceMember(ISymbol declaredSymbol,
        SyntaxToken directlyConvertedCsIdentifier, IEnumerable<ISymbol> explicitInterfaceImplementations)
    {
        bool IsRenamed(ISymbol csIdentifier) =>
            declaredSymbol switch {
                IMethodSymbol methodSymbol => !StringComparer.OrdinalIgnoreCase.Equals(directlyConvertedCsIdentifier.Value, csIdentifier.Name) && methodSymbol.ExplicitInterfaceImplementations.Any(),
                IPropertySymbol propertySymbol => !StringComparer.OrdinalIgnoreCase.Equals(directlyConvertedCsIdentifier.Value, csIdentifier.Name) && propertySymbol.ExplicitInterfaceImplementations.Any(),
                _ => throw new ArgumentOutOfRangeException(nameof(declaredSymbol))
            };

        return explicitInterfaceImplementations.Any(IsRenamed);
    }

    private static ArgumentListSyntax CreateDelegatingArgList(ParameterListSyntax parameterList)
    {
        var refKinds = parameterList.Parameters.Select(GetSingleModifier).ToArray();
        return parameterList.Parameters.Select(p => SyntaxFactory.IdentifierName(p.Identifier)).CreateCsArgList(refKinds);
    }

    private static CSSyntaxKind? GetSingleModifier(ParameterSyntax p)
    {
        var argKinds = new CSSyntaxKind?[] { CSSyntaxKind.RefKeyword, CSSyntaxKind.OutKeyword, CSSyntaxKind.InKeyword };
        return p.Modifiers.Select(Microsoft.CodeAnalysis.CSharp.CSharpExtensions.Kind)
            .Select<CSSyntaxKind, CSSyntaxKind?>(k => k)
            .FirstOrDefault(argKinds.Contains);
    }

    private static TokenContext GetMemberContext(VBSyntax.StatementSyntax member)
    {
        var parentType = member.GetAncestorOrThis<VBSyntax.TypeBlockSyntax>();
        var parentTypeKind = parentType?.Kind();
        switch (parentTypeKind) {
            case VBasic.SyntaxKind.ModuleBlock:
                return TokenContext.MemberInModule;
            case VBasic.SyntaxKind.ClassBlock:
                return TokenContext.MemberInClass;
            case VBasic.SyntaxKind.InterfaceBlock:
                return TokenContext.MemberInInterface;
            case VBasic.SyntaxKind.StructureBlock:
                return TokenContext.MemberInStruct;
            default:
                throw new ArgumentOutOfRangeException(nameof(member));
        }
    }

    public override async Task<CSharpSyntaxNode> VisitEventBlock(VBSyntax.EventBlockSyntax node)
    {
        var block = node.EventStatement;
        var attributes = await block.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node));

        var rawType = await (block.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? ValidSyntaxFactory.VarType;

        var convertedAccessors = await node.Accessors.SelectAsync(async a => await a.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingDeclarationVisitor));
        _additionalDeclarations.Add(node, convertedAccessors.OfType<MemberDeclarationSyntax>().ToArray());
        return SyntaxFactory.EventDeclaration(
            SyntaxFactory.List(attributes),
            modifiers,
            rawType,
            null, CommonConversions.ConvertIdentifier(block.Identifier),
            SyntaxFactory.AccessorList(SyntaxFactory.List(convertedAccessors.OfType<AccessorDeclarationSyntax>()))
        );
    }

    public override async Task<CSharpSyntaxNode> VisitEventStatement(VBSyntax.EventStatementSyntax node)
    {
        var attributes = await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, GetMemberContext(node));
        var id = CommonConversions.ConvertIdentifier(node.Identifier);

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (node.AsClause == null && symbol.BaseMember() == null) {
            var delegateName = SyntaxFactory.Identifier(id.ValueText + "EventHandler");

            var delegateDecl = SyntaxFactory.DelegateDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                modifiers.RemoveWhere(m => m.IsKind(CSSyntaxKind.StaticKeyword)),
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword)),
                delegateName,
                null,
                await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ParameterList(),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>()
            );

            var eventDecl = SyntaxFactory.EventFieldDeclaration(
                SyntaxFactory.List(attributes),
                modifiers,
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(delegateName),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(id)))
            );

            _additionalDeclarations.Add(node, new MemberDeclarationSyntax[] { delegateDecl });
            return eventDecl;
        }
        var type = symbol.Type != null || node.AsClause == null ? CommonConversions.GetTypeSyntax(symbol.Type) : await node.AsClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor);
        var declaration = SyntaxFactory.VariableDeclaration(type,
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(id)));
        return SyntaxFactory.EventFieldDeclaration(
            SyntaxFactory.List(attributes),
            modifiers,
            declaration
        );
    }

    public override async Task<CSharpSyntaxNode> VisitOperatorBlock(VBSyntax.OperatorBlockSyntax node)
    {
        return await node.BlockStatement.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.SubNodesOnly);
    }

    public override async Task<CSharpSyntaxNode> VisitOperatorStatement(VBSyntax.OperatorStatementSyntax node)
    {
        var containingBlock = (VBSyntax.OperatorBlockSyntax) node.Parent;
        var attributes = await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var attributeList = SyntaxFactory.List(attributes);
        var returnType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword));
        var parameterList = await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor);
        var methodBodyVisitor = await CreateMethodBodyVisitorAsync(node);
        var body = SyntaxFactory.Block(await containingBlock.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor)));
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, GetMemberContext(node));

        var conversionModifiers = modifiers.Where(CommonConversions.IsConversionOperator).ToList();
        var nonConversionModifiers = SyntaxFactory.TokenList(modifiers.Except(conversionModifiers));

        if (conversionModifiers.Any()) {
            return SyntaxFactory.ConversionOperatorDeclaration(attributeList, nonConversionModifiers,
                conversionModifiers.Single(), returnType, parameterList, body, null);
        }

        return SyntaxFactory.OperatorDeclaration(attributeList, nonConversionModifiers, returnType, node.OperatorToken.ConvertToken(), parameterList, body, null);
    }

    public override async Task<CSharpSyntaxNode> VisitConstructorBlock(VBSyntax.ConstructorBlockSyntax node)
    {
        var block = node.BlockStatement;
        var attributes = await block.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node));

        var ctor = (node.Statements.FirstOrDefault() as VBSyntax.ExpressionStatementSyntax)?.Expression as VBSyntax.InvocationExpressionSyntax;
        var ctorExpression = ctor?.Expression as VBSyntax.MemberAccessExpressionSyntax;
        var ctorArgs = await (ctor?.ArgumentList).AcceptAsync<ArgumentListSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ArgumentList();

        IEnumerable<VBSyntax.StatementSyntax> statements;
        ConstructorInitializerSyntax ctorCall;
        if (ctorExpression == null || !ctorExpression.Name.Identifier.IsKindOrHasMatchingText(VBasic.SyntaxKind.NewKeyword)) {
            statements = node.Statements;
            ctorCall = null;
        } else if (ctorExpression.Expression is VBSyntax.MyBaseExpressionSyntax) {
            statements = node.Statements.Skip(1);
            ctorCall = SyntaxFactory.ConstructorInitializer(CSSyntaxKind.BaseConstructorInitializer, ctorArgs);
        } else if (ctorExpression.Expression is VBSyntax.MeExpressionSyntax || ctorExpression.Expression is VBSyntax.MyClassExpressionSyntax) {
            statements = node.Statements.Skip(1);
            ctorCall = SyntaxFactory.ConstructorInitializer(CSSyntaxKind.ThisConstructorInitializer, ctorArgs);
        } else {
            statements = node.Statements;
            ctorCall = null;
        }

        var methodBodyVisitor = await CreateMethodBodyVisitorAsync(node);
        return SyntaxFactory.ConstructorDeclaration(
            SyntaxFactory.List(attributes),
            modifiers, 
            CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier).WithoutSourceMapping(), //TODO Use semantic model for this name
            await block.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor),
            ctorCall,
            SyntaxFactory.Block(await statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor)))
        );
    }

    public override async Task<CSharpSyntaxNode> VisitDeclareStatement(VBSyntax.DeclareStatementSyntax node)
    {
        var importAttributes = new List<AttributeArgumentSyntax>();
        _extraUsingDirectives.Add(DllImportType.Namespace);
        _extraUsingDirectives.Add(CharSetType.Namespace);
        var dllImportAttributeName = SyntaxFactory.ParseName(DllImportType.Name.Replace("Attribute", ""));
        var dllImportLibLiteral = await node.LibraryName.AcceptAsync<ExpressionSyntax>(_triviaConvertingExpressionVisitor);
        importAttributes.Add(SyntaxFactory.AttributeArgument(dllImportLibLiteral));

        if (node.AliasName != null) {
            importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("EntryPoint"), null, await node.AliasName.AcceptAsync<ExpressionSyntax>(_triviaConvertingExpressionVisitor)));
        }

        if (!node.CharsetKeyword.IsKind(CSSyntaxKind.None)) {
            importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(CharSetType.Name), null, SyntaxFactory.MemberAccessExpression(CSSyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(CharSetType.Name), SyntaxFactory.IdentifierName(node.CharsetKeyword.Text))));
        }

        var attributeArguments = CommonConversions.CreateAttributeArgumentList(importAttributes.ToArray());
        var dllImportAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

        var attributeLists = (await CommonConversions.ConvertAttributesAsync(node.AttributeLists)).Add(dllImportAttributeList);

        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers).Add(SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(CSSyntaxKind.ExternKeyword));
        var returnType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSSyntaxKind.VoidKeyword));
        var parameterListSyntax = await (node.ParameterList).AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ??
                                  SyntaxFactory.ParameterList();

        return SyntaxFactory.MethodDeclaration(attributeLists, modifiers, returnType, null, CommonConversions.ConvertIdentifier(node.Identifier), null,
            parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), null, null).WithSemicolonToken(SemicolonToken);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterList(VBSyntax.TypeParameterListSyntax node)
    {
        return SyntaxFactory.TypeParameterList(
            SyntaxFactory.SeparatedList(await node.Parameters.SelectAsync(async p => await p.AcceptAsync<TypeParameterSyntax>(TriviaConvertingDeclarationVisitor)))
        );
    }

    private async Task<(TypeParameterListSyntax parameters, SyntaxList<TypeParameterConstraintClauseSyntax> constraints)> SplitTypeParametersAsync(VBSyntax.TypeParameterListSyntax typeParameterList)
    {
        var constraints = SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();
        if (typeParameterList == null) return (null, constraints);

        var paramList = new List<TypeParameterSyntax>();
        var constraintList = new List<TypeParameterConstraintClauseSyntax>();
        foreach (var p in typeParameterList.Parameters) {
            var tp = await p.AcceptAsync<TypeParameterSyntax>(TriviaConvertingDeclarationVisitor);
            paramList.Add(tp);
            var constraint = await (p.TypeParameterConstraintClause).AcceptAsync<TypeParameterConstraintClauseSyntax>(TriviaConvertingDeclarationVisitor);
            if (constraint != null)
                constraintList.Add(constraint);
        }
        var parameters = SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(paramList));
        constraints = SyntaxFactory.List(constraintList);
        return (parameters, constraints);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameter(VBSyntax.TypeParameterSyntax node)
    {
        SyntaxToken variance = default(SyntaxToken);
        if (!SyntaxTokenExtensions.IsKind(node.VarianceKeyword, VBasic.SyntaxKind.None)) {
            variance = SyntaxFactory.Token(SyntaxTokenExtensions.IsKind(node.VarianceKeyword, VBasic.SyntaxKind.InKeyword) ? CSSyntaxKind.InKeyword : CSSyntaxKind.OutKeyword);
        }
        return SyntaxFactory.TypeParameter(SyntaxFactory.List<AttributeListSyntax>(), variance, CommonConversions.ConvertIdentifier(node.Identifier));
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterSingleConstraintClause(VBSyntax.TypeParameterSingleConstraintClauseSyntax node)
    {
        var id = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
        return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SingletonSeparatedList(await node.Constraint.AcceptAsync<TypeParameterConstraintSyntax>(TriviaConvertingDeclarationVisitor)));
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterMultipleConstraintClause(VBSyntax.TypeParameterMultipleConstraintClauseSyntax node)
    {
        var id = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
        var constraints = await node.Constraints.SelectAsync(async c => await c.AcceptAsync<TypeParameterConstraintSyntax>(TriviaConvertingDeclarationVisitor));
        return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SeparatedList(constraints.OrderBy(c => c.Kind() == CSSyntaxKind.ConstructorConstraint ? 1 : 0)));
    }

    public override async Task<CSharpSyntaxNode> VisitSpecialConstraint(VBSyntax.SpecialConstraintSyntax node)
    {
        if (SyntaxTokenExtensions.IsKind(node.ConstraintKeyword, VBasic.SyntaxKind.NewKeyword))
            return SyntaxFactory.ConstructorConstraint();
        return SyntaxFactory.ClassOrStructConstraint(node.IsKind(VBasic.SyntaxKind.ClassConstraint) ? CSSyntaxKind.ClassConstraint : CSSyntaxKind.StructConstraint);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeConstraint(VBSyntax.TypeConstraintSyntax node)
    {
        return SyntaxFactory.TypeConstraint(await node.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlNamespaceImportsClause(VBSyntax.XmlNamespaceImportsClauseSyntax node)
    {
        var identifierName = await node.XmlNamespace.Name.AcceptAsync<IdentifierNameSyntax>(TriviaConvertingDeclarationVisitor);
        var valueLiteral = await node.XmlNamespace.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingDeclarationVisitor);
        var declarator = SyntaxFactory.VariableDeclarator(identifierName.Identifier, null, SyntaxFactory.EqualsValueClause(valueLiteral));
        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.InternalKeyword), 
                SyntaxFactory.Token(CSSyntaxKind.StaticKeyword), 
                SyntaxFactory.Token(CSSyntaxKind.ReadOnlyKeyword)),
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("XNamespace"), SyntaxFactory.SingletonSeparatedList(declarator)));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlName(VBSyntax.XmlNameSyntax node)
    {
        if (node.Prefix == null && node.LocalName.ValueText == "xmlns") {
            // default namespace             
            return XmlImportContext.DefaultIdentifierName;
        } else if (node.Prefix.Name.ValueText == "xmlns") { 
            // namespace alias
            return SyntaxFactory.IdentifierName(node.LocalName.ValueText);
        } else {
            // Having this case in VB would cause error BC31187: Namespace declaration must start with 'xmlns'
            throw new NotImplementedException($"Cannot convert non-xmlns attribute in XML namespace import").WithNodeInformation(node);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitXmlString(VBSyntax.XmlStringSyntax node) =>
        CommonConversions.Literal(node.TextTokens.Aggregate("", (a, b) => a + LiteralConversions.EscapeVerbatimQuotes(b.Text)));
}