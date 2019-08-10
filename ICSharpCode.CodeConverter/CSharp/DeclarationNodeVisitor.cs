using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using StringComparer = System.StringComparer;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using SyntaxToken = Microsoft.CodeAnalysis.SyntaxToken;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class DeclarationNodeVisitor : VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private static readonly Type ExtensionAttributeType = typeof(ExtensionAttribute);
        private static readonly Type _outAttributeType = typeof(OutAttribute);
        private static readonly Type DllImportType = typeof(DllImportAttribute);
        private static readonly Type CharSetType = typeof(CharSet);
        private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
        private static readonly TypeSyntax VarType = SyntaxFactory.ParseTypeName("var");
        private readonly CSharpCompilation _csCompilation;
        private readonly Compilation _compilation;
        private readonly SemanticModel _semanticModel;
        private MethodsWithHandles _methodsWithHandles;
        private readonly Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]> _additionalDeclarations = new Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]>();
        private readonly Stack<string> _withBlockTempVariableNames = new Stack<string>();
        private readonly AdditionalInitializers _additionalInitializers;
        private readonly AdditionalLocals _additionalLocals = new AdditionalLocals();
        private uint _failedMemberConversionMarkerCount;
        private readonly HashSet<string> _extraUsingDirectives = new HashSet<string>();
        public CommentConvertingNodesVisitor TriviaConvertingVisitor { get; }
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
        private static HashSet<string> _accessedThroughMyClass;
        private readonly ExpressionNodeVisitor _expressionNodeVisitor;

        private CommonConversions CommonConversions { get; }

        public DeclarationNodeVisitor(Compilation compilation, SemanticModel semanticModel, CSharpCompilation csCompilation)
        {
            _compilation = compilation;
            _semanticModel = semanticModel;
            this._csCompilation = csCompilation;
            _visualBasicEqualityComparison = new VisualBasicEqualityComparison(_semanticModel, _extraUsingDirectives);
            TriviaConverter triviaConverter = new TriviaConverter();
            TriviaConvertingVisitor = new CommentConvertingNodesVisitor(this, triviaConverter);
            var typeConversionAnalyzer = new TypeConversionAnalyzer(semanticModel, csCompilation, _extraUsingDirectives);
            CommonConversions = new CommonConversions(semanticModel, TriviaConvertingVisitor, typeConversionAnalyzer);
            _additionalInitializers = new AdditionalInitializers();
            _expressionNodeVisitor = new ExpressionNodeVisitor(semanticModel, _visualBasicEqualityComparison, _additionalLocals, csCompilation, _withBlockTempVariableNames, _methodsWithHandles, CommonConversions, triviaConverter);
        }

        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        public override CSharpSyntaxNode VisitXmlElement(VBSyntax.XmlElementSyntax node)
        {
            return _expressionNodeVisitor.VisitXmlElement(node);
        }

        public override CSharpSyntaxNode VisitGetTypeExpression(VBSyntax.GetTypeExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitGetTypeExpression(node);
        }

        public override CSharpSyntaxNode VisitGlobalName(VBSyntax.GlobalNameSyntax node)
        {
            return _expressionNodeVisitor.VisitGlobalName(node);
        }

        #region Attributes

        private SyntaxList<AttributeListSyntax> ConvertAttributes(SyntaxList<VBSyntax.AttributeListSyntax> attributeListSyntaxs)
        {
            return SyntaxFactory.List(attributeListSyntaxs.SelectMany(ConvertAttribute));
        }

        IEnumerable<AttributeListSyntax> ConvertAttribute(VBSyntax.AttributeListSyntax attributeList)
        {
            // These attributes' semantic effects are expressed differently in CSharp.
            return attributeList.Attributes.Where(a => !IsExtensionAttribute(a) && !IsOutAttribute(a))
                .Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor));
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

            var optionCompareText = node.Options.Any(x => x.NameKeyword.ValueText.Equals("Compare", StringComparison.OrdinalIgnoreCase) &&
                                                       x.ValueKeyword.ValueText.Equals("Text", StringComparison.OrdinalIgnoreCase));
            _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive = optionCompareText;

            var attributes = SyntaxFactory.List(node.Attributes.SelectMany(a => a.AttributeLists).SelectMany(ConvertAttribute));
            var sourceAndConverted = node.Members.Select(m => (Source: m, Converted: ConvertMember(m))).ToReadOnlyCollection();
            var convertedMembers = String.IsNullOrEmpty(options.RootNamespace)
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
                : SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Alias.Identifier)));
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
            if (_methodsWithHandles.Any()) _extraUsingDirectives.Add("System.Runtime.CompilerServices");//For MethodImplOptions.Synchronized
                
            if (parentType == null || !_methodsWithHandles.Any()) {
                return GetDirectlyConvertMembers();
            }

            var typeSymbol = (ITypeSymbol) _semanticModel.GetDeclaredSymbol(parentType);
            return _additionalInitializers.WithAdditionalInitializers(typeSymbol, GetDirectlyConvertMembers().ToList(), CommonConversions.ConvertIdentifier(parentType.BlockStatement.Identifier));

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
                    = SyntaxFactory.ClassDeclaration("_failedMemberConversionMarker" + ++_failedMemberConversionMarkerCount);
                return dummyClass.WithCsTrailingErrorComment(memberCausingError, e);
            }
        }

        public override CSharpSyntaxNode VisitClassBlock(VBSyntax.ClassBlockSyntax node)
        {
            _accessedThroughMyClass = GetMyClassAccessedNames(node);
            var classStatement = node.ClassStatement;
            var attributes = ConvertAttributes(classStatement.AttributeLists);
            SplitTypeParameters(classStatement.TypeParameterList, out var parameters, out var constraints);
            var convertedIdentifier = CommonConversions.ConvertIdentifier(classStatement.Identifier);

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
                attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), CommonConversions.ConvertIdentifier(stmt.Identifier),
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
                attributes, ConvertTypeBlockModifiers(stmt, TokenContext.Global), CommonConversions.ConvertIdentifier(stmt.Identifier),
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
                attributes, ConvertTypeBlockModifiers(stmt, TokenContext.InterfaceOrModule), CommonConversions.ConvertIdentifier(stmt.Identifier),
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
            return CommonConversions.ConvertModifiers(stmt, stmt.Modifiers, interfaceOrModule).AddRange(extraModifiers);
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
                SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(stmt, stmt.Modifiers, TokenContext.Global), CommonConversions.ConvertIdentifier(stmt.Identifier),
                baseList,
                members
            );
        }

        public override CSharpSyntaxNode VisitEnumMemberDeclaration(VBSyntax.EnumMemberDeclarationSyntax node)
        {
            var attributes = ConvertAttributes(node.AttributeLists);
            return SyntaxFactory.EnumMemberDeclaration(
                attributes, CommonConversions.ConvertIdentifier(node.Identifier),
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
                SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Global),
                returnType, CommonConversions.ConvertIdentifier(node.Identifier),
                typeParameters,
                (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                constraints
            );
        }

        #endregion

        #region Type Members

        public override CSharpSyntaxNode VisitFieldDeclaration(VBSyntax.FieldDeclarationSyntax node)
        {
            _additionalLocals.PushScope();
            var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToList();
            var convertableModifiers = node.Modifiers.Where(m => !SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
            var isWithEvents = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WithEventsKeyword));
            var convertedModifiers = CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], convertableModifiers, GetMemberContext(node));
            var declarations = new List<MemberDeclarationSyntax>(node.Declarators.Count);

            foreach (var declarator in node.Declarators) {
                foreach (var decl in CommonConversions.SplitVariableDeclarations(declarator, preferExplicitType: true).Values) {
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

                        var fieldDecls = _methodsWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl,
                            convertedModifiers, SyntaxFactory.List(attributes));
                        declarations.AddRange(fieldDecls);
                    } else {
                        FieldDeclarationSyntax baseFieldDeclarationSyntax;
                        if (_additionalLocals.Count() > 0) {
                            if (decl.Variables.Count > 1) {
                                // Currently no way to tell which _additionalLocals would apply to which initializer
                                throw new NotImplementedException("Fields with multiple declarations and initializers with ByRef parameters not currently supported");
                            }
                            var v = decl.Variables.First();
                            if (v.Initializer.Value.DescendantNodes().OfType<InvocationExpressionSyntax>().Count() > 1) {
                                throw new NotImplementedException("Field initializers with nested method calls not currently supported");
                            }
                            var calledMethodName = v.Initializer.Value.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First().DescendantNodes().OfType<IdentifierNameSyntax>().First();
                            var newMethodName = $"{calledMethodName.Identifier.ValueText}_{v.Identifier.ValueText}";
                            var localVars = _additionalLocals.Select(l => l.Value)
                                .Select(al => SyntaxFactory.LocalDeclarationStatement(CommonConversions.CreateVariableDeclarationAndAssignment(al.Prefix, al.Initializer)))
                                .Cast<StatementSyntax>().ToList();
                            var newInitializer = v.Initializer.Value.ReplaceNodes(v.Initializer.Value.GetAnnotatedNodes(AdditionalLocals.Annotation), (an, _) => {
                                // This should probably use a unique name like in MethodBodyVisitor - a collision is far less likely here
                                var id = (an as IdentifierNameSyntax).Identifier.ValueText;
                                return SyntaxFactory.IdentifierName(_additionalLocals[id].Prefix);
                            });
                            var body = SyntaxFactory.Block(localVars.Concat(SyntaxFactory.SingletonList(SyntaxFactory.ReturnStatement(newInitializer))));
                            var methodAttrs = SyntaxFactory.List<AttributeListSyntax>();
                            // Method calls in initializers must be static in C# - Supporting this is #281
                            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                            var typeConstraints = SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();
                            var parameterList = SyntaxFactory.ParameterList();
                            var methodDecl = SyntaxFactory.MethodDeclaration(methodAttrs, modifiers, decl.Type, null, SyntaxFactory.Identifier(newMethodName), null, parameterList, typeConstraints, body, null);
                            declarations.Add(methodDecl);

                            var newVar = v.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(newMethodName))));
                            var newVarDecl = SyntaxFactory.VariableDeclaration(decl.Type, SyntaxFactory.SingletonSeparatedList(newVar));

                            baseFieldDeclarationSyntax = SyntaxFactory.FieldDeclaration(SyntaxFactory.List(attributes), convertedModifiers, newVarDecl);
                        } else {
                            baseFieldDeclarationSyntax = SyntaxFactory.FieldDeclaration(SyntaxFactory.List(attributes), convertedModifiers, decl);
                        }
                        declarations.Add(baseFieldDeclarationSyntax);
                    }
                }
            }

            _additionalLocals.PopScope();
            _additionalDeclarations.Add(node, declarations.Skip(1).ToArray());
            return declarations.First();
        }

        private MethodsWithHandles GetMethodWithHandles(VBSyntax.TypeBlockSyntax parentType)
        {
            if (parentType == null) return new MethodsWithHandles(new List<MethodWithHandles>());

            var containingType = (ITypeSymbol) _semanticModel.GetDeclaredSymbol(parentType);
            var methodWithHandleses = containingType.GetMembers().OfType<IMethodSymbol>()
                .Where(m => VBasic.VisualBasicExtensions.HandledEvents(m).Any())
                .Select(m => {
                    var csPropIds = VBasic.VisualBasicExtensions.HandledEvents(m)
                        .Where(p => p.HandlesKind == VBasic.HandledEventKind.WithEvents)
                        .Select(p => (SyntaxFactory.Identifier(GetCSharpIdentifierText(p)), SyntaxFactory.Identifier(p.EventSymbol.Name)))
                        .ToList();
                    var csFormIds = VBasic.VisualBasicExtensions.HandledEvents(m)
                        .Where(p => p.HandlesKind != VBasic.HandledEventKind.WithEvents)
                        .Select(p => (SyntaxFactory.Identifier(GetCSharpIdentifierText(p)), SyntaxFactory.Identifier(p.EventSymbol.Name)))
                        .ToList();
                    if (!csPropIds.Any() && !csFormIds.Any()) return null;
                    var csMethodId = SyntaxFactory.Identifier(m.Name);
                    return new MethodWithHandles(csMethodId, csPropIds, csFormIds);
                }).Where(x => x != null)
                .ToList();
            return new MethodsWithHandles(methodWithHandleses);

            string GetCSharpIdentifierText(VBasic.HandledEvent p)
            {
                switch (p.HandlesKind) {
                    //For me, trying to use "MyClass" in a Handles expression is a syntax error. Events aren't overridable anyway so I'm not sure how this would get used.
                    case VBasic.HandledEventKind.MyClass:
                    case VBasic.HandledEventKind.Me:
                        return "this";
                    case VBasic.HandledEventKind.MyBase:
                        return "base";
                    case VBasic.HandledEventKind.WithEvents:
                        return p.EventContainer.Name;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override CSharpSyntaxNode VisitPropertyStatement(VBSyntax.PropertyStatementSyntax node)
        {
            var attributes = node.AttributeLists.SelectMany(ConvertAttribute).ToArray();
            var isReadonly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.ReadOnlyKeyword));
            var isWriteOnly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WriteOnlyKeyword));
            var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
            var modifiers = CommonConversions.ConvertModifiers(node, convertibleModifiers, GetMemberContext(node));
            var isIndexer = CommonConversions.IsDefaultIndexer(node);
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
            if (node.Parent is VBSyntax.PropertyBlockSyntax propertyBlock) {
                if (node.ParameterList?.Parameters.Any() == true && !isIndexer) {
                    if (accessedThroughMyClass) {
                        // Would need to create a delegating implementation to implement this
                        throw new NotImplementedException("MyClass indexing not implemented");
                    }
                    var accessorMethods = propertyBlock.Accessors.Select(a =>
                            (MethodDeclarationSyntax)a.Accept(TriviaConvertingVisitor))
                        .Select(WithMergedModifiers).ToArray();
                    _additionalDeclarations.Add(propertyBlock, accessorMethods.Skip(1).ToArray());
                    return accessorMethods[0];
                }

                accessors = SyntaxFactory.AccessorList(
                    SyntaxFactory.List(
                        (propertyBlock.Accessors.Select(a =>
                            (AccessorDeclarationSyntax)a.Accept(TriviaConvertingVisitor))
                        )
                    ));
            } else {
                accessors = ConvertSimpleAccessors(isWriteOnly, isReadonly, isInInterface);
            }


            if (isIndexer) {
                if (accessedThroughMyClass) {
                    // Not sure if this is possible
                    throw new NotImplementedException("MyClass indexing not implemented");
                }

                var parameters = SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(node.ParameterList.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
                return SyntaxFactory.IndexerDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    rawType,
                    null,
                    parameters,
                    accessors
                );
            } else {
                var csIdentifier = CommonConversions.ConvertIdentifier(node.Identifier);

                if (accessedThroughMyClass) {
                    string csIndentifierName = AddRealPropertyDelegatingToMyClassVersion(node, csIdentifier, attributes, modifiers, rawType);
                    modifiers = modifiers.Remove(modifiers.Single(m => m.IsKind(SyntaxKind.VirtualKeyword)));
                    csIdentifier = SyntaxFactory.Identifier(csIndentifierName);
                }

                var semicolonToken = SyntaxFactory.Token(initializer == null ? SyntaxKind.None : SyntaxKind.SemicolonToken);
                return SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.List(attributes),
                    modifiers,
                    rawType,
                    null,
                    csIdentifier, accessors,
                    null,
                    initializer,
                    semicolonToken);
            }

            MemberDeclarationSyntax WithMergedModifiers(MethodDeclarationSyntax member)
            {
                SyntaxTokenList originalModifiers = member.GetModifiers();
                var hasVisibility = originalModifiers.Any(m => m.IsCsVisibility(false, false));
                var modifiersToAdd = hasVisibility ? modifiers.Where(m => !m.IsCsVisibility(false, false)) : modifiers;
                return member.WithModifiers(SyntaxFactory.TokenList(originalModifiers.Concat(modifiersToAdd)));
            }
        }

        private string AddRealPropertyDelegatingToMyClassVersion(VBSyntax.PropertyStatementSyntax node, SyntaxToken csIdentifier,
            AttributeListSyntax[] attributes, SyntaxTokenList modifiers, TypeSyntax rawType)
        {
            var csIndentifierName = "MyClass" + csIdentifier.ValueText;
            ExpressionSyntax thisDotIdentifier = SyntaxFactory.ParseExpression($"this.{csIndentifierName}");
            var getReturn = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(thisDotIdentifier));
            var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getReturn);
            var setValue = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, thisDotIdentifier,
                    SyntaxFactory.IdentifierName("value"))));
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

            _additionalDeclarations.Add(node, new MemberDeclarationSyntax[] {realDecl});
            return csIndentifierName;
        }

        private static AccessorListSyntax ConvertSimpleAccessors(bool isWriteOnly, bool isReadonly, bool isInInterface)
        {
            AccessorListSyntax accessors;
            var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);
            var setAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);
            if (isWriteOnly)
            {
                getAccessor = getAccessor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }

            if (isReadonly)
            {
                setAccessor = setAccessor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }

            if (isInInterface && isReadonly)
            {
                accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {getAccessor}));
            }
            else if (isInInterface && isWriteOnly)
            {
                accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {setAccessor}));
            }
            else
            {
                // In VB, there's a backing field which can always be read and written to even on ReadOnly/WriteOnly properties.
                // Our conversion will rewrite usages of that field to use the property accessors which therefore must exist and be private at minimum.
                accessors = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {getAccessor, setAccessor}));
            }

            return accessors;
        }

        public override CSharpSyntaxNode VisitPropertyBlock(VBSyntax.PropertyBlockSyntax node)
        {
            return node.PropertyStatement.Accept(TriviaConvertingVisitor);
        }

        public override CSharpSyntaxNode VisitAccessorBlock(VBSyntax.AccessorBlockSyntax node)
        {
            SyntaxKind blockKind;
            bool isIterator = node.GetModifiers().Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.IteratorKeyword));
            var csReturnVariableOrNull = _expressionNodeVisitor.GetRetVariableNameOrNull(node);
            var convertedStatements = ConvertStatements(node.Statements, _expressionNodeVisitor.CreateMethodBodyVisitor(node, isIterator));
            var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);
            var attributes = ConvertAttributes(node.AccessorStatement.AttributeLists);
            var modifiers = CommonConversions.ConvertModifiers(node, node.AccessorStatement.Modifiers, TokenContext.Local);
            string potentialMethodId = null;
            var containingProperty = node.GetAncestor<VBSyntax.PropertyBlockSyntax>()?.PropertyStatement;
            switch (node.Kind()) {
                case VBasic.SyntaxKind.GetAccessorBlock:
                    blockKind = SyntaxKind.GetAccessorDeclaration;
                    potentialMethodId = $"get_{(containingProperty.Identifier.Text)}";

                    if (containingProperty.AsClause is VBSyntax.SimpleAsClauseSyntax getAsClause && 
                        TryConvertAsParameterizedProperty(out var method)) {
                        return method.WithReturnType((TypeSyntax)getAsClause.Type.Accept(TriviaConvertingVisitor));
                    }
                    break;
                case VBasic.SyntaxKind.SetAccessorBlock:
                    blockKind = SyntaxKind.SetAccessorDeclaration;
                    potentialMethodId = $"set_{(containingProperty.Identifier.Text)}";
                        
                    if (containingProperty.AsClause is VBSyntax.SimpleAsClauseSyntax setAsClause && TryConvertAsParameterizedProperty(out var setMethod)) {
                        var valueParameterType = (TypeSyntax)setAsClause.Type.Accept(TriviaConvertingVisitor);
                        return setMethod.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("value")).WithType(valueParameterType));
                    }
                    break;
                case VBasic.SyntaxKind.AddHandlerAccessorBlock:
                    blockKind = SyntaxKind.AddAccessorDeclaration;
                    break;
                case VBasic.SyntaxKind.RemoveHandlerAccessorBlock:
                    blockKind = SyntaxKind.RemoveAccessorDeclaration;
                    break;
                case VBasic.SyntaxKind.RaiseEventAccessorBlock:
                    var eventStatement = ((VBSyntax.EventBlockSyntax)node.Parent).EventStatement;
                    var eventName = CommonConversions.ConvertIdentifier(eventStatement.Identifier).ValueText;
                    potentialMethodId = $"On{eventName}";
                    var parameterListSyntax = (ParameterListSyntax)node.AccessorStatement.ParameterList.Accept(TriviaConvertingVisitor);
                    return CreateMethodDeclarationSyntax(parameterListSyntax);
                default:
                    throw new NotSupportedException(node.Kind().ToString());
            }

            return SyntaxFactory.AccessorDeclaration(blockKind, attributes, modifiers, body);

            bool TryConvertAsParameterizedProperty(out MethodDeclarationSyntax methodDeclarationSyntax)
            {
                if (containingProperty.ParameterList?.Parameters.Any() == true && !CommonConversions.IsDefaultIndexer(containingProperty))
                {
                    var parameterListSyntax =
                        (ParameterListSyntax) containingProperty?.ParameterList.Accept(TriviaConvertingVisitor);
                    methodDeclarationSyntax = CreateMethodDeclarationSyntax(parameterListSyntax);
                    return true;
                }

                methodDeclarationSyntax = null;
                return false;
            }

            MethodDeclarationSyntax CreateMethodDeclarationSyntax(ParameterListSyntax parameterListSyntax)
            {
                return SyntaxFactory.MethodDeclaration(attributes, modifiers,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), null,
                    SyntaxFactory.Identifier(potentialMethodId), null,
                    parameterListSyntax, SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), body, null);
            }
        }

        public override CSharpSyntaxNode VisitAccessorStatement(VBSyntax.AccessorStatementSyntax node)
        {
            return SyntaxFactory.AccessorDeclaration(node.Kind().ConvertToken(), null);
        }

        public override CSharpSyntaxNode VisitMethodBlock(VBSyntax.MethodBlockSyntax node)
        {
            var methodBlock = (BaseMethodDeclarationSyntax)node.SubOrFunctionStatement.Accept(TriviaConvertingVisitor);

            if (_semanticModel.GetDeclaredSymbol(node).IsPartialMethodDefinition()) {
                return methodBlock;
            }

            var csReturnVariableOrNull = _expressionNodeVisitor.GetRetVariableNameOrNull(node);
            var visualBasicSyntaxVisitor = _expressionNodeVisitor.CreateMethodBodyVisitor(node, ExpressionNodeVisitor.IsIterator(node), csReturnVariableOrNull);
            var convertedStatements = ConvertStatements(node.Statements, visualBasicSyntaxVisitor);
            var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);

            return methodBlock.WithBody(body);
        }

        private BlockSyntax WithImplicitReturnStatements(VBSyntax.MethodBlockBaseSyntax node, BlockSyntax convertedStatements,
            IdentifierNameSyntax csReturnVariableOrNull)
        {
            if (!ExpressionNodeVisitor.AllowsImplicitReturn(node)) return convertedStatements;

            var preBodyStatements = new List<StatementSyntax>();
            var postBodyStatements = new List<StatementSyntax>();

            var functionSym = _semanticModel.GetDeclaredSymbol(node);
            var returnType = _semanticModel.GetCsTypeSyntax(functionSym.GetReturnType(), node);

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

        public override CSharpSyntaxNode VisitMethodStatement(VBSyntax.MethodStatementSyntax node)
        {
            var attributes = ConvertAttributes(node.AttributeLists);
            bool hasBody = node.Parent is VBSyntax.MethodBlockBaseSyntax;

            if ("Finalize".Equals(node.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)
                && node.Modifiers.Any(m => VBasic.VisualBasicExtensions.Kind(m) == VBasic.SyntaxKind.OverridesKeyword)) {
                var decl = SyntaxFactory.DestructorDeclaration(CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier)
                ).WithAttributeLists(attributes);
                if (hasBody) return decl;
                return decl.WithSemicolonToken(SemicolonToken);
            } else {
                var tokenContext = GetMemberContext(node);
                var convertedModifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, tokenContext);
                var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
                bool accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, declaredSymbol);

                var isPartialDefinition = declaredSymbol.IsPartialMethodDefinition();

                if (declaredSymbol.IsPartialMethodImplementation() || isPartialDefinition) {
                    var privateModifier = convertedModifiers.SingleOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword));
                    if (privateModifier != default(SyntaxToken)) {
                        convertedModifiers = convertedModifiers.Remove(privateModifier);
                    }
                    if (!HasPartialKeyword(node.Modifiers)) {
                        convertedModifiers = convertedModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    }
                }
                SplitTypeParameters(node.TypeParameterList, out var typeParameters, out var constraints);

                var csIdentifier = CommonConversions.ConvertIdentifier(node.Identifier);
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
                        null, CommonConversions.ConvertIdentifier(node.Identifier),
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

        private bool HasOutAttribute(VBSyntax.AttributeListSyntax a)
        {
            return a.Attributes.Any(IsOutAttribute);
        }

        private bool IsExtensionAttribute(VBSyntax.AttributeSyntax a)
        {
            return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()
                       ?.Equals(ExtensionAttributeType.FullName) == true;
        }

        private bool IsOutAttribute(VBSyntax.AttributeSyntax a)
        {
            return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()
                       ?.Equals(_outAttributeType.FullName) == true;
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
            var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node));

            var rawType = (TypeSyntax)block.AsClause?.Type.Accept(TriviaConvertingVisitor) ?? VarType;

            var convertedAccessors = node.Accessors.Select(a => a.Accept(TriviaConvertingVisitor)).ToList();
            _additionalDeclarations.Add(node, convertedAccessors.OfType<MemberDeclarationSyntax>().ToArray());
            return SyntaxFactory.EventDeclaration(
                SyntaxFactory.List(attributes),
                modifiers,
                rawType,
                null, CommonConversions.ConvertIdentifier(block.Identifier),
                SyntaxFactory.AccessorList(SyntaxFactory.List(convertedAccessors.OfType<AccessorDeclarationSyntax>()))
            );
        }

        public override CSharpSyntaxNode VisitEventStatement(VBSyntax.EventStatementSyntax node)
        {
            var attributes = node.AttributeLists.SelectMany(ConvertAttribute);
            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, GetMemberContext(node));
            var id = CommonConversions.ConvertIdentifier(node.Identifier);

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
            var methodBodyVisitor = _expressionNodeVisitor.CreateMethodBodyVisitor(node);
            var body = SyntaxFactory.Block(containingBlock.Statements.SelectMany(s => s.Accept(methodBodyVisitor)));
            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, GetMemberContext(node));

            var conversionModifiers = modifiers.Where(CommonConversions.IsConversionOperator).ToList();
            var nonConversionModifiers = SyntaxFactory.TokenList(modifiers.Except(conversionModifiers));

            if (conversionModifiers.Any()) {
                return SyntaxFactory.ConversionOperatorDeclaration(attributes, nonConversionModifiers,
                    conversionModifiers.Single(), returnType, parameterList, body, null);
            }

            return SyntaxFactory.OperatorDeclaration(attributes, nonConversionModifiers, returnType, node.OperatorToken.ConvertToken(), parameterList, body, null);
        }

        public override CSharpSyntaxNode VisitConstructorBlock(VBSyntax.ConstructorBlockSyntax node)
        {
            var block = node.BlockStatement;
            var attributes = block.AttributeLists.SelectMany(ConvertAttribute);
            var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node), isConstructor: true);

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

            var methodBodyVisitor = _expressionNodeVisitor.CreateMethodBodyVisitor(node);
            return SyntaxFactory.ConstructorDeclaration(
                SyntaxFactory.List(attributes),
                modifiers, CommonConversions.ConvertIdentifier(node.GetAncestor<VBSyntax.TypeBlockSyntax>().BlockStatement.Identifier),
                (ParameterListSyntax)block.ParameterList.Accept(TriviaConvertingVisitor),
                ctorCall,
                SyntaxFactory.Block(statements.SelectMany(s => s.Accept(methodBodyVisitor)))
            );
        }

        public override CSharpSyntaxNode VisitDeclareStatement(VBSyntax.DeclareStatementSyntax node)
        {
            var importAttributes = new List<AttributeArgumentSyntax>();
            _extraUsingDirectives.Add(DllImportType.Namespace);
            _extraUsingDirectives.Add(CharSetType.Namespace);
            var dllImportAttributeName = SyntaxFactory.ParseName(DllImportType.Name.Replace("Attribute", ""));
            var dllImportLibLiteral = node.LibraryName.Accept(TriviaConvertingVisitor);
            importAttributes.Add(SyntaxFactory.AttributeArgument((ExpressionSyntax)dllImportLibLiteral));

            if (node.AliasName != null) {
                importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("EntryPoint"), null, (ExpressionSyntax) node.AliasName.Accept(TriviaConvertingVisitor)));
            }

            if (!node.CharsetKeyword.IsKind(SyntaxKind.None)) {
                importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(CharSetType.Name), null, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(CharSetType.Name), SyntaxFactory.IdentifierName(node.CharsetKeyword.Text))));
            }

            var attributeArguments = CommonConversions.CreateAttributeArgumentList(importAttributes.ToArray());
            var dllImportAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

            var attributeLists = ConvertAttributes(node.AttributeLists).Add(dllImportAttributeList);

            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ExternKeyword));
            var returnType = (TypeSyntax)node.AsClause?.Type.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.ParseTypeName("void");
            var parameterListSyntax = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor) ??
                                      SyntaxFactory.ParameterList();

            return SyntaxFactory.MethodDeclaration(attributeLists, modifiers, returnType, null, CommonConversions.ConvertIdentifier(node.Identifier), null,
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
            if (node.Parent is VBSyntax.PropertyStatementSyntax && CommonConversions.IsDefaultIndexer(node.Parent)) {
                return SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameterSyntaxs));
            }
            return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameterSyntaxs));
        }

        public override CSharpSyntaxNode VisitParameter(VBSyntax.ParameterSyntax node)
        {
            var id = CommonConversions.ConvertIdentifier(node.Identifier.Identifier);
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
            var paramSymbol = _semanticModel.GetDeclaredSymbol(node);
            var csParamSymbol = _expressionNodeVisitor.GetCsSymbolOrNull(paramSymbol) as IParameterSymbol;
                
            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Local);
            if (csParamSymbol?.RefKind == RefKind.Out || node.AttributeLists.Any(HasOutAttribute)) {
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
            return _expressionNodeVisitor.VisitAwaitExpression(node);
        }

        public override CSharpSyntaxNode VisitCatchBlock(VBSyntax.CatchBlockSyntax node)
        {
            return _expressionNodeVisitor.VisitCatchBlock(node);
        }

        public override CSharpSyntaxNode VisitCatchFilterClause(VBSyntax.CatchFilterClauseSyntax node)
        {
            return _expressionNodeVisitor.VisitCatchFilterClause(node);
        }

        public override CSharpSyntaxNode VisitFinallyBlock(VBSyntax.FinallyBlockSyntax node)
        {
            return _expressionNodeVisitor.VisitFinallyBlock(node);
        }

        public override CSharpSyntaxNode VisitCTypeExpression(VBSyntax.CTypeExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitCTypeExpression(node);
        }

        public override CSharpSyntaxNode VisitDirectCastExpression(VBSyntax.DirectCastExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitDirectCastExpression(node);
        }

        public override CSharpSyntaxNode VisitPredefinedCastExpression(VBSyntax.PredefinedCastExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitPredefinedCastExpression(node);
        }

        public override CSharpSyntaxNode VisitTryCastExpression(VBSyntax.TryCastExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitTryCastExpression(node);
        }

        public override CSharpSyntaxNode VisitLiteralExpression(VBSyntax.LiteralExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitLiteralExpression(node);
        }

        public override CSharpSyntaxNode VisitInterpolation(VBSyntax.InterpolationSyntax node)
        {
            return _expressionNodeVisitor.VisitInterpolation(node);
        }

        public override CSharpSyntaxNode VisitInterpolatedStringExpression(VBSyntax.InterpolatedStringExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitInterpolatedStringExpression(node);
        }

        public override CSharpSyntaxNode VisitInterpolatedStringText(VBSyntax.InterpolatedStringTextSyntax node)
        {
            return _expressionNodeVisitor.VisitInterpolatedStringText(node);
        }

        public override CSharpSyntaxNode VisitInterpolationAlignmentClause(VBSyntax.InterpolationAlignmentClauseSyntax node)
        {
            return _expressionNodeVisitor.VisitInterpolationAlignmentClause(node);
        }

        public override CSharpSyntaxNode VisitInterpolationFormatClause(VBSyntax.InterpolationFormatClauseSyntax node)
        {
            return _expressionNodeVisitor.VisitInterpolationFormatClause(node);
        }

        public override CSharpSyntaxNode VisitMeExpression(VBSyntax.MeExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitMeExpression(node);
        }

        public override CSharpSyntaxNode VisitMyBaseExpression(VBSyntax.MyBaseExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitMyBaseExpression(node);
        }

        public override CSharpSyntaxNode VisitParenthesizedExpression(VBSyntax.ParenthesizedExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitParenthesizedExpression(node);
        }

        public override CSharpSyntaxNode VisitMemberAccessExpression(VBSyntax.MemberAccessExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitMemberAccessExpression(node);
        }

        public override CSharpSyntaxNode VisitConditionalAccessExpression(VBSyntax.ConditionalAccessExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitConditionalAccessExpression(node);
        }

        public override CSharpSyntaxNode VisitArgumentList(VBSyntax.ArgumentListSyntax node)
        {
            return _expressionNodeVisitor.VisitArgumentList(node);
        }

        public override CSharpSyntaxNode VisitSimpleArgument(VBSyntax.SimpleArgumentSyntax node)
        {
            return _expressionNodeVisitor.VisitSimpleArgument(node);
        }

        public override CSharpSyntaxNode VisitNameOfExpression(VBSyntax.NameOfExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitNameOfExpression(node);
        }

        public override CSharpSyntaxNode VisitEqualsValue(VBSyntax.EqualsValueSyntax node)
        {
            return _expressionNodeVisitor.VisitEqualsValue(node);
        }

        public override CSharpSyntaxNode VisitObjectMemberInitializer(VBSyntax.ObjectMemberInitializerSyntax node)
        {
            return _expressionNodeVisitor.VisitObjectMemberInitializer(node);
        }

        public override CSharpSyntaxNode VisitAnonymousObjectCreationExpression(VBSyntax.AnonymousObjectCreationExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitAnonymousObjectCreationExpression(node);
        }

        public override CSharpSyntaxNode VisitInferredFieldInitializer(VBSyntax.InferredFieldInitializerSyntax node)
        {
            return _expressionNodeVisitor.VisitInferredFieldInitializer(node);
        }

        public override CSharpSyntaxNode VisitObjectCreationExpression(VBSyntax.ObjectCreationExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitObjectCreationExpression(node);
        }

        public override CSharpSyntaxNode VisitArrayCreationExpression(VBSyntax.ArrayCreationExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitArrayCreationExpression(node);
        }

        public override CSharpSyntaxNode VisitCollectionInitializer(VBSyntax.CollectionInitializerSyntax node)
        {
            return _expressionNodeVisitor.VisitCollectionInitializer(node);
        }

        public override CSharpSyntaxNode VisitQueryExpression(VBSyntax.QueryExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitQueryExpression(node);
        }

        public override CSharpSyntaxNode VisitOrdering(VBSyntax.OrderingSyntax node)
        {
            return _expressionNodeVisitor.VisitOrdering(node);
        }

        public override CSharpSyntaxNode VisitNamedFieldInitializer(VBSyntax.NamedFieldInitializerSyntax node)
        {
            return _expressionNodeVisitor.VisitNamedFieldInitializer(node);
        }

        public override CSharpSyntaxNode VisitObjectCollectionInitializer(VBSyntax.ObjectCollectionInitializerSyntax node)
        {
            return _expressionNodeVisitor.VisitObjectCollectionInitializer(node);
        }

        public override CSharpSyntaxNode VisitBinaryConditionalExpression(VBSyntax.BinaryConditionalExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitBinaryConditionalExpression(node);
        }

        public override CSharpSyntaxNode VisitTernaryConditionalExpression(VBSyntax.TernaryConditionalExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitTernaryConditionalExpression(node);
        }

        public override CSharpSyntaxNode VisitTypeOfExpression(VBSyntax.TypeOfExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitTypeOfExpression(node);
        }

        public override CSharpSyntaxNode VisitUnaryExpression(VBSyntax.UnaryExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitUnaryExpression(node);
        }

        public override CSharpSyntaxNode VisitBinaryExpression(VBSyntax.BinaryExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitBinaryExpression(node);
        }

        public override CSharpSyntaxNode VisitInvocationExpression(VBSyntax.InvocationExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitInvocationExpression(node);
        }

        public override CSharpSyntaxNode VisitSingleLineLambdaExpression(VBSyntax.SingleLineLambdaExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitSingleLineLambdaExpression(node);
        }

        public override CSharpSyntaxNode VisitMultiLineLambdaExpression(VBSyntax.MultiLineLambdaExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitMultiLineLambdaExpression(node);
        }

        #endregion

        #region Type Name / Modifier

        public override CSharpSyntaxNode VisitTupleType(VBSyntax.TupleTypeSyntax node)
        {
            return _expressionNodeVisitor.VisitTupleType(node);
        }

        public override CSharpSyntaxNode VisitTypedTupleElement(VBSyntax.TypedTupleElementSyntax node)
        {
            return _expressionNodeVisitor.VisitTypedTupleElement(node);
        }

        public override CSharpSyntaxNode VisitNamedTupleElement(VBSyntax.NamedTupleElementSyntax node)
        {
            return _expressionNodeVisitor.VisitNamedTupleElement(node);
        }

        public override CSharpSyntaxNode VisitTupleExpression(VBSyntax.TupleExpressionSyntax node)
        {
            return _expressionNodeVisitor.VisitTupleExpression(node);
        }

        public override CSharpSyntaxNode VisitPredefinedType(VBSyntax.PredefinedTypeSyntax node)
        {
            return _expressionNodeVisitor.VisitPredefinedType(node);
        }

        public override CSharpSyntaxNode VisitNullableType(VBSyntax.NullableTypeSyntax node)
        {
            return _expressionNodeVisitor.VisitNullableType(node);
        }

        public override CSharpSyntaxNode VisitArrayType(VBSyntax.ArrayTypeSyntax node)
        {
            return _expressionNodeVisitor.VisitArrayType(node);
        }

        public override CSharpSyntaxNode VisitArrayRankSpecifier(VBSyntax.ArrayRankSpecifierSyntax node)
        {
            return _expressionNodeVisitor.VisitArrayRankSpecifier(node);
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
            return SyntaxFactory.TypeParameter(SyntaxFactory.List<AttributeListSyntax>(), variance, CommonConversions.ConvertIdentifier(node.Identifier));
        }

        public override CSharpSyntaxNode VisitTypeParameterSingleConstraintClause(VBSyntax.TypeParameterSingleConstraintClauseSyntax node)
        {
            var id = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
            return SyntaxFactory.TypeParameterConstraintClause(id, SyntaxFactory.SingletonSeparatedList((TypeParameterConstraintSyntax)node.Constraint.Accept(TriviaConvertingVisitor)));
        }

        public override CSharpSyntaxNode VisitTypeParameterMultipleConstraintClause(VBSyntax.TypeParameterMultipleConstraintClauseSyntax node)
        {
            var id = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(((VBSyntax.TypeParameterSyntax)node.Parent).Identifier));
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
            return _expressionNodeVisitor.VisitIdentifierName(node);
        }

        public override CSharpSyntaxNode VisitQualifiedName(VBSyntax.QualifiedNameSyntax node)
        {
            return _expressionNodeVisitor.VisitQualifiedName(node);
        }

        public override CSharpSyntaxNode VisitGenericName(VBSyntax.GenericNameSyntax node)
        {
            return _expressionNodeVisitor.VisitGenericName(node);
        }

        public override CSharpSyntaxNode VisitTypeArgumentList(VBSyntax.TypeArgumentListSyntax node)
        {
            return _expressionNodeVisitor.VisitTypeArgumentList(node);
        }

        #endregion
    }
}
