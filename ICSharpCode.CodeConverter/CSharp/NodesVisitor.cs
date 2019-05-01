using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using SyntaxToken = Microsoft.CodeAnalysis.SyntaxToken;

namespace ICSharpCode.CodeConverter.CSharp
{
    public partial class VisualBasicConverter
    {
        class NodesVisitor : VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode>
        {
            private readonly CSharpCompilation _csCompilation;
            private readonly SemanticModel _semanticModel;
            private readonly Dictionary<ITypeSymbol, string> _createConvertMethodsLookupByReturnType;
            private List<MethodWithHandles> _methodsWithHandles;
            private readonly Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]> _additionalDeclarations = new Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]>();
            private readonly Stack<string> _withBlockTempVariableNames = new Stack<string>();
            private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            private static readonly TypeSyntax VarType = SyntaxFactory.ParseTypeName("var");
            private readonly AdditionalInitializers _additionalInitializers;
            private readonly Dictionary<string, AdditionalLocal> _additionalLocals = new Dictionary<string, AdditionalLocal>();
            private readonly QueryConverter _queryConverter;
            private uint failedMemberConversionMarkerCount;
            private HashSet<string> _extraUsingDirectives = new HashSet<string>();
            private static readonly Type ExtensionAttributeType = typeof(ExtensionAttribute);
            private readonly TypeConversionAnalyzer _typeConversionAnalyzer;
            public CommentConvertingNodesVisitor TriviaConvertingVisitor { get; }

            private CommonConversions CommonConversions { get; }

            public NodesVisitor(SemanticModel semanticModel, CSharpCompilation csCompilation)
            {
                this._semanticModel = semanticModel;
                this._csCompilation = csCompilation;
                TriviaConvertingVisitor = new CommentConvertingNodesVisitor(this);
                _createConvertMethodsLookupByReturnType = CreateConvertMethodsLookupByReturnType(semanticModel);
                CommonConversions = new CommonConversions(semanticModel, TriviaConvertingVisitor);
                _typeConversionAnalyzer = new TypeConversionAnalyzer(semanticModel, csCompilation, CommonConversions, _extraUsingDirectives);
                _queryConverter = new QueryConverter(CommonConversions, TriviaConvertingVisitor);
                _additionalInitializers = new AdditionalInitializers();
            }

            private static Dictionary<ITypeSymbol, string> CreateConvertMethodsLookupByReturnType(SemanticModel semanticModel)
            {
                var systemDotConvert = typeof(Convert).FullName;
                var convertMethods = semanticModel.Compilation.GetTypeByMetadataName(systemDotConvert).GetMembers().Where(m =>
                    m.Name.StartsWith("To", StringComparison.Ordinal) && m.GetParameters().Length == 1);
                var methodsByType = convertMethods.Where(m => m.Name != nameof(Convert.ToBase64String))
                    .GroupBy(m => new { ReturnType = m.GetReturnType(), Name = $"{systemDotConvert}.{m.Name}" })
                    .ToDictionary(m => m.Key.ReturnType, m => m.Key.Name);
                return methodsByType;
            }

            public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
            {
                throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                    .WithNodeInformation(node);
            }

