using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp;

internal class AccessorDeclarationNodeConverter
{
    private static readonly SyntaxToken SemicolonToken = CS.SyntaxFactory.Token(CS.SyntaxKind.SemicolonToken);
    private readonly SemanticModel _semanticModel;
    private readonly Func<VisualBasicSyntaxNode, IReadOnlyCollection<VBSyntax.StatementSyntax>, bool, CSSyntax.IdentifierNameSyntax, Task<IReadOnlyCollection<CSSyntax.StatementSyntax>>> _convertMethodBodyStatementsAsync;
    private readonly Dictionary<VBSyntax.StatementSyntax, CSSyntax.MemberDeclarationSyntax[]> _additionalDeclarations;

    private CommonConversions CommonConversions { get; }
    private CommentConvertingVisitorWrapper TriviaConvertingDeclarationVisitor { get; }
    public HashSet<string> AccessedThroughMyClass { get; set; }


    public AccessorDeclarationNodeConverter(SemanticModel semanticModel, CommonConversions commonConversions, CommentConvertingVisitorWrapper triviaConvertingDeclarationVisitor,
        Dictionary<VBSyntax.StatementSyntax, CSSyntax.MemberDeclarationSyntax[]> additionalDeclarations,
        Func<VisualBasicSyntaxNode, IReadOnlyCollection<VBSyntax.StatementSyntax>, bool, CSSyntax.IdentifierNameSyntax, Task<IReadOnlyCollection<CSSyntax.StatementSyntax>>>
            convertMethodBodyStatementsAsync)
    {
        _semanticModel = semanticModel;
        _additionalDeclarations = additionalDeclarations;
        _convertMethodBodyStatementsAsync = convertMethodBodyStatementsAsync;
        CommonConversions = commonConversions;
        TriviaConvertingDeclarationVisitor = triviaConvertingDeclarationVisitor;
    }

