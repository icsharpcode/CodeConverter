using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using static ICSharpCode.CodeConverter.CSharp.SyntaxKindExtensions;
using SyntaxExtensions = ICSharpCode.CodeConverter.Util.SyntaxExtensions;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using SyntaxToken = Microsoft.CodeAnalysis.SyntaxToken;

namespace ICSharpCode.CodeConverter.CSharp
{
    public partial class VisualBasicConverter
    {
        class NodesVisitor : VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode>
        {
            private readonly SemanticModel _semanticModel;
            private readonly Dictionary<ITypeSymbol, string> _createConvertMethodsLookupByReturnType;
            private List<MethodWithHandles> _methodsWithHandles;
            private readonly Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]> _additionalDeclarations = new Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]>();
            private readonly Stack<string> _withBlockTempVariableNames = new Stack<string>();
            private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            private static readonly TypeSyntax VarType = SyntaxFactory.ParseTypeName("var");
            private readonly AdditionalInitializers _additionalInitializers;
            public CommentConvertingNodesVisitor TriviaConvertingVisitor { get; }

            private CommonConversions CommonConversions { get; }

            public NodesVisitor(SemanticModel semanticModel)
            {
                this._semanticModel = semanticModel;
                TriviaConvertingVisitor = new CommentConvertingNodesVisitor(this);
                _createConvertMethodsLookupByReturnType = CreateConvertMethodsLookupByReturnType(semanticModel);
                CommonConversions = new CommonConversions(semanticModel, TriviaConvertingVisitor);
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

            private Func<SyntaxNode, SyntaxNode> DelegateConversion(Func<SyntaxNode, SyntaxList<StatementSyntax>> convert)
            {
                return node => SyntaxFactory.Block(convert(node));
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
                var convertedMembers = node.Members.Select(m => (MemberDeclarationSyntax)m.Accept(TriviaConvertingVisitor)).ToReadOnlyCollection();
                if (!string.IsNullOrEmpty(options.RootNamespace))
                {
                    var rootNamespaceIdentifier = SyntaxFactory.IdentifierName(options.RootNamespace);
                    convertedMembers = PrependRootNamespace(convertedMembers, rootNamespaceIdentifier);
                }

                return SyntaxFactory.CompilationUnit(
                    SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                    SyntaxFactory.List(importsClauses.GroupBy(c => c.ToString()).Select(g => g.First()).Select(c => (UsingDirectiveSyntax)c.Accept(TriviaConvertingVisitor))),
                    attributes,
                    SyntaxFactory.List(convertedMembers)
                );
            }

            private IReadOnlyCollection<MemberDeclarationSyntax> PrependRootNamespace(
                    IReadOnlyCollection<MemberDeclarationSyntax> memberDeclarations,
                    IdentifierNameSyntax rootNamespaceIdentifier)
            {
                if (memberDeclarations.Count == 1 && memberDeclarations.First() is NamespaceDeclarationSyntax nsDecl) {
                    return new [] { nsDecl.WithName(PrependName(nsDecl.Name, rootNamespaceIdentifier)) };
                }

                var newNamespaceDecl = (MemberDeclarationSyntax)SyntaxFactory.NamespaceDeclaration(rootNamespaceIdentifier)
                        .WithMembers(SyntaxFactory.List(memberDeclarations));
                return new [] { newNamespaceDecl };
            }

            private NameSyntax PrependName(NameSyntax name, IdentifierNameSyntax toPrepend)
            {
                if (name is IdentifierNameSyntax identName) {
                    return SyntaxFactory.QualifiedName(toPrepend, identName);
                }
                else if (name is QualifiedNameSyntax qName) {
                    return SyntaxFactory.QualifiedName(PrependName(qName.Left, toPrepend), qName.Right);
                }
                else {
                    throw new ArgumentOutOfRangeException(nameof(name), name, $"{name.GetType().Name} of kind {name.Kind()} not expected within namespace declaration");
                }
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
                var members = node.Members.Select(m => (MemberDeclarationSyntax)m.Accept(TriviaConvertingVisitor));

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
                IEnumerable<MemberDeclarationSyntax> ConvertMembersInner()
                {
                    foreach (var member in members)
                    {
                        yield return (MemberDeclarationSyntax) member.Accept(TriviaConvertingVisitor);

                        if (_additionalDeclarations.TryGetValue(member, out var additionalStatements))
                        {
                            _additionalDeclarations.Remove(member);
                            foreach (var additionalStatement in additionalStatements)
                            {
                                yield return additionalStatement;
                            }
                        }
                    }
                }

                var parentType = members.FirstOrDefault()?.GetAncestor<VBSyntax.TypeBlockSyntax>();
                _methodsWithHandles = GetMethodWithHandles(parentType);

                if (parentType == null || !_methodsWithHandles.Any()) {
                    return ConvertMembersInner();
                }

                return _additionalInitializers.WithAdditionalInitializers(ConvertMembersInner().ToList(), ConvertIdentifier(parentType.BlockStatement.Identifier));
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
                var declarations = new List<MemberDeclarationSyntax>(node.Declarators.Count);

                foreach (var declarator in node.Declarators) {
                    foreach (var decl in CommonConversions.SplitVariableDeclarations(declarator).Values) {
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
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute);
                var isReadonly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.ReadOnlyKeyword));
                var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
                var modifiers = CommonConversions.ConvertModifiers(convertibleModifiers, GetMemberContext(node));
                var isIndexer = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.DefaultKeyword));

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
                    var accessorList = new List<AccessorDeclarationSyntax> {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SemicolonToken)
                    };
                    if (!isReadonly) {
                        accessorList.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken));
                    }
                    accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(accessorList));
                } else {
                    accessors = SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            ((VBSyntax.PropertyBlockSyntax)node.Parent).Accessors.Select(a => (AccessorDeclarationSyntax)a.Accept(TriviaConvertingVisitor))
                        )
                    );
                }

                if (isIndexer)
                    return SyntaxFactory.IndexerDeclaration(
                        SyntaxFactory.List(attributes),
                        modifiers,
                        rawType,
                        null,
                        SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(node.ParameterList.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor)))),
                        accessors
                    );
                else {
                    return SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.List(attributes),
                        modifiers,
                        rawType,
                        null,
                        ConvertIdentifier(node.Identifier), accessors,
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
                var body = VisitStatements(node.Statements, isIterator);
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
                BaseMethodDeclarationSyntax block = (BaseMethodDeclarationSyntax)node.SubOrFunctionStatement.Accept(TriviaConvertingVisitor);
                bool isIterator = node.SubOrFunctionStatement.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.IteratorKeyword));
                return _semanticModel.GetDeclaredSymbol(node).IsPartialDefinition() ? block
                    : block.WithBody(VisitStatements(node.Statements, isIterator));
            }

            private BlockSyntax VisitStatements(SyntaxList<VBSyntax.StatementSyntax> statements, bool isIterator)
            {
                return SyntaxFactory.Block(statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor(isIterator))));
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

                    var decl = SyntaxFactory.MethodDeclaration(
                        attributes,
                        convertedModifiers,
                        (TypeSyntax)node.AsClause?.Type?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        null,
                        ConvertIdentifier(node.Identifier),
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
                return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()?.Equals("System.Runtime.CompilerServices.ExtensionAttribute") == true;
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
                throw new NotSupportedException();
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
                var body = SyntaxFactory.Block(containingBlock.Statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor())));
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, GetMemberContext(node));

                var conversionModifiers = modifiers.Where(CommonConversions.IsConversionOperator).ToList();
                var nonConversionModifiers = SyntaxFactory.TokenList(modifiers.Except(conversionModifiers));

                if (conversionModifiers.Any()) {
                    return SyntaxFactory.ConversionOperatorDeclaration(attributes, nonConversionModifiers,
                        conversionModifiers.Single(), returnType, parameterList, body, null);
                }

                return SyntaxFactory.OperatorDeclaration(attributes, nonConversionModifiers, returnType, node.OperatorToken.ConvertToken(), parameterList, body, null);
            }

            private VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(bool isIterator = false)
            {
                var methodBodyVisitor = new VisualBasicConverter.MethodBodyVisitor(_semanticModel, TriviaConvertingVisitor, _withBlockTempVariableNames, TriviaConvertingVisitor.TriviaConverter) {IsIterator = isIterator};
                return methodBodyVisitor.CommentConvertingVisitor;
            }

            public override CSharpSyntaxNode VisitConstructorBlock(VBSyntax.ConstructorBlockSyntax node)
            {
                var block = node.BlockStatement;
                var attributes = block.AttributeLists.SelectMany(ConvertAttribute);
                var modifiers = CommonConversions.ConvertModifiers(block.Modifiers, GetMemberContext(node));

                var ctor = (node.Statements.FirstOrDefault() as VBSyntax.ExpressionStatementSyntax)?.Expression as VBSyntax.InvocationExpressionSyntax;
                var ctorExpression = ctor?.Expression as VBSyntax.MemberAccessExpressionSyntax;
                var ctorArgs = (ArgumentListSyntax)ctor?.ArgumentList.Accept(TriviaConvertingVisitor);

                IEnumerable<VBSyntax.StatementSyntax> statements;
                ConstructorInitializerSyntax ctorCall;
                if (ctorExpression == null || !ctorExpression.Name.Identifier.IsKindOrHasMatchingText(VBasic.SyntaxKind.NewKeyword)) {
                    statements = node.Statements;
                    ctorCall = null;
                } else if (ctorExpression.Expression is VBSyntax.MyBaseExpressionSyntax) {
                    statements = node.Statements.Skip(1);
                    ctorCall = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ctorArgs ?? SyntaxFactory.ArgumentList());
                } else if (ctorExpression.Expression is VBSyntax.MeExpressionSyntax || ctorExpression.Expression is VBSyntax.MyClassExpressionSyntax) {
                    statements = node.Statements.Skip(1);
                    ctorCall = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, ctorArgs ?? SyntaxFactory.ArgumentList());
                } else {
                    statements = node.Statements;
                    ctorCall = null;
                }

                return SyntaxFactory.ConstructorDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier),
                    (ParameterListSyntax)block.ParameterList.Accept(TriviaConvertingVisitor),
                    ctorCall,
                    SyntaxFactory.Block(statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor())))
                );
            }

            public override CSharpSyntaxNode VisitDeclareStatement(VBSyntax.DeclareStatementSyntax node)
            {
                var dllImportAttributeName = SyntaxFactory.ParseName("System.Runtime.InteropServices.DllImport");
                var dllImportLibLiteral = node.LibraryName.Accept(TriviaConvertingVisitor);

                var attributeArguments = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument((ExpressionSyntax) dllImportLibLiteral));
                var dllImportAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

                var attributeLists = ConvertAttributes(node.AttributeLists).Add(dllImportAttributeList);

                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ExternKeyword));
                var returnType = (TypeSyntax)node.AsClause?.Type.Accept(TriviaConvertingVisitor);
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
                EqualsValueClauseSyntax @default = null;
                if (node.Default != null) {
                    @default = SyntaxFactory.EqualsValueClause((ExpressionSyntax)node.Default?.Value.Accept(TriviaConvertingVisitor));
                }
                var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToList();
                int outAttributeIndex = attributes.FindIndex(a => a.Attributes.Single().Name.ToString() == "Out");
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Local);
                if (outAttributeIndex > -1) {
                    attributes.RemoveAt(outAttributeIndex);
                    modifiers = modifiers.Replace(SyntaxFactory.Token(SyntaxKind.RefKeyword), SyntaxFactory.Token(SyntaxKind.OutKeyword));
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

                return SyntaxFactory.CatchClause(
                    catcher,
                    filter,
                    SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor())))
                );
            }

            public override CSharpSyntaxNode VisitCatchFilterClause(VBSyntax.CatchFilterClauseSyntax node)
            {
                return SyntaxFactory.CatchFilterClause((ExpressionSyntax)node.Filter.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitFinallyBlock(VBSyntax.FinallyBlockSyntax node)
            {
                return SyntaxFactory.FinallyClause(SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor()))));
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
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName("DateTime"),
                        expressionSyntax
                    );
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
                var convertedType = _semanticModel.GetTypeInfo(type).ConvertedType;
                return _createConvertMethodsLookupByReturnType.TryGetValue(convertedType, out var convertMethodName)
                    ? SyntaxFactory.ParseExpression(convertMethodName) : null;
            }

            public override CSharpSyntaxNode VisitTryCastExpression(VBSyntax.TryCastExpressionSyntax node)
            {
                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AsExpression,
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)
                );
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
                    return !type.IsReferenceType ? SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(type.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart))) : CommonConversions.Literal(null);
                }
                return CommonConversions.Literal(node.Token.Value, node.Token.Text);
            }

            public override CSharpSyntaxNode VisitInterpolatedStringExpression(VBSyntax.InterpolatedStringExpressionSyntax node)
            {
                var useVerbatim = node.DescendantNodes().OfType<VBSyntax.InterpolatedStringTextSyntax>().Any(c => CommonConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
                var startTokenKind = useVerbatim ? SyntaxKind.InterpolatedVerbatimStringStartToken : SyntaxKind.InterpolatedStringStartToken;
                return SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(startTokenKind), SyntaxFactory.List(node.Contents.Select(c => (InterpolatedStringContentSyntax)c.Accept(TriviaConvertingVisitor))));
            }

            public override CSharpSyntaxNode VisitInterpolatedStringText(VBSyntax.InterpolatedStringTextSyntax node)
            {
                return SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringTextToken, node.TextToken.Text, node.TextToken.ValueText, default(SyntaxTriviaList)));
            }

            public override CSharpSyntaxNode VisitInterpolation(VBSyntax.InterpolationSyntax node)
            {
                return SyntaxFactory.Interpolation((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override CSharpSyntaxNode VisitInterpolationFormatClause(VBSyntax.InterpolationFormatClauseSyntax node)
            {
                return base.VisitInterpolationFormatClause(node);
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

                var left = (ExpressionSyntax)node.Expression?.Accept(TriviaConvertingVisitor);
                if (left == null) {
                    if (IsSubPartOfConditionalAccess(node)) {
                        return SyntaxFactory.MemberBindingExpression(simpleNameSyntax);
                    }
                    left = SyntaxFactory.IdentifierName(_withBlockTempVariableNames.Peek());
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
                var argumentSyntaxs = ConvertArguments(node);
                return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxs));
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
                int argId = ((VBSyntax.ArgumentListSyntax)node.Parent).Arguments.IndexOf(node);
                var invocation = node.Parent.Parent;
                if (invocation is VBSyntax.ArrayCreationExpressionSyntax)
                    return node.Expression.Accept(TriviaConvertingVisitor);
                var symbol = GetInvocationSymbol(invocation);
                SyntaxToken token = default(SyntaxToken);
                if (symbol != null) {
                    var parameterKinds = symbol.GetParameters().Select(param => param.RefKind).ToList();
                    //WARNING: If named parameters can reach here it won't work properly for them
                    var refKind = argId >= parameterKinds.Count ? RefKind.None : parameterKinds[argId];
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
                return SyntaxFactory.Argument(
                    node.IsNamed ? SyntaxFactory.NameColon((IdentifierNameSyntax)node.NameColonEquals.Name.Accept(TriviaConvertingVisitor)) : null,
                    token,
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                );
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
                var vbBodyClauses = node.Clauses;
                var vbFromClause = vbBodyClauses.OfType<VBSyntax.FromClauseSyntax>().First();
                var fromClauseSyntax = ConvertFromClauseSyntax(vbFromClause);
                var vbGroupClause = vbBodyClauses.OfType<VBSyntax.GroupByClauseSyntax>().SingleOrDefault();
                var vbSelectClause = vbBodyClauses.OfType<VBSyntax.SelectClauseSyntax>().SingleOrDefault();
                var selectClauseSyntax = vbSelectClause != null
                    ? ConvertSelectClauseSyntax(vbSelectClause)
                    : CreateDefaultSelectClause(fromClauseSyntax);
                var alreadyConverted = new VBSyntax.QueryClauseSyntax[] { vbFromClause, vbGroupClause, vbSelectClause };
                SelectOrGroupClauseSyntax selectOrGroup = null;
                QueryContinuationSyntax queryContinuation = null;
                var queryClauseSyntaxs = SyntaxFactory.List(vbBodyClauses.TakeWhile(x => x != vbGroupClause).Except(alreadyConverted).Select(ConvertQueryBodyClause));
                var continuedClauses = SyntaxFactory.List(vbBodyClauses.SkipWhile(x => x != vbGroupClause).Skip(1).Except(alreadyConverted).Select(ConvertQueryBodyClause));
                if (vbGroupClause != null) {
                    selectOrGroup = ConvertGroupByClause(vbGroupClause);
                    queryContinuation = SyntaxFactory.QueryContinuation(GetGroupIdentifier(vbGroupClause),
                        SyntaxFactory.QueryBody(continuedClauses, selectClauseSyntax, null));
                } else {
                    selectOrGroup = selectClauseSyntax;
                }

                var queryBodySyntax = SyntaxFactory.QueryBody(queryClauseSyntaxs, selectOrGroup, queryContinuation);
                return SyntaxFactory.QueryExpression(fromClauseSyntax, queryBodySyntax);
            }

            private FromClauseSyntax ConvertFromClauseSyntax(VBSyntax.FromClauseSyntax vbFromClause)
            {
                var collectionRangeVariableSyntax = vbFromClause.Variables.Single();
                var fromClauseSyntax = SyntaxFactory.FromClause(
                    ConvertIdentifier(collectionRangeVariableSyntax.Identifier.Identifier),
                    (ExpressionSyntax) collectionRangeVariableSyntax.Expression.Accept(TriviaConvertingVisitor));
                return fromClauseSyntax;
            }

            private SelectClauseSyntax ConvertSelectClauseSyntax(VBSyntax.SelectClauseSyntax vbFromClause)
            {
                var collectionRangeVariableSyntax = vbFromClause.Variables.Single();
                return SyntaxFactory.SelectClause(
                    (ExpressionSyntax)collectionRangeVariableSyntax.Expression.Accept(TriviaConvertingVisitor)
                );
            }

            private static SelectClauseSyntax CreateDefaultSelectClause(FromClauseSyntax fromClauseSyntax)
            {
                return SyntaxFactory.SelectClause(SyntaxFactory.IdentifierName(fromClauseSyntax.Identifier));
            }

            private QueryClauseSyntax ConvertQueryBodyClause(VBSyntax.QueryClauseSyntax node)
            {
                return node.TypeSwitch<VBSyntax.QueryClauseSyntax, VBSyntax.FromClauseSyntax, VBSyntax.JoinClauseSyntax, VBSyntax.LetClauseSyntax, VBSyntax.OrderByClauseSyntax, VBSyntax.WhereClauseSyntax, QueryClauseSyntax>(
                    //(VBSyntax.AggregateClauseSyntax ags) => null,
                    //(VBSyntax.DistinctClauseSyntax ds) => null,
                    ConvertFromClauseSyntax,
                    ConvertJoinClause,
                    ConvertLetClause,
                    ConvertOrderByClause,
                    //(VBSyntax.PartitionClauseSyntax ps) => null, // TODO Convert to Skip and Take methods (they don't exist in C#s query syntax)
                    //(VBSyntax.PartitionWhileClauseSyntax pws) => null, // TODO Convert to SkipWhile and TakeWhile methods (they don't exist in C#s query syntax)
                    ConvertWhereClause,
                    _ => throw new NotImplementedException($"Conversion for query clause with kind '{node.Kind()}' not implemented"));
            }

            private GroupClauseSyntax ConvertGroupByClause(VBSyntax.GroupByClauseSyntax gs)
            {
                if (gs == null) return null;
                var item = (IdentifierNameSyntax) gs.Items.Single().Expression.Accept(TriviaConvertingVisitor);
                var keyExpression = (ExpressionSyntax) gs.Keys.Single().Expression.Accept(TriviaConvertingVisitor);
                return SyntaxFactory.GroupClause(item, keyExpression);
            }

            private SyntaxToken GetGroupIdentifier(VBSyntax.GroupByClauseSyntax gs)
            {
                return ConvertIdentifier(gs.AggregationVariables.Select(x => x.Aggregation).OfType<VBSyntax.FunctionAggregationSyntax>().First().FunctionName);
            }

            private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false)
            {
                return CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
            }

            private QueryClauseSyntax ConvertWhereClause(VBSyntax.WhereClauseSyntax ws)
            {
                return SyntaxFactory.WhereClause((ExpressionSyntax) ws.Condition.Accept(TriviaConvertingVisitor));
            }

            private QueryClauseSyntax ConvertLetClause(VBSyntax.LetClauseSyntax ls)
            {
                var singleVariable = ls.Variables.Single();
                return SyntaxFactory.LetClause(ConvertIdentifier(singleVariable.NameEquals.Identifier.Identifier), (ExpressionSyntax) singleVariable.Expression.Accept(TriviaConvertingVisitor));
            }

            private QueryClauseSyntax ConvertOrderByClause(VBSyntax.OrderByClauseSyntax os)
            {
                return SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(os.Orderings.Select(o => (OrderingSyntax) o.Accept(TriviaConvertingVisitor))));
            }

            public override CSharpSyntaxNode VisitOrdering(VBSyntax.OrderingSyntax node)
            {
                var convertToken = node.Kind().ConvertToken();
                var expressionSyntax = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
                var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
                return SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
            }

            private QueryClauseSyntax ConvertJoinClause(VBSyntax.JoinClauseSyntax js)
            {
                var variable = js.JoinedVariables.Single();
                var joinLhs = SingleExpression(js.JoinConditions.Select(c => c.Left.Accept(TriviaConvertingVisitor))
                    .Cast<ExpressionSyntax>().ToList());
                var joinRhs = SingleExpression(js.JoinConditions.Select(c => c.Right.Accept(TriviaConvertingVisitor))
                    .Cast<ExpressionSyntax>().ToList());
                var convertIdentifier = ConvertIdentifier(variable.Identifier.Identifier);
                var expressionSyntax = (ExpressionSyntax) variable.Expression.Accept(TriviaConvertingVisitor);

                JoinIntoClauseSyntax joinIntoClauseSyntax = null;
                if (js is VBSyntax.GroupJoinClauseSyntax gjs) {
                    joinIntoClauseSyntax = gjs.AggregationVariables
                        .Where(a => a.Aggregation is VBSyntax.GroupAggregationSyntax)
                        .Select(a => SyntaxFactory.JoinIntoClause(ConvertIdentifier(a.NameEquals.Identifier.Identifier)))
                        .SingleOrDefault();
                }
                return SyntaxFactory.JoinClause(null, convertIdentifier, expressionSyntax, joinLhs, joinRhs, joinIntoClauseSyntax);
            }

            private ExpressionSyntax SingleExpression(IReadOnlyCollection<ExpressionSyntax> expressions)
            {
                if (expressions.Count == 1) return expressions.Single();
                return SyntaxFactory.AnonymousObjectCreationExpression(SyntaxFactory.SeparatedList(expressions.Select((e, i) =>
                    SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals($"key{i}"), e)
                )));
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

                if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation))
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
                if (node.IsKind(VBasic.SyntaxKind.TypeOfIsNotExpression))
                    return SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            SyntaxFactory.ParenthesizedExpression(expr));
                return expr;
            }

            public override CSharpSyntaxNode VisitUnaryExpression(VBSyntax.UnaryExpressionSyntax node)
            {
                var expr = (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor);
                if (node.IsKind(VBasic.SyntaxKind.AddressOfExpression))
                    return expr;
                var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
                return SyntaxFactory.PrefixUnaryExpression(
                    kind,
                    SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind)),
                    MaybeParenthesize(expr, kind)
                );
            }
            private ExpressionSyntax MaybeParenthesize(ExpressionSyntax expr, SyntaxKind operatorKind)
            {
                return expr is BinaryExpressionSyntax && operatorKind == SyntaxKind.LogicalNotExpression
                    ? SyntaxFactory.ParenthesizedExpression(expr)
                    : expr;
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

                var lhs = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                var rhs = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);

                // e.g. VB DivideExpression "/" is always on doubles unless you use the "\" IntegerDivideExpression, so need to cast in C#
                // Need the unconverted type, since the whole point is that it gets converted to a double by the operator
                if (node.IsKind(VBasic.SyntaxKind.DivideExpression) && !node.HasOperandOfUnconvertedType("System.Double", _semanticModel)) {
                    var doubleType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
                    rhs = SyntaxFactory.CastExpression(doubleType, rhs);
                }

                if (node.IsKind(VBasic.SyntaxKind.ExponentiateExpression,
                    VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseExpression($"{nameof(Math)}.{nameof(Math.Pow)}"),
                        ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
                }

                var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken(TokenContext.Local);
                var op = SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));
                return SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
            }

            public override CSharpSyntaxNode VisitInvocationExpression(VBSyntax.InvocationExpressionSyntax node)
            {
                var invocationSymbol = _semanticModel.GetSymbolInfo(node).ExtractBestMatch();
                var symbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch();
                var symbolReturnType = symbol?.GetReturnType();
                // Chances of having an unknown delegate stored as a field/local seem lower than having an unknown non-delegate type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
                if (invocationSymbol?.IsIndexer() == true || symbolReturnType.IsArrayType() && !(symbol is IMethodSymbol) || symbolReturnType.IsErrorType() && node.Expression is VBSyntax.IdentifierNameSyntax)
                {
                    return SyntaxFactory.ElementAccessExpression(
                        (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(node.ArgumentList.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor)))));
                }

                return SyntaxFactory.InvocationExpression(
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ArgumentList()
                );
            }

            public override CSharpSyntaxNode VisitSingleLineLambdaExpression(VBSyntax.SingleLineLambdaExpressionSyntax node)
            {
                CSharpSyntaxNode body;
                if (node.Body is VBSyntax.StatementSyntax statement) {
                    var convertedStatements = statement.Accept(CreateMethodBodyVisitor());
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
                var body = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CreateMethodBodyVisitor())));
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
                return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SeparatedList(node.Constraints.Select(c => (TypeParameterConstraintSyntax)c.Accept(TriviaConvertingVisitor))));
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

                return !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer)
                                    || node.Parent is VBSyntax.MemberAccessExpressionSyntax maes && maes.Expression == node
                                    || node.Parent is VBSyntax.QualifiedNameSyntax qns && qns.Left == node
                    ? QualifyNode(node, identifier) : identifier;
            }

            private ExpressionSyntax QualifyNode(SyntaxNode node, ExpressionSyntax left)
            {
                var qualifiedLeft = left;
                var nodeSymbolInfo = GetSymbolInfoInDocument(node);
                if (qualifiedLeft != null &&
                    node?.IsKind(VBasic.SyntaxKind.IdentifierName) == true &&
                    nodeSymbolInfo?.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol && 
                    !ContextImplicitlyQualfiesSymbol(node, containingSymbol)) {

                    if (containingSymbol.IsType() &&
                        !nodeSymbolInfo.IsConstructor() /* Constructors are implicitly qualified with their type */) {
                        // Qualify with a type to handle VB's type promotion https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion
                        var qualification =
                            containingSymbol.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart);
                        qualifiedLeft = Qualify(qualification, qualifiedLeft);
                    } else if (nodeSymbolInfo.IsNamespace()) {
                        // Turn partial namespace qualification into full namespace qualification
                        var qualification =
                            containingSymbol.ToCSharpDisplayString();
                        qualifiedLeft = Qualify(qualification, qualifiedLeft);
                    }
                }

                return qualifiedLeft;
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
                    .Concat(FollowProperty(typeContext, n => n.ContainingSymbol))
                    .ToList();

                return implicitCsQualifications.Contains(symbolToCheck);
            }

            private static QualifiedNameSyntax Qualify(string qualification, ExpressionSyntax toBeQualified)
            {
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.ParseName(qualification),
                    (SimpleNameSyntax)toBeQualified);
            }

            private static IEnumerable<T> FollowProperty<T>(T start, Func<T, T> getProperty) where T : class
            {
                for (var current = start; current != null; current = getProperty(current)) {
                    yield return current;
                }
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
                if (partOfNamespaceDeclaration) {
                    if (leftIsGlobal) return rhsSyntax;
                    qualifiedName = lhsSyntax;
                } else {
                    qualifiedName = QualifyNode(node.Left, lhsSyntax);
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