            public override CSharpSyntaxNode VisitGetTypeExpression(VBSyntax.GetTypeExpressionSyntax node)
            {
                return SyntaxFactory.TypeOfExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitGlobalName(VBSyntax.GlobalNameSyntax node)
            {
                return SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
            }

            #region Attributes

            private SyntaxList<AttributeListSyntax> ConvertAttributes(SyntaxList<VBSyntax.AttributeListSyntax> attributeListSyntaxs)
            {
                return SyntaxFactory.List(attributeListSyntaxs.SelectMany(ConvertAttribute));
            }

            IEnumerable<AttributeListSyntax> ConvertAttribute(VBSyntax.AttributeListSyntax attributeList)
            {
                return attributeList.Attributes.Where(a => !IsExtensionAttribute(a)).Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitAttribute(VBSyntax.AttributeSyntax node)
            {
                return SyntaxFactory.AttributeList(
                    node.Target == null ? null : SyntaxFactory.AttributeTargetSpecifier(node.Target.AttributeModifier.ConvertToken()),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute((NameSyntax)node.Name.Accept(TriviaConvertingVisitor), (AttributeArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor)))
                );
            }

            #endregion

            public override CSharpSyntaxNode VisitCompilationUnit(VBSyntax.CompilationUnitSyntax node)
            {
                var options = (VBasic.VisualBasicCompilationOptions)_semanticModel.Compilation.Options;
                var importsClauses = options.GlobalImports.Select(gi => gi.Clause).Concat(node.Imports.SelectMany(imp => imp.ImportsClauses)).ToList();

                var attributes = SyntaxFactory.List(node.Attributes.SelectMany(a => a.AttributeLists).SelectMany(ConvertAttribute));
                var sourceAndConverted = node.Members.Select(m => (Source: m, Converted: ConvertMember(m))).ToReadOnlyCollection();
                var convertedMembers = string.IsNullOrEmpty(options.RootNamespace)
                    ? sourceAndConverted.Select(sd => sd.Converted)
                    : PrependRootNamespace(sourceAndConverted, SyntaxFactory.IdentifierName(options.RootNamespace));

                var usingDirectiveSyntax = importsClauses.GroupBy(c => c.ToString()).Select(g => g.First())
                    .Select(c => (UsingDirectiveSyntax)c.Accept(TriviaConvertingVisitor))
                    .Concat(_extraUsingDirectives.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))))
                    .GroupBy(u => u.ToString())
                    .Select(g => g.First());

                return SyntaxFactory.CompilationUnit(
                    SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                    SyntaxFactory.List(usingDirectiveSyntax),
                    attributes,
                    SyntaxFactory.List(convertedMembers)
                );
            }

            private IReadOnlyCollection<MemberDeclarationSyntax> PrependRootNamespace(
                    IReadOnlyCollection<(VBSyntax.StatementSyntax VbNode, MemberDeclarationSyntax CsNode)> memberConversion,
                    IdentifierNameSyntax rootNamespaceIdentifier)
            {
                var inGlobalNamespace = memberConversion
                    .ToLookup(m => IsNamespaceDeclarationInGlobalScope(m.VbNode), m => m.CsNode);
                var members = inGlobalNamespace[true].ToList();
                if (inGlobalNamespace[false].Any()) {
                    var newNamespaceDecl = (MemberDeclarationSyntax)SyntaxFactory.NamespaceDeclaration(rootNamespaceIdentifier)
                        .WithMembers(SyntaxFactory.List(inGlobalNamespace[false]));
                    members.Add(newNamespaceDecl);
                }
                return members;
            }

            private bool IsNamespaceDeclarationInGlobalScope(VBSyntax.StatementSyntax m)
            {
                if (!(m is VBSyntax.NamespaceBlockSyntax nss)) return false;
                if (!(_semanticModel.GetSymbolInfo(nss.NamespaceStatement.Name).Symbol is INamespaceSymbol nsSymbol)) return false;
                return nsSymbol.ContainingNamespace.IsGlobalNamespace;
            }

            public override CSharpSyntaxNode VisitSimpleImportsClause(VBSyntax.SimpleImportsClauseSyntax node)
            {
                var nameEqualsSyntax = node.Alias == null ? null
                    : SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Alias.Identifier)));
                var usingDirective = SyntaxFactory.UsingDirective(nameEqualsSyntax, (NameSyntax)node.Name.Accept(TriviaConvertingVisitor));
                return usingDirective;
            }

            public override CSharpSyntaxNode VisitNamespaceBlock(VBSyntax.NamespaceBlockSyntax node)
            {
                var members = node.Members.Select(ConvertMember);

                var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                    (NameSyntax)node.NamespaceStatement.Name.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                    SyntaxFactory.List<UsingDirectiveSyntax>(),
                    SyntaxFactory.List(members)
                );

                return namespaceDeclaration;
            }

            #region Namespace Members

            IEnumerable<MemberDeclarationSyntax> ConvertMembers(SyntaxList<VBSyntax.StatementSyntax> members)
            {
                var parentType = members.FirstOrDefault()?.GetAncestor<VBSyntax.TypeBlockSyntax>();
                _methodsWithHandles = GetMethodWithHandles(parentType);

                if (parentType == null || !_methodsWithHandles.Any()) {
                    return GetDirectlyConvertMembers();
                }

                return _additionalInitializers.WithAdditionalInitializers(GetDirectlyConvertMembers().ToList(), ConvertIdentifier(parentType.BlockStatement.Identifier));

                IEnumerable<MemberDeclarationSyntax> GetDirectlyConvertMembers()
                {
                    foreach (var member in members) {
                        yield return ConvertMember(member);

                        if (_additionalDeclarations.TryGetValue(member, out var additionalStatements)) {
                            _additionalDeclarations.Remove(member);
                            foreach (var additionalStatement in additionalStatements) {
                                yield return additionalStatement;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// In case of error, creates a dummy class to attach the error comment to.
            /// This is because:
            /// * Empty statements are invalid in many contexts in C#.
            /// * There may be no previous node to attach to.
            /// * Attaching to a parent would result in the code being out of order from where it was originally.
            /// </summary>
            private MemberDeclarationSyntax ConvertMember(VBSyntax.StatementSyntax member)
            {
                try {
                    return (MemberDeclarationSyntax)member.Accept(TriviaConvertingVisitor);
                } catch (Exception e) {
                    return CreateErrorMember(member, e);
                }

                MemberDeclarationSyntax CreateErrorMember(VBSyntax.StatementSyntax memberCausingError, Exception e)
                {
                    var dummyClass
                        = SyntaxFactory.ClassDeclaration("_failedMemberConversionMarker" + ++failedMemberConversionMarkerCount);
                    return dummyClass.WithCsTrailingErrorComment(memberCausingError, e);
                }
            }

            public override CSharpSyntaxNode VisitClassBlock(VBSyntax.ClassBlockSyntax node)
            {
                var classStatement = node.ClassStatement;
                var attributes = ConvertAttributes(classStatement.AttributeLists);
                SplitTypeParameters(classStatement.TypeParameterList, out var parameters, out var constraints);
                var convertedIdentifier = ConvertIdentifier(classStatement.Identifier);

                return SyntaxFactory.ClassDeclaration(
                    attributes, ConvertTypeBlockModifiers(classStatement, TokenContext.Global),
                    convertedIdentifier,
                    parameters,
                    ConvertInheritsAndImplements(node.Inherits, node.Implements),
                    constraints,
                    SyntaxFactory.List(ConvertMembers(node.Members))
                    );
            }

            private BaseListSyntax ConvertInheritsAndImplements(SyntaxList<VBSyntax.InheritsStatementSyntax> inherits, SyntaxList<VBSyntax.ImplementsStatementSyntax> implements)
            {
                if (inherits.Count + implements.Count == 0)
                    return null;
                var baseTypes = new List<BaseTypeSyntax>();
                foreach (var t in inherits.SelectMany(c => c.Types).Concat(implements.SelectMany(c => c.Types)))
                    baseTypes.Add(SyntaxFactory.SimpleBaseType((TypeSyntax)t.Accept(TriviaConvertingVisitor)));
                return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes));
            }

            public override CSharpSyntaxNode VisitModuleBlock(VBSyntax.ModuleBlockSyntax node)
            {
                var stmt = node.ModuleStatement;
                var attributes = ConvertAttributes(stmt.AttributeLists);
                var members = SyntaxFactory.List(ConvertMembers(node.Members));

                TypeParameterListSyntax parameters;
                SyntaxList<TypeParameterConstraintClauseSyntax> constraints;
                SplitTypeParameters(stmt.TypeParameterList, out parameters, out constraints);

                return SyntaxFactory.ClassDeclaration(
                    attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                    ConvertIdentifier(stmt.Identifier),
                    parameters,
                    ConvertInheritsAndImplements(node.Inherits, node.Implements),
                    constraints,
                    members
                );
            }

            public override CSharpSyntaxNode VisitStructureBlock(VBSyntax.StructureBlockSyntax node)
            {
                var stmt = node.StructureStatement;
                var attributes = ConvertAttributes(stmt.AttributeLists);
                var members = SyntaxFactory.List(ConvertMembers(node.Members));

                TypeParameterListSyntax parameters;
                SyntaxList<TypeParameterConstraintClauseSyntax> constraints;
                SplitTypeParameters(stmt.TypeParameterList, out parameters, out constraints);

                return SyntaxFactory.StructDeclaration(
                    attributes, ConvertTypeBlockModifiers(stmt, TokenContext.Global),
                    ConvertIdentifier(stmt.Identifier),
                    parameters,
                    ConvertInheritsAndImplements(node.Inherits, node.Implements),
                    constraints,
                    members
                );
            }

            public override CSharpSyntaxNode VisitInterfaceBlock(VBSyntax.InterfaceBlockSyntax node)
            {
                var stmt = node.InterfaceStatement;
                var attributes = ConvertAttributes(stmt.AttributeLists);
                var members = SyntaxFactory.List(ConvertMembers(node.Members));

                TypeParameterListSyntax parameters;
                SyntaxList<TypeParameterConstraintClauseSyntax> constraints;
                SplitTypeParameters(stmt.TypeParameterList, out parameters, out constraints);

                return SyntaxFactory.InterfaceDeclaration(
                    attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule),
                    ConvertIdentifier(stmt.Identifier),
                    parameters,
                    ConvertInheritsAndImplements(node.Inherits, node.Implements),
                    constraints,
                    members
                );
            }

            private SyntaxTokenList ConvertTypeBlockModifiers(VBSyntax.TypeStatementSyntax stmt, TokenContext interfaceOrModule)
            {
                var extraModifiers = IsPartialType(stmt) && !HasPartialKeyword(stmt.Modifiers)
                    ? new[] {SyntaxFactory.Token(SyntaxKind.PartialKeyword)}
                    : new SyntaxToken[0];
                return CommonConversions.ConvertModifiers(stmt.Modifiers, interfaceOrModule).AddRange(extraModifiers);
            }

            private static bool HasPartialKeyword(SyntaxTokenList modifiers)
            {
                return modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.PartialKeyword));
            }

            private bool IsPartialType(VBSyntax.DeclarationStatementSyntax stmt)
            {
                var declaredSymbol = _semanticModel.GetDeclaredSymbol(stmt);
                return declaredSymbol.GetDeclarations().Count() > 1;
            }

            public override CSharpSyntaxNode VisitEnumBlock(VBSyntax.EnumBlockSyntax node)
            {
                var stmt = node.EnumStatement;
                // we can cast to SimpleAsClause because other types make no sense as enum-type.
                var asClause = (VBSyntax.SimpleAsClauseSyntax)stmt.UnderlyingType;
                var attributes = stmt.AttributeLists.SelectMany(ConvertAttribute);
                BaseListSyntax baseList = null;
                if (asClause != null) {
                    baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType((TypeSyntax)asClause.Type.Accept(TriviaConvertingVisitor))));
                    if (asClause.AttributeLists.Count > 0) {
                        attributes = attributes.Concat(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword)),
                                SyntaxFactory.SeparatedList(asClause.AttributeLists.SelectMany(l => ConvertAttribute(l).SelectMany(a => a.Attributes)))
                            )
                        );
                    }
                }
                var members = SyntaxFactory.SeparatedList(node.Members.Select(m => (EnumMemberDeclarationSyntax)m.Accept(TriviaConvertingVisitor)));
                return SyntaxFactory.EnumDeclaration(
                    SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(stmt.Modifiers, TokenContext.Global),
                    ConvertIdentifier(stmt.Identifier),
                    baseList,
                    members
                );
            }

            public override CSharpSyntaxNode VisitEnumMemberDeclaration(VBSyntax.EnumMemberDeclarationSyntax node)
            {
                var attributes = ConvertAttributes(node.AttributeLists);
                return SyntaxFactory.EnumMemberDeclaration(
                    attributes,
                    ConvertIdentifier(node.Identifier),
                    (EqualsValueClauseSyntax)node.Initializer?.Accept(TriviaConvertingVisitor)
                );
            }

            public override CSharpSyntaxNode VisitDelegateStatement(VBSyntax.DelegateStatementSyntax node)
            {
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute);

                TypeParameterListSyntax typeParameters;
                SyntaxList<TypeParameterConstraintClauseSyntax> constraints;
                SplitTypeParameters(node.TypeParameterList, out typeParameters, out constraints);

                TypeSyntax returnType;
                var asClause = node.AsClause;
                if (asClause == null) {
                    returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                } else {
                    returnType = (TypeSyntax)asClause.Type.Accept(TriviaConvertingVisitor);
                    if (asClause.AttributeLists.Count > 0) {
                        attributes = attributes.Concat(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword)),
                                SyntaxFactory.SeparatedList(asClause.AttributeLists.SelectMany(l => ConvertAttribute(l).SelectMany(a => a.Attributes)))
                            )
                        );
                    }
                }

                return SyntaxFactory.DelegateDeclaration(
                    SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Global),
                    returnType,
                    ConvertIdentifier(node.Identifier),
                    typeParameters,
                    (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                    constraints
                );
            }

            #endregion

            #region Type Members

            public override CSharpSyntaxNode VisitFieldDeclaration(VBSyntax.FieldDeclarationSyntax node)
            {
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToList();
                var convertableModifiers = node.Modifiers.Where(m => !SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
                var isWithEvents = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
                var convertedModifiers = CommonConversions.ConvertModifiers(convertableModifiers, GetMemberContext(node), true);
                var isConst = convertedModifiers.Any(a => a.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstKeyword));
                var declarations = new List<MemberDeclarationSyntax>(node.Declarators.Count);

                foreach (var declarator in node.Declarators) {
                    foreach (var decl in CommonConversions.SplitVariableDeclarations(declarator, preferExplicitType: isConst).Values) {
                        if (isWithEvents) {
                            var initializers = decl.Variables
                                .Where(a => a.Initializer != null)
                                .ToDictionary(v => v.Identifier.Text, v => v.Initializer);
                            var fieldDecl = decl.RemoveNodes(initializers.Values, SyntaxRemoveOptions.KeepNoTrivia);
                            var initializerCollection = convertedModifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
                                ? _additionalInitializers.AdditionalStaticInitializers
                                : _additionalInitializers.AdditionalInstanceInitializers;
                            foreach (var initializer in initializers) {
                                initializerCollection.Add(initializer.Key, initializer.Value.Value);
                            }

                            var fieldDecls = MethodWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl,
                                convertedModifiers, SyntaxFactory.List(attributes), _methodsWithHandles);
                            declarations.AddRange(fieldDecls);
                        } else {
                            var baseFieldDeclarationSyntax = SyntaxFactory.FieldDeclaration(SyntaxFactory.List(attributes), convertedModifiers, decl);
                            declarations.Add(baseFieldDeclarationSyntax);
                        }
                    }
                }

                _additionalDeclarations.Add(node, declarations.Skip(1).ToArray());
                return declarations.First();
            }

            private List<MethodWithHandles> GetMethodWithHandles(VBSyntax.TypeBlockSyntax parentType)
            {
                if (parentType == null) return new List<MethodWithHandles>();
                return parentType.Members.OfType<VBSyntax.MethodBlockSyntax>()
                    .Select(m => {
                        var handlesClauseSyntax = m.SubOrFunctionStatement.HandlesClause;
                        if (handlesClauseSyntax == null) return null;
                        var csPropIds = handlesClauseSyntax.Events
                            .Where(e => e.EventContainer is VBSyntax.WithEventsEventContainerSyntax)
                            .Select(p => (ConvertIdentifier(((VBSyntax.WithEventsEventContainerSyntax) p.EventContainer).Identifier), ConvertIdentifier(p.EventMember.Identifier)))
                            .ToList();
                        if (!csPropIds.Any()) return null;
                        var csMethodId = ConvertIdentifier(m.SubOrFunctionStatement.Identifier);
                        return new MethodWithHandles(csMethodId, csPropIds);
                    }).Where(x => x != null)
                    .ToList();
            }

            public override CSharpSyntaxNode VisitPropertyStatement(VBSyntax.PropertyStatementSyntax node)
            {
                bool hasBody = node.Parent is VBSyntax.PropertyBlockSyntax;
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToArray();
                var isReadonly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.ReadOnlyKeyword));
                var isWriteOnly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WriteOnlyKeyword));
                var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
                var modifiers = CommonConversions.ConvertModifiers(convertibleModifiers, GetMemberContext(node));
                var isIndexer = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.DefaultKeyword));
                var accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, _semanticModel.GetDeclaredSymbol(node));
                bool isInInterface = node.Ancestors().OfType<VBSyntax.InterfaceBlockSyntax>().FirstOrDefault() != null;

                var initializer = (EqualsValueClauseSyntax)node.Initializer?.Accept(TriviaConvertingVisitor);
                var rawType = (TypeSyntax)node.AsClause?.TypeSwitch(
                    (VBSyntax.SimpleAsClauseSyntax c) => c.Type,
                    (VBSyntax.AsNewClauseSyntax c) => {
                        initializer = SyntaxFactory.EqualsValueClause((ExpressionSyntax)c.NewExpression.Accept(TriviaConvertingVisitor));
                        return VBasic.SyntaxExtensions.Type(c.NewExpression.WithoutTrivia()); // We'll end up visiting this twice so avoid trivia this time
                    },
                    _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
                )?.Accept(TriviaConvertingVisitor) ?? VarType;

                AccessorListSyntax accessors = null;
                if (!hasBody) {
                    var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken);
                    var setAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken);
                    if (isWriteOnly) {
                        getAccessor = getAccessor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    }
                    if (isReadonly) {
                        setAccessor = setAccessor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    }
                    if (isInInterface && isReadonly) {
                        accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getAccessor }));
                    } else if (isInInterface && isWriteOnly) {
                        accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { setAccessor }));
                    } else {
                        // In VB, there's a backing field which can always be read and written to even on ReadOnly/WriteOnly properties.
                        // Our conversion will rewrite usages of that field to use the property accessors which therefore must exist and be private at minimum.
                        accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getAccessor, setAccessor }));
                    }
                } else {
                    accessors = SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            ((VBSyntax.PropertyBlockSyntax)node.Parent).Accessors.Select(a => (AccessorDeclarationSyntax)a.Accept(TriviaConvertingVisitor))
                        )
                    );
                }

                if (isIndexer) {
                    if (accessedThroughMyClass) {
                        // Not sure if this is possible
                        throw new NotImplementedException("MyClass indexing not implemented");
                    }

                    return SyntaxFactory.IndexerDeclaration(
                        SyntaxFactory.List(attributes),
                        modifiers,
                        rawType,
                        null,
                        SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(node.ParameterList.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor)))),
                        accessors
                    );
                } else {
                    var csIdentifier = ConvertIdentifier(node.Identifier);
                    if (accessedThroughMyClass) {
                        var csIndentifierName = "MyClass" + csIdentifier.ValueText;
                        ExpressionSyntax thisDotIdentifier = SyntaxFactory.ParseExpression($"this.{csIndentifierName}");
                        var getReturn = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(thisDotIdentifier));
                        var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getReturn);
                        var setValue = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, thisDotIdentifier, SyntaxFactory.IdentifierName("value"))));
                        var setAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setValue);
                        var realAccessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {getAccessor, setAccessor}));
                        var realDecl = SyntaxFactory.PropertyDeclaration(
                            SyntaxFactory.List(attributes),
                            modifiers,
                            rawType,
                            null,
                            csIdentifier, realAccessors,
                            null,
                            null,
                            SyntaxFactory.Token(SyntaxKind.None));

                        _additionalDeclarations.Add(node, new MemberDeclarationSyntax[] { realDecl });
                        modifiers = modifiers.Remove(modifiers.Single(m => m.IsKind(SyntaxKind.VirtualKeyword)));
                        csIdentifier = SyntaxFactory.Identifier(csIndentifierName);
                    }

                    return SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.List(attributes),
                        modifiers,
                        rawType,
                        null,
                        csIdentifier, accessors,
                        null,
                        initializer,
                        SyntaxFactory.Token(initializer == null ? SyntaxKind.None : SyntaxKind.SemicolonToken));
                }
            }

            public override CSharpSyntaxNode VisitPropertyBlock(VBSyntax.PropertyBlockSyntax node)
            {
                return node.PropertyStatement.Accept(TriviaConvertingVisitor);
            }

            public override CSharpSyntaxNode VisitAccessorBlock(VBSyntax.AccessorBlockSyntax node)
            {
                SyntaxKind blockKind;
                bool isIterator = node.GetModifiers().Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.IteratorKeyword));
                var body = ConvertStatements(node.Statements, CreateMethodBodyVisitor(node, isIterator));
                var attributes = ConvertAttributes(node.AccessorStatement.AttributeLists);
                var modifiers = CommonConversions.ConvertModifiers(node.AccessorStatement.Modifiers, TokenContext.Local);

                switch (node.Kind()) {
                    case VBasic.SyntaxKind.GetAccessorBlock:
                        blockKind = SyntaxKind.GetAccessorDeclaration;
                        break;
                    case VBasic.SyntaxKind.SetAccessorBlock:
                        blockKind = SyntaxKind.SetAccessorDeclaration;
                        break;
                    case VBasic.SyntaxKind.AddHandlerAccessorBlock:
                        blockKind = SyntaxKind.AddAccessorDeclaration;
                        break;
                    case VBasic.SyntaxKind.RemoveHandlerAccessorBlock:
                        blockKind = SyntaxKind.RemoveAccessorDeclaration;
                        break;
                    case VBasic.SyntaxKind.RaiseEventAccessorBlock:
                        blockKind = SyntaxKind.MethodDeclaration;
                        break;
                    default:
                        throw new NotSupportedException(node.Kind().ToString());
                }

                if (blockKind == SyntaxKind.MethodDeclaration) {
                    var parameterListSyntax = (ParameterListSyntax) node.AccessorStatement.ParameterList.Accept(TriviaConvertingVisitor);
                    var eventStatement = ((VBSyntax.EventBlockSyntax)node.Parent).EventStatement;
                    var eventName = ConvertIdentifier(eventStatement.Identifier).ValueText;
                    return SyntaxFactory.MethodDeclaration(attributes, modifiers,
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), null,
                        SyntaxFactory.Identifier($"On{eventName}"), null,
                        parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), body, null);
                }
                return SyntaxFactory.AccessorDeclaration(blockKind, attributes, modifiers, body);
            }

            public override CSharpSyntaxNode VisitAccessorStatement(VBSyntax.AccessorStatementSyntax node)
            {
                return SyntaxFactory.AccessorDeclaration(node.Kind().ConvertToken(), null);
            }

            public override CSharpSyntaxNode VisitMethodBlock(VBSyntax.MethodBlockSyntax node)
            {
                var methodBlock = (BaseMethodDeclarationSyntax)node.SubOrFunctionStatement.Accept(TriviaConvertingVisitor);

                if (_semanticModel.GetDeclaredSymbol(node).IsPartialDefinition()) {
                    return methodBlock;
                }

                var csReturnVariableOrNull = GetRetVariableNameOrNull(node);
                var generatedNames = new HashSet<string>();
                var visualBasicSyntaxVisitor = CreateMethodBodyVisitor(node, IsIterator(node), csReturnVariableOrNull, generatedNames);
                var convertedStatements = ConvertStatements(node.Statements, visualBasicSyntaxVisitor);
                var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);
                body = WithAdditionalLocalVariables(node, body, generatedNames);

                return methodBlock.WithBody(body);
            }

            private static bool AllowsImplicitReturn(VBSyntax.MethodBlockSyntax node)
            {
                return !IsIterator(node) && node.IsKind(VBasic.SyntaxKind.FunctionBlock);
            }

            private static bool IsIterator(VBSyntax.MethodBlockSyntax node)
            {
                return node.SubOrFunctionStatement.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.IteratorKeyword));
            }

            private BlockSyntax WithAdditionalLocalVariables(VBasic.VisualBasicSyntaxNode vbNode, BlockSyntax block, HashSet<string> generatedNames)
            {
                var additionalDeclarations = new List<StatementSyntax>();

                var newNames = new Dictionary<string, string>();

                block = block.ReplaceNodes(block.GetAnnotatedNodes(AdditionalLocal.Annotation), (an, _) => {
                    var id = (an as IdentifierNameSyntax).Identifier.ValueText;
                    newNames[id] = NameGenerator.GetUniqueVariableNameInScope(_semanticModel, generatedNames, vbNode, _additionalLocals[id].Prefix);
                    return SyntaxFactory.IdentifierName(newNames[id]);
                });

                foreach (var additionalLocal in _additionalLocals) {
                    var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Key], additionalLocal.Value.Initializer);
                    additionalDeclarations.Add(SyntaxFactory.LocalDeclarationStatement(decl));
                }
                _additionalLocals.Clear();

                return SyntaxFactory.Block(additionalDeclarations.Concat(block.Statements));
            }

            private BlockSyntax WithImplicitReturnStatements(VBSyntax.MethodBlockSyntax node, BlockSyntax convertedStatements,
                IdentifierNameSyntax csReturnVariableOrNull)
            {
                if (!AllowsImplicitReturn(node)) return convertedStatements;

                var preBodyStatements = new List<StatementSyntax>();
                var postBodyStatements = new List<StatementSyntax>();

                var functionSym = _semanticModel.GetDeclaredSymbol(node);
                var returnType = CommonConversions.ToCsTypeSyntax(functionSym.GetReturnType(), node);

                if (csReturnVariableOrNull != null)
                {
                    var retDeclaration = CommonConversions.CreateVariableDeclarationAndAssignment(
                        csReturnVariableOrNull.Identifier.ValueText, SyntaxFactory.DefaultExpression(returnType), returnType);
                    preBodyStatements.Add(SyntaxFactory.LocalDeclarationStatement(retDeclaration));
                }

                ControlFlowAnalysis controlFlowAnalysis = null;
                if (!node.Statements.IsEmpty())
                    controlFlowAnalysis = _semanticModel.AnalyzeControlFlow(node.Statements.First(), node.Statements.Last());

                bool mayNeedReturn = controlFlowAnalysis?.EndPointIsReachable != false;
                if (mayNeedReturn)
                {
                    var csReturnExpression = csReturnVariableOrNull ?? (ExpressionSyntax) SyntaxFactory.DefaultExpression(returnType);
                    postBodyStatements.Add(SyntaxFactory.ReturnStatement(csReturnExpression));
                }

                var statements = preBodyStatements
                    .Concat(convertedStatements.Statements)
                    .Concat(postBodyStatements);

                return SyntaxFactory.Block(statements);
            }

            private IdentifierNameSyntax GetRetVariableNameOrNull(VBSyntax.MethodBlockSyntax node)
            {
                if (!AllowsImplicitReturn(node)) return null;

                bool assignsToMethodNameVariable = false;

                if (!node.Statements.IsEmpty()) {
                    string methodName = node.SubOrFunctionStatement.Identifier.ValueText;
                    Func<ISymbol, bool> equalsMethodName = s => s.IsKind(SymbolKind.Local) && s.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase);
                    var flow = _semanticModel.AnalyzeDataFlow(node.Statements.First(), node.Statements.Last());

                    assignsToMethodNameVariable = flow.ReadInside.Any(equalsMethodName) || flow.WrittenInside.Any(equalsMethodName);
                }

                IdentifierNameSyntax csReturnVariable = null;

                if (assignsToMethodNameVariable)
                {
                    // In VB, assigning to the method name implicitly creates a variable that is returned when the method exits
                    var csReturnVariableName =
                        CommonConversions.ConvertIdentifier(node.SubOrFunctionStatement.Identifier).ValueText + "Ret";
                    csReturnVariable = SyntaxFactory.IdentifierName(csReturnVariableName);
                }

                return csReturnVariable;
            }

            private BlockSyntax ConvertStatements(SyntaxList<VBSyntax.StatementSyntax> statements, VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> methodBodyVisitor)
            {
                return SyntaxFactory.Block(statements.SelectMany(s => s.Accept(methodBodyVisitor)));
            }

            private static bool IsAccessedThroughMyClass(SyntaxNode node, SyntaxToken identifier, ISymbol symbol)
            {
                bool accessedThroughMyClass = false;
                if (symbol.IsVirtual && !symbol.IsAbstract) {
                    var classBlock = node.Ancestors().OfType<VBSyntax.ClassBlockSyntax>().FirstOrDefault();
                    if (classBlock != null) {
                        var memberAccesses = classBlock.DescendantNodes().OfType<VBSyntax.MemberAccessExpressionSyntax>();
                        accessedThroughMyClass = memberAccesses.Any(mae => {
                            bool isMyClass = mae.Expression is VBSyntax.MyClassExpressionSyntax;
                            bool namesMatch = mae.Name.Identifier.ValueText.Equals(identifier.ValueText, StringComparison.OrdinalIgnoreCase);
                            return isMyClass && namesMatch;
                        });
                    }
                }

                return accessedThroughMyClass;
            }

            public override CSharpSyntaxNode VisitMethodStatement(VBSyntax.MethodStatementSyntax node)
            {
                var attributes = ConvertAttributes(node.AttributeLists);
                bool hasBody = node.Parent is VBSyntax.MethodBlockBaseSyntax;

                if ("Finalize".Equals(node.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)
                    && node.Modifiers.Any(m => VBasic.VisualBasicExtensions.Kind(m) == VBasic.SyntaxKind.OverridesKeyword)) {
                    var decl = SyntaxFactory.DestructorDeclaration(
                        ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier)
                    ).WithAttributeLists(attributes);
                    if (hasBody) return decl;
                    return decl.WithSemicolonToken(SemicolonToken);
                } else {
                    var tokenContext = GetMemberContext(node);
                    var convertedModifiers = CommonConversions.ConvertModifiers(node.Modifiers, tokenContext);
                    var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
                    bool accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, declaredSymbol);

                    var isPartialDefinition = declaredSymbol.IsPartialDefinition();

                    if (declaredSymbol.IsPartialImplementation() || isPartialDefinition) {
                        var privateModifier = convertedModifiers.SingleOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword));
                        if (privateModifier != default(SyntaxToken)) {
                            convertedModifiers = convertedModifiers.Remove(privateModifier);
                        }
                        if (!HasPartialKeyword(node.Modifiers)) {
                            convertedModifiers = convertedModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                        }
                    }
                    SplitTypeParameters(node.TypeParameterList, out var typeParameters, out var constraints);

                    var csIdentifier = ConvertIdentifier(node.Identifier);
                    // If the method is virtual, and there is a MyClass.SomeMethod() call,
                    // we need to emit a non-virtual method for it to call
                    if (accessedThroughMyClass)
                    {
                        var identifierName = "MyClass" + csIdentifier.ValueText;
                        var arrowClause = SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.ParseExpression($"this.{identifierName}();\n")
                        );
                        var realDecl = SyntaxFactory.MethodDeclaration(
                            attributes,
                            convertedModifiers,
                            (TypeSyntax)node.AsClause?.Type?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                            null,
                            ConvertIdentifier(node.Identifier),
                            typeParameters,
                            (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ParameterList(),
                            constraints,
                            null,
                            arrowClause
                        );

                        var declNode = (VBSyntax.StatementSyntax)node.Parent;
                        _additionalDeclarations.Add(declNode, new MemberDeclarationSyntax[] { realDecl });
                        convertedModifiers = convertedModifiers.Remove(convertedModifiers.Single(m => m.IsKind(SyntaxKind.VirtualKeyword)));
                        csIdentifier = SyntaxFactory.Identifier(identifierName);
                    }

                    var decl = SyntaxFactory.MethodDeclaration(
                        attributes,
                        convertedModifiers,
                        (TypeSyntax)node.AsClause?.Type?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        null,
                        csIdentifier,
                        typeParameters,
                        (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ParameterList(),
                        constraints,
                        null,
                        null
                    );
                    if (hasBody && !isPartialDefinition) return decl;
                    return decl.WithSemicolonToken(SemicolonToken);
                }
            }
            private bool HasExtensionAttribute(VBSyntax.AttributeListSyntax a)
            {
                return a.Attributes.Any(IsExtensionAttribute);
            }

            private bool IsExtensionAttribute(VBSyntax.AttributeSyntax a)
            {
                return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()?.Equals(ExtensionAttributeType.FullName) == true;
            }

            private TokenContext GetMemberContext(VBSyntax.StatementSyntax member)
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

            public override CSharpSyntaxNode VisitEventBlock(VBSyntax.EventBlockSyntax node)
            {
                var block = node.EventStatement;
                var attributes = block.AttributeLists.SelectMany(ConvertAttribute);
                var modifiers = CommonConversions.ConvertModifiers(block.Modifiers, GetMemberContext(node));

                var rawType = (TypeSyntax)block.AsClause?.Type.Accept(TriviaConvertingVisitor) ?? VarType;

                var convertedAccessors = node.Accessors.Select(a => a.Accept(TriviaConvertingVisitor)).ToList();
                _additionalDeclarations.Add(node, convertedAccessors.OfType<MemberDeclarationSyntax>().ToArray());
                return SyntaxFactory.EventDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    rawType,
                    null,
                    ConvertIdentifier(block.Identifier),
                    SyntaxFactory.AccessorList(SyntaxFactory.List(convertedAccessors.OfType<AccessorDeclarationSyntax>()))
                );
            }

            public override CSharpSyntaxNode VisitEventStatement(VBSyntax.EventStatementSyntax node)
            {
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute);
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));
                var id = ConvertIdentifier(node.Identifier);

                if (node.AsClause == null) {
                    var delegateName = SyntaxFactory.Identifier(id.ValueText + "EventHandler");

                    var delegateDecl = SyntaxFactory.DelegateDeclaration(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        modifiers,
                        SyntaxFactory.ParseTypeName("void"),
                        delegateName,
                        null,
                        (ParameterListSyntax)node.ParameterList.Accept(TriviaConvertingVisitor),
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

                return SyntaxFactory.EventFieldDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    SyntaxFactory.VariableDeclaration((TypeSyntax)node.AsClause.Type.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(id)))
                );
            }



            public override CSharpSyntaxNode VisitOperatorBlock(VBSyntax.OperatorBlockSyntax node)
            {
                return node.BlockStatement.Accept(TriviaConvertingVisitor);
            }

            public override CSharpSyntaxNode VisitOperatorStatement(VBSyntax.OperatorStatementSyntax node)
            {
                var containingBlock = (VBSyntax.OperatorBlockSyntax) node.Parent;
                var attributes = SyntaxFactory.List(node.AttributeLists.SelectMany(ConvertAttribute));
                var returnType = (TypeSyntax)node.AsClause?.Type.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                var parameterList = (ParameterListSyntax)node.ParameterList.Accept(TriviaConvertingVisitor);
                var methodBodyVisitor = CreateMethodBodyVisitor(node);
                var body = SyntaxFactory.Block(containingBlock.Statements.SelectMany(s => s.Accept(methodBodyVisitor)));
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));

                var conversionModifiers = modifiers.Where(CommonConversions.IsConversionOperator).ToList();
                var nonConversionModifiers = SyntaxFactory.TokenList(modifiers.Except(conversionModifiers));

                if (conversionModifiers.Any()) {
                    return SyntaxFactory.ConversionOperatorDeclaration(attributes, nonConversionModifiers,
                        conversionModifiers.Single(), returnType, parameterList, body, null);
                }

                return SyntaxFactory.OperatorDeclaration(attributes, nonConversionModifiers, returnType, node.OperatorToken.ConvertToken(), parameterList, body, null);
            }

            private VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(VBasic.VisualBasicSyntaxNode node, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null, HashSet<string> generatedNames = null)
            {
                if (generatedNames == null) {
                    generatedNames = new HashSet<string>();
                }
                var methodBodyVisitor = new MethodBodyVisitor(node, _semanticModel, TriviaConvertingVisitor, _withBlockTempVariableNames, _extraUsingDirectives, generatedNames, TriviaConvertingVisitor.TriviaConverter) {
                    IsIterator = isIterator,
                    ReturnVariable = csReturnVariable,
                };
                return methodBodyVisitor.CommentConvertingVisitor;
            }

            public override CSharpSyntaxNode VisitConstructorBlock(VBSyntax.ConstructorBlockSyntax node)
            {
                var block = node.BlockStatement;
                var attributes = block.AttributeLists.SelectMany(ConvertAttribute);
                var modifiers = CommonConversions.ConvertModifiers(block.Modifiers, GetMemberContext(node), isConstructor: true);

                var ctor = (node.Statements.FirstOrDefault() as VBSyntax.ExpressionStatementSyntax)?.Expression as VBSyntax.InvocationExpressionSyntax;
                var ctorExpression = ctor?.Expression as VBSyntax.MemberAccessExpressionSyntax;
                var ctorArgs = (ArgumentListSyntax)ctor?.ArgumentList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList();

                IEnumerable<VBSyntax.StatementSyntax> statements;
                ConstructorInitializerSyntax ctorCall;
                if (ctorExpression == null || !ctorExpression.Name.Identifier.IsKindOrHasMatchingText(VBasic.SyntaxKind.NewKeyword)) {
                    statements = node.Statements;
                    ctorCall = null;
                } else if (ctorExpression.Expression is VBSyntax.MyBaseExpressionSyntax) {
                    statements = node.Statements.Skip(1);
                    ctorCall = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ctorArgs);
                } else if (ctorExpression.Expression is VBSyntax.MeExpressionSyntax || ctorExpression.Expression is VBSyntax.MyClassExpressionSyntax) {
                    statements = node.Statements.Skip(1);
                    ctorCall = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, ctorArgs);
                } else {
                    statements = node.Statements;
                    ctorCall = null;
                }

                var methodBodyVisitor = CreateMethodBodyVisitor(node);
                return SyntaxFactory.ConstructorDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier),
                    (ParameterListSyntax)block.ParameterList.Accept(TriviaConvertingVisitor),
                    ctorCall,
                    SyntaxFactory.Block(statements.SelectMany(s => s.Accept(methodBodyVisitor)))
                );
            }

            public override CSharpSyntaxNode VisitDeclareStatement(VBSyntax.DeclareStatementSyntax node)
            {
                var importAttributes = new List<AttributeArgumentSyntax>();
                Type dllImportType = typeof(DllImportAttribute);
                Type charSetType = typeof(CharSet);
                _extraUsingDirectives.Add(dllImportType.Namespace);
                _extraUsingDirectives.Add(charSetType.Namespace);
                var dllImportAttributeName = SyntaxFactory.ParseName(dllImportType.Name.Replace("Attribute", ""));
                var dllImportLibLiteral = node.LibraryName.Accept(TriviaConvertingVisitor);
                importAttributes.Add(SyntaxFactory.AttributeArgument((ExpressionSyntax)dllImportLibLiteral));

                if (node.AliasName != null) {
                    importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("EntryPoint"), null, (ExpressionSyntax) node.AliasName.Accept(TriviaConvertingVisitor)));
                }

                if (!node.CharsetKeyword.IsKind(SyntaxKind.None)) {
                    importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(charSetType.Name), null, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(charSetType.Name), SyntaxFactory.IdentifierName(node.CharsetKeyword.Text))));
                }

                var attributeArguments = CommonConversions.CreateAttributeArgumentList(importAttributes.ToArray());
                var dllImportAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

                var attributeLists = ConvertAttributes(node.AttributeLists).Add(dllImportAttributeList);

                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ExternKeyword));
                var returnType = (TypeSyntax)node.AsClause?.Type.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ParseTypeName("void");
                var parameterListSyntax = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor) ??
                                          SyntaxFactory.ParameterList();

                return SyntaxFactory.MethodDeclaration(attributeLists, modifiers, returnType, null,
                    ConvertIdentifier(node.Identifier), null,
                    parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), null, null).WithSemicolonToken(SemicolonToken);
            }

            public override CSharpSyntaxNode VisitTypeParameterList(VBSyntax.TypeParameterListSyntax node)
            {
                return SyntaxFactory.TypeParameterList(
                    SyntaxFactory.SeparatedList(node.Parameters.Select(p => (TypeParameterSyntax)p.Accept(TriviaConvertingVisitor)))
                );
            }

            public override CSharpSyntaxNode VisitParameterList(VBSyntax.ParameterListSyntax node)
            {
                var parameterSyntaxs = node.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor));
                if (node.Parent is VBSyntax.PropertyStatementSyntax) {
                    return SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameterSyntaxs));
                }
                return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameterSyntaxs));
            }

            public override CSharpSyntaxNode VisitParameter(VBSyntax.ParameterSyntax node)
            {
                var id = ConvertIdentifier(node.Identifier.Identifier);
                var returnType = (TypeSyntax)node.AsClause?.Type.Accept(TriviaConvertingVisitor);
                if (node.Parent?.Parent?.IsKind(VBasic.SyntaxKind.FunctionStatement,
                    VBasic.SyntaxKind.SubStatement) == true) {
                    returnType = returnType ?? SyntaxFactory.ParseTypeName("object");
                }

                var rankSpecifiers = CommonConversions.ConvertArrayRankSpecifierSyntaxes(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
                if (rankSpecifiers.Any() && returnType != null) {
                    returnType = SyntaxFactory.ArrayType(returnType, rankSpecifiers);
                }

                if (returnType != null && !SyntaxTokenExtensions.IsKind(node.Identifier.Nullable, SyntaxKind.None)) {
                    var arrayType = returnType as ArrayTypeSyntax;
                    if (arrayType == null) {
                        returnType = SyntaxFactory.NullableType(returnType);
                    } else {
                        returnType = arrayType.WithElementType(SyntaxFactory.NullableType(arrayType.ElementType));
                    }
                }

                var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToList();
                int outAttributeIndex = attributes.FindIndex(a => a.Attributes.Single().Name.ToString() == "Out");
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Local);
                if (outAttributeIndex > -1) {
                    attributes.RemoveAt(outAttributeIndex);
                    modifiers = modifiers.Replace(SyntaxFactory.Token(SyntaxKind.RefKeyword), SyntaxFactory.Token(SyntaxKind.OutKeyword));
                }
                
                EqualsValueClauseSyntax @default = null;
                if (node.Default != null) {
                    if (node.Default.Value is VBSyntax.LiteralExpressionSyntax les && les.Token.Value is DateTime dt)
                    {
                        var dateTimeAsLongCsLiteral = CommonConversions.GetLiteralExpression(dt.Ticks, dt.Ticks + "L");
                        var dateTimeArg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(dateTimeAsLongCsLiteral));
                        _extraUsingDirectives.Add("System.Runtime.InteropServices");
                        _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                        var optionalDateTimeAttributes = new[] {
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Optional")),
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("DateTimeConstant"), dateTimeArg)
                        };
                        attributes.Insert(0,
                            SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                    } else {
                        @default = SyntaxFactory.EqualsValueClause(
                            (ExpressionSyntax)node.Default.Value.Accept(TriviaConvertingVisitor));
                    }
                }

                if (node.Parent.Parent is VBSyntax.MethodStatementSyntax mss
                    && mss.AttributeLists.Any(HasExtensionAttribute) && node.Parent.ChildNodes().First() == node) {
                    modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.ThisKeyword));
                }
                return SyntaxFactory.Parameter(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    returnType,
                    id,
                    @default
                );
            }

            #endregion

            #region Expressions

            public override CSharpSyntaxNode VisitAwaitExpression(VBSyntax.AwaitExpressionSyntax node)
            {
                return SyntaxFactory.AwaitExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitCatchBlock(VBSyntax.CatchBlockSyntax node)
            {
                var stmt = node.CatchStatement;
                CatchDeclarationSyntax catcher;
                if (stmt.IdentifierName == null)
                    catcher = null;
                else {
                    var typeInfo = _semanticModel.GetTypeInfo(stmt.IdentifierName).Type;
                    catcher = SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.ParseTypeName(typeInfo.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart)),
                        ConvertIdentifier(stmt.IdentifierName.Identifier)
                    );
                }

                var filter = (CatchFilterClauseSyntax)stmt.WhenClause?.Accept(TriviaConvertingVisitor);
                var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
                return SyntaxFactory.CatchClause(
                    catcher,
                    filter,
                    SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor)))
                );
            }

            public override CSharpSyntaxNode VisitCatchFilterClause(VBSyntax.CatchFilterClauseSyntax node)
            {
                return SyntaxFactory.CatchFilterClause((ExpressionSyntax)node.Filter.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitFinallyBlock(VBSyntax.FinallyBlockSyntax node)
            {
                var methodBodyVisitor = CreateMethodBodyVisitor(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
                return SyntaxFactory.FinallyClause(SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor))));
            }

            public override CSharpSyntaxNode VisitCTypeExpression(VBSyntax.CTypeExpressionSyntax node)
            {
                var convertMethodForKeywordOrNull = GetConvertMethodForKeywordOrNull(node.Type);
                return ConvertCastExpression(node, convertMethodForKeywordOrNull);
            }

            public override CSharpSyntaxNode VisitDirectCastExpression(VBSyntax.DirectCastExpressionSyntax node)
            {
                return ConvertCastExpression(node);
            }

            private CSharpSyntaxNode ConvertCastExpression(VBSyntax.CastExpressionSyntax node, ExpressionSyntax convertMethodOrNull = null)
            {
                var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);

                if (convertMethodOrNull != null)
                {
                    return
                        SyntaxFactory.InvocationExpression(convertMethodOrNull,
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(expressionSyntax)))
                        );
                }

                var castExpr = SyntaxFactory.CastExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor), expressionSyntax);
                if (node.Parent is VBSyntax.MemberAccessExpressionSyntax)
                {
                    return (ExpressionSyntax)SyntaxFactory.ParenthesizedExpression(castExpr);
                }
                return castExpr;
            }

            public override CSharpSyntaxNode VisitPredefinedCastExpression(VBSyntax.PredefinedCastExpressionSyntax node)
            {
                var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
                if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.CDateKeyword)) {

                    _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Conversions.ToDate"), SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(expressionSyntax))));
                }

                var convertMethodForKeywordOrNull = GetConvertMethodForKeywordOrNull(node);

                return convertMethodForKeywordOrNull != null ? (ExpressionSyntax)
                    SyntaxFactory.InvocationExpression(convertMethodForKeywordOrNull,
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(expressionSyntax)))
                    ) // Hopefully will be a compile error if it's wrong
                    : SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(node.Keyword.ConvertToken()),
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                );
            }

            private ExpressionSyntax GetConvertMethodForKeywordOrNull(SyntaxNode type)
            {
                var convertedType = _semanticModel.GetTypeInfo(type).Type;
                return _createConvertMethodsLookupByReturnType.TryGetValue(convertedType, out var convertMethodName)
                    ? SyntaxFactory.ParseExpression(convertMethodName) : null;
            }

            public override CSharpSyntaxNode VisitTryCastExpression(VBSyntax.TryCastExpressionSyntax node)
            {
                return CommonConversions.ParenthesizeIfPrecedenceCouldChange(node, SyntaxFactory.BinaryExpression(
                    SyntaxKind.AsExpression,
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)
                ));
            }

            public override CSharpSyntaxNode VisitLiteralExpression(VBSyntax.LiteralExpressionSyntax node)
            {
                if (node.Token.Value == null) {
                    var type = _semanticModel.GetTypeInfo(node).ConvertedType;
                    if (type == null) {
                        return CommonConversions.Literal(null)
                            .WithTrailingTrivia(
                                SyntaxFactory.Comment("/* TODO Change to default(_) if this is not a reference type */"));
                    }
                    return !type.IsReferenceType ? SyntaxFactory.DefaultExpression(CommonConversions.ToCsTypeSyntax(type, node)) : CommonConversions.Literal(null);
                }
                return CommonConversions.Literal(node.Token.Value, node.Token.Text);
            }

            public override CSharpSyntaxNode VisitInterpolation(VBSyntax.InterpolationSyntax node)
            {
                return SyntaxFactory.Interpolation((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor), (InterpolationAlignmentClauseSyntax) node.AlignmentClause?.Accept(TriviaConvertingVisitor), (InterpolationFormatClauseSyntax) node.FormatClause?.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitInterpolatedStringExpression(VBSyntax.InterpolatedStringExpressionSyntax node)
            {
                var useVerbatim = node.DescendantNodes().OfType<VBSyntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
                var startToken = useVerbatim ? 
                    SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedVerbatimStringStartToken, "$@\"", "$@\"", default(SyntaxTriviaList))
                    : SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringStartToken, "$\"", "$\"", default(SyntaxTriviaList));
                InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax = SyntaxFactory.InterpolatedStringExpression(startToken, SyntaxFactory.List(node.Contents.Select(c => (InterpolatedStringContentSyntax)c.Accept(TriviaConvertingVisitor))), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
                return interpolatedStringExpressionSyntax;
            }

            public override CSharpSyntaxNode VisitInterpolatedStringText(VBSyntax.InterpolatedStringTextSyntax node)
            {
                var useVerbatim = node.Parent.DescendantNodes().OfType<VBSyntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
                var textForUser = CommonConversions.EscapeQuotes(node.TextToken.Text, node.TextToken.ValueText, useVerbatim);
                InterpolatedStringTextSyntax interpolatedStringTextSyntax = SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringTextToken, textForUser, node.TextToken.ValueText, default(SyntaxTriviaList)));
                return interpolatedStringTextSyntax;
            }

            public override CSharpSyntaxNode VisitInterpolationAlignmentClause(VBSyntax.InterpolationAlignmentClauseSyntax node)
            {
                return SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), (ExpressionSyntax) node.Value.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitInterpolationFormatClause(VBSyntax.InterpolationFormatClauseSyntax node)
            {
                SyntaxToken formatStringToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, node.FormatStringToken.Text, node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
                return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatStringToken);
            }

            public override CSharpSyntaxNode VisitMeExpression(VBSyntax.MeExpressionSyntax node)
            {
                return SyntaxFactory.ThisExpression();
            }

            public override CSharpSyntaxNode VisitMyBaseExpression(VBSyntax.MyBaseExpressionSyntax node)
            {
                return SyntaxFactory.BaseExpression();
            }

            public override CSharpSyntaxNode VisitParenthesizedExpression(VBSyntax.ParenthesizedExpressionSyntax node)
            {
                return SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitMemberAccessExpression(VBSyntax.MemberAccessExpressionSyntax node)
            {
                var simpleNameSyntax = (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor);

                var symbolInfo = _semanticModel.GetSymbolInfo(node.Name);
                ExpressionSyntax left = null;
                if (node.Expression is VBSyntax.MyClassExpressionSyntax) {
                    if (symbolInfo.Symbol.IsStatic) {
                        var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                        left = CommonConversions.ToCsTypeSyntax(typeInfo.Type, node);
                    } else {
                        left = SyntaxFactory.ThisExpression();
                        if (symbolInfo.Symbol.IsVirtual && !symbolInfo.Symbol.IsAbstract) {
                            simpleNameSyntax = SyntaxFactory.IdentifierName($"MyClass{ConvertIdentifier(node.Name.Identifier).ValueText}");
                        }
                    }
                }
                if (left == null && symbolInfo.Symbol?.IsStatic == true) {
                    var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                    var symbol = _semanticModel.GetSymbolInfo(node.Expression);
                    if (typeInfo.Type != null && !symbol.Symbol.IsType()) {
                        left = CommonConversions.ToCsTypeSyntax(typeInfo.Type, node);
                    }
                }
                if (left == null) {
                    left = (ExpressionSyntax)node.Expression?.Accept(TriviaConvertingVisitor);
                }
                if (left == null) {
                    if (IsSubPartOfConditionalAccess(node)) {
                        return SyntaxFactory.MemberBindingExpression(simpleNameSyntax);
                    }
                    left = SyntaxFactory.IdentifierName(_withBlockTempVariableNames.Peek());
                } else if (TryGetTypePromotedModuleSymbol(node, out var promotedModuleSymbol)) {
                    left = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left,
                        SyntaxFactory.IdentifierName(promotedModuleSymbol.Name));
                }

                if (node.Expression.IsKind(VBasic.SyntaxKind.GlobalName)) {
                    return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)left, simpleNameSyntax);
                }

                var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, simpleNameSyntax);
                if (_semanticModel.GetSymbolInfo(node).Symbol.IsKind(SymbolKind.Method) && node.Parent?.IsKind(VBasic.SyntaxKind.InvocationExpression) != true) {
                    var visitMemberAccessExpression = SyntaxFactory.InvocationExpression(memberAccessExpressionSyntax, SyntaxFactory.ArgumentList());
                    return visitMemberAccessExpression;
                }

                return memberAccessExpressionSyntax;
            }

            /// <remarks>https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion</remarks>
            private bool TryGetTypePromotedModuleSymbol(VBSyntax.MemberAccessExpressionSyntax node, out INamedTypeSymbol moduleSymbol)
            {
                if (_semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch() is INamespaceSymbol
                        expressionSymbol &&
                    _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch()?.ContainingSymbol is INamedTypeSymbol
                        nameContainingSymbol &&
                    nameContainingSymbol.ContainingSymbol.Equals(expressionSymbol)) {
                    moduleSymbol = nameContainingSymbol;
                    return true;
                }

                moduleSymbol = null;
                return false;
            }

            private static bool IsSubPartOfConditionalAccess(VBSyntax.MemberAccessExpressionSyntax node)
            {
                var firstPossiblyConditionalAncestor = node.Parent;
                while (firstPossiblyConditionalAncestor != null &&
                       firstPossiblyConditionalAncestor.IsKind(VBasic.SyntaxKind.InvocationExpression,
                           VBasic.SyntaxKind.SimpleMemberAccessExpression))
                {
                    firstPossiblyConditionalAncestor = firstPossiblyConditionalAncestor.Parent;
                }

                return firstPossiblyConditionalAncestor?.IsKind(VBasic.SyntaxKind.ConditionalAccessExpression) == true;
            }

            public override CSharpSyntaxNode VisitConditionalAccessExpression(VBSyntax.ConditionalAccessExpressionSyntax node)
            {
                var leftExpression = (ExpressionSyntax)node.Expression?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.IdentifierName(_withBlockTempVariableNames.Peek());
                return SyntaxFactory.ConditionalAccessExpression(leftExpression, (ExpressionSyntax)node.WhenNotNull.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitArgumentList(VBSyntax.ArgumentListSyntax node)
            {
                if (node.Parent.IsKind(VBasic.SyntaxKind.Attribute)) {
                    return CommonConversions.CreateAttributeArgumentList(node.Arguments.Select(ToAttributeArgument).ToArray());
                }
                var argumentSyntaxes = ConvertArguments(node);
                return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
            }

            private IEnumerable<ArgumentSyntax> ConvertArguments(VBSyntax.ArgumentListSyntax node)
            {
                ISymbol invocationSymbolForForcedNames = null;
                var argumentSyntaxs = node.Arguments.Select((a, i) =>
                {
                    if (a.IsOmitted)
                    {
                        invocationSymbolForForcedNames = GetInvocationSymbol(node.Parent);
                        if (invocationSymbolForForcedNames != null)
                        {
                            return null;
                        }

                        var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                            .WithTrailingTrivia(
                                SyntaxFactory.Comment("/* Conversion error: Set to default value for this argument */"));
                        return SyntaxFactory.Argument(nullLiteral);
                    }

                    var argumentSyntax = (ArgumentSyntax) a.Accept(TriviaConvertingVisitor);

                    if (invocationSymbolForForcedNames != null)
                    {
                        var elementAtOrDefault = invocationSymbolForForcedNames.GetParameters().ElementAt(i).Name;
                        return argumentSyntax.WithNameColon(SyntaxFactory.NameColon(elementAtOrDefault));
                    }

                    return argumentSyntax;
                }).Where(a => a != null);
                return argumentSyntaxs;
            }

            public override CSharpSyntaxNode VisitSimpleArgument(VBSyntax.SimpleArgumentSyntax node)
            {
                var invocation = node.Parent.Parent;
                if (invocation is VBSyntax.ArrayCreationExpressionSyntax)
                    return node.Expression.Accept(TriviaConvertingVisitor);
                var symbol = GetInvocationSymbol(invocation);
                SyntaxToken token = default(SyntaxToken);
                string argName = null;
                RefKind refKind = RefKind.None;
                if (symbol != null) {
                    int argId = ((VBSyntax.ArgumentListSyntax)node.Parent).Arguments.IndexOf(node);
                    var parameters = symbol.GetParameters();
                    //WARNING: If named parameters can reach here it won't work properly for them
                    if (argId < parameters.Count()) {
                        refKind = parameters[argId].RefKind;
                        argName = parameters[argId].Name;
                    }
                    switch (refKind) {
                        case RefKind.None:
                            token = default(SyntaxToken);
                            break;
                        case RefKind.Ref:
                            token = SyntaxFactory.Token(SyntaxKind.RefKeyword);
                            break;
                        case RefKind.Out:
                            token = SyntaxFactory.Token(SyntaxKind.OutKeyword);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                var expression = _typeConversionAnalyzer.AddExplicitConversion(node.Expression, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor), alwaysExplicit: refKind != RefKind.None);
                AdditionalLocal local = null;
                if (refKind != RefKind.None && NeedsVariableForArgument(node)) {
                    local = new AdditionalLocal($"arg{argName}", expression);
                    _additionalLocals.Add(local.ID, local);
                }
                var nameColon = node.IsNamed ? SyntaxFactory.NameColon((IdentifierNameSyntax)node.NameColonEquals.Name.Accept(TriviaConvertingVisitor)) : null;
                if (local == null) {
                    return SyntaxFactory.Argument(nameColon, token, expression);
                } else {
                    return SyntaxFactory.Argument(nameColon, token, SyntaxFactory.IdentifierName(local.ID).WithAdditionalAnnotations(AdditionalLocal.Annotation));
                }
            }

            private bool NeedsVariableForArgument(VBSyntax.SimpleArgumentSyntax node)
            {
                bool isIdentifier = node.Expression is VBSyntax.IdentifierNameSyntax;
                bool isMemberAccess = node.Expression is VBSyntax.MemberAccessExpressionSyntax;

                var symbolInfo = GetSymbolInfoInDocument(node.Expression);
                bool isProperty = symbolInfo != null && symbolInfo.IsKind(SymbolKind.Property);

                var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                bool isTypeMismatch = !typeInfo.Type.Equals(typeInfo.ConvertedType);

                return (!isIdentifier && !isMemberAccess) || isProperty || isTypeMismatch;
            }

            private ISymbol GetInvocationSymbol(SyntaxNode invocation)
            {
                var symbol = invocation.TypeSwitch(
                    (VBSyntax.InvocationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch(),
                    (VBSyntax.ObjectCreationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch(),
                    (VBSyntax.RaiseEventStatementSyntax e) => _semanticModel.GetSymbolInfo(e.Name).ExtractBestMatch(),
                    _ => { throw new NotSupportedException(); }
                );
                return symbol;
            }

            private AttributeArgumentSyntax ToAttributeArgument(VBSyntax.ArgumentSyntax arg)
            {
                if (!(arg is VBSyntax.SimpleArgumentSyntax))
                    throw new NotSupportedException();
                var a = (VBSyntax.SimpleArgumentSyntax)arg;
                var attr = SyntaxFactory.AttributeArgument((ExpressionSyntax)a.Expression.Accept(TriviaConvertingVisitor));
                if (a.IsNamed) {
                    attr = attr.WithNameEquals(SyntaxFactory.NameEquals((IdentifierNameSyntax)a.NameColonEquals.Name.Accept(TriviaConvertingVisitor)));
                }
                return attr;
            }

            public override CSharpSyntaxNode VisitNameOfExpression(VBSyntax.NameOfExpressionSyntax node)
            {
                return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("nameof"), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax)node.Argument.Accept(TriviaConvertingVisitor)))));
            }

            public override CSharpSyntaxNode VisitEqualsValue(VBSyntax.EqualsValueSyntax node)
            {
                return SyntaxFactory.EqualsValueClause((ExpressionSyntax)node.Value.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitObjectMemberInitializer(VBSyntax.ObjectMemberInitializerSyntax node)
            {
                var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                    node.Initializers.Select(initializer => initializer.Accept(TriviaConvertingVisitor)).Cast<ExpressionSyntax>());
                return SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, memberDeclaratorSyntaxs);
            }

            public override CSharpSyntaxNode VisitAnonymousObjectCreationExpression(VBSyntax.AnonymousObjectCreationExpressionSyntax node)
            {
                var memberDeclaratorSyntaxs = SyntaxFactory.SeparatedList(
                    node.Initializer.Initializers.Select(initializer => initializer.Accept(TriviaConvertingVisitor)).Cast<AnonymousObjectMemberDeclaratorSyntax>());
                return SyntaxFactory.AnonymousObjectCreationExpression(memberDeclaratorSyntaxs);
            }

            public override CSharpSyntaxNode VisitInferredFieldInitializer(VBSyntax.InferredFieldInitializerSyntax node)
            {
                return SyntaxFactory.AnonymousObjectMemberDeclarator((ExpressionSyntax) node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitObjectCreationExpression(VBSyntax.ObjectCreationExpressionSyntax node)
            {
                return SyntaxFactory.ObjectCreationExpression(
                    (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor),
                    // VB can omit empty arg lists:
                    (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList(),
                    (InitializerExpressionSyntax)node.Initializer?.Accept(TriviaConvertingVisitor)
                );
            }

            public override CSharpSyntaxNode VisitArrayCreationExpression(VBSyntax.ArrayCreationExpressionSyntax node)
            {
                var bounds = CommonConversions.ConvertArrayRankSpecifierSyntaxes(node.RankSpecifiers, node.ArrayBounds);
                var allowInitializer = node.Initializer.Initializers.Any() || node.ArrayBounds == null ||
                                       node.ArrayBounds.Arguments.All(b => b.IsOmitted || _semanticModel.GetConstantValue(b.GetExpression()).HasValue);
                var initializerToConvert = allowInitializer ? node.Initializer : null;
                return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor), bounds),
                    (InitializerExpressionSyntax)initializerToConvert?.Accept(TriviaConvertingVisitor)
                );
            }

            public override CSharpSyntaxNode VisitCollectionInitializer(VBSyntax.CollectionInitializerSyntax node)
            {
                var isExplicitCollectionInitializer = node.Parent is VBSyntax.ObjectCollectionInitializerSyntax
                        || node.Parent is VBSyntax.CollectionInitializerSyntax
                        || node.Parent is VBSyntax.ArrayCreationExpressionSyntax;
                var initializerType = isExplicitCollectionInitializer ? SyntaxKind.CollectionInitializerExpression : SyntaxKind.ArrayInitializerExpression;
                var initializer = SyntaxFactory.InitializerExpression(initializerType, SyntaxFactory.SeparatedList(node.Initializers.Select(i => (ExpressionSyntax)i.Accept(TriviaConvertingVisitor))));
                return isExplicitCollectionInitializer
                    ? initializer
                    : (CSharpSyntaxNode)SyntaxFactory.ImplicitArrayCreationExpression(initializer);
            }

            public override CSharpSyntaxNode VisitQueryExpression(VBSyntax.QueryExpressionSyntax node)
            {
                return _queryConverter.ConvertClauses(node.Clauses);
            }

            private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false)
            {
                return CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
            }

            public override CSharpSyntaxNode VisitOrdering(VBSyntax.OrderingSyntax node)
            {
                var convertToken = node.Kind().ConvertToken();
                var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
                var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
                return SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
            }

            public override CSharpSyntaxNode VisitNamedFieldInitializer(VBSyntax.NamedFieldInitializerSyntax node)
            {
                if (node?.Parent?.Parent is VBSyntax.AnonymousObjectCreationExpressionSyntax) {
                    return SyntaxFactory.AnonymousObjectMemberDeclarator(
                        SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Name.Identifier))),
                        (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
                }

                return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    (ExpressionSyntax)node.Name.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                );
            }

            public override CSharpSyntaxNode VisitObjectCollectionInitializer(VBSyntax.ObjectCollectionInitializerSyntax node)
            {
                return node.Initializer.Accept(TriviaConvertingVisitor); //Dictionary initializer comes through here despite the FROM keyword not being in the source code
            }

            public override CSharpSyntaxNode VisitBinaryConditionalExpression(VBSyntax.BinaryConditionalExpressionSyntax node)
            {
                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    (ExpressionSyntax)node.FirstExpression.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.SecondExpression.Accept(TriviaConvertingVisitor)
                );
            }

            public override CSharpSyntaxNode VisitTernaryConditionalExpression(VBSyntax.TernaryConditionalExpressionSyntax node)
            {
                var expr = SyntaxFactory.ConditionalExpression(
                    (ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.WhenTrue.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.WhenFalse.Accept(TriviaConvertingVisitor)
                );

                if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || CommonConversions.PrecedenceCouldChange(node))
                    return SyntaxFactory.ParenthesizedExpression(expr);

                return expr;
            }

            public override CSharpSyntaxNode VisitTypeOfExpression(VBSyntax.TypeOfExpressionSyntax node)
            {
                var expr = SyntaxFactory.BinaryExpression(
                    SyntaxKind.IsExpression,
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)
                );
                return node.IsKind(VBasic.SyntaxKind.TypeOfIsNotExpression) ? expr.InvertCondition() : expr;
            }

            public override CSharpSyntaxNode VisitUnaryExpression(VBSyntax.UnaryExpressionSyntax node)
            {
                var expr = (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor);
                if (node.IsKind(VBasic.SyntaxKind.AddressOfExpression))
                    return expr;
                var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
                SyntaxKind csTokenKind = CSharpUtil.GetExpressionOperatorTokenKind(kind);
                return SyntaxFactory.PrefixUnaryExpression(
                    kind,
                    SyntaxFactory.Token(csTokenKind),
                    expr.AddParensIfRequired()
                );
            }

            public override CSharpSyntaxNode VisitBinaryExpression(VBSyntax.BinaryExpressionSyntax node)
            {
                if (node.IsKind(VBasic.SyntaxKind.IsExpression)) {
                    ExpressionSyntax otherArgument = null;
                    if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                    }
                    if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                    }
                    if (otherArgument != null) {
                        return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                    }
                }
                if (node.IsKind(VBasic.SyntaxKind.IsNotExpression)) {
                    ExpressionSyntax otherArgument = null;
                    if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                    }
                    if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                    }
                    if (otherArgument != null) {
                        return SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, otherArgument, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                    }
                }

                var lhs = _typeConversionAnalyzer.AddExplicitConversion(node.Left, (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor));
                var rhs = _typeConversionAnalyzer.AddExplicitConversion(node.Right, (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor));

                if (node.IsKind(VBasic.SyntaxKind.ConcatenateExpression)) {
                    var stringType = _semanticModel.Compilation.GetTypeByMetadataName("System.String");
                    var lhsTypeInfo = _semanticModel.GetTypeInfo(node.Left);
                    var rhsTypeInfo = _semanticModel.GetTypeInfo(node.Right);
                    if (lhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                        lhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String &&
                        rhsTypeInfo.Type.SpecialType != SpecialType.System_String &&
                        rhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String) {
                        lhs = _typeConversionAnalyzer.AddExplicitConvertTo(node.Left, lhs, stringType);
                    }
                }

                if (node.IsKind(VBasic.SyntaxKind.ExponentiateExpression,
                    VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseExpression($"{nameof(Math)}.{nameof(Math.Pow)}"),
                        ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
                }

                var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
                var op = SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));

                var csBinExp = SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
                return _typeConversionAnalyzer.AddExplicitConversion(node, csBinExp, addParenthesisIfNeeded: true);
            }

            public override CSharpSyntaxNode VisitInvocationExpression(VBSyntax.InvocationExpressionSyntax node)
            {
                var invocationSymbol = _semanticModel.GetSymbolInfo(node).ExtractBestMatch();
                var symbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch();
                var symbolReturnType = symbol?.GetReturnType();

                if (symbol?.ContainingNamespace.MetadataName == "VisualBasic" && TrySubstituteVisualBasicMethod(node, out var csEquivalent)) {
                    return csEquivalent;
                }

                if(TryGetElementAccessExpressionToConvert(out var expressionToConvert)) {
                    return SyntaxFactory.ElementAccessExpression(
                        (ExpressionSyntax)expressionToConvert.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(node.ArgumentList.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor)))));
                }

                if (symbol != null && symbol.IsKind(SymbolKind.Property)) {
                    return node.Expression.Accept(TriviaConvertingVisitor);
                } else {
                    return SyntaxFactory.InvocationExpression(
                        (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                        ConvertArgumentListOrEmpty(node.ArgumentList)
                    );
                }

                bool TryGetElementAccessExpressionToConvert(out VBSyntax.ExpressionSyntax toConvert)
                {
                    toConvert = null;

                    if (invocationSymbol?.IsIndexer() == true
                        // Chances of having an unknown delegate stored as a field/local seem lower than having an unknown non-delegate type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
                        || symbolReturnType.IsErrorType() && node.Expression is VBSyntax.IdentifierNameSyntax
                        || symbolReturnType.IsArrayType() && !(symbol is IMethodSymbol)
                    ) {
                        toConvert = node.Expression;
                    }

                    // VB can use an imaginary member "Item" when an object has an indexer
                    if ((toConvert != null || invocationSymbol.IsErrorType()) && node.Expression is VBSyntax.MemberAccessExpressionSyntax memberAccessExpression && memberAccessExpression.Name.Identifier.Text == "Item") {
                        toConvert = memberAccessExpression.Expression;
                    }
                    return toConvert != null;
                }
            }

            private ArgumentListSyntax ConvertArgumentListOrEmpty(VBSyntax.ArgumentListSyntax argumentListSyntax)
            {
                return (ArgumentListSyntax)argumentListSyntax?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList();
            }

            private bool TrySubstituteVisualBasicMethod(VBSyntax.InvocationExpressionSyntax node, out CSharpSyntaxNode cSharpSyntaxNode)
            {
                cSharpSyntaxNode = null;
                var symbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch();
                if (symbol?.Name == "ChrW" || symbol?.Name == "Chr") {
                    cSharpSyntaxNode = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("char"),
                        ConvertArguments(node.ArgumentList).Single().Expression);
                }

                return cSharpSyntaxNode != null;
            }

            public override CSharpSyntaxNode VisitSingleLineLambdaExpression(VBSyntax.SingleLineLambdaExpressionSyntax node)
            {
                CSharpSyntaxNode body;
                if (node.Body is VBSyntax.StatementSyntax statement) {
                    var convertedStatements = statement.Accept(CreateMethodBodyVisitor(node));
                    if (convertedStatements.Count == 1
                            && convertedStatements.Single() is ExpressionStatementSyntax exprStmt) {
                        // Assignment is an example of a statement in VB that becomes an expression in C#
                        body = exprStmt.Expression;
                    } else {
                        body = SyntaxFactory.Block(convertedStatements).UnpackNonNestedBlock();
                    }
                }
                else {
                    body = node.Body.Accept(TriviaConvertingVisitor);
                }
                var param = (ParameterListSyntax)node.SubOrFunctionHeader.ParameterList.Accept(TriviaConvertingVisitor);
                return CreateLambdaExpression(param, body);
            }

            public override CSharpSyntaxNode VisitMultiLineLambdaExpression(VBSyntax.MultiLineLambdaExpressionSyntax node)
            {
                var methodBodyVisitor = CreateMethodBodyVisitor(node);
                var body = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(methodBodyVisitor)));
                var param = (ParameterListSyntax)node.SubOrFunctionHeader.ParameterList.Accept(TriviaConvertingVisitor);
                return CreateLambdaExpression(param, body);
            }

            private static CSharpSyntaxNode CreateLambdaExpression(ParameterListSyntax param, CSharpSyntaxNode body)
            {
                if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null)
                    return SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
                return SyntaxFactory.ParenthesizedLambdaExpression(param, body);
            }

            #endregion

            #region Type Name / Modifier

            public override CSharpSyntaxNode VisitTupleType(VBSyntax.TupleTypeSyntax node)
            {
                var elements = node.Elements.Select(e => (TupleElementSyntax)e.Accept(TriviaConvertingVisitor));
                return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
            }

            public override CSharpSyntaxNode VisitTypedTupleElement(VBSyntax.TypedTupleElementSyntax node)
            {
                return SyntaxFactory.TupleElement((TypeSyntax) node.Type.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitNamedTupleElement(VBSyntax.NamedTupleElementSyntax node)
            {
                return SyntaxFactory.TupleElement((TypeSyntax)node.AsClause.Type.Accept(TriviaConvertingVisitor), CommonConversions.ConvertIdentifier(node.Identifier));
            }

            public override CSharpSyntaxNode VisitTupleExpression(VBSyntax.TupleExpressionSyntax node)
            {
                var args = node.Arguments.Select(a => {
                    var expr = (ExpressionSyntax)a.Expression.Accept(TriviaConvertingVisitor);
                    return SyntaxFactory.Argument(expr);
                });
                return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(args));
            }

            public override CSharpSyntaxNode VisitPredefinedType(VBSyntax.PredefinedTypeSyntax node)
            {
                if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.DateKeyword)) {
                    return SyntaxFactory.IdentifierName("DateTime");
                }
                return SyntaxFactory.PredefinedType(node.Keyword.ConvertToken());
            }

            public override CSharpSyntaxNode VisitNullableType(VBSyntax.NullableTypeSyntax node)
            {
                return SyntaxFactory.NullableType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitArrayType(VBSyntax.ArrayTypeSyntax node)
            {
                return SyntaxFactory.ArrayType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor), SyntaxFactory.List(node.RankSpecifiers.Select(r => (ArrayRankSpecifierSyntax)r.Accept(TriviaConvertingVisitor))));
            }

            public override CSharpSyntaxNode VisitArrayRankSpecifier(VBSyntax.ArrayRankSpecifierSyntax node)
            {
                return SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), node.Rank)));
            }

            private void SplitTypeParameters(VBSyntax.TypeParameterListSyntax typeParameterList, out TypeParameterListSyntax parameters, out SyntaxList<TypeParameterConstraintClauseSyntax> constraints)
            {
                parameters = null;
                constraints = SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();
                if (typeParameterList == null)
                    return;
                var paramList = new List<TypeParameterSyntax>();
                var constraintList = new List<TypeParameterConstraintClauseSyntax>();
                foreach (var p in typeParameterList.Parameters) {
                    var tp = (TypeParameterSyntax)p.Accept(TriviaConvertingVisitor);
                    paramList.Add(tp);
                    var constraint = (TypeParameterConstraintClauseSyntax)p.TypeParameterConstraintClause?.Accept(TriviaConvertingVisitor);
                    if (constraint != null)
                        constraintList.Add(constraint);
                }
                parameters = SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(paramList));
                constraints = SyntaxFactory.List(constraintList);
            }

            public override CSharpSyntaxNode VisitTypeParameter(VBSyntax.TypeParameterSyntax node)
            {
                SyntaxToken variance = default(SyntaxToken);
                if (!SyntaxTokenExtensions.IsKind(node.VarianceKeyword, VBasic.SyntaxKind.None)) {
                    variance = SyntaxFactory.Token(SyntaxTokenExtensions.IsKind(node.VarianceKeyword, VBasic.SyntaxKind.InKeyword) ? SyntaxKind.InKeyword : SyntaxKind.OutKeyword);
                }
                return SyntaxFactory.TypeParameter(SyntaxFactory.List<AttributeListSyntax>(), variance, ConvertIdentifier(node.Identifier));
            }

            public override CSharpSyntaxNode VisitTypeParameterSingleConstraintClause(VBSyntax.TypeParameterSingleConstraintClauseSyntax node)
            {
                var id = SyntaxFactory.IdentifierName(ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
                return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SingletonSeparatedList((TypeParameterConstraintSyntax)node.Constraint.Accept(TriviaConvertingVisitor)));
            }

            public override CSharpSyntaxNode VisitTypeParameterMultipleConstraintClause(VBSyntax.TypeParameterMultipleConstraintClauseSyntax node)
            {
                var id = SyntaxFactory.IdentifierName(ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
                var constraints = node.Constraints.Select(c => (TypeParameterConstraintSyntax)c.Accept(TriviaConvertingVisitor));
                return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SeparatedList(constraints.OrderBy(c => c.Kind() == SyntaxKind.ConstructorConstraint ? 1 : 0)));
            }

            public override CSharpSyntaxNode VisitSpecialConstraint(VBSyntax.SpecialConstraintSyntax node)
            {
                if (SyntaxTokenExtensions.IsKind(node.ConstraintKeyword, VBasic.SyntaxKind.NewKeyword))
                    return SyntaxFactory.ConstructorConstraint();
                return SyntaxFactory.ClassOrStructConstraint(node.IsKind(VBasic.SyntaxKind.ClassConstraint) ? SyntaxKind.ClassConstraint : SyntaxKind.StructConstraint);
            }

            public override CSharpSyntaxNode VisitTypeConstraint(VBSyntax.TypeConstraintSyntax node)
            {
                return SyntaxFactory.TypeConstraint((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
            }

            #endregion

            #region NameSyntax

            public override CSharpSyntaxNode VisitIdentifierName(VBSyntax.IdentifierNameSyntax node)
            {
                var identifier = SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier, node.GetAncestor<VBSyntax.AttributeSyntax>() != null));

                var qualifiedIdentifier = !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer)
                                                    || node.Parent is VBSyntax.MemberAccessExpressionSyntax maes && maes.Expression == node
                                                    || node.Parent is VBSyntax.QualifiedNameSyntax qns && qns.Left == node
                    ? QualifyNode(node, identifier) : identifier;

                var withArgList = AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
                var sym = GetSymbolInfoInDocument(node);
                if (sym != null && sym.Kind == SymbolKind.Local) {
                    var vbMethodBlock = node.Ancestors().OfType<VBSyntax.MethodBlockSyntax>().FirstOrDefault();
                    if (vbMethodBlock != null &&
                        !node.Parent.IsKind(VBasic.SyntaxKind.NameOfExpression) &&
                        node.Identifier.ValueText.Equals(vbMethodBlock.SubOrFunctionStatement.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)) {
                        var retVar = GetRetVariableNameOrNull(vbMethodBlock);
                        if (retVar != null) {
                            return retVar;
                        }
                    }
                }
                return withArgList;
            }

            private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(VBSyntax.IdentifierNameSyntax node, ExpressionSyntax id)
            {
                var symbol = GetSymbolInfoInDocument(node);
                if (symbol != null &&
                    (symbol.IsOrdinaryMethod() || symbol.IsExtensionMethod()) &&
                    !node.Parent.IsKind(VBasic.SyntaxKind.InvocationExpression, VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.AddressOfExpression)) {
                    return SyntaxFactory.InvocationExpression(id);
                }

                return id;
            }

            private ExpressionSyntax QualifyNode(SyntaxNode node, SimpleNameSyntax left)
            {
                var nodeSymbolInfo = GetSymbolInfoInDocument(node);
                if (left != null &&
                    nodeSymbolInfo?.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol &&
                    !ContextImplicitlyQualfiesSymbol(node, containingSymbol)) {

                    if (containingSymbol is ITypeSymbol containingTypeSymbol &&
                        !nodeSymbolInfo.IsConstructor() /* Constructors are implicitly qualified with their type */) {
                        // Qualify with a type to handle VB's type promotion https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion
                        var qualification =
                            containingTypeSymbol.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart);
                        return Qualify(qualification, left);
                    } else if (nodeSymbolInfo.IsNamespace()) {
                        // Turn partial namespace qualification into full namespace qualification
                        var qualification =
                            containingSymbol.ToCSharpDisplayString();
                        return Qualify(qualification, left);
                    }
                }

                return left;
            }

            private bool ContextImplicitlyQualfiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
            {
                return symbolToCheck is INamespaceSymbol ns && ns.IsGlobalNamespace ||
                       EnclosingTypeImplicitlyQualifiesSymbol(syntaxNodeContext, symbolToCheck);
            }

            private bool EnclosingTypeImplicitlyQualifiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
            {
                ISymbol typeContext = syntaxNodeContext.GetEnclosingDeclaredTypeSymbol(_semanticModel);
                var implicitCsQualifications = ((ITypeSymbol) typeContext).GetBaseTypesAndThis()
                    .Concat(typeContext.FollowProperty(n => n.ContainingSymbol))
                    .ToList();

                return implicitCsQualifications.Contains(symbolToCheck);
            }

            private static QualifiedNameSyntax Qualify(string qualification, ExpressionSyntax toBeQualified)
            {
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.ParseName(qualification),
                    (SimpleNameSyntax)toBeQualified);
            }

            /// <returns>The ISymbol if available in this document, otherwise null</returns>
                private ISymbol GetSymbolInfoInDocument(SyntaxNode node)
            {
                return _semanticModel.SyntaxTree == node.SyntaxTree ? _semanticModel.GetSymbolInfo(node).Symbol : null;
            }

            public override CSharpSyntaxNode VisitQualifiedName(VBSyntax.QualifiedNameSyntax node)
            {
                var lhsSyntax = (NameSyntax)node.Left.Accept(TriviaConvertingVisitor);
                var rhsSyntax = (SimpleNameSyntax)node.Right.Accept(TriviaConvertingVisitor);

                VBSyntax.NameSyntax topLevelName = node;
                while (topLevelName.Parent is VBSyntax.NameSyntax parentName)
                {
                    topLevelName = parentName;
                }
                var partOfNamespaceDeclaration = topLevelName.Parent.IsKind(VBasic.SyntaxKind.NamespaceStatement);
                var leftIsGlobal = node.Left.IsKind(VBasic.SyntaxKind.GlobalName);

                ExpressionSyntax qualifiedName;
                if (partOfNamespaceDeclaration || !(lhsSyntax is SimpleNameSyntax sns)) {
                    if (leftIsGlobal) return rhsSyntax;
                    qualifiedName = lhsSyntax;
                } else {
                    qualifiedName = QualifyNode(node.Left, sns);
                }

                return leftIsGlobal
                    ? (CSharpSyntaxNode)SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)lhsSyntax, rhsSyntax)
                    : SyntaxFactory.QualifiedName((NameSyntax) qualifiedName, rhsSyntax);
            }

            public override CSharpSyntaxNode VisitGenericName(VBSyntax.GenericNameSyntax node)
            {
                return SyntaxFactory.GenericName(ConvertIdentifier(node.Identifier), (TypeArgumentListSyntax)node.TypeArgumentList?.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitTypeArgumentList(VBSyntax.TypeArgumentListSyntax node)
            {
                return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (TypeSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            #endregion
        }
    }
}
