using System.Collections.Immutable;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using ICSharpCode.CodeConverter.VB.Trivia;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax;
using ArgumentSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayRankSpecifierSyntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax;
using AttributeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
using InterpolatedStringContentSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringContentSyntax;
using NameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax;
using OrderingSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.OrderingSyntax;
using ParameterListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterListSyntax;
using ParameterSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax;
using QueryClauseSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.QueryClauseSyntax;
using SimpleNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleNameSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeArgumentListSyntax;
using TypeParameterConstraintClauseSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeParameterConstraintClauseSyntax;
using TypeParameterListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeParameterListSyntax;
using TypeParameterSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeParameterSyntax;
using TypeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax;

namespace ICSharpCode.CodeConverter.VB;

/// <summary>
/// TODO: Split into DeclarationNodeVisitor and ExpressionNodeVisitor to match VB -> C# architecture
/// </summary>
internal class NodesVisitor : CS.CSharpSyntaxVisitor<VisualBasicSyntaxNode>
{
    private readonly CS.CSharpCompilation _compilation;
    private readonly SemanticModel _semanticModel;
    private readonly VisualBasicCompilation _vbViewOfCsSymbols;
    private readonly SyntaxGenerator _vbSyntaxGenerator;
    private readonly List<CSSyntax.UsingDirectiveSyntax> _importsToConvert = new();
    private readonly HashSet<string> _extraImports = new();
    private readonly CSharpHelperMethodDefinition _cSharpHelperMethodDefinition;
    private readonly CommonConversions _commonConversions;
    private readonly HashSet<string> _addedNames = new();

    private int _placeholder = 1;
    public CommentConvertingVisitorWrapper<VisualBasicSyntaxNode> TriviaConvertingVisitor { get; }
    public LanguageVersion LanguageVersion { get => _vbViewOfCsSymbols.Options.ParseOptions.LanguageVersion; }

    private string GeneratePlaceholder(string v)
    {
        return $"__{v}{_placeholder++}__";
    }

    public NodesVisitor(CS.CSharpCompilation compilation, SemanticModel semanticModel,
        VisualBasicCompilation vbViewOfCsSymbols, SyntaxGenerator vbSyntaxGenerator)
    {
        _compilation = compilation;
        _semanticModel = semanticModel;
        _vbViewOfCsSymbols = vbViewOfCsSymbols;
        _vbSyntaxGenerator = vbSyntaxGenerator;
        TriviaConvertingVisitor = new CommentConvertingVisitorWrapper<VisualBasicSyntaxNode>(this);
        _commonConversions = new CommonConversions(semanticModel, vbSyntaxGenerator, TriviaConvertingVisitor);
        _cSharpHelperMethodDefinition = new CSharpHelperMethodDefinition();
    }

