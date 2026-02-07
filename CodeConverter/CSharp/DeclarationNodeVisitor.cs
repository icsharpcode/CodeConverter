using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;

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
    private static readonly SyntaxToken SemicolonToken = CS.SyntaxFactory.Token(CS.SyntaxKind.SemicolonToken);
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
    public CommentConvertingVisitorWrapper TriviaConvertingDeclarationVisitor { get; }
    private readonly CommentConvertingVisitorWrapper _triviaConvertingExpressionVisitor;
    private string _topAncestorNamespace;
    private readonly AccessorDeclarationNodeConverter _accessorDeclarationNodeConverter;

    private CommonConversions CommonConversions { get; }
    private Func<VisualBasicSyntaxNode, IReadOnlyCollection<VBSyntax.StatementSyntax>, bool, IdentifierNameSyntax, Task<IReadOnlyCollection<StatementSyntax>>> _convertMethodBodyStatementsAsync { get; }

    internal PerScopeState AdditionalLocals => _typeContext.PerScopeState;

    public DeclarationNodeVisitor(Document document, Compilation compilation, SemanticModel semanticModel,
        CSharpCompilation csCompilation, SyntaxGenerator csSyntaxGenerator, ILookup<ITypeSymbol, ITypeSymbol> typeToInheritors)
    {
        _vbCompilation = compilation;
        _csSyntaxGenerator = csSyntaxGenerator;
        _typeToInheritors = typeToInheritors;
        _xmlImportContext = new XmlImportContext(document);
        _semanticModel = semanticModel;
        _visualBasicEqualityComparison = new VisualBasicEqualityComparison(_semanticModel, _extraUsingDirectives);
        TriviaConvertingDeclarationVisitor = new CommentConvertingVisitorWrapper(this, _semanticModel.SyntaxTree);
        var expressionEvaluator = new ExpressionEvaluator(semanticModel, _visualBasicEqualityComparison);
        var nullableExpressionsConverter = new VisualBasicNullableExpressionsConverter(_semanticModel);
        var typeConversionAnalyzer = new TypeConversionAnalyzer(semanticModel, csCompilation, _extraUsingDirectives, _csSyntaxGenerator, expressionEvaluator, nullableExpressionsConverter);
        CommonConversions = new CommonConversions(document, semanticModel, typeConversionAnalyzer, csSyntaxGenerator, compilation, csCompilation, _typeContext, _visualBasicEqualityComparison);
        var expressionNodeVisitor = new ExpressionNodeVisitor(semanticModel, _visualBasicEqualityComparison, _typeContext, CommonConversions, _extraUsingDirectives, _xmlImportContext, nullableExpressionsConverter);
        _accessorDeclarationNodeConverter = new AccessorDeclarationNodeConverter(semanticModel, CommonConversions, TriviaConvertingDeclarationVisitor, _additionalDeclarations, expressionNodeVisitor.ConvertMethodBodyStatementsAsync);
        _triviaConvertingExpressionVisitor = CommonConversions.TriviaConvertingExpressionVisitor;
        _convertMethodBodyStatementsAsync = expressionNodeVisitor.ConvertMethodBodyStatementsAsync;
        nullableExpressionsConverter.QueryTracker = _triviaConvertingExpressionVisitor;
        _visualBasicEqualityComparison.QueryTracker = _triviaConvertingExpressionVisitor;
    }

    private async Task<IReadOnlyCollection<StatementSyntax>> ConvertMethodBodyStatementsAsync(VisualBasicSyntaxNode node,
        IReadOnlyCollection<Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax> statements, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {
        return await _convertMethodBodyStatementsAsync(node, statements, isIterator, csReturnVariable);
    }

    public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }

    public override Task<CSharpSyntaxNode> VisitPropertyStatement(VBSyntax.PropertyStatementSyntax node) => _accessorDeclarationNodeConverter.ConvertPropertyStatementAsync(node);
    public override Task<CSharpSyntaxNode> VisitPropertyBlock(VBSyntax.PropertyBlockSyntax node) => _accessorDeclarationNodeConverter.ConvertPropertyBlockAsync(node);
    public override Task<CSharpSyntaxNode> VisitAccessorBlock(VBSyntax.AccessorBlockSyntax node) => _accessorDeclarationNodeConverter.VisitAccessorBlockAsync(node);
    public override Task<CSharpSyntaxNode> VisitAccessorStatement(VBSyntax.AccessorStatementSyntax node) => _accessorDeclarationNodeConverter.VisitAccessorStatementAsync(node);

    public override async Task<CSharpSyntaxNode> VisitCompilationUnit(VBSyntax.CompilationUnitSyntax node)
    {
        var options = (VBasic.VisualBasicCompilationOptions)_semanticModel.Compilation.Options;
        var importsClauses = options.GlobalImports.Select(gi => gi.Clause).Concat(node.Imports.SelectMany(imp => imp.ImportsClauses)).ToList();

        _topAncestorNamespace = node.Members.Any(m => !IsNamespaceDeclaration(m)) ? options.RootNamespace : null;
        var fileOptionCompareValue = node.Options.Where(x => x.NameKeyword.IsKind(VBasic.SyntaxKind.CompareKeyword)).LastOrDefault()?.ValueKeyword;
        _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive = fileOptionCompareValue?.IsKind(VBasic.SyntaxKind.TextKeyword) ?? options.OptionCompareText;

        var attributes = CS.SyntaxFactory.List(await node.Attributes.SelectMany(a => a.AttributeLists).SelectManyAsync(CommonConversions.ConvertAttributeAsync));

        var xmlImportHelperClassDeclarationOrNull = (await _xmlImportContext.HandleImportsAsync(importsClauses, x => x.AcceptAsync<FieldDeclarationSyntax>(TriviaConvertingDeclarationVisitor))).GenerateHelper();
            
        var sourceAndConverted = await node.Members
            .Where(m => !(m is VBSyntax.OptionStatementSyntax))
            .SelectAsync(async m => (Source: m, Converted: await ConvertMemberAsync(m)));


        var convertedMembers = string.IsNullOrEmpty(options.RootNamespace)
            ? sourceAndConverted.Select(sd => sd.Converted)
            : PrependRootNamespace(sourceAndConverted, options.RootNamespace);
            
        var usings = await importsClauses
            .SelectAsync(async c => await c.AcceptAsync<UsingDirectiveSyntax>(TriviaConvertingDeclarationVisitor));
        var usingDirectiveSyntax = usings
            .Concat(_extraUsingDirectives.Select(u => CS.SyntaxFactory.UsingDirective(CS.SyntaxFactory.ParseName(u))))
            .OrderByDescending(IsSystemUsing).ThenBy(u => u.Name.ToString().Replace("global::", "")).ThenByDescending(HasSourceMapAnnotations)
            .GroupBy(u => (Name: u.Name.ToString(), Alias: u.Alias))
            .Select(g => g.First())
            .Concat(xmlImportHelperClassDeclarationOrNull.YieldNotNull().Select(_ => CS.SyntaxFactory.UsingDirective(CS.SyntaxFactory.NameEquals(XmlImportContext.HelperClassShortIdentifierName), _xmlImportContext.HelperClassUniqueIdentifierName)));

        return CS.SyntaxFactory.CompilationUnit(
            CS.SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            CS.SyntaxFactory.List(usingDirectiveSyntax),
            attributes,
            CS.SyntaxFactory.List(xmlImportHelperClassDeclarationOrNull.YieldNotNull().Concat(convertedMembers))
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
        string rootNamespace)
    {

        if (_topAncestorNamespace != null) {
            var csMembers = membersConversions.ToLookup(c => ShouldBeNestedInRootNamespace(c.VbNode, rootNamespace), c => c.CsNode);
            var nestedMembers = csMembers[true].Select<MemberDeclarationSyntax, SyntaxNode>(x => x);
            var newNamespaceDecl = (MemberDeclarationSyntax) _csSyntaxGenerator.NamespaceDeclaration(rootNamespace, nestedMembers);
            return csMembers[false].Concat(new[] { newNamespaceDecl }).ToArray();
        }
        return membersConversions.Select(n => n.CsNode).ToArray();
    }

    private bool ShouldBeNestedInRootNamespace(VBSyntax.StatementSyntax vbStatement, string rootNamespace)
    {
        ISymbol symbol = vbStatement switch {
            VBSyntax.TypeBlockSyntax tb => _semanticModel.GetDeclaredSymbol(tb.BlockStatement as VBSyntax.TypeStatementSyntax),
            VBSyntax.MethodBlockSyntax mb => _semanticModel.GetDeclaredSymbol(mb.SubOrFunctionStatement),
            VBSyntax.FieldDeclarationSyntax fd => _semanticModel.GetDeclaredSymbol(fd.Declarators.First().Names.First()),
            VBSyntax.NamespaceBlockSyntax nb => _semanticModel.GetDeclaredSymbol(nb.NamespaceStatement),
            VBSyntax.PropertyBlockSyntax pb => _semanticModel.GetDeclaredSymbol(pb.PropertyStatement),
            VBSyntax.EnumBlockSyntax eb => _semanticModel.GetDeclaredSymbol(eb.EnumStatement),
            _ => null
        };
        return (symbol?.ToDisplayString()).StartsWith(rootNamespace, StringComparison.InvariantCulture) == true;
    }

    private static bool IsNamespaceDeclaration(VBSyntax.StatementSyntax m)
    {
        return m is VBSyntax.NamespaceBlockSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitSimpleImportsClause(VBSyntax.SimpleImportsClauseSyntax node)
    {
        var nameEqualsSyntax = node.Alias == null
            ? null
            : CS.SyntaxFactory.NameEquals(CS.SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Alias.Identifier)));

        var name = await node.Name.AcceptAsync<NameSyntax>(_triviaConvertingExpressionVisitor);

        //Add static keyword for class and module imports
        var classification = await CommonConversions.GetClassificationLastTokenAsync(node);
        var staticToken = CS.SyntaxFactory.Token(CS.SyntaxKind.StaticKeyword);
        var staticClassifications = new List<string>
        {
            ClassificationTypeNames.ClassName,
            ClassificationTypeNames.ModuleName
        };

        var usingDirective = staticClassifications.Contains(classification)
            ? ValidSyntaxFactory.UsingDirective(staticToken, nameEqualsSyntax, name)
            : CS.SyntaxFactory.UsingDirective(nameEqualsSyntax, name);

        return usingDirective;
    }

    public override async Task<CSharpSyntaxNode> VisitNamespaceBlock(VBSyntax.NamespaceBlockSyntax node)
    {
        var members = (await node.Members.SelectAsync(ConvertMemberAsync)).Where(m => m != null);
        var sym = _semanticModel.GetDeclaredSymbol(node.NamespaceStatement);
        string namespaceToDeclare = await WithDeclarationNameCasingAsync(node, sym);
        var parentNamespaceSyntax = node.GetAncestor<VBSyntax.NamespaceBlockSyntax>();
        var parentNamespaceDecl = parentNamespaceSyntax != null ? _semanticModel.GetDeclaredSymbol(parentNamespaceSyntax.NamespaceStatement) : null;
        var parentNamespaceFullName = parentNamespaceDecl?.ToDisplayString() ?? _topAncestorNamespace;
        if (parentNamespaceFullName != null && namespaceToDeclare.StartsWith(parentNamespaceFullName + ".", StringComparison.InvariantCulture))
            namespaceToDeclare = namespaceToDeclare.Substring(parentNamespaceFullName.Length + 1);

        var cSharpSyntaxNode = (CSharpSyntaxNode) _csSyntaxGenerator.NamespaceDeclaration(namespaceToDeclare, CS.SyntaxFactory.List(members));
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

        var namedTypeSymbol = _semanticModel.GetDeclaredSymbol(parentType.BlockStatement);
        var additionalInitializers = new AdditionalInitializers(parentType, namedTypeSymbol, _vbCompilation);
        var methodsWithHandles = await GetMethodWithHandlesAsync(parentType, additionalInitializers.DesignerGeneratedInitializeComponentOrNull);

        if (methodsWithHandles.AnySynchronizedPropertiesGenerated()) _extraUsingDirectives.Add("System.Runtime.CompilerServices");//For MethodImplOptions.Synchronized
            
        _typeContext.Push(methodsWithHandles, additionalInitializers);
        try {
            var membersFromBase = additionalInitializers.IsBestPartToAddTypeInit ? methodsWithHandles.GetDeclarationsForHandlingBaseMembers() : Array.Empty<MemberDeclarationSyntax>();
            var convertedMembers = await members.SelectManyAsync(async member => {
                _typeContext.PerScopeState.PushScope();
                try {
                    var convertedMember = (await ConvertMemberAsync(member)).Yield().Concat(GetAdditionalDeclarations(member));
                    IEnumerable<MemberDeclarationSyntax> convertedPlusAdditional = (await _typeContext.PerScopeState.CreateVbStaticFieldsAsync(
                            parentType, namedTypeSymbol, convertedMember, _generatedNames, _semanticModel)
                        );
                    return convertedPlusAdditional;
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
                var (staticHandlers, instanceHandlers) = methodsWithHandles.GetConstructorEventHandlersWhereNoInitializeComponent();
                additionalInitializers.AdditionalStaticInitializers.AddRange(staticHandlers);
                additionalInitializers.AdditionalInstanceInitializers.AddRange(instanceHandlers);
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
        var invocation = CS.SyntaxFactory.InvocationExpression(ValidSyntaxFactory.IdentifierName((initializerFunctionName)), CS.SyntaxFactory.ArgumentList());
        return new Assignment(ValidSyntaxFactory.IdentifierName(csId), CS.SyntaxKind.SimpleAssignmentExpression, invocation);
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
            var sourceTriviaMapKind = member is VBSyntax.PropertyBlockSyntax propBlock && AccessorDeclarationNodeConverter.ShouldConvertAsParameterizedProperty(propBlock.PropertyStatement)
                ? SourceTriviaMapKind.SubNodesOnly
                : SourceTriviaMapKind.All;
            return await member.AcceptAsync<MemberDeclarationSyntax>(TriviaConvertingDeclarationVisitor, sourceTriviaMapKind);
        } catch (Exception e) {
            return CreateErrorMember(member, e);
        }

        MemberDeclarationSyntax CreateErrorMember(VBSyntax.StatementSyntax memberCausingError, Exception e)
        {
            var dummyClass
                = CS.SyntaxFactory.ClassDeclaration("_failedMemberConversionMarker" + ++_failedMemberConversionMarkerCount);
            return dummyClass.WithCsTrailingErrorComment(memberCausingError, e);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitClassBlock(VBSyntax.ClassBlockSyntax node)
    {
        _accessorDeclarationNodeConverter.AccessedThroughMyClass = GetMyClassAccessedNames(node);
        var classStatement = node.ClassStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(classStatement.AttributeLists);
        var (parameters, constraints) = await SplitTypeParametersAsync(classStatement.TypeParameterList);
        var convertedIdentifier = CommonConversions.ConvertIdentifier(classStatement.Identifier);

        return CS.SyntaxFactory.ClassDeclaration(
            attributes, ConvertTypeBlockModifiers(classStatement, TokenContext.Global),
            convertedIdentifier,
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            CS.SyntaxFactory.List(await ConvertMembersAsync(node))
        );
    }

    private async Task<BaseListSyntax> ConvertInheritsAndImplementsAsync(SyntaxList<VBSyntax.InheritsStatementSyntax> inherits, SyntaxList<VBSyntax.ImplementsStatementSyntax> implements)
    {
        if (inherits.Count + implements.Count == 0)
            return null;
        var baseTypes = new List<BaseTypeSyntax>();
        foreach (var t in inherits.SelectMany(c => c.Types).Concat(implements.SelectMany(c => c.Types)))
            baseTypes.Add(CS.SyntaxFactory.SimpleBaseType(await t.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor)));
        return CS.SyntaxFactory.BaseList(CS.SyntaxFactory.SeparatedList(baseTypes));
    }

    public override async Task<CSharpSyntaxNode> VisitModuleBlock(VBSyntax.ModuleBlockSyntax node)
    {
        var stmt = node.ModuleStatement;
        var attributes = await CommonConversions.ConvertAttributesAsync(stmt.AttributeLists);
        var members = CS.SyntaxFactory.List(await ConvertMembersAsync(node));
        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return CS.SyntaxFactory.ClassDeclaration(
            attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule, CS.SyntaxKind.StaticKeyword),
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
        var members = CS.SyntaxFactory.List(await ConvertMembersAsync(node));

        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return CS.SyntaxFactory.StructDeclaration(
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
        var members = CS.SyntaxFactory.List(await ConvertMembersAsync(node));

        var (parameters, constraints) = await SplitTypeParametersAsync(stmt.TypeParameterList);

        return CS.SyntaxFactory.InterfaceDeclaration(
            attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule), CommonConversions.ConvertIdentifier(stmt.Identifier),
            parameters,
            await ConvertInheritsAndImplementsAsync(node.Inherits, node.Implements),
            constraints,
            members
        );
    }

    private SyntaxTokenList ConvertTypeBlockModifiers(VBSyntax.TypeStatementSyntax stmt,
        TokenContext interfaceOrModule, params CS.SyntaxKind[] extraModifiers)
    {
        if (IsPartialType(stmt) && !HasPartialKeyword(stmt.Modifiers)) {
            extraModifiers = extraModifiers.Concat(new[] { CS.SyntaxKind.PartialKeyword})
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
        return _semanticModel.GetDeclaredSymbol((VBSyntax.TypeStatementSyntax)stmt).IsPartialClassDefinition();
    }

    public override async Task<CSharpSyntaxNode> VisitEnumBlock(VBSyntax.EnumBlockSyntax node)
    {
        var stmt = node.EnumStatement;
        // we can cast to SimpleAsClause because other types make no sense as enum-type.
        var asClause = (VBSyntax.SimpleAsClauseSyntax)stmt.UnderlyingType;
        var attributes = await stmt.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        BaseListSyntax baseList = null;
        if (asClause != null) {
            baseList = CS.SyntaxFactory.BaseList(CS.SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(CS.SyntaxFactory.SimpleBaseType(await asClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor))));
            if (asClause.AttributeLists.Count > 0) {
                var attributeLists = await asClause.AttributeLists.SelectManyAsync(l => CommonConversions.ConvertAttributeAsync(l));
                attributes = attributes.Concat(
                    CS.SyntaxFactory.AttributeList(
                        CS.SyntaxFactory.AttributeTargetSpecifier(CS.SyntaxFactory.Token(CS.SyntaxKind.ReturnKeyword)),
                        CS.SyntaxFactory.SeparatedList(attributeLists.SelectMany(a => a.Attributes)))
                ).ToArray();
            }
        }
        var members = CS.SyntaxFactory.SeparatedList(await node.Members.SelectAsync(async m => await m.AcceptAsync<EnumMemberDeclarationSyntax>(TriviaConvertingDeclarationVisitor)));
        return CS.SyntaxFactory.EnumDeclaration(
            CS.SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(stmt, stmt.Modifiers), CommonConversions.ConvertIdentifier(stmt.Identifier),
            baseList,
            members
        );
    }

    public override async Task<CSharpSyntaxNode> VisitEnumMemberDeclaration(VBSyntax.EnumMemberDeclarationSyntax node)
    {
        var attributes = await CommonConversions.ConvertAttributesAsync(node.AttributeLists);
        return CS.SyntaxFactory.EnumMemberDeclaration(
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
            returnType = CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword));
        } else {
            returnType = await asClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor);
            if (asClause.AttributeLists.Count > 0) {
                var attributeListSyntaxs = await asClause.AttributeLists.SelectManyAsync(l => CommonConversions.ConvertAttributeAsync(l));
                attributes = attributes.Concat(
                    CS.SyntaxFactory.AttributeList(
                        CS.SyntaxFactory.AttributeTargetSpecifier(CS.SyntaxFactory.Token(CS.SyntaxKind.ReturnKeyword)),
                        CS.SyntaxFactory.SeparatedList(attributeListSyntaxs.SelectMany(a => a.Attributes)))
                ).ToArray();
            }
        }

        return CS.SyntaxFactory.DelegateDeclaration(
            CS.SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(node, node.Modifiers),
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
            node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.WithEventsKeyword));
        var isWithEvents = node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.WithEventsKeyword));
        var convertedModifiers =
            CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], convertableModifiers.ToList(), node.GetMemberContext());
        var declarations = new List<MemberDeclarationSyntax>(node.Declarators.Count);

        foreach (var declarator in node.Declarators)
        {
            var splitDeclarations = await CommonConversions.SplitVariableDeclarationsAsync(declarator, preferExplicitType: true);
            declarations.AddRange(CreateMemberDeclarations(splitDeclarations.Variables, isWithEvents, convertedModifiers, attributes));
            declarations.AddRange(splitDeclarations.Methods.Cast<MemberDeclarationSyntax>());
        }

        return declarations;
    }

    private IEnumerable<MemberDeclarationSyntax> CreateMemberDeclarations(IReadOnlyCollection<CommonConversions.VariablesDeclaration> splitDeclarationVariables,
        bool isWithEvents, SyntaxTokenList convertedModifiers, List<AttributeListSyntax> attributes)
    {

        foreach (var variablesDecl in splitDeclarationVariables)
        {
            var thisFieldModifiers = convertedModifiers;
            if (variablesDecl.Type?.SpecialType == SpecialType.System_DateTime) {
                var index = thisFieldModifiers.IndexOf(CS.SyntaxKind.ConstKeyword);
                if (index >= 0) {
                    thisFieldModifiers = thisFieldModifiers.Replace(thisFieldModifiers[index], CS.SyntaxFactory.Token(CS.SyntaxKind.StaticKeyword));
                }
            }

            if (isWithEvents) {
                var fieldDecls = CreateWithEventsMembers(thisFieldModifiers, attributes, variablesDecl.Decl);
                foreach (var f in fieldDecls) yield return f;
            } else {
                foreach (var method in CreateExtraMethodMembers()) yield return method;

                if (AdditionalLocals.GetDeclarations().Any()) {
                    foreach (var additionalDecl in CreateAdditionalLocalMembers(thisFieldModifiers, attributes, variablesDecl.Decl)) {
                        yield return additionalDecl;
                    }
                } else {
                    yield return CS.SyntaxFactory.FieldDeclaration(CS.SyntaxFactory.List(attributes), thisFieldModifiers, variablesDecl.Decl);
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
        var initializerCollection = convertedModifiers.Any(m => m.IsKind(CS.SyntaxKind.StaticKeyword))
            ? initializerState.AdditionalStaticInitializers
            : initializerState.AdditionalInstanceInitializers;
        foreach (var initializer in initializers) {
            initializerCollection.Add(new Assignment(ValidSyntaxFactory.IdentifierName(initializer.Key), CS.SyntaxKind.SimpleAssignmentExpression, initializer.Value.Value));
        }

        var fieldDecls = _typeContext.HandledEventsAnalysis.GetDeclarationsForFieldBackedProperty(fieldDecl,
            convertedModifiers, CS.SyntaxFactory.List(attributes));
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

        string newMethodName;
        if (invocations.OnlyOrDefault() is { Expression: {} e} && e.ChildNodes().OfType<SimpleNameSyntax>().LastOrDefault() is {} sns) {
            newMethodName = $"{sns?.Identifier.ValueText}_{v.Identifier.ValueText}";
        } else {
            newMethodName = "init" + v.Identifier.ValueText.UppercaseFirstLetter();
        }

        var declarationInfo = AdditionalLocals.GetDeclarations();

        var localVars = declarationInfo
            .Select(al => CommonConversions.CreateLocalVariableDeclarationAndAssignment(al.Prefix, al.Initializer))
            .ToArray<StatementSyntax>();

        // This should probably use a unique name like in MethodBodyVisitor - a collision is far less likely here
        var newNames = declarationInfo.ToDictionary(l => l.Id, l => l.Prefix);
        var newInitializer = PerScopeState.ReplaceNames(v.Initializer.Value, newNames);

        var body = CS.SyntaxFactory.Block(localVars.Concat(CS.SyntaxFactory.ReturnStatement(newInitializer).Yield()));
        // Method calls in initializers must be static in C# - Supporting this is #281
        var methodDecl = ValidSyntaxFactory.CreateParameterlessMethod(newMethodName, decl.Type, body);
        yield return methodDecl;

        var newVar =
            v.WithInitializer(CS.SyntaxFactory.EqualsValueClause(
                CS.SyntaxFactory.InvocationExpression(ValidSyntaxFactory.IdentifierName(newMethodName))));
        var newVarDecl =
            CS.SyntaxFactory.VariableDeclaration(decl.Type, CS.SyntaxFactory.SingletonSeparatedList(newVar));

        yield return CS.SyntaxFactory.FieldDeclaration(CS.SyntaxFactory.List(attributes), convertedModifiers, newVarDecl);
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
        if (parentType == null || _semanticModel.GetDeclaredSymbol(parentType.BlockStatement as VBSyntax.TypeStatementSyntax) is not INamedTypeSymbol containingType) {
            return new HandledEventsAnalysis(CommonConversions, null, Array.Empty<(HandledEventsAnalysis.EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] HandledMethods)>());
        }
        return await HandledEventsAnalyzer.AnalyzeAsync(CommonConversions, containingType, designerGeneratedInitializeComponentOrNull, _typeToInheritors);
    }

    public override async Task<CSharpSyntaxNode> VisitMethodBlock(VBSyntax.MethodBlockSyntax node)
    {
        var methodBlock = await node.SubOrFunctionStatement.AcceptAsync<BaseMethodDeclarationSyntax>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.SubNodesOnly);

        var declaredSymbol = _semanticModel.GetDeclaredSymbol(node.SubOrFunctionStatement);
        if (declaredSymbol?.CanHaveMethodBody() == false) {
            return methodBlock;
        }
        var csReturnVariableOrNull = CommonConversions.GetRetVariableNameOrNull(node);
        var convertedStatements = CS.SyntaxFactory.Block(await ConvertMethodBodyStatementsAsync(node, node.Statements, node.IsIterator(), csReturnVariableOrNull));

        //  Just class events - for property events, see other use of IsDesignerGeneratedTypeWithInitializeComponent
        if (node.SubOrFunctionStatement.Identifier.Text == "InitializeComponent" && node.SubOrFunctionStatement.IsKind(VBasic.SyntaxKind.SubStatement) && declaredSymbol.ContainingType.GetDesignerGeneratedInitializeComponentOrNull(_vbCompilation) != null) {
            var firstResumeLayout = convertedStatements.Statements.FirstOrDefault(IsThisResumeLayoutInvocation) ?? convertedStatements.Statements.Last();
            convertedStatements = convertedStatements.InsertNodesBefore(firstResumeLayout, _typeContext.HandledEventsAnalysis.GetInitializeComponentClassEventHandlers());
        }

        var body = _accessorDeclarationNodeConverter.WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);

        return methodBlock.WithBody(body);
    }

    private static bool IsThisResumeLayoutInvocation(StatementSyntax s)
    {
        return s is ExpressionStatementSyntax ess && ess.Expression is InvocationExpressionSyntax ies && ies.Expression.ToString().Equals("this.ResumeLayout", StringComparison.Ordinal);
    }

    private static async Task<BlockSyntax> ConvertStatementsAsync(SyntaxList<VBSyntax.StatementSyntax> statements, VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> methodBodyVisitor)
    {
        return CS.SyntaxFactory.Block(await statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor)));
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
            node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.OverridesKeyword)))
        {
            var declaration = CS.SyntaxFactory.
                DestructorDeclaration(CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier)).
                WithAttributeLists(attributes);
            return hasBody ? declaration : declaration.WithSemicolonToken(SemicolonToken);
        } 
            
        var tokenContext = node.GetMemberContext();
        var declaredSymbol = (IMethodSymbol)_semanticModel.GetDeclaredSymbol(node);
        var extraCsModifierKinds = declaredSymbol?.IsExtern == true ? new[] { CS.SyntaxKind.ExternKeyword } : Array.Empty<CS.SyntaxKind>();
        var convertedModifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, tokenContext, extraCsModifierKinds: extraCsModifierKinds);

        bool accessedThroughMyClass = _accessorDeclarationNodeConverter.IsAccessedThroughMyClass(node, node.Identifier, declaredSymbol);

        if (declaredSymbol.IsPartialMethodImplementation() || declaredSymbol.IsPartialMethodDefinition()) 
        {
            var privateModifier = convertedModifiers.SingleOrDefault(m => m.IsKind(CS.SyntaxKind.PrivateKeyword));
            if (privateModifier != default) {
                convertedModifiers = convertedModifiers.Remove(privateModifier);
            }
            if (!HasPartialKeyword(node.Modifiers)) {
                convertedModifiers = convertedModifiers.Add(CS.SyntaxFactory.Token(CS.SyntaxKind.PartialKeyword));
            }
        }
        var (typeParameters, constraints) = await SplitTypeParametersAsync(node.TypeParameterList);

        var returnType = (declaredSymbol != null ? CommonConversions.GetTypeSyntax(declaredSymbol.ReturnType) :
            await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword)));

        var directlyConvertedCsIdentifier = CommonConversions.CsEscapedIdentifier(node.Identifier.Value as string);
        var parameterList = await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.ParameterList();
        var additionalDeclarations = new List<MemberDeclarationSyntax>();

        var hasExplicitInterfaceImplementation = declaredSymbol.IsNonPublicInterfaceImplementation() || declaredSymbol.IsRenamedInterfaceMember(directlyConvertedCsIdentifier, declaredSymbol.ExplicitInterfaceImplementations);
        directlyConvertedCsIdentifier = hasExplicitInterfaceImplementation ? directlyConvertedCsIdentifier : CommonConversions.ConvertIdentifier(node.Identifier);

        if (hasExplicitInterfaceImplementation) {
            var delegatingClause = ExpressionSyntaxExtensions.GetDelegatingClause(parameterList, directlyConvertedCsIdentifier, false);
            var explicitInterfaceModifiers = convertedModifiers.RemoveWhere(m => m.IsCsMemberVisibility() || m.IsKind(CS.SyntaxKind.VirtualKeyword, CS.SyntaxKind.AbstractKeyword) || m.IsKind(CS.SyntaxKind.OverrideKeyword, CS.SyntaxKind.NewKeyword));

            var interfaceDeclParams = new MethodDeclarationParameters(attributes, explicitInterfaceModifiers, returnType, typeParameters, parameterList, constraints, delegatingClause);
            _accessorDeclarationNodeConverter.AddInterfaceMemberDeclarations(declaredSymbol.ExplicitInterfaceImplementations, additionalDeclarations, interfaceDeclParams);
        }

        // If the method is virtual, and there is a MyClass.SomeMethod() call,
        // we need to emit a non-virtual method for it to call
        if (accessedThroughMyClass) {
            var identifierName = "MyClass" + directlyConvertedCsIdentifier.ValueText;
            var arrowClause = CS.SyntaxFactory.ArrowExpressionClause(CS.SyntaxFactory.InvocationExpression(ValidSyntaxFactory.IdentifierName(identifierName), parameterList.CreateDelegatingArgList()));
            var declModifiers = convertedModifiers;

            var originalNameDecl = CS.SyntaxFactory.MethodDeclaration(
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
                CS.SyntaxFactory.Token(CS.SyntaxKind.SemicolonToken)
            );

            additionalDeclarations.Add(originalNameDecl);
            convertedModifiers = convertedModifiers.Remove(convertedModifiers.Single(m => m.IsKind(CS.SyntaxKind.VirtualKeyword)));
            directlyConvertedCsIdentifier = CS.SyntaxFactory.Identifier(identifierName);
        }

        if (additionalDeclarations.Any()) {
            var declNode = (VBSyntax.StatementSyntax)node.FirstAncestorOrSelf<VBSyntax.MethodBlockBaseSyntax>() ?? node;
            _additionalDeclarations.Add(declNode, additionalDeclarations.ToArray());
        }

        var decl = CS.SyntaxFactory.MethodDeclaration(
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

        bool canHaveMethodBody = declaredSymbol?.CanHaveMethodBody() != false;
        return hasBody && canHaveMethodBody ? decl : decl.WithSemicolonToken(SemicolonToken);
    }

    public override async Task<CSharpSyntaxNode> VisitEventBlock(VBSyntax.EventBlockSyntax node)
    {
        var block = node.EventStatement;
        var attributes = await block.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, node.GetMemberContext());

        var rawType = await (block.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? ValidSyntaxFactory.VarType;

        var convertedAccessors = await node.Accessors.SelectAsync(async a => await a.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingDeclarationVisitor));
        _additionalDeclarations.Add(node, convertedAccessors.OfType<MemberDeclarationSyntax>().ToArray());
        return CS.SyntaxFactory.EventDeclaration(
            CS.SyntaxFactory.List(attributes),
            modifiers,
            rawType,
            null, CommonConversions.ConvertIdentifier(block.Identifier),
            CS.SyntaxFactory.AccessorList(CS.SyntaxFactory.List(convertedAccessors.OfType<AccessorDeclarationSyntax>()))
        );
    }

    public override async Task<CSharpSyntaxNode> VisitEventStatement(VBSyntax.EventStatementSyntax node)
    {
        var attributes = await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, node.GetMemberContext());
        var id = CommonConversions.ConvertIdentifier(node.Identifier);

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (node.AsClause == null && symbol.BaseMember() == null) {
            var delegateName = CS.SyntaxFactory.Identifier(id.ValueText + "EventHandler");

            var delegateDecl = CS.SyntaxFactory.DelegateDeclaration(
                CS.SyntaxFactory.List<AttributeListSyntax>(),
                modifiers.RemoveWhere(m => m.IsKind(CS.SyntaxKind.StaticKeyword)),
                CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword)),
                delegateName,
                null,
                await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.ParameterList(),
                CS.SyntaxFactory.List<TypeParameterConstraintClauseSyntax>()
            );

            var eventDecl = CS.SyntaxFactory.EventFieldDeclaration(
                CS.SyntaxFactory.List(attributes),
                modifiers,
                CS.SyntaxFactory.VariableDeclaration(ValidSyntaxFactory.IdentifierName(delegateName),
                    CS.SyntaxFactory.SingletonSeparatedList(CS.SyntaxFactory.VariableDeclarator(id)))
            );

            _additionalDeclarations.Add(node, new MemberDeclarationSyntax[] { delegateDecl });
            return eventDecl;
        }
        var type = symbol.Type != null || node.AsClause == null ? CommonConversions.GetTypeSyntax(symbol.Type) : await node.AsClause.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor);
        var declaration = CS.SyntaxFactory.VariableDeclaration(type,
            CS.SyntaxFactory.SingletonSeparatedList(CS.SyntaxFactory.VariableDeclarator(id)));
        return CS.SyntaxFactory.EventFieldDeclaration(
            CS.SyntaxFactory.List(attributes),
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
        var attributeList = CS.SyntaxFactory.List(attributes);
        var returnType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword));
        var parameterList = await node.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor);
        var methodBodyVisitor = await ConvertMethodBodyStatementsAsync(node, containingBlock.Statements);
        var body = CS.SyntaxFactory.Block(methodBodyVisitor);
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, node.GetMemberContext());

        var conversionModifiers = modifiers.Where(CommonConversions.IsConversionOperator).ToList();
        var nonConversionModifiers = CS.SyntaxFactory.TokenList(modifiers.Except(conversionModifiers));

        if (conversionModifiers.Any()) {
            return CS.SyntaxFactory.ConversionOperatorDeclaration(attributeList, nonConversionModifiers,
                conversionModifiers.Single(), returnType, parameterList, body, null);
        }

        return CS.SyntaxFactory.OperatorDeclaration(attributeList, nonConversionModifiers, returnType, node.OperatorToken.ConvertToken(), parameterList, body, null);
    }

    public override async Task<CSharpSyntaxNode> VisitConstructorBlock(VBSyntax.ConstructorBlockSyntax node)
    {
        var block = node.BlockStatement;
        var attributes = await block.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync);
        var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, node.GetMemberContext());

        var ctor = (node.Statements.FirstOrDefault() as VBSyntax.ExpressionStatementSyntax)?.Expression as VBSyntax.InvocationExpressionSyntax;
        var ctorExpression = ctor?.Expression as VBSyntax.MemberAccessExpressionSyntax;
        var ctorArgs = await (ctor?.ArgumentList).AcceptAsync<ArgumentListSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.ArgumentList();

        IEnumerable<VBSyntax.StatementSyntax> statements;
        ConstructorInitializerSyntax ctorCall;
        if (ctorExpression == null || !ctorExpression.Name.Identifier.IsKindOrHasMatchingText(VBasic.SyntaxKind.NewKeyword)) {
            statements = node.Statements;
            ctorCall = null;
        } else if (ctorExpression.Expression is VBSyntax.MyBaseExpressionSyntax) {
            statements = node.Statements.Skip(1);
            ctorCall = CS.SyntaxFactory.ConstructorInitializer(CS.SyntaxKind.BaseConstructorInitializer, ctorArgs);
        } else if (ctorExpression.Expression is VBSyntax.MeExpressionSyntax || ctorExpression.Expression is VBSyntax.MyClassExpressionSyntax) {
            statements = node.Statements.Skip(1);
            ctorCall = CS.SyntaxFactory.ConstructorInitializer(CS.SyntaxKind.ThisConstructorInitializer, ctorArgs);
        } else {
            statements = node.Statements;
            ctorCall = null;
        }

        var convertedBodyStatements = await ConvertMethodBodyStatementsAsync(node, statements.ToArray());
        return CS.SyntaxFactory.ConstructorDeclaration(
            CS.SyntaxFactory.List(attributes),
            modifiers, 
            CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier).WithoutSourceMapping(), //TODO Use semantic model for this name
            await block.ParameterList.AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor),
            ctorCall,
            CS.SyntaxFactory.Block(convertedBodyStatements)
        );
    }

    public override async Task<CSharpSyntaxNode> VisitDeclareStatement(VBSyntax.DeclareStatementSyntax node)
    {
        var importAttributes = new List<AttributeArgumentSyntax>();
        _extraUsingDirectives.Add(DllImportType.Namespace);
        _extraUsingDirectives.Add(CharSetType.Namespace);
        var dllImportAttributeName = CS.SyntaxFactory.ParseName(DllImportType.Name.Replace("Attribute", ""));
        var dllImportLibLiteral = await node.LibraryName.AcceptAsync<ExpressionSyntax>(_triviaConvertingExpressionVisitor);
        importAttributes.Add(CS.SyntaxFactory.AttributeArgument(dllImportLibLiteral));

        if (node.AliasName != null) {
            importAttributes.Add(CS.SyntaxFactory.AttributeArgument(CS.SyntaxFactory.NameEquals("EntryPoint"), null, await node.AliasName.AcceptAsync<ExpressionSyntax>(_triviaConvertingExpressionVisitor)));
        }

        if (!node.CharsetKeyword.IsKind(CS.SyntaxKind.None)) {
            importAttributes.Add(CS.SyntaxFactory.AttributeArgument(CS.SyntaxFactory.NameEquals(CharSetType.Name), null, CS.SyntaxFactory.MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, CS.SyntaxFactory.ParseTypeName(CharSetType.Name), ValidSyntaxFactory.IdentifierName(node.CharsetKeyword.Text))));
        }

        var attributeArguments = CommonConversions.CreateAttributeArgumentList(importAttributes.ToArray());
        var dllImportAttributeList = CS.SyntaxFactory.AttributeList(CS.SyntaxFactory.SingletonSeparatedList(CS.SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

        var attributeLists = (await CommonConversions.ConvertAttributesAsync(node.AttributeLists)).Add(dllImportAttributeList);

        var tokenContext = node.GetMemberContext();
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, tokenContext);
        if (!modifiers.Any(m => m.IsKind(CS.SyntaxKind.StaticKeyword))) {
            modifiers = modifiers.Add(CS.SyntaxFactory.Token(CS.SyntaxKind.StaticKeyword));
        }
        modifiers = modifiers.Add(CS.SyntaxFactory.Token(CS.SyntaxKind.ExternKeyword));

        var returnType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor) ?? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword));
        var parameterListSyntax = await (node.ParameterList).AcceptAsync<ParameterListSyntax>(_triviaConvertingExpressionVisitor) ??
                                  CS.SyntaxFactory.ParameterList();

        return CS.SyntaxFactory.MethodDeclaration(attributeLists, modifiers, returnType, null, CommonConversions.ConvertIdentifier(node.Identifier), null,
            parameterListSyntax, CS.SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), null, null).WithSemicolonToken(SemicolonToken);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterList(VBSyntax.TypeParameterListSyntax node)
    {
        return CS.SyntaxFactory.TypeParameterList(
            CS.SyntaxFactory.SeparatedList(await node.Parameters.SelectAsync(async p => await p.AcceptAsync<TypeParameterSyntax>(TriviaConvertingDeclarationVisitor)))
        );
    }

    private async Task<(TypeParameterListSyntax parameters, SyntaxList<TypeParameterConstraintClauseSyntax> constraints)> SplitTypeParametersAsync(VBSyntax.TypeParameterListSyntax typeParameterList)
    {
        var constraints = CS.SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();
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
        var parameters = CS.SyntaxFactory.TypeParameterList(CS.SyntaxFactory.SeparatedList(paramList));
        constraints = CS.SyntaxFactory.List(constraintList);
        return (parameters, constraints);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameter(VBSyntax.TypeParameterSyntax node)
    {
        SyntaxToken variance = default(SyntaxToken);
        if (!node.VarianceKeyword.IsKind(VBasic.SyntaxKind.None)) {
            variance = CS.SyntaxFactory.Token(node.VarianceKeyword.IsKind(VBasic.SyntaxKind.InKeyword) ? CS.SyntaxKind.InKeyword : CS.SyntaxKind.OutKeyword);
        }
        return CS.SyntaxFactory.TypeParameter(CS.SyntaxFactory.List<AttributeListSyntax>(), variance, CommonConversions.ConvertIdentifier(node.Identifier));
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterSingleConstraintClause(VBSyntax.TypeParameterSingleConstraintClauseSyntax node)
    {
        var id = CS.SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
        return CS.SyntaxFactory.TypeParameterConstraintClause(id, CS.SyntaxFactory.SingletonSeparatedList(await node.Constraint.AcceptAsync<TypeParameterConstraintSyntax>(TriviaConvertingDeclarationVisitor)));
    }

    public override async Task<CSharpSyntaxNode> VisitTypeParameterMultipleConstraintClause(VBSyntax.TypeParameterMultipleConstraintClauseSyntax node)
    {
        var id = CS.SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
        var constraints = await node.Constraints.SelectAsync(async c => await c.AcceptAsync<TypeParameterConstraintSyntax>(TriviaConvertingDeclarationVisitor));
        return CS.SyntaxFactory.TypeParameterConstraintClause(id, CS.SyntaxFactory.SeparatedList(constraints.OrderBy(c => c.Kind() == CS.SyntaxKind.ConstructorConstraint ? 1 : 0)));
    }

    public override async Task<CSharpSyntaxNode> VisitSpecialConstraint(VBSyntax.SpecialConstraintSyntax node)
    {
        if (node.ConstraintKeyword.IsKind(VBasic.SyntaxKind.NewKeyword))
            return CS.SyntaxFactory.ConstructorConstraint();
        return CS.SyntaxFactory.ClassOrStructConstraint(node.IsKind(VBasic.SyntaxKind.ClassConstraint) ? CS.SyntaxKind.ClassConstraint : CS.SyntaxKind.StructConstraint);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeConstraint(VBSyntax.TypeConstraintSyntax node)
    {
        return CS.SyntaxFactory.TypeConstraint(await node.Type.AcceptAsync<TypeSyntax>(_triviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlNamespaceImportsClause(VBSyntax.XmlNamespaceImportsClauseSyntax node)
    {
        var identifierName = await node.XmlNamespace.Name.AcceptAsync<IdentifierNameSyntax>(TriviaConvertingDeclarationVisitor);
        var valueLiteral = await node.XmlNamespace.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingDeclarationVisitor);
        var declarator = CS.SyntaxFactory.VariableDeclarator(identifierName.Identifier, null, CS.SyntaxFactory.EqualsValueClause(valueLiteral));
        return CS.SyntaxFactory.FieldDeclaration(
            CS.SyntaxFactory.List<AttributeListSyntax>(),
            CS.SyntaxFactory.TokenList(CS.SyntaxFactory.Token(CS.SyntaxKind.InternalKeyword), 
                CS.SyntaxFactory.Token(CS.SyntaxKind.StaticKeyword), 
                CS.SyntaxFactory.Token(CS.SyntaxKind.ReadOnlyKeyword)),
            CS.SyntaxFactory.VariableDeclaration(ValidSyntaxFactory.IdentifierName("XNamespace"), CS.SyntaxFactory.SingletonSeparatedList(declarator)));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlName(VBSyntax.XmlNameSyntax node)
    {
        if (node.Prefix == null && node.LocalName.ValueText == "xmlns") {
            // default namespace             
            return XmlImportContext.DefaultIdentifierName;
        } else if (node.Prefix.Name.ValueText == "xmlns") { 
            // namespace alias
            return ValidSyntaxFactory.IdentifierName(node.LocalName.ValueText);
        } else {
            // Having this case in VB would cause error BC31187: Namespace declaration must start with 'xmlns'
            throw new NotImplementedException($"Cannot convert non-xmlns attribute in XML namespace import").WithNodeInformation(node);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitXmlString(VBSyntax.XmlStringSyntax node) =>
        CommonConversions.Literal(node.TextTokens.Aggregate("", (a, b) => a + LiteralConversions.EscapeVerbatimQuotes(b.Text)));
}
