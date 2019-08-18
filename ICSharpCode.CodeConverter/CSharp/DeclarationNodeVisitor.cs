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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualBasic.CompilerServices;
using StringComparer = System.StringComparer;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using SyntaxToken = Microsoft.CodeAnalysis.SyntaxToken;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Declaration nodes, and nodes only used directly in that declaration (i.e. never within an expression)
    /// e.g. Class, Enum, TypeConstraint
    /// </summary>
    /// <remarks>The split between this and the <see cref="ExpressionNodeVisitor"/> is purely organizational and serves no real runtime purpose.</remarks>
    internal class DeclarationNodeVisitor : VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private static readonly Type DllImportType = typeof(DllImportAttribute);
        private static readonly Type CharSetType = typeof(CharSet);
        private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
        private static readonly TypeSyntax VarType = SyntaxFactory.ParseTypeName("var");
        private readonly CSharpCompilation _csCompilation;
        private readonly SyntaxGenerator _csSyntaxGenerator;
        private readonly Compilation _compilation;
        private readonly SemanticModel _semanticModel;
        private readonly MethodsWithHandles _methodsWithHandles = new MethodsWithHandles();
        private readonly Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]> _additionalDeclarations = new Dictionary<VBSyntax.StatementSyntax, MemberDeclarationSyntax[]>();
        private readonly AdditionalInitializers _additionalInitializers;
        private readonly AdditionalLocals _additionalLocals = new AdditionalLocals();
        private uint _failedMemberConversionMarkerCount;
        private readonly HashSet<string> _extraUsingDirectives = new HashSet<string>();
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
        private static HashSet<string> _accessedThroughMyClass;
        public CommentConvertingNodesVisitor TriviaConvertingVisitor { get; }
        private readonly CommentConvertingVisitorWrapper<CSharpSyntaxNode> _triviaConvertingExpressionVisitor;
        private readonly ExpressionNodeVisitor _expressionNodeVisitor;

        private CommonConversions CommonConversions { get; }

        public DeclarationNodeVisitor(Compilation compilation, SemanticModel semanticModel,
            CSharpCompilation csCompilation, SyntaxGenerator csSyntaxGenerator)
        {
            _compilation = compilation;
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _csSyntaxGenerator = csSyntaxGenerator;
            _visualBasicEqualityComparison = new VisualBasicEqualityComparison(_semanticModel, _extraUsingDirectives);
            TriviaConverter triviaConverter = new TriviaConverter();
            TriviaConvertingVisitor = new CommentConvertingNodesVisitor(this, triviaConverter);
            var typeConversionAnalyzer = new TypeConversionAnalyzer(semanticModel, csCompilation, _extraUsingDirectives, _csSyntaxGenerator);
            CommonConversions = new CommonConversions(semanticModel, typeConversionAnalyzer, csSyntaxGenerator);
            _additionalInitializers = new AdditionalInitializers();
            _expressionNodeVisitor = new ExpressionNodeVisitor(semanticModel, _visualBasicEqualityComparison, _additionalLocals, csCompilation, _methodsWithHandles, CommonConversions, triviaConverter, _extraUsingDirectives);
            _triviaConvertingExpressionVisitor = _expressionNodeVisitor.TriviaConvertingVisitor;
            CommonConversions.TriviaConvertingExpressionVisitor = _triviaConvertingExpressionVisitor;
        }

        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }
        
        public override CSharpSyntaxNode VisitCompilationUnit(VBSyntax.CompilationUnitSyntax node)
        {
            var options = (VBasic.VisualBasicCompilationOptions)_semanticModel.Compilation.Options;
            var importsClauses = options.GlobalImports.Select(gi => gi.Clause).Concat(node.Imports.SelectMany(imp => imp.ImportsClauses)).ToList();

            var optionCompareText = node.Options.Any(x => x.NameKeyword.ValueText.Equals("Compare", StringComparison.OrdinalIgnoreCase) &&
                                                       x.ValueKeyword.ValueText.Equals("Text", StringComparison.OrdinalIgnoreCase));
            _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive = optionCompareText;

            var attributes = SyntaxFactory.List(node.Attributes.SelectMany(a => a.AttributeLists).SelectMany(_expressionNodeVisitor.ConvertAttribute));
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
            var usingDirective = SyntaxFactory.UsingDirective(nameEqualsSyntax, (NameSyntax)node.Name.Accept(_triviaConvertingExpressionVisitor));
            return usingDirective;
        }

        public override CSharpSyntaxNode VisitNamespaceBlock(VBSyntax.NamespaceBlockSyntax node)
        {
            var members = node.Members.Select(ConvertMember);

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                (NameSyntax)node.NamespaceStatement.Name.Accept(_triviaConvertingExpressionVisitor),
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
            _methodsWithHandles.Initialize(GetMethodWithHandles(parentType));
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
            var attributes = _expressionNodeVisitor.ConvertAttributes(classStatement.AttributeLists);
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
                baseTypes.Add(SyntaxFactory.SimpleBaseType((TypeSyntax)t.Accept(_triviaConvertingExpressionVisitor)));
            return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes));
        }

        public override CSharpSyntaxNode VisitModuleBlock(VBSyntax.ModuleBlockSyntax node)
        {
            var stmt = node.ModuleStatement;
            var attributes = _expressionNodeVisitor.ConvertAttributes(stmt.AttributeLists);
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
            var attributes = _expressionNodeVisitor.ConvertAttributes(stmt.AttributeLists);
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
            var attributes = _expressionNodeVisitor.ConvertAttributes(stmt.AttributeLists);
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
            var attributes = stmt.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute);
            BaseListSyntax baseList = null;
            if (asClause != null) {
                baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType((TypeSyntax)asClause.Type.Accept(_triviaConvertingExpressionVisitor))));
                if (asClause.AttributeLists.Count > 0) {
                    attributes = attributes.Concat(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword)),
                            SyntaxFactory.SeparatedList(asClause.AttributeLists.SelectMany(l => _expressionNodeVisitor.ConvertAttribute(l).SelectMany(a => a.Attributes)))
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
            var attributes = _expressionNodeVisitor.ConvertAttributes(node.AttributeLists);
            return SyntaxFactory.EnumMemberDeclaration(
                attributes, CommonConversions.ConvertIdentifier(node.Identifier),
                (EqualsValueClauseSyntax)node.Initializer?.Accept(_triviaConvertingExpressionVisitor)
            );
        }

        public override CSharpSyntaxNode VisitDelegateStatement(VBSyntax.DelegateStatementSyntax node)
        {
            var attributes = node.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute);

            TypeParameterListSyntax typeParameters;
            SyntaxList<TypeParameterConstraintClauseSyntax> constraints;
            SplitTypeParameters(node.TypeParameterList, out typeParameters, out constraints);

            TypeSyntax returnType;
            var asClause = node.AsClause;
            if (asClause == null) {
                returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            } else {
                returnType = (TypeSyntax)asClause.Type.Accept(_triviaConvertingExpressionVisitor);
                if (asClause.AttributeLists.Count > 0) {
                    attributes = attributes.Concat(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ReturnKeyword)),
                            SyntaxFactory.SeparatedList(asClause.AttributeLists.SelectMany(l => _expressionNodeVisitor.ConvertAttribute(l).SelectMany(a => a.Attributes)))
                        )
                    );
                }
            }

            return SyntaxFactory.DelegateDeclaration(
                SyntaxFactory.List(attributes), CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Global),
                returnType, CommonConversions.ConvertIdentifier(node.Identifier),
                typeParameters,
                (ParameterListSyntax)node.ParameterList?.Accept(_triviaConvertingExpressionVisitor),
                constraints
            );
        }

        #endregion

        #region Type Members

        public override CSharpSyntaxNode VisitFieldDeclaration(VBSyntax.FieldDeclarationSyntax node)
        {
            _additionalLocals.PushScope();
            var attributes = node.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute).ToList();
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

        private List<MethodWithHandles> GetMethodWithHandles(VBSyntax.TypeBlockSyntax parentType)
        {
            if (parentType == null) return new List<MethodWithHandles>();

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
            return methodWithHandleses;

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
            var attributes = node.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute).ToArray();
            var isReadonly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.ReadOnlyKeyword));
            var isWriteOnly = node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.WriteOnlyKeyword));
            var convertibleModifiers = node.Modifiers.Where(m => !m.IsKind(VBasic.SyntaxKind.ReadOnlyKeyword, VBasic.SyntaxKind.WriteOnlyKeyword, VBasic.SyntaxKind.DefaultKeyword));
            var modifiers = CommonConversions.ConvertModifiers(node, convertibleModifiers, GetMemberContext(node));
            var isIndexer = CommonConversions.IsDefaultIndexer(node);
            var accessedThroughMyClass = IsAccessedThroughMyClass(node, node.Identifier, _semanticModel.GetDeclaredSymbol(node));
            bool isInInterface = node.Ancestors().OfType<VBSyntax.InterfaceBlockSyntax>().FirstOrDefault() != null;

            var initializer = (EqualsValueClauseSyntax)node.Initializer?.Accept(_triviaConvertingExpressionVisitor);
            var rawType = (TypeSyntax)node.AsClause?.TypeSwitch(
                              (VBSyntax.SimpleAsClauseSyntax c) => c.Type,
                              (VBSyntax.AsNewClauseSyntax c) => {
                                  initializer = SyntaxFactory.EqualsValueClause((ExpressionSyntax)c.NewExpression.Accept(_triviaConvertingExpressionVisitor));
                                  return VBasic.SyntaxExtensions.Type(c.NewExpression.WithoutTrivia()); // We'll end up visiting this twice so avoid trivia this time
                              },
                              _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
                          )?.Accept(_triviaConvertingExpressionVisitor) ?? VarType;

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

                var parameters = SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(node.ParameterList.Parameters.Select(p => (ParameterSyntax)p.Accept(_triviaConvertingExpressionVisitor))));
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
            bool isIterator = node.IsIterator();
            var csReturnVariableOrNull = _expressionNodeVisitor.GetRetVariableNameOrNull(node);
            var convertedStatements = ConvertStatements(node.Statements, _expressionNodeVisitor.CreateMethodBodyVisitor(node, isIterator));
            var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);
            var attributes = _expressionNodeVisitor.ConvertAttributes(node.AccessorStatement.AttributeLists);
            var modifiers = CommonConversions.ConvertModifiers(node, node.AccessorStatement.Modifiers, TokenContext.Local);
            string potentialMethodId = null;
            var containingProperty = node.GetAncestor<VBSyntax.PropertyBlockSyntax>()?.PropertyStatement;
            switch (node.Kind()) {
                case VBasic.SyntaxKind.GetAccessorBlock:
                    blockKind = SyntaxKind.GetAccessorDeclaration;
                    potentialMethodId = $"get_{(containingProperty.Identifier.Text)}";

                    if (containingProperty.AsClause is VBSyntax.SimpleAsClauseSyntax getAsClause && 
                        TryConvertAsParameterizedProperty(out var method)) {
                        return method.WithReturnType((TypeSyntax)getAsClause.Type.Accept(_triviaConvertingExpressionVisitor));
                    }
                    break;
                case VBasic.SyntaxKind.SetAccessorBlock:
                    blockKind = SyntaxKind.SetAccessorDeclaration;
                    potentialMethodId = $"set_{(containingProperty.Identifier.Text)}";
                        
                    if (containingProperty.AsClause is VBSyntax.SimpleAsClauseSyntax setAsClause && TryConvertAsParameterizedProperty(out var setMethod)) {
                        var valueParameterType = (TypeSyntax)setAsClause.Type.Accept(_triviaConvertingExpressionVisitor);
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
                    var parameterListSyntax = (ParameterListSyntax)node.AccessorStatement.ParameterList.Accept(_triviaConvertingExpressionVisitor);
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
                        (ParameterListSyntax) containingProperty?.ParameterList.Accept(_triviaConvertingExpressionVisitor);
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
            var visualBasicSyntaxVisitor = _expressionNodeVisitor.CreateMethodBodyVisitor(node, node.IsIterator(), csReturnVariableOrNull);
            var convertedStatements = ConvertStatements(node.Statements, visualBasicSyntaxVisitor);
            var body = WithImplicitReturnStatements(node, convertedStatements, csReturnVariableOrNull);

            return methodBlock.WithBody(body);
        }

        private BlockSyntax WithImplicitReturnStatements(VBSyntax.MethodBlockBaseSyntax node, BlockSyntax convertedStatements,
            IdentifierNameSyntax csReturnVariableOrNull)
        {
            if (!node.AllowsImplicitReturn()) return convertedStatements;

            var preBodyStatements = new List<StatementSyntax>();
            var postBodyStatements = new List<StatementSyntax>();

            var functionSym = _semanticModel.GetDeclaredSymbol(node);
            var returnType = CommonConversions.GetTypeSyntax(functionSym.GetReturnType());

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
            var attributes = _expressionNodeVisitor.ConvertAttributes(node.AttributeLists);
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
                        (TypeSyntax)node.AsClause?.Type?.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        null, CommonConversions.ConvertIdentifier(node.Identifier),
                        typeParameters,
                        (ParameterListSyntax)node.ParameterList?.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ParameterList(),
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
                    (TypeSyntax)node.AsClause?.Type?.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    null,
                    csIdentifier,
                    typeParameters,
                    (ParameterListSyntax)node.ParameterList?.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ParameterList(),
                    constraints,
                    null,
                    null
                );
                if (hasBody && !isPartialDefinition) return decl;
                return decl.WithSemicolonToken(SemicolonToken);
            }
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
            var attributes = block.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute);
            var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node));

            var rawType = (TypeSyntax)block.AsClause?.Type.Accept(_triviaConvertingExpressionVisitor) ?? VarType;

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
            var attributes = node.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute);
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
                    (ParameterListSyntax)node.ParameterList.Accept(_triviaConvertingExpressionVisitor),
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
                SyntaxFactory.VariableDeclaration((TypeSyntax)node.AsClause.Type.Accept(_triviaConvertingExpressionVisitor),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(id)))
            );
        }

        private ISymbol GetDeclaredCsSymbolOrNull(VBasic.VisualBasicSyntaxNode node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            return declaredSymbol != null ? _expressionNodeVisitor.GetCsSymbolOrNull(declaredSymbol) : null;
        }


        public override CSharpSyntaxNode VisitOperatorBlock(VBSyntax.OperatorBlockSyntax node)
        {
            return node.BlockStatement.Accept(TriviaConvertingVisitor);
        }

        public override CSharpSyntaxNode VisitOperatorStatement(VBSyntax.OperatorStatementSyntax node)
        {
            var containingBlock = (VBSyntax.OperatorBlockSyntax) node.Parent;
            var attributes = SyntaxFactory.List(node.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute));
            var returnType = (TypeSyntax)node.AsClause?.Type.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var parameterList = (ParameterListSyntax)node.ParameterList.Accept(_triviaConvertingExpressionVisitor);
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
            var attributes = block.AttributeLists.SelectMany(_expressionNodeVisitor.ConvertAttribute);
            var modifiers = CommonConversions.ConvertModifiers(block, block.Modifiers, GetMemberContext(node), isConstructor: true);

            var ctor = (node.Statements.FirstOrDefault() as VBSyntax.ExpressionStatementSyntax)?.Expression as VBSyntax.InvocationExpressionSyntax;
            var ctorExpression = ctor?.Expression as VBSyntax.MemberAccessExpressionSyntax;
            var ctorArgs = (ArgumentListSyntax)ctor?.ArgumentList?.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ArgumentList();

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
                (ParameterListSyntax)block.ParameterList.Accept(_triviaConvertingExpressionVisitor),
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
            var dllImportLibLiteral = node.LibraryName.Accept(_triviaConvertingExpressionVisitor);
            importAttributes.Add(SyntaxFactory.AttributeArgument((ExpressionSyntax)dllImportLibLiteral));

            if (node.AliasName != null) {
                importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("EntryPoint"), null, (ExpressionSyntax) node.AliasName.Accept(_triviaConvertingExpressionVisitor)));
            }

            if (!node.CharsetKeyword.IsKind(SyntaxKind.None)) {
                importAttributes.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(CharSetType.Name), null, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(CharSetType.Name), SyntaxFactory.IdentifierName(node.CharsetKeyword.Text))));
            }

            var attributeArguments = CommonConversions.CreateAttributeArgumentList(importAttributes.ToArray());
            var dllImportAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(dllImportAttributeName, attributeArguments)));

            var attributeLists = _expressionNodeVisitor.ConvertAttributes(node.AttributeLists).Add(dllImportAttributeList);

            var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ExternKeyword));
            var returnType = (TypeSyntax)node.AsClause?.Type.Accept(_triviaConvertingExpressionVisitor) ?? SyntaxFactory.ParseTypeName("void");
            var parameterListSyntax = (ParameterListSyntax)node.ParameterList?.Accept(_triviaConvertingExpressionVisitor) ??
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

        #endregion


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
            return SyntaxFactory.TypeConstraint((TypeSyntax)node.Type.Accept(_triviaConvertingExpressionVisitor));
        }

    }
}