    public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException($"Conversion for {CS.CSharpExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }

    public override VisualBasicSyntaxNode VisitCompilationUnit(CSSyntax.CompilationUnitSyntax node)
    {
        _importsToConvert.AddRange(node.Usings);
        foreach (var @extern in node.Externs)
            @extern.Accept(TriviaConvertingVisitor);
        var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => SyntaxFactory.AttributesStatement(SyntaxFactory.SingletonList((AttributeListSyntax)a.Accept(TriviaConvertingVisitor)))));
        var members = SyntaxFactory.List(node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor)));

        //TODO Add Usings from compilationoptions
        var importsStatementSyntaxes = SyntaxFactory.List(TidyImportsList(_importsToConvert).Select(import => (ImportsStatementSyntax) import.Accept(TriviaConvertingVisitor)).Concat(_extraImports.Select(Import)));
        return SyntaxFactory.CompilationUnit(
            SyntaxFactory.List<OptionStatementSyntax>(),
            importsStatementSyntaxes,
            attributes,
            members
        );
    }
    private static IEnumerable<CSSyntax.UsingDirectiveSyntax> TidyImportsList(IEnumerable<CSSyntax.UsingDirectiveSyntax> usings)
    {
        return usings
            .GroupBy(x => x.ToString())
            .Select(g => g.First());
    }

    private static ImportsStatementSyntax Import(string import)
    {
        return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName(import))));
    }

    #region Attributes
    public override VisualBasicSyntaxNode VisitAttributeList(CSSyntax.AttributeListSyntax node)
    {
        return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(node.Attributes.Select(a => (AttributeSyntax)a.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitAttribute(CSSyntax.AttributeSyntax node)
    {
        var list = (CSSyntax.AttributeListSyntax)node.Parent;
        return SyntaxFactory.Attribute((AttributeTargetSyntax)list.Target?.Accept(TriviaConvertingVisitor), (TypeSyntax)node.Name.Accept(TriviaConvertingVisitor), (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitAttributeTargetSpecifier(CSSyntax.AttributeTargetSpecifierSyntax node)
    {
        SyntaxToken id;
        switch (CS.CSharpExtensions.Kind(node.Identifier)) {
            case CS.SyntaxKind.AssemblyKeyword:
                id = SyntaxFactory.Token(SyntaxKind.AssemblyKeyword);
                break;
            case CS.SyntaxKind.ReturnKeyword:
                // not necessary, return attributes are moved by ConvertAndSplitAttributes.
                return null;
            default:
                throw new NotSupportedException();
        }
        return SyntaxFactory.AttributeTarget(id);
    }

    public override VisualBasicSyntaxNode VisitAttributeArgumentList(CSSyntax.AttributeArgumentListSyntax node)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitAttributeArgument(CSSyntax.AttributeArgumentSyntax node)
    {
        NameColonEqualsSyntax name = null;
        if (node.NameColon != null) {
            name = SyntaxFactory.NameColonEquals((IdentifierNameSyntax)node.NameColon.Name.Accept(TriviaConvertingVisitor));
        } else if (node.NameEquals != null) {
            name = SyntaxFactory.NameColonEquals((IdentifierNameSyntax)node.NameEquals.Name.Accept(TriviaConvertingVisitor));
        }
        var value = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        return SyntaxFactory.SimpleArgument(name, value);
    }
    #endregion

    public override VisualBasicSyntaxNode VisitNamespaceDeclaration(CSSyntax.NamespaceDeclarationSyntax node)
    {
        foreach (var @using in node.Usings)
            _importsToConvert.AddRange(node.Usings);
        foreach (var @extern in node.Externs)
            @extern.Accept(TriviaConvertingVisitor);
        var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor));

        return SyntaxFactory.NamespaceBlock(
            SyntaxFactory.NamespaceStatement((NameSyntax)node.Name.Accept(TriviaConvertingVisitor)),
            SyntaxFactory.List(members)
        );
    }

    public override VisualBasicSyntaxNode VisitUsingDirective(CSSyntax.UsingDirectiveSyntax node)
    {
        var nameToImport = _semanticModel.GetSymbolInfo(node.Name).Symbol is INamespaceOrTypeSymbol toImport
            ? _commonConversions.GetFullyQualifiedNameSyntax(toImport, false)
            : (NameSyntax)node.Name.Accept(TriviaConvertingVisitor);
        SimpleImportsClauseSyntax clause = SyntaxFactory.SimpleImportsClause(nameToImport);

        if (node.Alias != null) {
            var name = node.Alias.Name;
            var id = _commonConversions.ConvertIdentifier(name.Identifier);
            var alias = SyntaxFactory.ImportAliasClause(id);
            clause = clause.WithAlias(alias);
        }

        return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(clause));
    }

    #region Namespace Members

    public override VisualBasicSyntaxNode VisitClassDeclaration(CSSyntax.ClassDeclarationSyntax node)
    {
        var members = ConvertMembers(node).ToList();
        var id = _commonConversions.ConvertIdentifier(node.Identifier);

        List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
        List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
        _commonConversions.ConvertBaseList(node, inherits, implements);
        var declaredSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        members.AddRange(_cSharpHelperMethodDefinition.GetExtraMembers(declaredSymbol));
        if (CanBeModule(node)) {
            return SyntaxFactory.ModuleBlock(
                SyntaxFactory.ModuleStatement(
                    SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.InterfaceOrModule),
                    id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
                ),
                SyntaxFactory.List(inherits),
                SyntaxFactory.List(implements),
                SyntaxFactory.List(members)
            );
        }

        return SyntaxFactory.ClassBlock(
            SyntaxFactory.ClassStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers),
                id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
            ),
            SyntaxFactory.List(inherits),
            SyntaxFactory.List(implements),
            SyntaxFactory.List(members)
        );
    }

    private IEnumerable<StatementSyntax> ConvertMembers(CSSyntax.TypeDeclarationSyntax node)
    {
        var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor));
        var newmembers = _commonConversions.InsertGeneratedClassMemberDeclarations(SyntaxFactory.List(members), node, CanBeModule(node));
        return newmembers;
    }

    public override VisualBasicSyntaxNode VisitStructDeclaration(CSSyntax.StructDeclarationSyntax node)
    {
        var members = ConvertMembers(node).ToList();

        List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
        List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
        _commonConversions.ConvertBaseList(node, inherits, implements);
        var declaredSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        members.AddRange(_cSharpHelperMethodDefinition.GetExtraMembers(declaredSymbol));

        return SyntaxFactory.StructureBlock(
            SyntaxFactory.StructureStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers), _commonConversions.ConvertIdentifier(node.Identifier),
                (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
            ),
            SyntaxFactory.List(inherits),
            SyntaxFactory.List(implements),
            SyntaxFactory.List(members)
        );
    }

    public override VisualBasicSyntaxNode VisitInterfaceDeclaration(CSSyntax.InterfaceDeclarationSyntax node)
    {
        var members = ConvertMembers(node).ToArray();

        List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
        List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
        _commonConversions.ConvertBaseList(node, inherits, implements);

        return SyntaxFactory.InterfaceBlock(
            SyntaxFactory.InterfaceStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers), _commonConversions.ConvertIdentifier(node.Identifier),
                (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
            ),
            SyntaxFactory.List(inherits),
            SyntaxFactory.List(implements),
            SyntaxFactory.List(members)
        );
    }

    public override VisualBasicSyntaxNode VisitEnumDeclaration(CSSyntax.EnumDeclarationSyntax node)
    {
        var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor));
        var baseType = (TypeSyntax)node.BaseList?
            .Types.OfType<CSSyntax.SimpleBaseTypeSyntax>().Single().Type.Accept(TriviaConvertingVisitor);
        return SyntaxFactory.EnumBlock(
            SyntaxFactory.EnumStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers), _commonConversions.ConvertIdentifier(node.Identifier),
                baseType == null ? null : SyntaxFactory.SimpleAsClause(baseType)
            ),
            SyntaxFactory.List(members)
        );
    }

    public override VisualBasicSyntaxNode VisitEnumMemberDeclaration(CSSyntax.EnumMemberDeclarationSyntax node)
    {
        var initializer = (ExpressionSyntax)node.EqualsValue?.Value.Accept(TriviaConvertingVisitor);
        return SyntaxFactory.EnumMemberDeclaration(
            SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), _commonConversions.ConvertIdentifier(node.Identifier),
            initializer == null ? null : SyntaxFactory.EqualsValue(initializer)
        );
    }

    public override VisualBasicSyntaxNode VisitDelegateDeclaration(CSSyntax.DelegateDeclarationSyntax node)
    {
        var id = _commonConversions.ConvertIdentifier(node.Identifier);
        var methodInfo = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        if (methodInfo?.DelegateInvokeMethod.GetReturnType()?.SpecialType == SpecialType.System_Void) {
            return SyntaxFactory.DelegateSubStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers),
                id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                null
            );
        }

        return SyntaxFactory.DelegateFunctionStatement(
            SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers),
            id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
            (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
            SyntaxFactory.SimpleAsClause((TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor))
        );
    }

    #endregion

    #region Type Members

    public override VisualBasicSyntaxNode VisitFieldDeclaration(CSSyntax.FieldDeclarationSyntax node)
    {
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));
        if (modifiers.Count == 0)
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))),
            modifiers, _commonConversions.RemodelVariableDeclaration(node.Declaration)
        );
    }

    public override VisualBasicSyntaxNode VisitConstructorDeclaration(CSSyntax.ConstructorDeclarationSyntax node)
    {
        var initializer = new[] { (StatementSyntax)node.Initializer?.Accept(TriviaConvertingVisitor) }.Where(x => x != null);
        return SyntaxFactory.ConstructorBlock(
            SyntaxFactory.SubNewStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node), true),
                (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor)
            ),
            SyntaxFactory.List(initializer.Concat(_commonConversions.ConvertBody(node.Body, node.ExpressionBody, false)))
        );
    }

    public override VisualBasicSyntaxNode VisitIsPatternExpression(CSSyntax.IsPatternExpressionSyntax node)
    {
        ExpressionSyntax lhs = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        switch (node.Pattern) {
            case CSSyntax.DeclarationPatternSyntax d: {
                var left = (ExpressionSyntax)d.Designation.Accept(TriviaConvertingVisitor);
                ExpressionSyntax right = SyntaxFactory.TryCastExpression(
                    lhs,
                    (TypeSyntax)d.Type.Accept(TriviaConvertingVisitor));

                var tryCast = CreateInlineAssignmentExpression(left, right, GetStructOrClassSymbol(node));
                var nothingExpression = SyntaxFactory.LiteralExpression(SyntaxKind.NothingLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.NothingKeyword));
                return SyntaxFactory.IsNotExpression(tryCast, nothingExpression);
            }
            case CSSyntax.ConstantPatternSyntax cps:
                return SyntaxFactory.IsExpression(lhs,
                    (ExpressionSyntax)cps.Expression.Accept(TriviaConvertingVisitor));
            default:
                throw new ArgumentOutOfRangeException(nameof(node), node.Pattern, null);
        }
    }

    public override VisualBasicSyntaxNode VisitDeclarationExpression(CSSyntax.DeclarationExpressionSyntax node)
    {
        return node.Designation.Accept(TriviaConvertingVisitor);
    }

    public override VisualBasicSyntaxNode VisitSingleVariableDesignation(CSSyntax.SingleVariableDesignationSyntax node)
    {
        return SyntaxFactory.IdentifierName(_commonConversions.ConvertIdentifier(node.Identifier));
    }

    public override VisualBasicSyntaxNode VisitDiscardDesignation(CSSyntax.DiscardDesignationSyntax node)
    {
        return SyntaxFactory.IdentifierName("__");
    }

    public override VisualBasicSyntaxNode VisitConstructorInitializer(CSSyntax.ConstructorInitializerSyntax node)
    {
        var initializerExpression = GetInitializerExpression(node);
        var newMethodCall = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            initializerExpression, SyntaxFactory.Token(SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("New"));

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(newMethodCall, (ArgumentListSyntax)node.ArgumentList.Accept(TriviaConvertingVisitor)));
    }

    private static ExpressionSyntax GetInitializerExpression(CSSyntax.ConstructorInitializerSyntax node)
    {
        if (node.IsKind(CS.SyntaxKind.BaseConstructorInitializer)) {
            return SyntaxFactory.MyBaseExpression();
        }

        if (node.IsKind(CS.SyntaxKind.ThisConstructorInitializer)) {
            return SyntaxFactory.MeExpression();
        }

        throw new ArgumentOutOfRangeException(nameof(node), node, $"{CS.CSharpExtensions.Kind(node)} unknown");
    }

    public override VisualBasicSyntaxNode VisitDestructorDeclaration(CSSyntax.DestructorDeclarationSyntax node)
    {
        return SyntaxFactory.SubBlock(
            SyntaxFactory.SubStatement(
                SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverridesKeyword)),
                SyntaxFactory.Identifier("Finalize"), null,
                (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                null, null, null
            ), _commonConversions.ConvertBody(node.Body, node.ExpressionBody, false)
        );
    }

    public override VisualBasicSyntaxNode VisitMethodDeclaration(CSSyntax.MethodDeclarationSyntax node)
    {
        var isIteratorState = new MethodBodyExecutableStatementVisitor(_semanticModel, TriviaConvertingVisitor, _commonConversions);
        bool requiresBody = node.Body != null || node.ExpressionBody != null || node.Modifiers.Any(m => m.IsKind(CS.SyntaxKind.ExternKeyword, CS.SyntaxKind.PartialKeyword));
        var methodInfo = _semanticModel.GetDeclaredSymbol(node);
        bool isVoidSub = methodInfo?.GetReturnType()?.SpecialType == SpecialType.System_Void;
        var block = _commonConversions.ConvertBody(node.Body, node.ExpressionBody, !isVoidSub, isIteratorState);
        var id = _commonConversions.ConvertIdentifier(node.Identifier);
        var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor)));
        var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));
        if (isIteratorState.IsIterator)
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.IteratorKeyword));
        if (node.ParameterList.Parameters.Count > 0 && node.ParameterList.Parameters[0].Modifiers.Any(CS.SyntaxKind.ThisKeyword)) {
            attributes = attributes.Insert(0, SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(null, SyntaxFactory.ParseTypeName("Extension"), SyntaxFactory.ArgumentList()))));
            if (!((CS.CSharpSyntaxTree)node.SyntaxTree).HasUsingDirective("System.Runtime.CompilerServices"))
                _extraImports.Add(nameof(System) + "." + nameof(System.Runtime) + "." + nameof(System.Runtime.CompilerServices));
        }
        var needsOverloads = methodInfo?.ContainingType?.GetMembers(methodInfo.Name).Except(methodInfo.Yield()).Any(m => m.IsOverride);
        if (needsOverloads == true) {
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverloadsKeyword));
        }
        var implementsClause = methodInfo == null ? null : CreateImplementsClauseSyntaxOrNull(methodInfo, ref id);
        if (isVoidSub) {
            var stmt = SyntaxFactory.SubStatement(
                attributes,
                modifiers,
                id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                parameterList,
                null, null, implementsClause
            );
            if (!requiresBody) return stmt;
            return SyntaxFactory.SubBlock(stmt, block);
        } else {
            var stmt = SyntaxFactory.FunctionStatement(
                attributes,
                modifiers,
                id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                parameterList,
                SyntaxFactory.SimpleAsClause((TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor)), null, implementsClause
            );
            if (!requiresBody) return stmt;
            return SyntaxFactory.FunctionBlock(stmt, block);
        }
    }

    /// <remarks>
    /// PERF: Computational complexity high due to starting with all members and narrowing down
    /// </remarks>
    private ImplementsClauseSyntax CreateImplementsClauseSyntaxOrNull(ISymbol memberInfo, ref SyntaxToken id)
    {
        var originalId = id;
        var explicitImplementors = memberInfo.ExplicitInterfaceImplementations();
        if (explicitImplementors.Any()) {
            //https://github.com/icsharpcode/CodeConverter/issues/492
#pragma warning disable RS1024 // Compare symbols correctly - analyzer bug, I'm using a string not the default ambiguous comparer
            var memberNames = memberInfo.ContainingType.GetMembers().ToLookup(s => UndottedMemberName(s.Name), StringComparer.OrdinalIgnoreCase);
#pragma warning restore RS1024 // Compare symbols correctly
            string explicitMemberName = UndottedMemberName(memberInfo.Name);
            var hasDuplicateNames = memberNames[explicitMemberName].Count() > 1;
            if (hasDuplicateNames) id = SyntaxFactory.Identifier(NameGenerator.GenerateUniqueName(explicitMemberName, n => !memberNames.Contains(n) && _addedNames.Add(n)));
        } else {
            var containingType = memberInfo.ContainingType;
            var baseClassesAndInterfaces = containingType.GetAllBaseClassesAndInterfaces(true);
            explicitImplementors = baseClassesAndInterfaces.Except(new[] { containingType })
                .SelectMany(t => t.GetMembers().Where(m => memberInfo.Name.EndsWith(m.Name, StringComparison.InvariantCulture)))
                .Where(m => containingType.FindImplementationForInterfaceMember(m)?.Equals(memberInfo, SymbolEqualityComparer.IncludeNullability) == true)
                .ToImmutableArray();
        }

        return !explicitImplementors.Any() ? null : CreateImplementsClauseSyntax(explicitImplementors, originalId);
    }

    private static string UndottedMemberName(string n)
    {
        return n.Split('.').Last();
    }

    private ImplementsClauseSyntax CreateImplementsClauseSyntax(IEnumerable<ISymbol> implementors, SyntaxToken id) {
        return SyntaxFactory.ImplementsClause(implementors.Select(x => {
                NameSyntax nameSyntax = _commonConversions.GetFullyQualifiedNameSyntax(x.ContainingSymbol as INamedTypeSymbol);
                return SyntaxFactory.QualifiedName(nameSyntax, SyntaxFactory.IdentifierName(id));
            }).ToArray()
        );
    }

    public override VisualBasicSyntaxNode VisitPropertyDeclaration(CSSyntax.PropertyDeclarationSyntax node)
    {
        var id = _commonConversions.ConvertIdentifier(node.Identifier);
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));
        var initializer = node.Initializer == null ? null
            : SyntaxFactory.EqualsValue((ExpressionSyntax)_commonConversions.ConvertTopLevelExpression(node.Initializer.Value));
        return ConvertPropertyBlock(node, id, modifiers, null, node.ExpressionBody, initializer);
    }

    public override VisualBasicSyntaxNode VisitIndexerDeclaration(CSSyntax.IndexerDeclarationSyntax node)
    {
        var id = _commonConversions.ConvertIdentifier(node.ThisKeyword);
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));
        if (modifiers.Any(x => x.Kind() == SyntaxKind.PrivateKeyword)) {
        } else {
            modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.DefaultKeyword));
        }
        var parameterListSyntax = (ParameterListSyntax) node.ParameterList?.Accept(TriviaConvertingVisitor);
        return ConvertPropertyBlock(node, id, modifiers, parameterListSyntax, node.ExpressionBody, null);
    }

    private VisualBasicSyntaxNode ConvertPropertyBlock(CSSyntax.BasePropertyDeclarationSyntax node,
        SyntaxToken id, SyntaxTokenList modifiers,
        ParameterListSyntax parameterListSyntax, CSSyntax.ArrowExpressionClauseSyntax arrowExpressionClauseSyntax,
        EqualsValueSyntax initializerOrNull)
    {
        ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);

        bool isIterator = false;
        List<AccessorBlockSyntax> accessors = new List<AccessorBlockSyntax>();
        var hasAccessors = node.AccessorList != null;
        IPropertySymbol declaredSymbol = _semanticModel.GetDeclaredSymbol(node) as IPropertySymbol;
        modifiers = modifiers.AddRange(GetAccessLimitationSyntaxKinds(declaredSymbol).Select(x => SyntaxFactory.Token(x)));
        Func<PropertyStatementSyntax> getStatementSyntax = () => {
            var implementsClauseSyntaxOrNull = declaredSymbol == null ? null : CreateImplementsClauseSyntaxOrNull(declaredSymbol, ref id);
            return SyntaxFactory.PropertyStatement(
                attributes,
                modifiers,
                id,
                parameterListSyntax,
                SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)),
                initializerOrNull,
                implementsClauseSyntaxOrNull
            );
        };

        if (hasAccessors && !RequiresAccessorBody(node.AccessorList))
            return getStatementSyntax();

        if (hasAccessors) {
            var csAccessors = node.AccessorList.Accessors;
            bool isAutoImplementedProperty = !node.IsKind(CS.SyntaxKind.IndexerDeclaration) && csAccessors.All(x => x.Body == null);
            foreach (var a in csAccessors)
            {
                accessors.Add(_commonConversions.ConvertAccessor(a, out var isAIterator, isAutoImplementedProperty));
                isIterator |= isAIterator;
            }
            if (isIterator) modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.IteratorKeyword));
        }
        else {
            StatementSyntax expressionStatementSyntax =
                SyntaxFactory.ReturnStatement(
                    (ExpressionSyntax) arrowExpressionClauseSyntax.Expression.Accept(TriviaConvertingVisitor));
            var accessorStatementSyntax = SyntaxFactory.AccessorStatement(SyntaxKind.GetAccessorStatement,
                SyntaxFactory.Token(SyntaxKind.GetKeyword));
            accessors.Add(SyntaxFactory.GetAccessorBlock(accessorStatementSyntax,
                SyntaxFactory.SingletonList(expressionStatementSyntax), SyntaxFactory.EndGetStatement()));
        }
        return SyntaxFactory.PropertyBlock(getStatementSyntax(), SyntaxFactory.List(accessors));
    }

    private static IEnumerable<SyntaxKind> GetAccessLimitationSyntaxKinds(IPropertySymbol propertySymbol)
    {
        if (propertySymbol.IsReadOnly)
            return SyntaxKind.ReadOnlyKeyword.Yield();
        if (propertySymbol.IsWriteOnly)
            return SyntaxKind.WriteOnlyKeyword.Yield();
        return Enumerable.Empty<SyntaxKind>();
    }

    private static bool RequiresAccessorBody(CSSyntax.AccessorListSyntax accessorListSyntaxOrNull)
    {
        return accessorListSyntaxOrNull.Accessors.Any(a => a.Body != null || a.ExpressionBody != null || a.Modifiers.ContainsDeclaredVisibility());
    }

    private static TokenContext GetMemberContext(CSSyntax.MemberDeclarationSyntax member)
    {
        var parentType = member.GetAncestorOrThis<CSSyntax.TypeDeclarationSyntax>();
        var parentTypeKind = parentType?.Kind();
        switch (parentTypeKind) {
            case CS.SyntaxKind.ClassDeclaration:
                return CanBeModule(parentType) ? TokenContext.MemberInModule : TokenContext.MemberInClass;
            case CS.SyntaxKind.InterfaceDeclaration:
                return TokenContext.MemberInInterface;
            case CS.SyntaxKind.StructDeclaration:
                return TokenContext.MemberInStruct;
            default:
                throw new ArgumentOutOfRangeException(nameof(member), parentTypeKind, null);
        }
    }
    private static bool CanBeModule(CSSyntax.TypeDeclarationSyntax type) {
        var parentType = type.GetAncestor<CSSyntax.TypeDeclarationSyntax>();
        return type.Modifiers.Any(CS.SyntaxKind.StaticKeyword) && type.TypeParameterList == null && parentType == null;
    }
    public override VisualBasicSyntaxNode VisitEventDeclaration(CSSyntax.EventDeclarationSyntax node)
    {
        ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);
        var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
        var id = _commonConversions.ConvertIdentifier(node.Identifier);
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node))
            .Add(SyntaxFactory.Token(SyntaxKind.CustomKeyword));
        var implementsClauseSyntaxOrNull = declaredSymbol == null ? null : CreateImplementsClauseSyntaxOrNull(declaredSymbol, ref id);
        var stmt = SyntaxFactory.EventStatement(
            attributes, modifiers, id, null,
            SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)),
            implementsClauseSyntaxOrNull
        );
        if (!RequiresAccessorBody(node.AccessorList))
            return stmt;
        var accessors = node.AccessorList?.Accessors.Select(a => _commonConversions.ConvertAccessor(a, out bool unused)).ToList();

        var eventHandlerSymbol = _semanticModel.GetTypeInfo(node.Type).Type.GetDelegateInvokeMethod();
        var raiseEventParameters = eventHandlerSymbol.Parameters.Select(x =>
            SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(x.Name))
                .WithAsClause(SyntaxFactory.SimpleAsClause(GetTypeSyntax(x.Type)))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)))
        );
        var csEventFieldIdentifier = node.AccessorList?.Accessors
            .SelectMany(x => x.Body.Statements)
            .OfType<CSSyntax.ExpressionStatementSyntax>()
            .Where(x => x.Expression != null)
            .Select(x => x.Expression)
            .OfType<CSSyntax.AssignmentExpressionSyntax>()
            .SelectMany(x => x.Left.DescendantNodesAndSelf().OfType<CSSyntax.IdentifierNameSyntax>())
            .FirstOrDefault();
        var eventFieldIdentifier = (IdentifierNameSyntax)csEventFieldIdentifier?.Accept(TriviaConvertingVisitor, false);

        var raiseEventAccessor = SyntaxFactory.RaiseEventAccessorBlock(
            SyntaxFactory.RaiseEventAccessorStatement(
                attributes,
                SyntaxFactory.TokenList(),
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(raiseEventParameters))
            )
        );
        if (eventFieldIdentifier != null) {
            if (_semanticModel.GetSymbolInfo(csEventFieldIdentifier).Symbol.Kind == SymbolKind.Event) {
                raiseEventAccessor = raiseEventAccessor.WithStatements(SyntaxFactory.SingletonList(
                        (StatementSyntax)SyntaxFactory.RaiseEventStatement(eventFieldIdentifier,
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(raiseEventParameters.Select(x => SyntaxFactory.SimpleArgument(SyntaxFactory.IdentifierName(x.Identifier.Identifier))).Cast<ArgumentSyntax>())))
                    )
                );
            } else {
                if ((int)LanguageVersion < 14) {
                    var conditionalStatement = _vbSyntaxGenerator.IfStatement(
                        _vbSyntaxGenerator.ReferenceNotEqualsExpression(eventFieldIdentifier,
                            _vbSyntaxGenerator.NullLiteralExpression()),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.ParseExpression(eventFieldIdentifier.Identifier.ValueText),
                            raiseEventParameters.Select(x => SyntaxFactory.IdentifierName(x.Identifier.Identifier)).CreateVbArgList()).Yield()
                    );
                    raiseEventAccessor = raiseEventAccessor.WithStatements(SyntaxFactory.SingletonList(conditionalStatement));
                } else {
                    var invocationExpression =
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.ParseExpression(
                                eventFieldIdentifier.Identifier.ValueText +
                                "?"), //I think this syntax tree is the wrong shape, but using the right shape causes the simplifier to fail
                            raiseEventParameters.Select(x => SyntaxFactory.IdentifierName(x.Identifier.Identifier))
                                .CreateVbArgList()
                        );
                    raiseEventAccessor = raiseEventAccessor.WithStatements(
                        SyntaxFactory.SingletonList(
                            (StatementSyntax)SyntaxFactory.ExpressionStatement(invocationExpression)));
                }
            }
        }

        accessors.Add(raiseEventAccessor);
        return SyntaxFactory.EventBlock(stmt, SyntaxFactory.List(accessors));
    }

    public override VisualBasicSyntaxNode VisitEventFieldDeclaration(CSSyntax.EventFieldDeclarationSyntax node)
    {
        var decl = node.Declaration.Variables.Single();
        var id = SyntaxFactory.Identifier(decl.Identifier.ValueText, SyntaxFacts.IsKeywordKind(decl.Identifier.Kind()), decl.Identifier.GetIdentifierText(), TypeCharacter.None);
        ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);
        var declaredSymbol = _semanticModel.GetDeclaredSymbol(decl);
        var implementsClauseSyntaxOrNull = declaredSymbol == null ? null : CreateImplementsClauseSyntaxOrNull(declaredSymbol, ref id);
        return SyntaxFactory.EventStatement(
            attributes,
            CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node)),
            id,
            null,
            SyntaxFactory.SimpleAsClause(returnAttributes,
                (TypeSyntax)node.Declaration.Type.Accept(TriviaConvertingVisitor)),
            implementsClauseSyntaxOrNull);
    }
    private TypeSyntax GetTypeSyntax(ITypeSymbol typeInfo) {
        return (TypeSyntax) _vbSyntaxGenerator.TypeExpression(typeInfo);
    }

    private void ConvertAndSplitAttributes(SyntaxList<CSSyntax.AttributeListSyntax> attributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes)
    {
        var retAttr = new List<AttributeListSyntax>();
        var attr = new List<AttributeListSyntax>();

        foreach (var attrList in attributeLists) {
            var targetIdentifier = attrList.Target?.Identifier;
            if (targetIdentifier != null && SyntaxTokenExtensions.IsKind((SyntaxToken)targetIdentifier, CS.SyntaxKind.ReturnKeyword))
                retAttr.Add((AttributeListSyntax)attrList.Accept(TriviaConvertingVisitor));
            else
                attr.Add((AttributeListSyntax)attrList.Accept(TriviaConvertingVisitor));
        }
        returnAttributes = SyntaxFactory.List(retAttr);
        attributes = SyntaxFactory.List(attr);
    }

    public override VisualBasicSyntaxNode VisitOperatorDeclaration(CSSyntax.OperatorDeclarationSyntax node)
    {
        ConvertAndSplitAttributes(node.AttributeLists, out var attributes, out var returnAttributes);
        var body = _commonConversions.ConvertBody(node.Body, node.ExpressionBody, true);
        var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
        var firstParam = node.ParameterList?.Parameters.FirstOrDefault()
                         ?? throw new NotSupportedException("Operator overloads with no parameters aren't supported");
        var firstParameterIsString = _semanticModel.GetTypeInfo(firstParam.Type).ConvertedType.SpecialType == SpecialType.System_String;
        var stmt = SyntaxFactory.OperatorStatement(
            attributes, CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node)),
            SyntaxFactory.Token(ConvertOperatorDeclarationToken(CS.CSharpExtensions.Kind(node.OperatorToken), firstParameterIsString)),
            parameterList,
            SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor))
        );
        return SyntaxFactory.OperatorBlock(stmt, body);
    }



    private static SyntaxKind ConvertOperatorDeclarationToken(CS.SyntaxKind syntaxKind, bool firstParameterIsString)
    {
        switch (syntaxKind) {
            case CS.SyntaxKind.PlusToken:
                return firstParameterIsString ? SyntaxKind.AmpersandToken : SyntaxKind.PlusToken;
            case CS.SyntaxKind.MinusToken:
                return SyntaxKind.MinusToken;
            case CS.SyntaxKind.ExclamationToken:
                return SyntaxKind.NotKeyword;
            case CS.SyntaxKind.AsteriskToken:
                return SyntaxKind.AsteriskToken;
            case CS.SyntaxKind.SlashToken:
                return SyntaxKind.SlashToken;
            case CS.SyntaxKind.PercentToken:
                return SyntaxKind.ModKeyword;
            case CS.SyntaxKind.LessThanLessThanToken:
                return SyntaxKind.LessThanLessThanToken;
            case CS.SyntaxKind.GreaterThanGreaterThanToken:
                return SyntaxKind.GreaterThanGreaterThanToken;
            case CS.SyntaxKind.EqualsEqualsToken:
                return SyntaxKind.EqualsToken;
            case CS.SyntaxKind.ExclamationEqualsToken:
                return SyntaxKind.LessThanGreaterThanToken;
            case CS.SyntaxKind.GreaterThanToken:
                return SyntaxKind.GreaterThanToken;
            case CS.SyntaxKind.LessThanToken:
                return SyntaxKind.LessThanToken;
            case CS.SyntaxKind.GreaterThanEqualsToken:
                return SyntaxKind.GreaterThanEqualsToken;
            case CS.SyntaxKind.LessThanEqualsToken:
                return SyntaxKind.LessThanEqualsToken;
            case CS.SyntaxKind.AmpersandToken:
                return SyntaxKind.AndKeyword;
            case CS.SyntaxKind.BarToken:
                return SyntaxKind.OrKeyword;
        }
        throw new NotSupportedException($"{nameof(syntaxKind)} of {syntaxKind} cannot be converted");
    }

    public override VisualBasicSyntaxNode VisitConversionOperatorDeclaration(CSSyntax.ConversionOperatorDeclarationSyntax node)
    {
        ConvertAndSplitAttributes(node.AttributeLists, out var attributes, out var returnAttributes);
        var body = _commonConversions.ConvertBody(node.Body, node.ExpressionBody, true);
        var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
        var modifiers = node.GetModifiers();
        modifiers= modifiers.Add(node.ImplicitOrExplicitKeyword);
        var stmt = SyntaxFactory.OperatorStatement(
            attributes, CommonConversions.ConvertModifiers(modifiers, GetMemberContext(node)),
            SyntaxFactory.Token(SyntaxKind.CTypeKeyword),
            parameterList,
            SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor))
        );
        return SyntaxFactory.OperatorBlock(stmt, body);
    }

    public override VisualBasicSyntaxNode VisitParameterList(CSSyntax.ParameterListSyntax node)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(node.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitBracketedParameterList(CSSyntax.BracketedParameterListSyntax node)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(node.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitTupleType(CSSyntax.TupleTypeSyntax node)
    {
        var elements = node.Elements.Select(e => (TupleElementSyntax)e.Accept(TriviaConvertingVisitor));
        return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
    }

    public override VisualBasicSyntaxNode VisitTupleElement(CSSyntax.TupleElementSyntax node)
    {
        return SyntaxFactory.TypedTupleElement((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitTupleExpression(CSSyntax.TupleExpressionSyntax node)
    {
        var args = node.Arguments.Select(a => {
            var expr = (ExpressionSyntax)a.Expression.Accept(TriviaConvertingVisitor);
            return SyntaxFactory.SimpleArgument(expr);
        });
        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(args));
    }

    public override VisualBasicSyntaxNode VisitParenthesizedVariableDesignation(CSSyntax.ParenthesizedVariableDesignationSyntax node)
    {
        return SyntaxFactory.IdentifierName(CommonConversions.GetTupleName(node));
    }

    public override VisualBasicSyntaxNode VisitParameter(CSSyntax.ParameterSyntax node)
    {
        var id = _commonConversions.ConvertIdentifier(node.Identifier);
        var returnType = (TypeSyntax)node.Type?.Accept(TriviaConvertingVisitor);
        EqualsValueSyntax @default = null;
        if (node.Default != null) {
            @default = SyntaxFactory.EqualsValue((ExpressionSyntax)node.Default?.Value.Accept(TriviaConvertingVisitor));
        }
        AttributeListSyntax[] newAttributes;
        var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Local);
        if ((modifiers.Count == 0 && returnType != null) || node.Modifiers.Any(CS.SyntaxKind.ThisKeyword)) {
            modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword));
            newAttributes = Array.Empty<AttributeListSyntax>();
        } else if (node.Modifiers.Any(CS.SyntaxKind.OutKeyword)) {
            newAttributes = new[] {
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseTypeName("Out"))
                    )
                )
            };
            _extraImports.Add(nameof(System) + "." + nameof(System.Runtime) + "." + nameof(System.Runtime.InteropServices));
        } else {
            newAttributes = Array.Empty<AttributeListSyntax>();
        }
        if (@default != null) {
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.OptionalKeyword));
        }
        return SyntaxFactory.Parameter(
            SyntaxFactory.List(newAttributes.Concat(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor)))),
            modifiers,
            SyntaxFactory.ModifiedIdentifier(id),
            returnType == null ? null : SyntaxFactory.SimpleAsClause(returnType),
            @default
        );
    }

    #endregion

    #region Expressions

    public override VisualBasicSyntaxNode VisitLiteralExpression(CSSyntax.LiteralExpressionSyntax node)
    {
        if (node.IsKind(CS.SyntaxKind.DefaultLiteralExpression)) {
            return CreateTypedNothing(node);
        }

        if (node.IsKind(CS.SyntaxKind.NullLiteralExpression)) {
            return VisualBasicSyntaxFactory.NothingExpression;
        }

        if (node.IsKind(CS.SyntaxKind.StringLiteralExpression) && CS.CSharpExtensions.IsVerbatimStringLiteral(node.Token) && (int)LanguageVersion >= 14) {
            return SyntaxFactory.StringLiteralExpression(
                SyntaxFactory.StringLiteralToken(
                    node.Token.Text.Substring(1),
                    (string)node.Token.Value
                )
            );
        }

        return _commonConversions.Literal(node.Token.Value, node.Token.Text);
    }

    public override VisualBasicSyntaxNode VisitInterpolatedStringExpression(CSSyntax.InterpolatedStringExpressionSyntax node)
    {
        return SyntaxFactory.InterpolatedStringExpression(node.Contents.Select(c => (InterpolatedStringContentSyntax)c.Accept(TriviaConvertingVisitor)).ToArray());
    }

    public override VisualBasicSyntaxNode VisitInterpolatedStringText(CSSyntax.InterpolatedStringTextSyntax node)
    {
        return SyntaxFactory.InterpolatedStringText(SyntaxFactory.InterpolatedStringTextToken(ConvertUserText(node.TextToken), node.TextToken.ValueText));
    }

    private static string ConvertUserText(SyntaxToken token)
    {
        if (CS.CSharpExtensions.IsVerbatimStringLiteral(token)) return token.Text;
        return token.ValueText.Replace("\"", "\"\"");
    }
    private static string ConvertUserFormatText(SyntaxToken token)
    {
        if (CS.CSharpExtensions.IsVerbatimStringLiteral(token)) return token.Text;
        return token.ValueText.Replace("\\\\", "\\");
    }

    public override VisualBasicSyntaxNode VisitInterpolation(CSSyntax.InterpolationSyntax node)
    {
        return SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), (ExpressionSyntax) node.Expression.Accept(TriviaConvertingVisitor), (InterpolationAlignmentClauseSyntax) node.AlignmentClause?.Accept(TriviaConvertingVisitor), (InterpolationFormatClauseSyntax) node.FormatClause?.Accept(TriviaConvertingVisitor), SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
    }

    public override VisualBasicSyntaxNode VisitInterpolationFormatClause(CSSyntax.InterpolationFormatClauseSyntax node)
    {
        SyntaxToken formatStringToken = SyntaxFactory.InterpolatedStringTextToken(SyntaxTriviaList.Empty,
            ConvertUserFormatText(node.FormatStringToken), node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
        return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatStringToken);
    }

    public override VisualBasicSyntaxNode VisitInterpolationAlignmentClause(CSSyntax.InterpolationAlignmentClauseSyntax node)
    {
        return SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), (ExpressionSyntax)node.Value.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitParenthesizedExpression(CSSyntax.ParenthesizedExpressionSyntax node)
    {
        return node.Expression.Accept(TriviaConvertingVisitor)
            .TypeSwitch<VisualBasicSyntaxNode, AssignmentStatementSyntax, CTypeExpressionSyntax, TryCastExpressionSyntax, ExpressionSyntax, VisualBasicSyntaxNode>(
                statement => {
                    var subOrFunctionHeader = SyntaxFactory.LambdaHeader(
                        SyntaxKind.FunctionLambdaHeader,
                        SyntaxFactory.Token(SyntaxKind.FunctionKeyword)
                    ).WithParameterList(SyntaxFactory.ParameterList());
                    var multiLineFunctionLambdaExpression = SyntaxFactory.MultiLineFunctionLambdaExpression(
                        subOrFunctionHeader,
                        new SyntaxList<StatementSyntax>(new StatementSyntax[] {
                            statement,
                            SyntaxFactory.ReturnStatement(statement.Left)
                        }),
                        SyntaxFactory.EndFunctionStatement()
                    );
                    return SyntaxFactory.InvocationExpression(multiLineFunctionLambdaExpression, SyntaxFactory.ArgumentList());
                },
                cTypeExpression => cTypeExpression,
                tryCastExpression => tryCastExpression,
                expression => SyntaxFactory.ParenthesizedExpression(expression)
            );
    }

    public override VisualBasicSyntaxNode VisitPrefixUnaryExpression(CSSyntax.PrefixUnaryExpressionSyntax node)
    {
        var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
        if (kind == SyntaxKind.AddAssignmentStatement || kind == SyntaxKind.SubtractAssignmentStatement) {
            string operatorName;
            if (kind == SyntaxKind.AddAssignmentStatement)
                operatorName = "Increment";
            else
                operatorName = "Decrement";
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName(GetQualifiedName(nameof(System), nameof(System.Threading), nameof(Interlocked), operatorName)),
                ExpressionSyntaxExtensions.CreateArgList((ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor))
            );
        }
        return SyntaxFactory.UnaryExpression(kind, SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()), (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor));
    }
    private static string GetQualifiedName(params string[] names) {
        return names.Aggregate((seed, current) => seed + '.' + current);
    }

    public override VisualBasicSyntaxNode VisitAssignmentExpression(CSSyntax.AssignmentExpressionSyntax node)
    {
        var left = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
        var right = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
        if (IsReturnValueDiscarded(node)) {
            if (_semanticModel.GetTypeInfo(node.Right).ConvertedType.IsDelegateType()) {
                if (_semanticModel.GetSymbolInfo(node.Left).Symbol.Kind != SymbolKind.Event) {
                    var kind = node.GetAncestor<CSSyntax.AccessorDeclarationSyntax>()?.Kind();
                    if (kind != null && (kind.Value == CS.SyntaxKind.AddAccessorDeclaration || kind.Value == CS.SyntaxKind.RemoveAccessorDeclaration)) {
                        var methodName = kind.Value == CS.SyntaxKind.AddAccessorDeclaration ? "Combine" : "Remove";
                        var delegateMethod = MemberAccess("[Delegate]", methodName);
                        var invokeDelegateMethod = SyntaxFactory.InvocationExpression(delegateMethod, ExpressionSyntaxExtensions.CreateArgList(left, right));
                        return SyntaxFactory.SimpleAssignmentStatement(left, invokeDelegateMethod);
                    }
                } else {
                    if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.PlusEqualsToken)) {
                        return SyntaxFactory.AddHandlerStatement(left, right);
                    }
                    if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.MinusEqualsToken)) {
                        return SyntaxFactory.RemoveHandlerStatement(left, right);
                    }
                }
            }
            return MakeAssignmentStatement(node, left, right);
        }
        if (node.Parent is CSSyntax.ForStatementSyntax) {
            return MakeAssignmentStatement(node, left, right);
        }
        if (node.Parent.IsParentKind(CS.SyntaxKind.CoalesceExpression)) {
            return MakeAssignmentStatement(node, left, right);
        }
        if (node.Parent is CSSyntax.InitializerExpressionSyntax) {
            if (node.Left is CSSyntax.ImplicitElementAccessSyntax) {
                return SyntaxFactory.CollectionInitializer(
                    SyntaxFactory.SeparatedList(new[] { left, right })
                );
            }

            return SyntaxFactory.NamedFieldInitializer((IdentifierNameSyntax)left, right);
        }
        return CreateInlineAssignmentExpression(left, right, GetStructOrClassSymbol(node));
    }

    private static MemberAccessExpressionSyntax MemberAccess(params string[] nameParts)
    {
        MemberAccessExpressionSyntax lhs = null;
        foreach (var namePart in nameParts.Skip(1)) {
            lhs = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                lhs ?? (ExpressionSyntax) SyntaxFactory.IdentifierName(nameParts[0]), SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName(namePart)
            );
        }

        return lhs;
    }

    private ExpressionSyntax CreateInlineAssignmentExpression(ExpressionSyntax left, ExpressionSyntax right, INamedTypeSymbol containingType)
    {
        _cSharpHelperMethodDefinition.AddAssignMethod(containingType);
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(CSharpHelperMethodDefinition.QualifiedInlineAssignMethodName),
            ExpressionSyntaxExtensions.CreateArgList(left, right)
        );
    }

    public override VisualBasicSyntaxNode VisitPostfixUnaryExpression(CSSyntax.PostfixUnaryExpressionSyntax node)
    {
        var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
        if (IsReturnValueDiscarded(node)) {
            return SyntaxFactory.AssignmentStatement(CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local),
                (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor),
                SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()), _commonConversions.Literal(1)
            );
        }

        string operatorName, minMax;
        SyntaxKind op;
        if (kind == SyntaxKind.AddAssignmentStatement) {
            operatorName = "Increment";
            minMax = "Min";
            op = SyntaxKind.SubtractExpression;
        } else {
            operatorName = "Decrement";
            minMax = "Max";
            op = SyntaxKind.AddExpression;
        }
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseName(GetQualifiedName(nameof(System), nameof(Math), minMax)),
            new ExpressionSyntax[] {
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseName(GetQualifiedName(nameof(System), nameof(System.Threading), nameof(Interlocked), operatorName)),
                    ExpressionSyntaxExtensions.CreateArgList((ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor))
                ),
                SyntaxFactory.BinaryExpression(op, (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor), SyntaxFactory.Token(op.GetExpressionOperatorTokenKind()), _commonConversions.Literal(1))
            }.CreateVbArgList()
        );
    }

    private bool IsReturnValueDiscarded(CSSyntax.ExpressionSyntax node)
    {
        return node.Parent is CSSyntax.ParenthesizedLambdaExpressionSyntax ples && _commonConversions.ReturnsVoid(ples) ||
               node.Parent is CSSyntax.ExpressionStatementSyntax ||
               node.Parent is CSSyntax.SimpleLambdaExpressionSyntax ||
               node.Parent is CSSyntax.ForStatementSyntax ||
               node.Parent.IsParentKind(CS.SyntaxKind.SetAccessorDeclaration);
    }

    private static AssignmentStatementSyntax MakeAssignmentStatement(CSSyntax.AssignmentExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right)
    {
        var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
        if (node.IsKind(CS.SyntaxKind.AndAssignmentExpression, CS.SyntaxKind.OrAssignmentExpression, CS.SyntaxKind.ExclusiveOrAssignmentExpression, CS.SyntaxKind.ModuloAssignmentExpression)) {
            return SyntaxFactory.SimpleAssignmentStatement(
                left,
                SyntaxFactory.BinaryExpression(
                    kind,
                    left,
                    SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()),
                    right
                )
            );
        }
        return SyntaxFactory.AssignmentStatement(
            kind,
            left,
            SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()),
            right
        );
    }

    public override VisualBasicSyntaxNode VisitInvocationExpression(CSSyntax.InvocationExpressionSyntax node)
    {
        if (IsNameOfExpression(node)) {
            var argument = node.ArgumentList.Arguments.Single().Expression;
            var convertedExpression = (ExpressionSyntax)argument.Accept(TriviaConvertingVisitor);
            if (convertedExpression is UnaryExpressionSyntax ues) {
                // Don't wrap nameof operand in "AddressOf" if it's a method
                convertedExpression = ues.Operand;
            }
            return SyntaxFactory.NameOfExpression(convertedExpression);
        }

        if (TryCreateRaiseEventStatement(node.Expression, node.ArgumentList, out VisualBasicSyntaxNode visitInvocationExpression)) {
            return visitInvocationExpression;
        }

        var vbEventExpression = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        var argumentListSyntax = (ArgumentListSyntax)node.ArgumentList.Accept(TriviaConvertingVisitor);
        return SyntaxFactory.InvocationExpression(vbEventExpression, argumentListSyntax);
    }

    private bool TryCreateRaiseEventStatement(CSSyntax.ExpressionSyntax invokedCsExpression,
        CSSyntax.ArgumentListSyntax argumentListSyntax, out VisualBasicSyntaxNode visitInvocationExpression)
    {
        if (invokedCsExpression is CSSyntax.MemberAccessExpressionSyntax csMemberAccess &&
            IsInvokeIdentifier(csMemberAccess.Name)) {
            invokedCsExpression = csMemberAccess.Expression;
        }

        if (_commonConversions.IsEventHandlerIdentifier(invokedCsExpression))
        {
            var expressionSyntax = (ExpressionSyntax)invokedCsExpression.Accept(TriviaConvertingVisitor);
            var identifierNameSyntax = GetIdentifierNameFromName(expressionSyntax);
            var argumentList = (ArgumentListSyntax)argumentListSyntax.Accept(TriviaConvertingVisitor);
            visitInvocationExpression = SyntaxFactory.RaiseEventStatement(identifierNameSyntax, argumentList);
            return true;
        }

        visitInvocationExpression = null;
        return false;
    }

    private IdentifierNameSyntax GetIdentifierNameFromName(ExpressionSyntax expressionSyntax)
    {
        switch (expressionSyntax)
        {
            case IdentifierNameSyntax simpleName:
                return simpleName;
            case MemberAccessExpressionSyntax memberAccess:
                return GetIdentifierNameFromName(memberAccess.Name);
            default:
                throw new NotSupportedException(
                    $"Cannot get SimpleNameSyntax from {expressionSyntax.Kind()}:\r\n{expressionSyntax}");
        }
    }

    private bool IsNameOfExpression(CSSyntax.InvocationExpressionSyntax node)
    {
        return node.Expression is CSSyntax.IdentifierNameSyntax {Identifier.Text: "nameof"} methodIdentifier && _semanticModel.GetSymbolInfo(methodIdentifier).ExtractBestMatch<ISymbol>() == null;
    }

    public override VisualBasicSyntaxNode VisitConditionalExpression(CSSyntax.ConditionalExpressionSyntax node)
    {
        return SyntaxFactory.TernaryConditionalExpression(
            (ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor),
            (ExpressionSyntax)node.WhenTrue.Accept(TriviaConvertingVisitor),
            (ExpressionSyntax)node.WhenFalse.Accept(TriviaConvertingVisitor)
        );
    }

    public override VisualBasicSyntaxNode VisitConditionalAccessExpression(CSSyntax.ConditionalAccessExpressionSyntax node)
    {
        if (node.WhenNotNull is CSSyntax.InvocationExpressionSyntax invocation && TryCreateRaiseEventStatement(node.Expression, invocation.ArgumentList, out var raiseEventStatement)) {
            return raiseEventStatement;
        }

        return SyntaxFactory.ConditionalAccessExpression(
            (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
            SyntaxFactory.Token(SyntaxKind.QuestionToken),
            (ExpressionSyntax)node.WhenNotNull.Accept(TriviaConvertingVisitor)
        );
    }

    private static bool IsInvokeIdentifier(CSSyntax.SimpleNameSyntax sns)
    {
        return sns.Identifier.Value.Equals("Invoke");
    }

    public override VisualBasicSyntaxNode VisitMemberAccessExpression(CSSyntax.MemberAccessExpressionSyntax node)
    {
        return WrapTypedNameIfNecessary(SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
            SyntaxFactory.Token(SyntaxKind.DotToken),
            (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor)
        ), node);
    }

    public override VisualBasicSyntaxNode VisitImplicitElementAccess(CSSyntax.ImplicitElementAccessSyntax node)
    {
        if (node.ArgumentList.Arguments.Count > 1)
            throw new NotSupportedException("ImplicitElementAccess can only have one argument!");
        return node.ArgumentList.Arguments[0].Expression.Accept(TriviaConvertingVisitor);
    }

    public override VisualBasicSyntaxNode VisitElementAccessExpression(CSSyntax.ElementAccessExpressionSyntax node)
    {
        return SyntaxFactory.InvocationExpression(
            (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
            (ArgumentListSyntax)node.ArgumentList.Accept(TriviaConvertingVisitor)
        );
    }

    public override VisualBasicSyntaxNode VisitMemberBindingExpression(CSSyntax.MemberBindingExpressionSyntax node)
    {
        return SyntaxFactory.SimpleMemberAccessExpression((SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitDefaultExpression(CSSyntax.DefaultExpressionSyntax node)
    {
        return CreateTypedNothing(node);
    }

    private VisualBasicSyntaxNode CreateTypedNothing(CSSyntax.ExpressionSyntax node)
    {
        var nothing = VisualBasicSyntaxFactory.NothingExpression;
        return _semanticModel.GetTypeInfo(node).ConvertedType is { } t
            ? (VisualBasicSyntaxNode) _commonConversions.VbSyntaxGenerator.CastExpression(t, nothing)
            : nothing;
    }

    public override VisualBasicSyntaxNode VisitThisExpression(CSSyntax.ThisExpressionSyntax node)
    {
        return SyntaxFactory.MeExpression();
    }

    public override VisualBasicSyntaxNode VisitBaseExpression(CSSyntax.BaseExpressionSyntax node)
    {
        return SyntaxFactory.MyBaseExpression();
    }

    public override VisualBasicSyntaxNode VisitBinaryExpression(CSSyntax.BinaryExpressionSyntax node)
    {
        var vbLeft = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
        var vbRight = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);

        if (node.IsKind(CS.SyntaxKind.CoalesceExpression)) {
            return SyntaxFactory.BinaryConditionalExpression(
                vbLeft,
                vbRight
            );
        }
        if (node.IsKind(CS.SyntaxKind.AsExpression)) {
            bool isDefinitelyValueType = _semanticModel.GetTypeInfo(node).Type?.IsReferenceType == false;
            return isDefinitelyValueType
                ? SyntaxFactory.CTypeExpression(vbLeft, (TypeSyntax)vbRight)
                : SyntaxFactory.TryCastExpression(vbLeft, (TypeSyntax)vbRight);
        }
        if (node.IsKind(CS.SyntaxKind.IsExpression)) {
            return SyntaxFactory.TypeOfIsExpression(vbLeft, (TypeSyntax)vbRight);
        }

        var leftType = _semanticModel.GetTypeInfo(node.Left).ConvertedType;
        var rightType = _semanticModel.GetTypeInfo(node.Right).ConvertedType;

        bool isEquals = SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.EqualsEqualsToken);
        bool isNotEquals = SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.ExclamationEqualsToken);

        if (leftType.SpecialType == SpecialType.System_String && rightType.SpecialType == SpecialType.System_String && (isEquals || isNotEquals)) {
            var opEquality = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(nameof(Equals)), ExpressionSyntaxExtensions.CreateArgList(vbLeft, vbRight));
            return isNotEquals ? SyntaxFactory.NotExpression(opEquality) : opEquality;
        }

        var isReferenceComparison = node.Left.IsKind(CS.SyntaxKind.NullLiteralExpression) ||
                                    node.Right.IsKind(CS.SyntaxKind.NullLiteralExpression) ||
                                    leftType.IsReferenceType && rightType.IsReferenceType;

        if (isEquals && isReferenceComparison) {
            return SyntaxFactory.IsExpression(vbLeft, vbRight);
        }

        if (isNotEquals && isReferenceComparison) {
            return SyntaxFactory.IsNotExpression(vbLeft, vbRight);
        }

        var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
        if (node.IsKind(CS.SyntaxKind.AddExpression) && (leftType.SpecialType == SpecialType.System_String
                                                         || rightType.SpecialType == SpecialType.System_String)) {
            kind = SyntaxKind.ConcatenateExpression;
        }
        return SyntaxFactory.BinaryExpression(
            kind,
            vbLeft,
            SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()),
            vbRight
        );
    }

    public override VisualBasicSyntaxNode VisitTypeOfExpression(CSSyntax.TypeOfExpressionSyntax node)
    {
        return SyntaxFactory.GetTypeExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitCastExpression(CSSyntax.CastExpressionSyntax node)
    {
        var sourceType = _semanticModel.GetTypeInfo(node.Expression).Type;
        var destType = _semanticModel.GetTypeInfo(node.Type).Type;
        var expr = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        Func<SyntaxKind, VisualBasicSyntaxNode> toNumber = kind => {
            if (sourceType?.SpecialType == SpecialType.System_Char)
                return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(GetQualifiedName(nameof(Microsoft), nameof(Microsoft.VisualBasic), nameof(Microsoft.VisualBasic.Strings.AscW))), ExpressionSyntaxExtensions.CreateArgList(expr));
            return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(kind), expr);
        };
        switch (destType.SpecialType) {
            case SpecialType.System_Object:
                return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CObjKeyword), expr);
            case SpecialType.System_Boolean:
                return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CBoolKeyword), expr);
            case SpecialType.System_Char:
                return sourceType?.IsNumericType() == true
                    ? SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(GetQualifiedName(nameof(Microsoft), nameof(Microsoft.VisualBasic), nameof(Microsoft.VisualBasic.Strings.ChrW))), ExpressionSyntaxExtensions.CreateArgList(expr))
                    : SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CCharKeyword), expr);
            case SpecialType.System_SByte:
                return toNumber(SyntaxKind.CSByteKeyword);
            case SpecialType.System_Byte:
                return toNumber(SyntaxKind.CByteKeyword);
            case SpecialType.System_Int16:
                return toNumber(SyntaxKind.CShortKeyword);
            case SpecialType.System_UInt16:
                return toNumber(SyntaxKind.CUShortKeyword);
            case SpecialType.System_Int32:
                return toNumber(SyntaxKind.CIntKeyword);
            case SpecialType.System_UInt32:
                return toNumber(SyntaxKind.CUIntKeyword);
            case SpecialType.System_Int64:
                return toNumber(SyntaxKind.CLngKeyword);
            case SpecialType.System_UInt64:
                return toNumber(SyntaxKind.CULngKeyword);
            case SpecialType.System_Decimal:
                return toNumber(SyntaxKind.CDecKeyword);
            case SpecialType.System_Single:
                return toNumber(SyntaxKind.CSngKeyword);
            case SpecialType.System_Double:
                return toNumber(SyntaxKind.CDblKeyword);
            case SpecialType.System_String:
                return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CStrKeyword), expr);
            case SpecialType.System_DateTime:
                return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CDateKeyword), expr);
            default:
                return SyntaxFactory.CTypeExpression(expr, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
        }
    }

    public override VisualBasicSyntaxNode VisitObjectCreationExpression(CSSyntax.ObjectCreationExpressionSyntax node)
    {
        return SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.List<AttributeListSyntax>(),
            (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor),
            (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor),
            (ObjectCreationInitializerSyntax)node.Initializer?.Accept(TriviaConvertingVisitor)
        );
    }

    public override VisualBasicSyntaxNode VisitAnonymousObjectCreationExpression(CSSyntax.AnonymousObjectCreationExpressionSyntax node)
    {
        return SyntaxFactory.AnonymousObjectCreationExpression(
            SyntaxFactory.ObjectMemberInitializer(SyntaxFactory.SeparatedList(
                node.Initializers.Select(i => (FieldInitializerSyntax)i.Accept(TriviaConvertingVisitor))
            ))
        );
    }

    public override VisualBasicSyntaxNode VisitAnonymousObjectMemberDeclarator(CSSyntax.AnonymousObjectMemberDeclaratorSyntax node)
    {
        if (node.NameEquals == null) {
            return SyntaxFactory.InferredFieldInitializer((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
        }

        return SyntaxFactory.NamedFieldInitializer(
            (IdentifierNameSyntax)node.NameEquals.Name.Accept(TriviaConvertingVisitor),
            (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
        );
    }

    public override VisualBasicSyntaxNode VisitArrayCreationExpression(CSSyntax.ArrayCreationExpressionSyntax node)
    {
        var upperBoundArguments = node.Type.RankSpecifiers.First()?.Sizes.Where(s => !(s is CSSyntax.OmittedArraySizeExpressionSyntax)).Select(
            s => _commonConversions.ReduceArrayUpperBoundExpression(s));
        var rankSpecifiers = node.Type.RankSpecifiers.Select(rs => (ArrayRankSpecifierSyntax)rs.Accept(TriviaConvertingVisitor));

        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.Token(SyntaxKind.NewKeyword),
            SyntaxFactory.List<AttributeListSyntax>(),
            (TypeSyntax)node.Type.ElementType.Accept(TriviaConvertingVisitor),
            upperBoundArguments.Any() ? upperBoundArguments.CreateVbArgList() : null,
            upperBoundArguments.Any() ? SyntaxFactory.List(rankSpecifiers.Skip(1)) : SyntaxFactory.List(rankSpecifiers),
            (CollectionInitializerSyntax)node.Initializer?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.CollectionInitializer()
        );
    }

    public override VisualBasicSyntaxNode VisitImplicitArrayCreationExpression(CSSyntax.ImplicitArrayCreationExpressionSyntax node)
    {
        return SyntaxFactory.CollectionInitializer(
            SyntaxFactory.SeparatedList(node.Initializer.Expressions.Select(e => (ExpressionSyntax)e.Accept(TriviaConvertingVisitor)))
        );
    }

    public override VisualBasicSyntaxNode VisitInitializerExpression(CSSyntax.InitializerExpressionSyntax node)
    {
        if (node.IsKind(CS.SyntaxKind.ObjectInitializerExpression)) {
            var expressions = node.Expressions.Select(e => e.Accept(TriviaConvertingVisitor)).ToList();
            if (expressions.OfType<FieldInitializerSyntax>().Any()) {
                return SyntaxFactory.ObjectMemberInitializer(
                    SyntaxFactory.SeparatedList(expressions.OfType<FieldInitializerSyntax>())
                );
            }

            var collectionInitializerSyntax = SyntaxFactory.CollectionInitializer(
                SyntaxFactory.SeparatedList(expressions.OfType<ExpressionSyntax>())
            );

            var isObjectCollection = node.IsParentKind(CS.SyntaxKind.ObjectCreationExpression) && _semanticModel.GetTypeInfo(node.Parent).Type?.CanSupportCollectionInitializer(_semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(node.SpanStart, default)) == true;

            return isObjectCollection ? SyntaxFactory.ObjectCollectionInitializer(collectionInitializerSyntax) : collectionInitializerSyntax;
        }
        if (node.IsKind(CS.SyntaxKind.ArrayInitializerExpression))
            return SyntaxFactory.CollectionInitializer(
                SyntaxFactory.SeparatedList(node.Expressions.Select(e => (ExpressionSyntax)e.Accept(TriviaConvertingVisitor)))
            );
        if (node.IsKind(CS.SyntaxKind.CollectionInitializerExpression))
            return SyntaxFactory.ObjectCollectionInitializer(
                SyntaxFactory.CollectionInitializer(
                    SyntaxFactory.SeparatedList(node.Expressions.Select(e => (ExpressionSyntax)e.Accept(TriviaConvertingVisitor)))
                )
            );
        return SyntaxFactory.CollectionInitializer(SyntaxFactory.SeparatedList(node.Expressions.Select(e => (ExpressionSyntax)e.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitAnonymousMethodExpression(CSSyntax.AnonymousMethodExpressionSyntax node)
    {
        var parameterListParameters = node.ParameterList?.Parameters ?? Enumerable.Empty<CSSyntax.ParameterSyntax>();// May have no parameter list
        return _commonConversions.ConvertLambdaExpression(node, node.Block, parameterListParameters, SyntaxFactory.TokenList(node.AsyncKeyword));
    }

    public override VisualBasicSyntaxNode VisitSimpleLambdaExpression(CSSyntax.SimpleLambdaExpressionSyntax node)
    {
        return _commonConversions.ConvertLambdaExpression(node, node.Body, new[] { node.Parameter }, SyntaxFactory.TokenList(node.AsyncKeyword));
    }

    public override VisualBasicSyntaxNode VisitParenthesizedLambdaExpression(CSSyntax.ParenthesizedLambdaExpressionSyntax node)
    {
        return _commonConversions.ConvertLambdaExpression(node, node.Body, node.ParameterList.Parameters, SyntaxFactory.TokenList(node.AsyncKeyword));
    }

    public override VisualBasicSyntaxNode VisitAwaitExpression(CSSyntax.AwaitExpressionSyntax node)
    {
        return SyntaxFactory.AwaitExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitQueryExpression(CSSyntax.QueryExpressionSyntax node)
    {
        return SyntaxFactory.QueryExpression(
            SyntaxFactory.SingletonList((QueryClauseSyntax)node.FromClause.Accept(TriviaConvertingVisitor))
                .AddRange(node.Body.Clauses.Select(c => (QueryClauseSyntax)c.Accept(TriviaConvertingVisitor)))
                .AddRange(ConvertQueryBody(node.Body))
        );
    }

    public override VisualBasicSyntaxNode VisitFromClause(CSSyntax.FromClauseSyntax node)
    {
        return SyntaxFactory.FromClause(
            SyntaxFactory.CollectionRangeVariable(SyntaxFactory.ModifiedIdentifier(_commonConversions.ConvertIdentifier(node.Identifier)),
                (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor))
        );
    }

    public override VisualBasicSyntaxNode VisitWhereClause(CSSyntax.WhereClauseSyntax node)
    {
        return SyntaxFactory.WhereClause((ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitSelectClause(CSSyntax.SelectClauseSyntax node)
    {
        return SyntaxFactory.SelectClause(
            SyntaxFactory.ExpressionRangeVariable((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor))
        );
    }

    private IEnumerable<QueryClauseSyntax> ConvertQueryBody(CSSyntax.QueryBodySyntax body)
    {
        if (body.SelectOrGroup is CSSyntax.GroupClauseSyntax && body.Continuation == null)
            throw new NotSupportedException("group by clause without into not supported in VB");
        if (body.SelectOrGroup is CSSyntax.SelectClauseSyntax) {
            yield return (QueryClauseSyntax)body.SelectOrGroup.Accept(TriviaConvertingVisitor);
        } else {
            var group = (CSSyntax.GroupClauseSyntax)body.SelectOrGroup;
            var newGroupKeyName = GeneratePlaceholder("groupByKey");
            var csGroupId = _commonConversions.ConvertIdentifier(body.Continuation.Identifier);
            var groupIdEquals = SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(csGroupId));
            var aggregationRangeVariableSyntax = SyntaxFactory.AggregationRangeVariable(groupIdEquals, SyntaxFactory.GroupAggregation());
            yield return SyntaxFactory.GroupByClause(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ExpressionRangeVariable((ExpressionSyntax)group.GroupExpression.Accept(TriviaConvertingVisitor))),
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ExpressionRangeVariable(SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(newGroupKeyName)), (ExpressionSyntax)group.ByExpression.Accept(TriviaConvertingVisitor))),
                SyntaxFactory.SingletonSeparatedList(aggregationRangeVariableSyntax));
            if (body.Continuation.Body != null) {
                foreach (var clause in ConvertQueryBody(body.Continuation.Body)) {
                    var groupKeyAccesses = clause.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                        .Where(node => IsGroupKeyAccess(node, csGroupId));
                    yield return clause.ReplaceNodes(groupKeyAccesses, (_, _) => SyntaxFactory.IdentifierName(newGroupKeyName));
                }
            }
        }
    }

    private static bool IsGroupKeyAccess(MemberAccessExpressionSyntax node, SyntaxToken csGroupId)
    {
        return node.Name.Identifier.Text == "Key" &&
               node.Expression is IdentifierNameSyntax ins &&
               ins.Identifier.Text == csGroupId.Text;
    }

    public override VisualBasicSyntaxNode VisitLetClause(CSSyntax.LetClauseSyntax node)
    {
        return SyntaxFactory.LetClause(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.ExpressionRangeVariable(
                    SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(_commonConversions.ConvertIdentifier(node.Identifier))),
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                )
            )
        );
    }

    public override VisualBasicSyntaxNode VisitJoinClause(CSSyntax.JoinClauseSyntax node)
    {
        var asClause = node.Type == null
            ? null
            : SyntaxFactory.SimpleAsClause((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));

        var expression = (ExpressionSyntax)node.InExpression.Accept(TriviaConvertingVisitor);
        var identifierToken = _commonConversions.ConvertIdentifier(node.Identifier);
        var identifier = SyntaxFactory.ModifiedIdentifier(identifierToken);
        var collectionRangeVariable = SyntaxFactory.CollectionRangeVariable(identifier, asClause, expression);

        var joinedVariables = SyntaxFactory.SingletonSeparatedList(collectionRangeVariable);

        var leftJoinExpression = (ExpressionSyntax)node.LeftExpression.Accept(TriviaConvertingVisitor);
        var rightJoinExpression = (ExpressionSyntax)node.RightExpression.Accept(TriviaConvertingVisitor);
        var joinCondition = SyntaxFactory.JoinCondition(leftJoinExpression, rightJoinExpression);

        var joinConditions = SyntaxFactory.SingletonSeparatedList(joinCondition);

        if (node.Into == null) return SyntaxFactory.SimpleJoinClause(joinedVariables, joinConditions);

        var variableIdToken = _commonConversions.ConvertIdentifier(node.Into.Identifier);
        var variableId = SyntaxFactory.ModifiedIdentifier(variableIdToken);
        var aggregationNameEquals = SyntaxFactory.VariableNameEquals(variableId);

        var aggregationVariable = SyntaxFactory.AggregationRangeVariable(aggregationNameEquals, SyntaxFactory.GroupAggregation());
        var aggregationVariables = SyntaxFactory.SingletonSeparatedList(aggregationVariable);

        return SyntaxFactory.GroupJoinClause(joinedVariables, joinConditions, aggregationVariables);

    }

    public override VisualBasicSyntaxNode VisitOrderByClause(CSSyntax.OrderByClauseSyntax node)
    {
        return SyntaxFactory.OrderByClause(
            SyntaxFactory.SeparatedList(node.Orderings.Select(o => (OrderingSyntax)o.Accept(TriviaConvertingVisitor)))
        );
    }

    public override VisualBasicSyntaxNode VisitOrdering(CSSyntax.OrderingSyntax node)
    {
        if (node.IsKind(CS.SyntaxKind.DescendingOrdering)) {
            return SyntaxFactory.Ordering(SyntaxKind.DescendingOrdering, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
        }

        return SyntaxFactory.Ordering(SyntaxKind.AscendingOrdering, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitArgumentList(CSSyntax.ArgumentListSyntax node)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitBracketedArgumentList(CSSyntax.BracketedArgumentListSyntax node)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitArgument(CSSyntax.ArgumentSyntax node)
    {
        NameColonEqualsSyntax name = null;
        if (node.NameColon != null) {
            name = SyntaxFactory.NameColonEquals((IdentifierNameSyntax)node.NameColon.Name.Accept(TriviaConvertingVisitor));
        }
        var value = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        return SyntaxFactory.SimpleArgument(name, value);
    }

    public override VisualBasicSyntaxNode VisitThrowExpression(CSSyntax.ThrowExpressionSyntax node)
    {
        var convertedExceptionExpression = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
        if (IsReturnValueDiscarded(node)) return SyntaxFactory.ThrowStatement(convertedExceptionExpression);
        _cSharpHelperMethodDefinition.AddThrowMethod(GetStructOrClassSymbol(node));
        var convertedType = _semanticModel.GetTypeInfo(node.Parent).ConvertedType ?? _compilation.GetTypeByMetadataName("System.Object");
        var typeName = _commonConversions.GetFullyQualifiedNameSyntax(convertedType);
        var throwEx = SyntaxFactory.GenericName(CSharpHelperMethodDefinition.QualifiedThrowMethodName, SyntaxFactory.TypeArgumentList(typeName));
        var argList = ExpressionSyntaxExtensions.CreateArgList(convertedExceptionExpression);
        return SyntaxFactory.InvocationExpression(throwEx, argList);
    }

    public override VisualBasicSyntaxNode VisitCasePatternSwitchLabel(CSSyntax.CasePatternSwitchLabelSyntax node)
    {
        var condition = node.WhenClause.Condition.SkipIntoParens();
        switch (condition) {
            case CSSyntax.BinaryExpressionSyntax bes when node.Pattern.ToString().StartsWith("var", StringComparison.InvariantCulture): //VarPatternSyntax (not available in current library version)
                var basicSyntaxNode = (ExpressionSyntax)bes.Right.Accept(TriviaConvertingVisitor);
                SyntaxKind expressionKind = bes.Kind().ConvertToken();
                return SyntaxFactory.RelationalCaseClause(GetCaseClauseFromOperatorKind(expressionKind),
                    SyntaxFactory.Token(expressionKind.GetExpressionOperatorTokenKind()), basicSyntaxNode);
            default:
                throw new NotSupportedException(condition.GetType() + " in switch case");
        }
    }
    private INamedTypeSymbol GetStructOrClassSymbol(CS.CSharpSyntaxNode node) {
        return (INamedTypeSymbol)_semanticModel.GetDeclaredSymbol(node.Ancestors().First(x => x is CSSyntax.ClassDeclarationSyntax || x is CSSyntax.StructDeclarationSyntax));
    }
    private static SyntaxKind GetCaseClauseFromOperatorKind(SyntaxKind syntaxKind)
    {
        switch (syntaxKind) {
            case SyntaxKind.EqualsExpression:
                return SyntaxKind.CaseEqualsClause;
            case SyntaxKind.NotEqualsExpression:
                return SyntaxKind.CaseNotEqualsClause;
            case SyntaxKind.LessThanOrEqualExpression:
                return SyntaxKind.CaseLessThanOrEqualClause;
            case SyntaxKind.LessThanExpression:
                return SyntaxKind.CaseLessThanClause;
            case SyntaxKind.GreaterThanOrEqualExpression:
                return SyntaxKind.CaseGreaterThanOrEqualClause;
            case SyntaxKind.GreaterThanExpression:
                return SyntaxKind.CaseGreaterThanClause;
        }
        throw new NotImplementedException(syntaxKind + " in case clause");
    }


    public override VisualBasicSyntaxNode VisitCaseSwitchLabel(CSSyntax.CaseSwitchLabelSyntax node)
    {
        return SyntaxFactory.SimpleCaseClause((ExpressionSyntax)node.Value.Accept(TriviaConvertingVisitor));
    }

    #endregion

    #region Types / Modifiers

    public override VisualBasicSyntaxNode VisitArrayType(CSSyntax.ArrayTypeSyntax node)
    {
        return SyntaxFactory.ArrayType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor),
            SyntaxFactory.List(node.RankSpecifiers.Select(rs => (ArrayRankSpecifierSyntax)rs.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitArrayRankSpecifier(CSSyntax.ArrayRankSpecifierSyntax node)
    {
        return SyntaxFactory.ArrayRankSpecifier(
            SyntaxFactory.Token(SyntaxKind.OpenParenToken),
            SyntaxFactory.TokenList(Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), node.Rank - 1)),
            SyntaxFactory.Token(SyntaxKind.CloseParenToken));
    }

    public override VisualBasicSyntaxNode VisitTypeParameterList(CSSyntax.TypeParameterListSyntax node)
    {
        return SyntaxFactory.TypeParameterList(node.Parameters.Select(p => (TypeParameterSyntax)p.Accept(TriviaConvertingVisitor)).ToArray());
    }

    public override VisualBasicSyntaxNode VisitTypeParameter(CSSyntax.TypeParameterSyntax node)
    {
        SyntaxToken variance = default(SyntaxToken);
        if (!SyntaxTokenExtensions.IsKind(node.VarianceKeyword, CS.SyntaxKind.None)) {
            variance = SyntaxFactory.Token(SyntaxTokenExtensions.IsKind(node.VarianceKeyword, CS.SyntaxKind.InKeyword) ? SyntaxKind.InKeyword : SyntaxKind.OutKeyword);
        }
        // copy generic constraints
        var clause = FindClauseForParameter(node);
        return SyntaxFactory.TypeParameter(variance, _commonConversions.ConvertIdentifier(node.Identifier), (TypeParameterConstraintClauseSyntax)clause?.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitTypeParameterConstraintClause(CSSyntax.TypeParameterConstraintClauseSyntax node)
    {
        if (node.Constraints.Count == 1)
            return SyntaxFactory.TypeParameterSingleConstraintClause((ConstraintSyntax)node.Constraints[0].Accept(TriviaConvertingVisitor));
        return SyntaxFactory.TypeParameterMultipleConstraintClause(SyntaxFactory.SeparatedList(node.Constraints.Select(c => (ConstraintSyntax)c.Accept(TriviaConvertingVisitor))));
    }

    public override VisualBasicSyntaxNode VisitClassOrStructConstraint(CSSyntax.ClassOrStructConstraintSyntax node)
    {
        if (node.IsKind(CS.SyntaxKind.ClassConstraint))
            return SyntaxFactory.ClassConstraint(SyntaxFactory.Token(SyntaxKind.ClassKeyword));
        if (node.IsKind(CS.SyntaxKind.StructConstraint))
            return SyntaxFactory.StructureConstraint(SyntaxFactory.Token(SyntaxKind.StructureKeyword));
        throw new NotSupportedException();
    }

    public override VisualBasicSyntaxNode VisitTypeConstraint(CSSyntax.TypeConstraintSyntax node)
    {
        return SyntaxFactory.TypeConstraint((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitConstructorConstraint(CSSyntax.ConstructorConstraintSyntax node)
    {
        return SyntaxFactory.NewConstraint(SyntaxFactory.Token(SyntaxKind.NewKeyword));
    }

    private static CSSyntax.TypeParameterConstraintClauseSyntax FindClauseForParameter(CSSyntax.TypeParameterSyntax node)
    {
        SyntaxList<CSSyntax.TypeParameterConstraintClauseSyntax> clauses;
        var parentBlock = node.Parent.Parent;
        clauses = parentBlock.TypeSwitch(
            (CSSyntax.MethodDeclarationSyntax m) => m.ConstraintClauses,
            (CSSyntax.ClassDeclarationSyntax c) => c.ConstraintClauses,
            (CSSyntax.DelegateDeclarationSyntax d) => d.ConstraintClauses,
            (CSSyntax.InterfaceDeclarationSyntax i) => i.ConstraintClauses,
            _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
        );
        return clauses.FirstOrDefault(c => c.Name.ToString() == node.ToString());
    }

    public override VisualBasicSyntaxNode VisitPredefinedType(CSSyntax.PredefinedTypeSyntax node)
    {
        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(CS.CSharpExtensions.Kind(node.Keyword).ConvertToken()));
    }

    public override VisualBasicSyntaxNode VisitNullableType(CSSyntax.NullableTypeSyntax node)
    {
        return SyntaxFactory.NullableType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor));
    }

    public override VisualBasicSyntaxNode VisitOmittedTypeArgument(CSSyntax.OmittedTypeArgumentSyntax node)
    {
        return SyntaxFactory.ParseTypeName("");
    }

    #endregion

    #region NameSyntax

    public override VisualBasicSyntaxNode VisitIdentifierName(CSSyntax.IdentifierNameSyntax node)
    {
        return WrapTypedNameIfNecessary(SyntaxFactory.IdentifierName(_commonConversions.ConvertIdentifier(node.Identifier)), node);
    }

    public override VisualBasicSyntaxNode VisitGenericName(CSSyntax.GenericNameSyntax node)
    {
        return WrapTypedNameIfNecessary(SyntaxFactory.GenericName(_commonConversions.ConvertIdentifier(node.Identifier), (TypeArgumentListSyntax)node.TypeArgumentList.Accept(TriviaConvertingVisitor)), node);
    }

    public override VisualBasicSyntaxNode VisitQualifiedName(CSSyntax.QualifiedNameSyntax node)
    {
        return WrapTypedNameIfNecessary(SyntaxFactory.QualifiedName((NameSyntax)node.Left.Accept(TriviaConvertingVisitor), (SimpleNameSyntax)node.Right.Accept(TriviaConvertingVisitor)), node);
    }

    public override VisualBasicSyntaxNode VisitAliasQualifiedName(CSSyntax.AliasQualifiedNameSyntax node)
    {
        return WrapTypedNameIfNecessary(SyntaxFactory.QualifiedName((NameSyntax)node.Alias.Accept(TriviaConvertingVisitor), (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor)), node);
    }

    public override VisualBasicSyntaxNode VisitTypeArgumentList(CSSyntax.TypeArgumentListSyntax node)
    {
        return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (TypeSyntax)a.Accept(TriviaConvertingVisitor))));
    }

    private VisualBasicSyntaxNode WrapTypedNameIfNecessary(ExpressionSyntax name, CSSyntax.ExpressionSyntax originalName)
    {
        if (originalName.Parent is CSSyntax.NameSyntax
            || originalName.Parent is CSSyntax.AttributeSyntax
            || originalName.Parent is CSSyntax.MemberAccessExpressionSyntax
            || originalName.Parent is CSSyntax.MemberBindingExpressionSyntax
            || originalName.Parent is CSSyntax.InvocationExpressionSyntax
            || _semanticModel.SyntaxTree != originalName.SyntaxTree)
            return name;

        var symbolInfo = _semanticModel.GetSymbolInfo(originalName);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (symbol.IsKind(SymbolKind.Method)) {
            var addressOf = SyntaxFactory.AddressOfExpression(name);

            var formalParameterTypeOrNull = GetOverloadedFormalParameterTypeOrNull(originalName);
            if (formalParameterTypeOrNull != null) {
                return SyntaxFactory.ObjectCreationExpression(formalParameterTypeOrNull)
                    .WithArgumentList(ExpressionSyntaxExtensions.CreateArgList(addressOf));
            }

            return addressOf;
        }

        return name;
    }

    private TypeSyntax GetOverloadedFormalParameterTypeOrNull(CSSyntax.ExpressionSyntax argumentChildExpression)
    {
        if (argumentChildExpression?.Parent is CSSyntax.ArgumentSyntax nameArgument &&
            nameArgument.Parent?.Parent is CSSyntax.InvocationExpressionSyntax ies) {
            var argIndex = ies.ArgumentList.Arguments.IndexOf(nameArgument);
            //TODO: Deal with named parameters
            var symbolInfo = _semanticModel.GetSymbolInfo(ies.Expression);
            var destinationType = symbolInfo.ExtractBestMatch<ISymbol>(m => m.GetParameters().Length > argIndex);
            if (destinationType != null) {
                var symbolType = destinationType.GetParameters()[argIndex].Type;
                return _commonConversions.GetFullyQualifiedNameSyntax(symbolType);
            }
        }

        return null;
    }

    #endregion
}