    public async Task<CSharpSyntaxNode> ConvertPropertyStatementAsync(VBSyntax.PropertyStatementSyntax node)
    {
        var attributes = CS.SyntaxFactory.List(await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync));
        var isReadonly = node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword));
        var isWriteOnly = node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.WriteOnlyKeyword));
        var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
        var modifiers = CommonConversions.ConvertModifiers(node, convertibleModifiers.ToList(), node.GetMemberContext());
        var isIndexer = CommonConversions.IsDefaultIndexer(node);
        IPropertySymbol propSymbol = node.Parent is VBSyntax.PropertyBlockSyntax pb
            ? _semanticModel.GetDeclaredSymbol(pb)
            : _semanticModel.GetDeclaredSymbol(node);
        var accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, propSymbol);

        var directlyConvertedCsIdentifier = CommonConversions.CsEscapedIdentifier(node.Identifier.Value as string);
        var additionalDeclarations = new List<CSSyntax.MemberDeclarationSyntax>();

        var hasExplicitInterfaceImplementation = propSymbol.IsNonPublicInterfaceImplementation() || propSymbol.IsRenamedInterfaceMember(directlyConvertedCsIdentifier, propSymbol.ExplicitInterfaceImplementations);
        var additionalInterfaceImplements = propSymbol.ExplicitInterfaceImplementations;
        directlyConvertedCsIdentifier = hasExplicitInterfaceImplementation ? directlyConvertedCsIdentifier : CommonConversions.ConvertIdentifier(node.Identifier);

        var explicitInterfaceModifiers = modifiers.RemoveWhere(m => m.IsCsMemberVisibility() || m.IsKind(CS.SyntaxKind.VirtualKeyword, CS.SyntaxKind.AbstractKeyword) || m.IsKind(CS.SyntaxKind.OverrideKeyword, CS.SyntaxKind.NewKeyword));
        var shouldConvertToMethods = ShouldConvertAsParameterizedProperty(node);
        var (initializer, vbType) = await GetVbReturnTypeAsync(node);

        var rawType = await vbType.AcceptAsync<CSSyntax.TypeSyntax>(CommonConversions.TriviaConvertingExpressionVisitor)
                      ?? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.ObjectKeyword));

        CSSyntax.AccessorListSyntax accessors;
        if (node.Parent is VBSyntax.PropertyBlockSyntax propertyBlock) {
            if (shouldConvertToMethods) {
                if (accessedThroughMyClass) {
                    // Would need to create a delegating implementation to implement this
                    throw new NotImplementedException("MyClass indexing not implemented");
                }
                var methodDeclarationSyntaxs = await propertyBlock.Accessors.SelectAsync(async a =>
                    await a.AcceptAsync<CSSyntax.MethodDeclarationSyntax>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.All));
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
                await a.AcceptAsync<CSSyntax.AccessorDeclarationSyntax>(TriviaConvertingDeclarationVisitor));
            accessors = CS.SyntaxFactory.AccessorList(CS.SyntaxFactory.List(convertedAccessors));

        } else if (shouldConvertToMethods && propSymbol.ContainingType.IsInterfaceType()) {
            var methodDeclarationSyntaxs = new List<CSSyntax.MemberDeclarationSyntax>();

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

            var parameters = await node.ParameterList.Parameters.SelectAsync(async p => await p.AcceptAsync<CSSyntax.ParameterSyntax>(CommonConversions.TriviaConvertingExpressionVisitor));
            var parameterList = CS.SyntaxFactory.BracketedParameterList(CS.SyntaxFactory.SeparatedList(parameters));
            return CS.SyntaxFactory.IndexerDeclaration(
                CS.SyntaxFactory.List(attributes),
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
                var explicitInterfaceAccessors = new SyntaxList<CSSyntax.AccessorDeclarationSyntax>();
                if (additionalInterface.IsReadOnly)
                    explicitInterfaceAccessors = explicitInterfaceAccessors.Add(delegatingAccessorList.Single(t => t.IsKind(CS.SyntaxKind.GetAccessorDeclaration)));
                else if (additionalInterface.IsWriteOnly)
                    explicitInterfaceAccessors = explicitInterfaceAccessors.Add(delegatingAccessorList.Single(t => t.IsKind(CS.SyntaxKind.SetAccessorDeclaration)));
                else
                    explicitInterfaceAccessors = delegatingAccessorList;

                var interfaceDeclParams = new PropertyDeclarationParameters(attributes, explicitInterfaceModifiers, rawType, CS.SyntaxFactory.AccessorList(explicitInterfaceAccessors));
                AddInterfaceMemberDeclarations(additionalInterface, additionalDeclarations, interfaceDeclParams);
            }
        }

        if (accessedThroughMyClass) {

            var realModifiers = modifiers.RemoveWhere(m => m.IsKind(CS.SyntaxKind.PrivateKeyword));
            string csIdentifierName = AddRealPropertyDelegatingToMyClassVersion(additionalDeclarations, directlyConvertedCsIdentifier, attributes, realModifiers, rawType, isReadonly, isWriteOnly);
            modifiers = modifiers.Remove(modifiers.Single(m => m.IsKind(CS.SyntaxKind.VirtualKeyword)));
            directlyConvertedCsIdentifier = CS.SyntaxFactory.Identifier(csIdentifierName);
        }

        if (additionalDeclarations.Any()) {
            var declNode = (VBSyntax.StatementSyntax)node.FirstAncestorOrSelf<VBSyntax.PropertyBlockSyntax>() ?? node;
            _additionalDeclarations.Add(declNode, additionalDeclarations.ToArray());
        }

        var semicolonToken = CS.SyntaxFactory.Token(initializer == null ? CS.SyntaxKind.None : CS.SyntaxKind.SemicolonToken);
        return CS.SyntaxFactory.PropertyDeclaration(
            attributes,
            modifiers,
            rawType,
            explicitInterfaceSpecifier: null,
            directlyConvertedCsIdentifier, 
            accessors,
            null,
            initializer,
            semicolonToken);

        CSSyntax.MethodDeclarationSyntax WithMergedModifiers(CSSyntax.MethodDeclarationSyntax member)
        {
            SyntaxTokenList originalModifiers = member.GetModifiers();
            var hasVisibility = originalModifiers.Any(m => m.IsCsMemberVisibility());
            var modifiersToAdd = hasVisibility ? modifiers.Where(m => !m.IsCsMemberVisibility()) : modifiers;
            var newModifiers = CS.SyntaxFactory.TokenList(originalModifiers.Concat(modifiersToAdd));
            return member.WithModifiers(newModifiers);
        }

        async Task<CSSyntax.MethodDeclarationSyntax> CreateMethodDeclarationSyntaxAsync(VBSyntax.ParameterListSyntax containingPropParameterList, string methodId, bool voidReturn)
        {
            var parameterListSyntax = await containingPropParameterList.AcceptAsync<CSSyntax.ParameterListSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
            var methodModifiers = CS.SyntaxFactory.TokenList(modifiers.Where(m => !m.IsCsVisibility(false, false)));
            CSSyntax.MethodDeclarationSyntax methodDeclarationSyntax = CS.SyntaxFactory.MethodDeclaration(attributes, methodModifiers,
                    voidReturn ? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword)) : rawType,
                    null,
                    CS.SyntaxFactory.Identifier(methodId), null,
                    parameterListSyntax, CS.SyntaxFactory.List<CSSyntax.TypeParameterConstraintClauseSyntax>(), null, null)
                .WithSemicolonToken(CS.SyntaxFactory.Token(CS.SyntaxKind.SemicolonToken));
            return methodDeclarationSyntax;
        }
    }

    public async Task<CSharpSyntaxNode> ConvertPropertyBlockAsync(VBSyntax.PropertyBlockSyntax node)
    {
        var converted = await node.PropertyStatement.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingDeclarationVisitor, SourceTriviaMapKind.SubNodesOnly);

        if (converted is CSSyntax.MethodDeclarationSyntax) {
            var first = (CSSyntax.MethodDeclarationSyntax)converted;

            var firstCsConvertedToken = first.GetFirstToken();
            var firstVbSourceToken = node.GetFirstToken();
            first = first.ReplaceToken(firstCsConvertedToken, firstCsConvertedToken.WithSourceMappingFrom(firstVbSourceToken));

            var members = _additionalDeclarations[node];
            var last = members.OfType<CSSyntax.MethodDeclarationSyntax>().LastOrDefault() ?? first;
            var lastIx = members.ToList().IndexOf(last);
            var lastIsFirst = lastIx < 0;
            var lastCsConvertedToken = last.GetLastToken();
            var lastVbSourceToken = node.GetLastToken();
            last = last.ReplaceToken(lastCsConvertedToken, lastCsConvertedToken.WithSourceMappingFrom(lastVbSourceToken));

            converted = lastIsFirst ? last : first;
            if (!lastIsFirst) {
                members[lastIx] = last;
            }
        }

        return converted;
    }

    public async Task<CSharpSyntaxNode> VisitAccessorBlockAsync(VBSyntax.AccessorBlockSyntax node)
    {
        CS.SyntaxKind blockKind;
        bool isIterator = node.IsIterator();
        var ancestoryPropertyBlock = node.GetAncestor<VBSyntax.PropertyBlockSyntax>();
        var containingPropertyStmt = ancestoryPropertyBlock?.PropertyStatement;
        var csReturnVariableOrNull = CommonConversions.GetRetVariableNameOrNull(node);
        var convertedStatements = CS.SyntaxFactory.Block(await _convertMethodBodyStatementsAsync(node, node.Statements, isIterator, csReturnVariableOrNull));
        var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);
        var attributes = await CommonConversions.ConvertAttributesAsync(node.AccessorStatement.AttributeLists);
        var modifiers = CommonConversions.ConvertModifiers(node, node.AccessorStatement.Modifiers, TokenContext.Local);
        var declaredPropSymbol = containingPropertyStmt != null ? _semanticModel.GetDeclaredSymbol(containingPropertyStmt) : null;

        string potentialMethodId;
        var sourceMap = ancestoryPropertyBlock?.Accessors.FirstOrDefault() == node ? SourceTriviaMapKind.All : SourceTriviaMapKind.None;
        var returnType = containingPropertyStmt?.AsClause is VBSyntax.SimpleAsClauseSyntax asClause ?
            await asClause.Type.AcceptAsync<CSSyntax.TypeSyntax>(CommonConversions.TriviaConvertingExpressionVisitor, sourceMap) :
            CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword));

        switch (node.Kind()) {
            case VBasic.SyntaxKind.GetAccessorBlock:
                blockKind = CS.SyntaxKind.GetAccessorDeclaration;
                potentialMethodId = GetMethodId(containingPropertyStmt.Identifier.Text);

                if (ShouldConvertAsParameterizedProperty(containingPropertyStmt)) {
                    var method = await CreateMethodDeclarationSyntax(containingPropertyStmt.ParameterList, false);
                    return method;
                }
                break;
            case VBasic.SyntaxKind.SetAccessorBlock:
                blockKind = CS.SyntaxKind.SetAccessorDeclaration;
                potentialMethodId = SetMethodId(containingPropertyStmt.Identifier.Text);

                if (ShouldConvertAsParameterizedProperty(containingPropertyStmt)) {
                    var setMethod = await CreateMethodDeclarationSyntax(containingPropertyStmt.ParameterList, true);
                    return AddValueSetParameter(declaredPropSymbol, setMethod, returnType, false);
                }
                break;
            case VBasic.SyntaxKind.AddHandlerAccessorBlock:
                blockKind = CS.SyntaxKind.AddAccessorDeclaration;
                break;
            case VBasic.SyntaxKind.RemoveHandlerAccessorBlock:
                blockKind = CS.SyntaxKind.RemoveAccessorDeclaration;
                break;
            case VBasic.SyntaxKind.RaiseEventAccessorBlock:
                var eventStatement = ((VBSyntax.EventBlockSyntax)node.Parent).EventStatement;
                var eventName = CommonConversions.ConvertIdentifier(eventStatement.Identifier).ValueText;
                potentialMethodId = $"On{eventName}";
                return await CreateMethodDeclarationSyntax(node.AccessorStatement.ParameterList, true);
            default:
                throw new NotSupportedException(node.Kind().ToString());
        }

        return CS.SyntaxFactory.AccessorDeclaration(blockKind, attributes, modifiers, body);

        async Task<CSSyntax.MethodDeclarationSyntax> CreateMethodDeclarationSyntax(VBSyntax.ParameterListSyntax containingPropParameterList, bool voidReturn)
        {
            var parameterListSyntax = await containingPropParameterList.AcceptAsync<CSSyntax.ParameterListSyntax>(CommonConversions.TriviaConvertingExpressionVisitor, sourceMap);

            CSSyntax.MethodDeclarationSyntax methodDeclarationSyntax = CS.SyntaxFactory.MethodDeclaration(attributes, modifiers,
                voidReturn ? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.VoidKeyword)) : returnType,
                explicitInterfaceSpecifier: null,
                CS.SyntaxFactory.Identifier(potentialMethodId), null,
                parameterListSyntax, CS.SyntaxFactory.List<CSSyntax.TypeParameterConstraintClauseSyntax>(), body, null);
            return methodDeclarationSyntax;
        }
    }

    public async Task<CSharpSyntaxNode> VisitAccessorStatementAsync(VBSyntax.AccessorStatementSyntax node)
    {
        return CS.SyntaxFactory.AccessorDeclaration(node.Kind().ConvertToken(), null);
    }

    private void AddRemainingInterfaceDeclarations(CSSyntax.MethodDeclarationSyntax method, SyntaxList<CSSyntax.AttributeListSyntax> attributes,
        SyntaxTokenList filteredModifiers, IEnumerable<IPropertySymbol> additionalInterfaceImplements,
        ICollection<CSSyntax.MemberDeclarationSyntax> additionalDeclarations, VBasic.SyntaxKind accessorKind)
    {
        var clause = ExpressionSyntaxExtensions.GetDelegatingClause(method.ParameterList, method.Identifier, false);

        additionalInterfaceImplements.Do(interfaceImplement => {
            var isGetterMethodForParametrizedProperty = accessorKind == VBasic.SyntaxKind.GetAccessorBlock;

            if (interfaceImplement.IsReadOnly && !isGetterMethodForParametrizedProperty)
                return;
            if (interfaceImplement.IsWriteOnly && isGetterMethodForParametrizedProperty)
                return;

            var identifier = CS.SyntaxFactory.Identifier(isGetterMethodForParametrizedProperty ? 
                GetMethodId(interfaceImplement.Name) : 
                SetMethodId(interfaceImplement.Name));
            var interfaceMethodDeclParams = new MethodDeclarationParameters(attributes, filteredModifiers,
                method.ReturnType, method.TypeParameterList, method.ParameterList, method.ConstraintClauses, clause, identifier);

            AddInterfaceMemberDeclarations(interfaceImplement, additionalDeclarations, interfaceMethodDeclParams);
        });
    }

    private async Task<(CSSyntax.EqualsValueClauseSyntax Initializer, VBSyntax.TypeSyntax VbType)> GetVbReturnTypeAsync(VBSyntax.PropertyStatementSyntax node)
    {
        var initializer = await node.Initializer.AcceptAsync<CSSyntax.EqualsValueClauseSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
        VBSyntax.TypeSyntax vbType;
        switch (node.AsClause)
        {
            case VBSyntax.SimpleAsClauseSyntax c:
                vbType = c.Type;
                break;
            case VBSyntax.AsNewClauseSyntax c:
                initializer = CS.SyntaxFactory.EqualsValueClause(
                    await c.NewExpression.AcceptAsync<CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor));
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

    private static SyntaxList<CSSyntax.AccessorDeclarationSyntax> GetDelegatingAccessorList(SyntaxToken csIdentifier, CSSyntax.AccessorListSyntax accessors)
    {
        var getArrowClause = ExpressionSyntaxExtensions.GetDelegatingClause(null, csIdentifier, false);
        var setArrowClause = ExpressionSyntaxExtensions.GetDelegatingClause(null, csIdentifier, true);

        var getSetDict = new Dictionary<CS.SyntaxKind, CSSyntax.ArrowExpressionClauseSyntax> {
            {CS.SyntaxKind.GetAccessorDeclaration, getArrowClause},
            {CS.SyntaxKind.SetAccessorDeclaration, setArrowClause}
        };

        var delegatingAccessors = accessors.Accessors.Select(a => {
            var attributes = a.AttributeLists;
            var modifiers = a.Modifiers;

            var delegatingAccessor = CS.SyntaxFactory.AccessorDeclaration(a.Kind(),
                attributes, modifiers, getSetDict[a.Kind()]).WithSemicolonToken(SemicolonToken);

            return delegatingAccessor;
        });

        return new SyntaxList<CSSyntax.AccessorDeclarationSyntax>(delegatingAccessors);
    }

    private static string AddRealPropertyDelegatingToMyClassVersion(List<CSSyntax.MemberDeclarationSyntax> additionalDeclarations, SyntaxToken csIdentifier,
        SyntaxList<CSSyntax.AttributeListSyntax> attributes, SyntaxTokenList modifiers, CSSyntax.TypeSyntax rawType, bool readOnly, bool writeOnly)
    {
        var csIdentifierName = "MyClass" + csIdentifier.ValueText;
        CSSyntax.ExpressionSyntax thisDotIdentifier = CS.SyntaxFactory.Identifier(csIdentifierName).GetSimpleMemberAccess();

        var accessors = CS.SyntaxFactory.List(Array.Empty<CSSyntax.AccessorDeclarationSyntax>());
        if (readOnly || !writeOnly) {
            var getReturn = CS.SyntaxFactory.Block(CS.SyntaxFactory.ReturnStatement(thisDotIdentifier));
            var getAccessor = CS.SyntaxFactory.AccessorDeclaration(CS.SyntaxKind.GetAccessorDeclaration, getReturn);
            accessors = accessors.Add(getAccessor);
        }

        if (writeOnly || !readOnly) {
            var setValue = CS.SyntaxFactory.Block(CS.SyntaxFactory.ExpressionStatement(
                CS.SyntaxFactory.AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, thisDotIdentifier,
                    ValidSyntaxFactory.IdentifierName(("value")))));
            var setAccessor = CS.SyntaxFactory.AccessorDeclaration(CS.SyntaxKind.SetAccessorDeclaration, setValue);
            accessors = accessors.Add(setAccessor);
        }

        var realAccessors = CS.SyntaxFactory.AccessorList(accessors);
        var realDecl = CS.SyntaxFactory.PropertyDeclaration(
            attributes,
            modifiers,
            rawType,
            null,
            csIdentifier, realAccessors,
            null,
            null,
            CS.SyntaxFactory.Token(CS.SyntaxKind.None));

        additionalDeclarations.Add(realDecl);
        return csIdentifierName;
    }

    private static CSSyntax.AccessorListSyntax ConvertSimpleAccessors(bool isWriteOnly, bool isReadonly,
        bool allowPrivateAccessorForDirectAccess, Accessibility declaredAccessibility)
    {
        var getAccessor = CS.SyntaxFactory.AccessorDeclaration(CS.SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(SemicolonToken);
        var setAccessor = CS.SyntaxFactory.AccessorDeclaration(CS.SyntaxKind.SetAccessorDeclaration)
            .WithSemicolonToken(SemicolonToken);

        if (isWriteOnly && declaredAccessibility != Accessibility.Private) {
            getAccessor = getAccessor.AddModifiers(CS.SyntaxFactory.Token(CS.SyntaxKind.PrivateKeyword));
        }

        if (isReadonly && declaredAccessibility != Accessibility.Private) {
            setAccessor = setAccessor.AddModifiers(CS.SyntaxFactory.Token(CS.SyntaxKind.PrivateKeyword));
        }

        // this could be improved by looking if there is actually a direct access somewhere
        // if not we could skip generating private property accessor
        var isReadOnlyInterface = !allowPrivateAccessorForDirectAccess && isReadonly;
        var isWriteOnlyInterface = !allowPrivateAccessorForDirectAccess && isWriteOnly;

        if (isReadOnlyInterface)
            return CS.SyntaxFactory.AccessorList(CS.SyntaxFactory.List(new[] { getAccessor }));
        if (isWriteOnlyInterface)
            return CS.SyntaxFactory.AccessorList(CS.SyntaxFactory.List(new[] { setAccessor }));

        return CS.SyntaxFactory.AccessorList(CS.SyntaxFactory.List(new[] { getAccessor, setAccessor }));
    }

    private static CSSyntax.MethodDeclarationSyntax AddValueSetParameter(IPropertySymbol declaredPropSymbol,
        CSSyntax.MethodDeclarationSyntax setMethod, CSSyntax.TypeSyntax returnType, bool hasExplicitInterfaceImplementation)
    {
        var valueParam = CS.SyntaxFactory.Parameter(CommonConversions.CsEscapedIdentifier("value")).WithType(returnType);
        if ((declaredPropSymbol?.Parameters.Any(p => p.IsOptional) ?? false) && !hasExplicitInterfaceImplementation) valueParam = valueParam.WithDefault(CS.SyntaxFactory.EqualsValueClause(ValidSyntaxFactory.DefaultExpression));
        return setMethod.AddParameterListParameters(valueParam);
    }

    private static string SetMethodId(string methodName) => $"set_{methodName}";
    private static string GetMethodId(string methodName) => $"get_{methodName}";

    public static bool ShouldConvertAsParameterizedProperty(VBSyntax.PropertyStatementSyntax propStmt)
    {
        return propStmt.ParameterList?.Parameters.Any() == true
               && !CommonConversions.IsDefaultIndexer(propStmt);
    }

    public CSSyntax.BlockSyntax WithImplicitReturnStatements(VBSyntax.MethodBlockBaseSyntax node, CSSyntax.BlockSyntax convertedStatements,
        CSSyntax.IdentifierNameSyntax csReturnVariableOrNull)
    {
        if (!node.MustReturn()) return convertedStatements;
        if (_semanticModel.GetDeclaredSymbol(node) is { } ms && ms.ReturnsVoidOrAsyncTask()) {
            return convertedStatements;
        }


        var preBodyStatements = new List<CSSyntax.StatementSyntax>();
        var postBodyStatements = new List<CSSyntax.StatementSyntax>();

        var symbolNode = node.TypeSwitch(
            (VBSyntax.MethodBlockSyntax mb) => (VisualBasicSyntaxNode)mb.SubOrFunctionStatement,
            (VBSyntax.AccessorBlockSyntax ab) => ab.AccessorStatement,
            _ => node
        );
        var functionSym = ModelExtensions.GetDeclaredSymbol(_semanticModel, symbolNode);
        if (functionSym != null) {
            var returnType = CommonConversions.GetTypeSyntax(functionSym.GetReturnType());

            if (csReturnVariableOrNull != null) {
                var retDeclaration = CommonConversions.CreateVariableDeclarationAndAssignment(
                    csReturnVariableOrNull.Identifier.ValueText, CS.SyntaxFactory.DefaultExpression(returnType),
                    returnType);
                preBodyStatements.Add(CS.SyntaxFactory.LocalDeclarationStatement(retDeclaration));
            }

            ControlFlowAnalysis controlFlowAnalysis = null;
            if (!node.Statements.IsEmpty())
                controlFlowAnalysis =
                    ModelExtensions.AnalyzeControlFlow(_semanticModel, node.Statements.First(), node.Statements.Last());

            bool mayNeedReturn = controlFlowAnalysis?.EndPointIsReachable != false;
            if (mayNeedReturn) {
                var csReturnExpression = csReturnVariableOrNull ??
                                         (CSSyntax.ExpressionSyntax)CS.SyntaxFactory.DefaultExpression(returnType);
                postBodyStatements.Add(CS.SyntaxFactory.ReturnStatement(csReturnExpression));
            }
        }

        var statements = preBodyStatements
            .Concat(convertedStatements.Statements)
            .Concat(postBodyStatements);

        return CS.SyntaxFactory.Block(statements);
    }

    public bool IsAccessedThroughMyClass(SyntaxNode node, SyntaxToken identifier, ISymbol symbolOrNull)
    {
        bool accessedThroughMyClass = false;
        if (symbolOrNull != null && symbolOrNull.IsVirtual && !symbolOrNull.IsAbstract) {
            var classBlock = node.Ancestors().OfType<VBSyntax.ClassBlockSyntax>().FirstOrDefault();
            if (classBlock != null) {
                accessedThroughMyClass = AccessedThroughMyClass.Contains(identifier.Text);
            }
        }

        return accessedThroughMyClass;
    }

    private void AddInterfaceMemberDeclarations(ISymbol interfaceImplement,
        ICollection<CSSyntax.MemberDeclarationSyntax> additionalDeclarations,
        DeclarationParameters declParams)
    {
        var semicolonToken = CS.SyntaxFactory.Token(CS.SyntaxKind.SemicolonToken);
        Func<CSSyntax.ExplicitInterfaceSpecifierSyntax, SyntaxToken, CSSyntax.MemberDeclarationSyntax>
            declDelegate = declParams switch {
                MethodDeclarationParameters methodParams => (explintfspec, identifier)
                    => CS.SyntaxFactory.MethodDeclaration(methodParams.Attributes, methodParams.Modifiers,
                        methodParams.ReturnType, explintfspec, identifier
                        , methodParams.TypeParameters, methodParams.ParameterList, methodParams.Constraints, null,
                        methodParams.ArrowClause, semicolonToken).WithoutSourceMapping(),

                PropertyDeclarationParameters propertyParams => (explintfspec, identifier)
                    => CS.SyntaxFactory.PropertyDeclaration(propertyParams.Attributes, propertyParams.Modifiers,
                        propertyParams.ReturnType, explintfspec, identifier, propertyParams.Accessors,
                        null, null).NormalizeWhitespace(),

                _ => throw new ArgumentOutOfRangeException(nameof(declParams), declParams, null)
            };

        AddMemberDeclaration(additionalDeclarations, interfaceImplement, declParams.Identifier, declDelegate);
    }

    public void AddInterfaceMemberDeclarations(IEnumerable<ISymbol> additionalInterfaceImplements,
        ICollection<CSSyntax.MemberDeclarationSyntax> additionalDeclarations,
        DeclarationParameters declParams)
    {
        additionalInterfaceImplements.Do(interfaceImplement => AddInterfaceMemberDeclarations(interfaceImplement, additionalDeclarations, declParams));
    }

    private void AddMemberDeclaration(ICollection<CSSyntax.MemberDeclarationSyntax> additionalDeclarations,
        ISymbol interfaceImplement, SyntaxToken identifier, Func<CSSyntax.ExplicitInterfaceSpecifierSyntax, SyntaxToken, CSSyntax.MemberDeclarationSyntax> declDelegate)
    {
        var explicitInterfaceName = CommonConversions.GetFullyQualifiedNameSyntax(interfaceImplement.ContainingType);
        var newExplicitInterfaceSpecifier = CS.SyntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceName);
        var interfaceImplIdentifier = identifier == default
            ? CS.SyntaxFactory.Identifier(interfaceImplement.Name)
            : identifier;

        var declaration = declDelegate.Invoke(newExplicitInterfaceSpecifier, interfaceImplIdentifier);
        additionalDeclarations.Add(declaration);
    }
}