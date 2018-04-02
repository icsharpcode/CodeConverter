using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax;
using ArgumentSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayRankSpecifierSyntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeListSyntax;
using AttributeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.AttributeSyntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
using InterpolatedStringContentSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InterpolatedStringContentSyntax;
using LambdaExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.LambdaExpressionSyntax;
using NameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax;
using OrderingSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.OrderingSyntax;
using ParameterListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterListSyntax;
using ParameterSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParameterSyntax;
using QueryClauseSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.QueryClauseSyntax;
using ReturnStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ReturnStatementSyntax;
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
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using YieldStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.YieldStatementSyntax;
using static ICSharpCode.CodeConverter.VB.SyntaxKindExtensions;

namespace ICSharpCode.CodeConverter.VB
{
    public partial class CSharpConverter
    {
        class NodesVisitor : CS.CSharpSyntaxVisitor<VisualBasicSyntaxNode>
        {
            readonly SemanticModel _semanticModel;
            readonly VisualBasicCompilationOptions _options;

            readonly List<ImportsStatementSyntax> _allImports = new List<ImportsStatementSyntax>();

            
            int _placeholder = 1;
            private readonly CSharpHelperMethodDefinition _cSharpHelperMethodDefinition;
            public CommentConvertingNodesVisitor TriviaConvertingVisitor { get; }

            string GeneratePlaceholder(string v)
            {
                return $"__{v}{_placeholder++}__";
            }

            IEnumerable<ImportsStatementSyntax> TidyImportsList(IEnumerable<ImportsStatementSyntax> allImports)
            {
                foreach (var import in allImports.GroupBy(c => c.ToString()).Select(g => g.First()))
                    foreach (var clause in import.ImportsClauses) {
                        if (ImportIsNecessary(clause))
                            yield return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(clause));
                    }
            }

            bool ImportIsNecessary(ImportsClauseSyntax import)
            {
                if (import is SimpleImportsClauseSyntax) {
                    var i = (SimpleImportsClauseSyntax)import;
                    if (i.Alias != null)
                        return true;
                    return _options?.GlobalImports.Any(g => i.ToString().Equals(g.Clause.ToString(), StringComparison.OrdinalIgnoreCase)) != true;
                }
                return true;
            }

            public NodesVisitor(SemanticModel semanticModel, VisualBasicCompilationOptions compilationOptions = null)
            {
                this._semanticModel = semanticModel;
                TriviaConvertingVisitor = new CommentConvertingNodesVisitor(this);
                _options = compilationOptions;
                _cSharpHelperMethodDefinition = new CSharpHelperMethodDefinition();
            }

            public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
            {
                throw new NotImplementedException($"Conversion for {CS.CSharpExtensions.Kind(node)} not implemented, please report this issue")
                    .WithNodeInformation(node);
            }

            private Func<SyntaxNode, SyntaxNode> DelegateConversion(Func<SyntaxNode, SyntaxList<StatementSyntax>> convert)
            {
                return node => MethodBodyVisitor.CreateBlock(convert(node));
            }

            public override VisualBasicSyntaxNode VisitCompilationUnit(CSS.CompilationUnitSyntax node)
            {
                foreach (var @using in node.Usings)
                    @using.Accept(TriviaConvertingVisitor);
                foreach (var @extern in node.Externs)
                    @extern.Accept(TriviaConvertingVisitor);
                var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => SyntaxFactory.AttributesStatement(SyntaxFactory.SingletonList((AttributeListSyntax)a.Accept(TriviaConvertingVisitor)))));
                var members = SyntaxFactory.List(node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor)));

                return SyntaxFactory.CompilationUnit(
                    SyntaxFactory.List<OptionStatementSyntax>(),
                    SyntaxFactory.List(TidyImportsList(_allImports)),
                    attributes,
                    members
                );
            }

            #region Attributes
            public override VisualBasicSyntaxNode VisitAttributeList(CSS.AttributeListSyntax node)
            {
                return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(node.Attributes.Select(a => (AttributeSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitAttribute(CSS.AttributeSyntax node)
            {
                var list = (CSS.AttributeListSyntax)node.Parent;
                return SyntaxFactory.Attribute((AttributeTargetSyntax)list.Target?.Accept(TriviaConvertingVisitor), (TypeSyntax)node.Name.Accept(TriviaConvertingVisitor), (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitAttributeTargetSpecifier(CSS.AttributeTargetSpecifierSyntax node)
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

            public override VisualBasicSyntaxNode VisitAttributeArgumentList(CSS.AttributeArgumentListSyntax node)
            {
                return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitAttributeArgument(CSS.AttributeArgumentSyntax node)
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

            public override VisualBasicSyntaxNode VisitNamespaceDeclaration(CSS.NamespaceDeclarationSyntax node)
            {
                foreach (var @using in node.Usings)
                    @using.Accept(TriviaConvertingVisitor);
                foreach (var @extern in node.Externs)
                    @extern.Accept(TriviaConvertingVisitor);
                var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor));

                return SyntaxFactory.NamespaceBlock(
                    SyntaxFactory.NamespaceStatement((NameSyntax)node.Name.Accept(TriviaConvertingVisitor)),
                    SyntaxFactory.List(members)
                );
            }

            public override VisualBasicSyntaxNode VisitUsingDirective(CSS.UsingDirectiveSyntax node)
            {
                ImportAliasClauseSyntax alias = null;
                if (node.Alias != null) {
                    var name = node.Alias.Name;
                    var id = CSharpConverter.ConvertIdentifier(name.Identifier);
                    alias = SyntaxFactory.ImportAliasClause(id);
                }
                ImportsClauseSyntax clause = SyntaxFactory.SimpleImportsClause(alias, (NameSyntax)node.Name.Accept(TriviaConvertingVisitor));
                var import = SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(clause));
                _allImports.Add(import);
                return null;
            }

            #region Namespace Members

            public override VisualBasicSyntaxNode VisitClassDeclaration(CSS.ClassDeclarationSyntax node)
            {
                var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor)).ToList();
                var id = CSharpConverter.ConvertIdentifier(node.Identifier);

                List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
                List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
                ConvertBaseList(node, inherits, implements);
                members.AddRange(_cSharpHelperMethodDefinition.GetExtraMembers());
                if (node.Modifiers.Any(CS.SyntaxKind.StaticKeyword)) {
                    return SyntaxFactory.ModuleBlock(
                        SyntaxFactory.ModuleStatement(
                            SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.InterfaceOrModule),
                            id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
                        ),
                        SyntaxFactory.List(inherits),
                        SyntaxFactory.List(implements),
                        SyntaxFactory.List(members)
                    );
                } else {
                    return SyntaxFactory.ClassBlock(
                        SyntaxFactory.ClassStatement(
                            SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers),
                            id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
                        ),
                        SyntaxFactory.List(inherits),
                        SyntaxFactory.List(implements),
                        SyntaxFactory.List(members)
                    );
                }
            }

            public override VisualBasicSyntaxNode VisitStructDeclaration(CSS.StructDeclarationSyntax node)
            {
                var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor)).ToList();

                List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
                List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
                ConvertBaseList(node, inherits, implements);
                members.AddRange(_cSharpHelperMethodDefinition.GetExtraMembers());

                return SyntaxFactory.StructureBlock(
                    SyntaxFactory.StructureStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers), CSharpConverter.ConvertIdentifier(node.Identifier),
                        (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
                    ),
                    SyntaxFactory.List(inherits),
                    SyntaxFactory.List(implements),
                    SyntaxFactory.List(members)
                );
            }

            public override VisualBasicSyntaxNode VisitInterfaceDeclaration(CSS.InterfaceDeclarationSyntax node)
            {
                var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor)).ToArray();

                List<InheritsStatementSyntax> inherits = new List<InheritsStatementSyntax>();
                List<ImplementsStatementSyntax> implements = new List<ImplementsStatementSyntax>();
                ConvertBaseList(node, inherits, implements);

                return SyntaxFactory.InterfaceBlock(
                    SyntaxFactory.InterfaceStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.InterfaceOrModule), CSharpConverter.ConvertIdentifier(node.Identifier),
                        (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor)
                    ),
                    SyntaxFactory.List(inherits),
                    SyntaxFactory.List(implements),
                    SyntaxFactory.List(members)
                );
            }

            public override VisualBasicSyntaxNode VisitEnumDeclaration(CSS.EnumDeclarationSyntax node)
            {
                var members = node.Members.Select(m => (StatementSyntax)m.Accept(TriviaConvertingVisitor));
                var baseType = (TypeSyntax)node.BaseList?.Types.Single().Accept(TriviaConvertingVisitor);
                return SyntaxFactory.EnumBlock(
                    SyntaxFactory.EnumStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers), CSharpConverter.ConvertIdentifier(node.Identifier),
                        baseType == null ? null : SyntaxFactory.SimpleAsClause(baseType)
                    ),
                    SyntaxFactory.List(members)
                );
            }

            public override VisualBasicSyntaxNode VisitEnumMemberDeclaration(CSS.EnumMemberDeclarationSyntax node)
            {
                var initializer = (ExpressionSyntax)node.EqualsValue?.Value.Accept(TriviaConvertingVisitor);
                return SyntaxFactory.EnumMemberDeclaration(
                    SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertIdentifier(node.Identifier),
                    initializer == null ? null : SyntaxFactory.EqualsValue(initializer)
                );
            }

            public override VisualBasicSyntaxNode VisitDelegateDeclaration(CSS.DelegateDeclarationSyntax node)
            {
                var id = CSharpConverter.ConvertIdentifier(node.Identifier);
                var methodInfo = ModelExtensions.GetDeclaredSymbol(_semanticModel, node) as INamedTypeSymbol;
                if (methodInfo.DelegateInvokeMethod.GetReturnType()?.SpecialType == SpecialType.System_Void) {
                    return SyntaxFactory.DelegateSubStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers),
                        id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                        (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                        null
                    );
                } else {
                    return SyntaxFactory.DelegateFunctionStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers),
                        id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                        (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.SimpleAsClause((TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor))
                    );
                }
            }

            #endregion

            #region Type Members

            public override VisualBasicSyntaxNode VisitFieldDeclaration(CSS.FieldDeclarationSyntax node)
            {
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.VariableOrConst);
                if (modifiers.Count == 0)
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))),
                    modifiers, CSharpConverter.RemodelVariableDeclaration(node.Declaration, this)
                );
            }

            public override VisualBasicSyntaxNode VisitConstructorDeclaration(CSS.ConstructorDeclarationSyntax node)
            {
                var initializer = new[] { (StatementSyntax) node.Initializer?.Accept(TriviaConvertingVisitor)}.Where(x => x != null);
                return SyntaxFactory.ConstructorBlock(
                    SyntaxFactory.SubNewStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))), CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member),
                        (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor)
                    ),
                    SyntaxFactory.List(initializer.Concat(ConvertBody(node.Body, node.ExpressionBody)))
                );
            }

            private SyntaxList<StatementSyntax> ConvertBody(CSS.BlockSyntax body,
                CSS.ArrowExpressionClauseSyntax expressionBody, MethodBodyVisitor iteratorState = null)
            {
                if (body != null) {
                    return SyntaxFactory.List(body.Statements.SelectMany(s =>
                        s.Accept(CreateMethodBodyVisitor(iteratorState))));
                }

                if (expressionBody != null) {
                    var convertedBody = expressionBody.Expression.Accept(TriviaConvertingVisitor);
                    if (convertedBody is ExpressionSyntax convertedBodyExpression) {
                        convertedBody = SyntaxFactory.ReturnStatement(convertedBodyExpression);
                    }

                    return SyntaxFactory.SingletonList((StatementSyntax) convertedBody);
                }

                return SyntaxFactory.List<StatementSyntax>();
            }

            public override VisualBasicSyntaxNode VisitConstructorInitializer(CSS.ConstructorInitializerSyntax node)
            {
                var initializerExpression = GetInitializerExpression(node);
                var newMethodCall = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    initializerExpression, SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName("New"));

                return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(newMethodCall, (ArgumentListSyntax) node.ArgumentList.Accept(TriviaConvertingVisitor)));
            }

            private static ExpressionSyntax GetInitializerExpression(CSS.ConstructorInitializerSyntax node)
            {
                if (node.IsKind(CS.SyntaxKind.BaseConstructorInitializer))
                {
                    return SyntaxFactory.MyBaseExpression();
                }

                if (node.IsKind(CS.SyntaxKind.ThisConstructorInitializer))
                {
                    return SyntaxFactory.MeExpression();
                }

                throw new ArgumentOutOfRangeException(nameof(node), node, $"{CS.CSharpExtensions.Kind(node)} unknown");
            }

            public override VisualBasicSyntaxNode VisitDestructorDeclaration(CSS.DestructorDeclarationSyntax node)
            {
                return SyntaxFactory.SubBlock(
                    SyntaxFactory.SubStatement(
                        SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverridesKeyword)),
                        SyntaxFactory.Identifier("Finalize"), null,
                        (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor),
                        null, null, null
                    ),
                    ConvertBody(node.Body, node.ExpressionBody)
                );
            }

            private CS.CSharpSyntaxVisitor<SyntaxList<StatementSyntax>> CreateMethodBodyVisitor(CSharpConverter.MethodBodyVisitor methodBodyVisitor = null)
            {
                var visitor = methodBodyVisitor ?? new CSharpConverter.MethodBodyVisitor(_semanticModel, TriviaConvertingVisitor, TriviaConvertingVisitor.TriviaConverter);
                return visitor.CommentConvertingVisitor;
            }

            public override VisualBasicSyntaxNode VisitMethodDeclaration(CSS.MethodDeclarationSyntax node)
            {
                var isIteratorState = new CSharpConverter.MethodBodyVisitor(_semanticModel, TriviaConvertingVisitor, TriviaConvertingVisitor.TriviaConverter);
                var block = ConvertBody(node.Body, node.ExpressionBody, isIteratorState);
                if (node.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, CS.SyntaxKind.ExternKeyword))) {
                    block = SyntaxFactory.List<StatementSyntax>();
                }
                var id = CSharpConverter.ConvertIdentifier(node.Identifier);
                var methodInfo = ModelExtensions.GetDeclaredSymbol(_semanticModel, node);
                var containingType = methodInfo?.ContainingType;
                var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor)));
                var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, containingType?.IsInterfaceType() == true ? TokenContext.Local : TokenContext.Member);
                if (isIteratorState.IsIterator)
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.IteratorKeyword));
                if (node.ParameterList.Parameters.Count > 0 && node.ParameterList.Parameters[0].Modifiers.Any(CS.SyntaxKind.ThisKeyword)) {
                    attributes = attributes.Insert(0, SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(null, SyntaxFactory.ParseTypeName("Extension"), SyntaxFactory.ArgumentList()))));
                    if (!((CS.CSharpSyntaxTree)node.SyntaxTree).HasUsingDirective("System.Runtime.CompilerServices"))
                        _allImports.Add(SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Runtime.CompilerServices")))));
                }
                if (containingType?.IsStatic == true) {
                    modifiers = SyntaxFactory.TokenList(modifiers.Where(t => !(t.IsKind(SyntaxKind.SharedKeyword, SyntaxKind.PublicKeyword))));
                }
                if (methodInfo?.GetReturnType()?.SpecialType == SpecialType.System_Void) {
                    var stmt = SyntaxFactory.SubStatement(
                        attributes,
                        modifiers,
                        id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                        parameterList,
                        null, null, null
                    );
                    if (node.Body == null && node.ExpressionBody == null)
                        return stmt;
                    return SyntaxFactory.SubBlock(stmt, block);
                } else {
                    var stmt = SyntaxFactory.FunctionStatement(
                        attributes,
                        modifiers,
                        id, (TypeParameterListSyntax)node.TypeParameterList?.Accept(TriviaConvertingVisitor),
                        parameterList,
                        SyntaxFactory.SimpleAsClause((TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor)), null, null
                    );
                    if (node.Body == null && node.ExpressionBody == null)
                        return stmt;
                    return SyntaxFactory.FunctionBlock(stmt, block);
                }
            }

            public override VisualBasicSyntaxNode VisitPropertyDeclaration(CSS.PropertyDeclarationSyntax node)
            {
                var id = CSharpConverter.ConvertIdentifier(node.Identifier);
                ConvertAndSplitAttributes(node.AttributeLists, out var attributes, out var returnAttributes);
                bool isIterator = false;
                List<AccessorBlockSyntax> accessors = new List<AccessorBlockSyntax>();
                var csAccessors = node.AccessorList.Accessors;
                foreach (var a in csAccessors) {
                    accessors.Add(ConvertAccessor(a, out var isAIterator));
                    isIterator |= isAIterator;
                }
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member);
                if (isIterator) modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.IteratorKeyword));
                if (csAccessors.Count == 1) {
                    var accessLimitation = csAccessors.Single().IsKind(CS.SyntaxKind.SetAccessorDeclaration)
                        ? SyntaxKind.WriteOnlyKeyword
                        : SyntaxKind.ReadOnlyKeyword;
                    modifiers = modifiers.Add(SyntaxFactory.Token(accessLimitation));
                }
                var stmt = SyntaxFactory.PropertyStatement(
                    attributes,
                    modifiers,
                    id, null,
                    SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)),
                    node.Initializer == null ? null : SyntaxFactory.EqualsValue((ExpressionSyntax)node.Initializer.Value.Accept(TriviaConvertingVisitor)), null
                );

                if (HasNoAccessorBody(node.AccessorList))
                    return stmt;
                return SyntaxFactory.PropertyBlock(stmt, SyntaxFactory.List(accessors));
            }

            public override VisualBasicSyntaxNode VisitIndexerDeclaration(CSS.IndexerDeclarationSyntax node)
            {
                var id = SyntaxFactory.Identifier("Item");
                ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);
                bool isIterator = false;
                List<AccessorBlockSyntax> accessors = new List<AccessorBlockSyntax>();
                foreach (var a in node.AccessorList?.Accessors) {
                    accessors.Add(ConvertAccessor(a, out var isAIterator));
                    isIterator |= isAIterator;
                }
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member).Insert(0, SyntaxFactory.Token(SyntaxKind.DefaultKeyword));
                if (isIterator) modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.IteratorKeyword));
                var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
                var stmt = SyntaxFactory.PropertyStatement(
                    attributes,
                    modifiers,
                    id, parameterList,
                    SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)), null, null
                );
                if (HasNoAccessorBody(node.AccessorList))
                    return stmt;
                return SyntaxFactory.PropertyBlock(stmt, SyntaxFactory.List(accessors));
            }

            private static bool HasNoAccessorBody(CSS.AccessorListSyntax accessorListSyntaxOrNull)
            {
                return accessorListSyntaxOrNull?.Accessors.All(a => a.Body == null && a.ExpressionBody == null) == true;
            }

            public override VisualBasicSyntaxNode VisitEventDeclaration(CSS.EventDeclarationSyntax node)
            {
                ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);
                var stmt = SyntaxFactory.EventStatement(
                    attributes, CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member), CSharpConverter.ConvertIdentifier(node.Identifier), null,
                    SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)), null
                );
                if (HasNoAccessorBody(node.AccessorList))
                    return stmt;
                var accessors = node.AccessorList?.Accessors.Select(a => ConvertAccessor(a, out bool unused)).ToArray();
                return SyntaxFactory.EventBlock(stmt, SyntaxFactory.List(accessors));
            }

            public override VisualBasicSyntaxNode VisitEventFieldDeclaration(CSS.EventFieldDeclarationSyntax node)
            {
                var decl = node.Declaration.Variables.Single();
                var id = SyntaxFactory.Identifier(decl.Identifier.ValueText, SyntaxFacts.IsKeywordKind(VisualBasicExtensions.Kind(decl.Identifier)), decl.Identifier.GetIdentifierText(), TypeCharacter.None);
                ConvertAndSplitAttributes(node.AttributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes);
                return SyntaxFactory.EventStatement(attributes, CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member), id, null, SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.Declaration.Type.Accept(TriviaConvertingVisitor)), null);
            }

            private void ConvertAndSplitAttributes(SyntaxList<CSS.AttributeListSyntax> attributeLists, out SyntaxList<AttributeListSyntax> attributes, out SyntaxList<AttributeListSyntax> returnAttributes)
            {
                var retAttr = new List<AttributeListSyntax>();
                var attr = new List<AttributeListSyntax>();

                foreach (var attrList in attributeLists) {
                    var targetIdentifier = attrList.Target?.Identifier;
                    if (targetIdentifier != null && SyntaxTokenExtensions.IsKind((SyntaxToken) targetIdentifier, CS.SyntaxKind.ReturnKeyword))
                        retAttr.Add((AttributeListSyntax)attrList.Accept(TriviaConvertingVisitor));
                    else
                        attr.Add((AttributeListSyntax)attrList.Accept(TriviaConvertingVisitor));
                }
                returnAttributes = SyntaxFactory.List(retAttr);
                attributes = SyntaxFactory.List(attr);
            }

            AccessorBlockSyntax ConvertAccessor(CSS.AccessorDeclarationSyntax node, out bool isIterator)
            {
                SyntaxKind blockKind;
                AccessorStatementSyntax stmt;
                EndBlockStatementSyntax endStmt;
                SyntaxList<StatementSyntax> body = SyntaxFactory.List<StatementSyntax>();
                isIterator = false;
                var isIteratorState = new CSharpConverter.MethodBodyVisitor(_semanticModel, TriviaConvertingVisitor, TriviaConvertingVisitor.TriviaConverter);
                body = ConvertBody(node.Body, node.ExpressionBody, isIteratorState);
                isIterator = isIteratorState.IsIterator;
                var attributes = SyntaxFactory.List(node.AttributeLists.Select(a => (AttributeListSyntax)a.Accept(TriviaConvertingVisitor)));
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Local);
                var parent = (CSS.BasePropertyDeclarationSyntax)node.Parent.Parent;
                ParameterSyntax valueParam;

                switch (CS.CSharpExtensions.Kind(node)) {
                    case CS.SyntaxKind.GetAccessorDeclaration:
                        blockKind = SyntaxKind.GetAccessorBlock;
                        stmt = SyntaxFactory.GetAccessorStatement(attributes, modifiers, null);
                        endStmt = SyntaxFactory.EndGetStatement();
                        break;
                    case CS.SyntaxKind.SetAccessorDeclaration:
                        blockKind = SyntaxKind.SetAccessorBlock;
                        valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                            .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(TriviaConvertingVisitor)))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                        stmt = SyntaxFactory.SetAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                        endStmt = SyntaxFactory.EndSetStatement();
                        break;
                    case CS.SyntaxKind.AddAccessorDeclaration:
                        blockKind = SyntaxKind.AddHandlerAccessorBlock;
                        valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                            .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(TriviaConvertingVisitor)))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                        stmt = SyntaxFactory.AddHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                        endStmt = SyntaxFactory.EndAddHandlerStatement();
                        break;
                    case CS.SyntaxKind.RemoveAccessorDeclaration:
                        blockKind = SyntaxKind.RemoveHandlerAccessorBlock;
                        valueParam = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier("value"))
                            .WithAsClause(SyntaxFactory.SimpleAsClause((TypeSyntax)parent.Type.Accept(TriviaConvertingVisitor)))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword)));
                        stmt = SyntaxFactory.RemoveHandlerAccessorStatement(attributes, modifiers, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParam)));
                        endStmt = SyntaxFactory.EndRemoveHandlerStatement();
                        break;
                    default:
                        throw new NotSupportedException();
                }
                return SyntaxFactory.AccessorBlock(blockKind, stmt, body, endStmt);
            }

            public override VisualBasicSyntaxNode VisitOperatorDeclaration(CSS.OperatorDeclarationSyntax node)
            {
                ConvertAndSplitAttributes(node.AttributeLists, out var attributes, out var returnAttributes);
                var body = ConvertBody(node.Body, node.ExpressionBody);
                var parameterList = (ParameterListSyntax)node.ParameterList?.Accept(TriviaConvertingVisitor);
                var stmt = SyntaxFactory.OperatorStatement(
                    attributes, CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Member),
                    SyntaxFactory.Token(ConvertOperatorDeclarationToken(CS.CSharpExtensions.Kind(node.OperatorToken))),
                    parameterList,
                    SyntaxFactory.SimpleAsClause(returnAttributes, (TypeSyntax)node.ReturnType.Accept(TriviaConvertingVisitor))
                );
                return SyntaxFactory.OperatorBlock(stmt, body);
            }

            SyntaxKind ConvertOperatorDeclarationToken(CS.SyntaxKind syntaxKind)
            {
                switch (syntaxKind) {
                    case CS.SyntaxKind.EqualsEqualsToken:
                        return SyntaxKind.EqualsToken;
                    case CS.SyntaxKind.ExclamationEqualsToken:
                        return SyntaxKind.LessThanGreaterThanToken;
                }
                throw new NotSupportedException();
            }

            public override VisualBasicSyntaxNode VisitConversionOperatorDeclaration(CSS.ConversionOperatorDeclarationSyntax node)
            {
                return base.VisitConversionOperatorDeclaration(node);
            }

            public override VisualBasicSyntaxNode VisitParameterList(CSS.ParameterListSyntax node)
            {
                return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(node.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitBracketedParameterList(CSS.BracketedParameterListSyntax node)
            {
                return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(node.Parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitParameter(CSS.ParameterSyntax node)
            {
                var id = CSharpConverter.ConvertIdentifier(node.Identifier);
                var returnType = (TypeSyntax)node.Type?.Accept(TriviaConvertingVisitor);
                EqualsValueSyntax @default = null;
                if (node.Default != null) {
                    @default = SyntaxFactory.EqualsValue((ExpressionSyntax)node.Default?.Value.Accept(TriviaConvertingVisitor));
                }
                AttributeListSyntax[] newAttributes;
                var modifiers = CSharpConverter.ConvertModifiers(node.Modifiers, TokenContext.Local);
                if ((modifiers.Count == 0 && returnType != null) || node.Modifiers.Any(CS.SyntaxKind.ThisKeyword)) {
                    modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword));
                    newAttributes = new AttributeListSyntax[0];
                } else if (node.Modifiers.Any(CS.SyntaxKind.OutKeyword)) {
                    newAttributes = new[] {
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Attribute(SyntaxFactory.ParseTypeName("Out"))
                            )
                        )
                    };
                    _allImports.Add(SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Runtime.InteropServices")))));
                } else {
                    newAttributes = new AttributeListSyntax[0];
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

            public override VisualBasicSyntaxNode VisitLiteralExpression(CSS.LiteralExpressionSyntax node)
            {
                // now this looks somehow hacky... is there a better way?
                if (node.IsKind(CS.SyntaxKind.StringLiteralExpression) && node.Token.Text.StartsWith("@", StringComparison.Ordinal)) {
                    return SyntaxFactory.StringLiteralExpression(
                        SyntaxFactory.StringLiteralToken(
                            node.Token.Text.Substring(1),
                            (string)node.Token.Value
                        )
                    );
                } else {
                    return CSharpConverter.Literal(node.Token.Value, node.Token.Text);
                }
            }

            public override VisualBasicSyntaxNode VisitInterpolatedStringExpression(CSS.InterpolatedStringExpressionSyntax node)
            {
                return SyntaxFactory.InterpolatedStringExpression(node.Contents.Select(c => (InterpolatedStringContentSyntax)c.Accept(TriviaConvertingVisitor)).ToArray());
            }

            public override VisualBasicSyntaxNode VisitInterpolatedStringText(CSS.InterpolatedStringTextSyntax node)
            {
                return SyntaxFactory.InterpolatedStringText(SyntaxFactory.InterpolatedStringTextToken(node.TextToken.Text, node.TextToken.ValueText));
            }

            public override VisualBasicSyntaxNode VisitInterpolation(CSS.InterpolationSyntax node)
            {
                return SyntaxFactory.Interpolation((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitInterpolationFormatClause(CSS.InterpolationFormatClauseSyntax node)
            {
                return base.VisitInterpolationFormatClause(node);
            }

            public override VisualBasicSyntaxNode VisitParenthesizedExpression(CSS.ParenthesizedExpressionSyntax node)
            {
                return SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitPrefixUnaryExpression(CSS.PrefixUnaryExpressionSyntax node)
            {
                var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
                if (IsReturnValueDiscarded(node)) {
                    return SyntaxFactory.AssignmentStatement(
                        kind,
                        (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)), CSharpConverter.Literal(1)
                    );
                }
                if (kind == SyntaxKind.AddAssignmentStatement || kind == SyntaxKind.SubtractAssignmentStatement) {
                    string operatorName;
                    if (kind == SyntaxKind.AddAssignmentStatement)
                        operatorName = "Increment";
                    else
                        operatorName = "Decrement";
                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseName("System.Threading.Interlocked." + operatorName),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new ArgumentSyntax[] {
                                    SyntaxFactory.SimpleArgument((ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor))
                                }
                            )
                        )
                    );
                }
                return SyntaxFactory.UnaryExpression(kind, SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)), (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitPostfixUnaryExpression(CSS.PostfixUnaryExpressionSyntax node)
            {
                var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
                if (IsReturnValueDiscarded(node)) {
                    return SyntaxFactory.AssignmentStatement(CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local),
                        (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)), CSharpConverter.Literal(1)
                    );
                } else {
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
                        SyntaxFactory.ParseName("Math." + minMax),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new ArgumentSyntax[] {
                                    SyntaxFactory.SimpleArgument(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.ParseName("System.Threading.Interlocked." + operatorName),
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                    SyntaxFactory.SimpleArgument((ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor))
                                                )
                                            )
                                        )
                                    ),
                                    SyntaxFactory.SimpleArgument(SyntaxFactory.BinaryExpression(op, (ExpressionSyntax)node.Operand.Accept(TriviaConvertingVisitor), SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(op)), CSharpConverter.Literal(1)))
                                }
                            )
                        )
                    );
                }
            }

            public override VisualBasicSyntaxNode VisitAssignmentExpression(CSS.AssignmentExpressionSyntax node)
            {
                if (IsReturnValueDiscarded(node)) {
                    if (_semanticModel.GetTypeInfo(node.Right).ConvertedType.IsDelegateType()) {
                        if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.PlusEqualsToken)) {
                            return SyntaxFactory.AddHandlerStatement((ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor), (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor));
                        }
                        if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.MinusEqualsToken)) {
                            return SyntaxFactory.RemoveHandlerStatement((ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor), (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor));
                        }
                    }
                    return MakeAssignmentStatement(node);
                }
                if (node.Parent is CSS.ForStatementSyntax) {
                    return MakeAssignmentStatement(node);
                }
                if (node.Parent is CSS.InitializerExpressionSyntax) {
                    if (node.Left is CSS.ImplicitElementAccessSyntax) {
                        return SyntaxFactory.CollectionInitializer(
                            SyntaxFactory.SeparatedList(new[] {
                                (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                                (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                            })
                        );
                    } else {
                        return SyntaxFactory.NamedFieldInitializer(
                            (IdentifierNameSyntax)node.Left.Accept(TriviaConvertingVisitor),
                            (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                        );
                    }
                }

                _cSharpHelperMethodDefinition.AddInlineAssignMethod = true;
                return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(CSharpHelperMethodDefinition.QualifiedInlineAssignMethodName),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new ArgumentSyntax[] {
                                SyntaxFactory.SimpleArgument((ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor)),
                                SyntaxFactory.SimpleArgument((ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor))
                            }
                        )
                    )
                );
            }

            private static bool IsReturnValueDiscarded(CSS.ExpressionSyntax node)
            {
                return node.Parent is CSS.ExpressionStatementSyntax
                       || node.Parent is CSS.ForStatementSyntax
                       || node.Parent.IsParentKind(CS.SyntaxKind.SetAccessorDeclaration);
            }

            AssignmentStatementSyntax MakeAssignmentStatement(CSS.AssignmentExpressionSyntax node)
            {
                var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
                if (node.IsKind(CS.SyntaxKind.AndAssignmentExpression, CS.SyntaxKind.OrAssignmentExpression, CS.SyntaxKind.ExclusiveOrAssignmentExpression, CS.SyntaxKind.ModuloAssignmentExpression)) {
                    return SyntaxFactory.SimpleAssignmentStatement(
                        (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.BinaryExpression(
                            kind,
                            (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                            SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)),
                            (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                        )
                    );
                }
                return SyntaxFactory.AssignmentStatement(
                    kind,
                    (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)),
                    (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitInvocationExpression(CSS.InvocationExpressionSyntax node)
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

                return SyntaxFactory.InvocationExpression(
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (ArgumentListSyntax)node.ArgumentList.Accept(TriviaConvertingVisitor)
                );
            }

            private bool IsNameOfExpression(CSS.InvocationExpressionSyntax node)
            {
                return node.Expression is CSS.IdentifierNameSyntax methodIdentifier
                       && methodIdentifier?.Identifier.Text == "nameof"
                       // nameof expressions don't have an associated method symbol, a method called nameof usually would
                       && _semanticModel.GetSymbolInfo(methodIdentifier).ExtractBestMatch() == null;
            }

            public override VisualBasicSyntaxNode VisitConditionalExpression(CSS.ConditionalExpressionSyntax node)
            {
                return SyntaxFactory.TernaryConditionalExpression(
                    (ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.WhenTrue.Accept(TriviaConvertingVisitor),
                    (ExpressionSyntax)node.WhenFalse.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitConditionalAccessExpression(CSS.ConditionalAccessExpressionSyntax node)
            {
                return SyntaxFactory.ConditionalAccessExpression(
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.Token(SyntaxKind.QuestionToken),
                    (ExpressionSyntax)node.WhenNotNull.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitMemberAccessExpression(CSS.MemberAccessExpressionSyntax node)
            {
                return WrapTypedNameIfNecessary(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor)
                ), node);
            }

            public override VisualBasicSyntaxNode VisitImplicitElementAccess(CSS.ImplicitElementAccessSyntax node)
            {
                if (node.ArgumentList.Arguments.Count > 1)
                    throw new NotSupportedException("ImplicitElementAccess can only have one argument!");
                return node.ArgumentList.Arguments[0].Expression.Accept(TriviaConvertingVisitor);
            }

            public override VisualBasicSyntaxNode VisitElementAccessExpression(CSS.ElementAccessExpressionSyntax node)
            {
                return SyntaxFactory.InvocationExpression(
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor),
                    (ArgumentListSyntax)node.ArgumentList.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitMemberBindingExpression(CSS.MemberBindingExpressionSyntax node)
            {
                return SyntaxFactory.SimpleMemberAccessExpression((SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitDefaultExpression(CSS.DefaultExpressionSyntax node)
            {
                return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword));
            }

            public override VisualBasicSyntaxNode VisitThisExpression(CSS.ThisExpressionSyntax node)
            {
                return SyntaxFactory.MeExpression();
            }

            public override VisualBasicSyntaxNode VisitBaseExpression(CSS.BaseExpressionSyntax node)
            {
                return SyntaxFactory.MyBaseExpression();
            }

            public override VisualBasicSyntaxNode VisitBinaryExpression(CSS.BinaryExpressionSyntax node)
            {
                if (node.IsKind(CS.SyntaxKind.CoalesceExpression)) {
                    return SyntaxFactory.BinaryConditionalExpression(
                         (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                         (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                    );
                }
                if (node.IsKind(CS.SyntaxKind.AsExpression)) {
                    return SyntaxFactory.TryCastExpression((ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor), (TypeSyntax)node.Right.Accept(TriviaConvertingVisitor));
                }
                if (node.IsKind(CS.SyntaxKind.IsExpression)) {
                    return SyntaxFactory.TypeOfIsExpression((ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor), (TypeSyntax)node.Right.Accept(TriviaConvertingVisitor));
                }
                if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.EqualsEqualsToken)) {
                    ExpressionSyntax otherArgument = null;
                    if (node.Left.IsKind(CS.SyntaxKind.NullLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                    }
                    if (node.Right.IsKind(CS.SyntaxKind.NullLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                    }
                    if (otherArgument != null) {
                        return SyntaxFactory.IsExpression(otherArgument, CSharpConverter.Literal(null));
                    }
                }
                if (SyntaxTokenExtensions.IsKind(node.OperatorToken, CS.SyntaxKind.ExclamationEqualsToken)) {
                    ExpressionSyntax otherArgument = null;
                    if (node.Left.IsKind(CS.SyntaxKind.NullLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor);
                    }
                    if (node.Right.IsKind(CS.SyntaxKind.NullLiteralExpression)) {
                        otherArgument = (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor);
                    }
                    if (otherArgument != null) {
                        return SyntaxFactory.IsNotExpression(otherArgument, CSharpConverter.Literal(null));
                    }
                }
                var kind = CS.CSharpExtensions.Kind(node).ConvertToken(TokenContext.Local);
                if (node.IsKind(CS.SyntaxKind.AddExpression) && (ModelExtensions.GetTypeInfo(_semanticModel, node.Left).ConvertedType?.SpecialType == SpecialType.System_String
                    || ModelExtensions.GetTypeInfo(_semanticModel, node.Right).ConvertedType?.SpecialType == SpecialType.System_String)) {
                    kind = SyntaxKind.ConcatenateExpression;
                }
                return SyntaxFactory.BinaryExpression(
                    kind,
                    (ExpressionSyntax)node.Left.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.Token(VBUtil.GetExpressionOperatorTokenKind(kind)),
                    (ExpressionSyntax)node.Right.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitTypeOfExpression(CSS.TypeOfExpressionSyntax node)
            {
                return SyntaxFactory.GetTypeExpression((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitCastExpression(CSS.CastExpressionSyntax node)
            {
                var sourceType = _semanticModel.GetTypeInfo(node.Expression).Type;
                var destType = _semanticModel.GetTypeInfo(node.Type).Type;
                var expr = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
                switch (destType.SpecialType) {
                    case SpecialType.System_Object:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CObjKeyword), expr);
                    case SpecialType.System_Boolean:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CBoolKeyword), expr);
                    case SpecialType.System_Char:
                        return sourceType?.IsNumericType() == true
                            ? (VisualBasicSyntaxNode) SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("ChrW"), ExpressionSyntaxExtensions.CreateArgList(expr))
                            : SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CCharKeyword), expr);
                    case SpecialType.System_SByte:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CSByteKeyword), expr);
                    case SpecialType.System_Byte:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CByteKeyword), expr);
                    case SpecialType.System_Int16:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CShortKeyword), expr);
                    case SpecialType.System_UInt16:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CUShortKeyword), expr);
                    case SpecialType.System_Int32:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CIntKeyword), expr);
                    case SpecialType.System_UInt32:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CUIntKeyword), expr);
                    case SpecialType.System_Int64:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CLngKeyword), expr);
                    case SpecialType.System_UInt64:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CULngKeyword), expr);
                    case SpecialType.System_Decimal:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CDecKeyword), expr);
                    case SpecialType.System_Single:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CSngKeyword), expr);
                    case SpecialType.System_Double:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CDblKeyword), expr);
                    case SpecialType.System_String:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CStrKeyword), expr);
                    case SpecialType.System_DateTime:
                        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CDateKeyword), expr);
                    default:
                        return SyntaxFactory.CTypeExpression(expr, (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
                }
            }

            public override VisualBasicSyntaxNode VisitObjectCreationExpression(CSS.ObjectCreationExpressionSyntax node)
            {
                return SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    (TypeSyntax)node.Type.Accept(TriviaConvertingVisitor),
                    (ArgumentListSyntax)node.ArgumentList?.Accept(TriviaConvertingVisitor),
                    (ObjectCreationInitializerSyntax)node.Initializer?.Accept(TriviaConvertingVisitor)
                );
            }

            public override VisualBasicSyntaxNode VisitAnonymousObjectCreationExpression(CSS.AnonymousObjectCreationExpressionSyntax node)
            {
                return SyntaxFactory.AnonymousObjectCreationExpression(
                    SyntaxFactory.ObjectMemberInitializer(SyntaxFactory.SeparatedList(
                        node.Initializers.Select(i => (FieldInitializerSyntax)i.Accept(TriviaConvertingVisitor))
                    ))
                );
            }

            public override VisualBasicSyntaxNode VisitAnonymousObjectMemberDeclarator(CSS.AnonymousObjectMemberDeclaratorSyntax node)
            {
                if (node.NameEquals == null) {
                    return SyntaxFactory.InferredFieldInitializer((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
                } else {
                    return SyntaxFactory.NamedFieldInitializer(
                        SyntaxFactory.Token(SyntaxKind.KeyKeyword),
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        (IdentifierNameSyntax)node.NameEquals.Name.Accept(TriviaConvertingVisitor),
                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                        (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                    );
                }
            }

            public override VisualBasicSyntaxNode VisitArrayCreationExpression(CSS.ArrayCreationExpressionSyntax node)
            {
                var upperBoundArguments = node.Type.RankSpecifiers.First()?.Sizes.Where(s => !(s is CSS.OmittedArraySizeExpressionSyntax)).Select(
                    s => (ArgumentSyntax)SyntaxFactory.SimpleArgument(ReduceArrayUpperBoundExpression(s)));
                var rankSpecifiers = node.Type.RankSpecifiers.Select(rs => (ArrayRankSpecifierSyntax)rs.Accept(TriviaConvertingVisitor));

                return SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.Token(SyntaxKind.NewKeyword),
                    SyntaxFactory.List<AttributeListSyntax>(),
                    (TypeSyntax)node.Type.ElementType.Accept(TriviaConvertingVisitor),
                    upperBoundArguments.Any() ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(upperBoundArguments)) : null,
                    upperBoundArguments.Any() ? SyntaxFactory.List(rankSpecifiers.Skip(1)) : SyntaxFactory.List(rankSpecifiers),
                    (CollectionInitializerSyntax)node.Initializer?.Accept(TriviaConvertingVisitor) ?? SyntaxFactory.CollectionInitializer()
                );
            }

            public override VisualBasicSyntaxNode VisitImplicitArrayCreationExpression(CSS.ImplicitArrayCreationExpressionSyntax node)
            {
                return SyntaxFactory.CollectionInitializer(
                    SyntaxFactory.SeparatedList(node.Initializer.Expressions.Select(e => (ExpressionSyntax)e.Accept(TriviaConvertingVisitor)))
                );
            }

            ExpressionSyntax ReduceArrayUpperBoundExpression(CSS.ExpressionSyntax expr)
            {
                var constant = _semanticModel.GetConstantValue(expr);
                if (constant.HasValue && constant.Value is int)
                    return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal((int)constant.Value - 1));

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    (ExpressionSyntax)expr.Accept(TriviaConvertingVisitor), SyntaxFactory.Token(SyntaxKind.MinusToken), SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal(1)));
            }

            public override VisualBasicSyntaxNode VisitInitializerExpression(CSS.InitializerExpressionSyntax node)
            {
                if (node.IsKind(CS.SyntaxKind.ObjectInitializerExpression)) {
                    var expressions = node.Expressions.Select(e => e.Accept(TriviaConvertingVisitor));
                    if (expressions.OfType<FieldInitializerSyntax>().Any()) {
                        return SyntaxFactory.ObjectMemberInitializer(
                           SyntaxFactory.SeparatedList(expressions.OfType<FieldInitializerSyntax>())
                       );
                    }

                    return SyntaxFactory.ObjectCollectionInitializer(
                        SyntaxFactory.CollectionInitializer(
                            SyntaxFactory.SeparatedList(expressions.OfType<ExpressionSyntax>())
                        )
                    );
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

            public override VisualBasicSyntaxNode VisitAnonymousMethodExpression(CSS.AnonymousMethodExpressionSyntax node)
            {
                var parameterListParameters = node.ParameterList?.Parameters ?? Enumerable.Empty<CSS.ParameterSyntax>();// May have no parameter list
                return ConvertLambdaExpression(node, node.Block.Statements, parameterListParameters, SyntaxFactory.TokenList(node.AsyncKeyword));
            }

            public override VisualBasicSyntaxNode VisitSimpleLambdaExpression(CSS.SimpleLambdaExpressionSyntax node)
            {
                return ConvertLambdaExpression(node, node.Body, new[] {node.Parameter}, SyntaxFactory.TokenList(node.AsyncKeyword));
            }

            public override VisualBasicSyntaxNode VisitParenthesizedLambdaExpression(CSS.ParenthesizedLambdaExpressionSyntax node)
            {
                return ConvertLambdaExpression(node, node.Body, node.ParameterList.Parameters, SyntaxFactory.TokenList(node.AsyncKeyword));
            }

            LambdaExpressionSyntax ConvertLambdaExpression(CSS.AnonymousFunctionExpressionSyntax node, object block, IEnumerable<CSS.ParameterSyntax> parameters, SyntaxTokenList modifiers)
            {
                var symbol = ModelExtensions.GetSymbolInfo(_semanticModel, node).Symbol as IMethodSymbol;
                var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(p => (ParameterSyntax)p.Accept(TriviaConvertingVisitor))));
                LambdaHeaderSyntax header;
                if (symbol.ReturnsVoid)
                    header = SyntaxFactory.SubLambdaHeader(SyntaxFactory.List<AttributeListSyntax>(), CSharpConverter.ConvertModifiers(modifiers, TokenContext.Local), parameterList, null);
                else
                    header = SyntaxFactory.FunctionLambdaHeader(SyntaxFactory.List<AttributeListSyntax>(), CSharpConverter.ConvertModifiers(modifiers, TokenContext.Local), parameterList, null);
                if (block is CSS.BlockSyntax)
                    block = ((CSS.BlockSyntax)block).Statements;
                if (block is CS.CSharpSyntaxNode) {
                    var syntaxKind = symbol.ReturnsVoid ? SyntaxKind.SingleLineSubLambdaExpression : SyntaxKind.SingleLineFunctionLambdaExpression;
                    return SyntaxFactory.SingleLineLambdaExpression(syntaxKind, header, ((CS.CSharpSyntaxNode)block).Accept(TriviaConvertingVisitor));
                }
                if (!(block is SyntaxList<CSS.StatementSyntax>))
                    throw new NotSupportedException();
                var statements = SyntaxFactory.List(((SyntaxList<CSS.StatementSyntax>)block).SelectMany(s => s.Accept(CreateMethodBodyVisitor())));

                if (statements.Count == 1 && UnpackExpressionFromStatement(statements[0], out ExpressionSyntax expression)) {
                    var syntaxKind = symbol.ReturnsVoid ? SyntaxKind.SingleLineSubLambdaExpression : SyntaxKind.SingleLineFunctionLambdaExpression;
                    return SyntaxFactory.SingleLineLambdaExpression(syntaxKind, header, expression);
                }

                EndBlockStatementSyntax endBlock;
                SyntaxKind expressionKind;
                if (symbol.ReturnsVoid) {
                    endBlock = SyntaxFactory.EndBlockStatement(SyntaxKind.EndSubStatement, SyntaxFactory.Token(SyntaxKind.SubKeyword));
                    expressionKind = SyntaxKind.MultiLineSubLambdaExpression;
                } else {
                    endBlock = SyntaxFactory.EndBlockStatement(SyntaxKind.EndFunctionStatement, SyntaxFactory.Token(SyntaxKind.FunctionKeyword));
                    expressionKind = SyntaxKind.MultiLineFunctionLambdaExpression;
                }
                return SyntaxFactory.MultiLineLambdaExpression(expressionKind, header, statements, endBlock);
            }

            public override VisualBasicSyntaxNode VisitAwaitExpression(CSS.AwaitExpressionSyntax node)
            {
                return SyntaxFactory.AwaitExpression((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
            }

            bool UnpackExpressionFromStatement(StatementSyntax statementSyntax, out ExpressionSyntax expression)
            {
                if (statementSyntax is ReturnStatementSyntax)
                    expression = ((ReturnStatementSyntax)statementSyntax).Expression;
                else if (statementSyntax is YieldStatementSyntax)
                    expression = ((YieldStatementSyntax)statementSyntax).Expression;
                else
                    expression = null;
                return expression != null;
            }

            public override VisualBasicSyntaxNode VisitQueryExpression(CSS.QueryExpressionSyntax node)
            {
                return SyntaxFactory.QueryExpression(
                    SyntaxFactory.SingletonList((QueryClauseSyntax)node.FromClause.Accept(TriviaConvertingVisitor))
                    .AddRange(node.Body.Clauses.Select(c => (QueryClauseSyntax)c.Accept(TriviaConvertingVisitor)))
                    .AddRange(ConvertQueryBody(node.Body))
                );
            }

            public override VisualBasicSyntaxNode VisitFromClause(CSS.FromClauseSyntax node)
            {
                return SyntaxFactory.FromClause(
                    SyntaxFactory.CollectionRangeVariable(SyntaxFactory.ModifiedIdentifier(CSharpConverter.ConvertIdentifier(node.Identifier)),
                    (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor))
                );
            }

            public override VisualBasicSyntaxNode VisitWhereClause(CSS.WhereClauseSyntax node)
            {
                return SyntaxFactory.WhereClause((ExpressionSyntax)node.Condition.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitSelectClause(CSS.SelectClauseSyntax node)
            {
                return SyntaxFactory.SelectClause(
                    SyntaxFactory.ExpressionRangeVariable((ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor))
                );
            }

            IEnumerable<QueryClauseSyntax> ConvertQueryBody(CSS.QueryBodySyntax body)
            {
                if (body.SelectOrGroup is CSS.GroupClauseSyntax && body.Continuation == null)
                    throw new NotSupportedException("group by clause without into not supported in VB");
                if (body.SelectOrGroup is CSS.SelectClauseSyntax) {
                    yield return (QueryClauseSyntax)body.SelectOrGroup.Accept(TriviaConvertingVisitor);
                } else {
                    var group = (CSS.GroupClauseSyntax)body.SelectOrGroup;
                    yield return SyntaxFactory.GroupByClause(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ExpressionRangeVariable((ExpressionSyntax)group.GroupExpression.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ExpressionRangeVariable(SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(GeneratePlaceholder("groupByKey"))), (ExpressionSyntax)group.ByExpression.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AggregationRangeVariable(SyntaxFactory.FunctionAggregation(CSharpConverter.ConvertIdentifier(body.Continuation.Identifier))))
                    );
                    if (body.Continuation.Body != null) {
                        foreach (var clause in ConvertQueryBody(body.Continuation.Body))
                            yield return clause;
                    }
                }
            }

            public override VisualBasicSyntaxNode VisitLetClause(CSS.LetClauseSyntax node)
            {
                return SyntaxFactory.LetClause(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.ExpressionRangeVariable(
                            SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(CSharpConverter.ConvertIdentifier(node.Identifier))),
                            (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor)
                        )
                    )
                );
            }

            public override VisualBasicSyntaxNode VisitJoinClause(CSS.JoinClauseSyntax node)
            {
                if (node.Into != null) {
                    return SyntaxFactory.GroupJoinClause(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.CollectionRangeVariable(SyntaxFactory.ModifiedIdentifier(CSharpConverter.ConvertIdentifier(node.Identifier)), node.Type == null ? null : SyntaxFactory.SimpleAsClause((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)), (ExpressionSyntax)node.InExpression.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.JoinCondition((ExpressionSyntax)node.LeftExpression.Accept(TriviaConvertingVisitor), (ExpressionSyntax)node.RightExpression.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AggregationRangeVariable(SyntaxFactory.VariableNameEquals(SyntaxFactory.ModifiedIdentifier(CSharpConverter.ConvertIdentifier(node.Into.Identifier))), SyntaxFactory.GroupAggregation()))
                    );
                } else {
                    return SyntaxFactory.SimpleJoinClause(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.CollectionRangeVariable(SyntaxFactory.ModifiedIdentifier(CSharpConverter.ConvertIdentifier(node.Identifier)), node.Type == null ? null : SyntaxFactory.SimpleAsClause((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor)), (ExpressionSyntax)node.InExpression.Accept(TriviaConvertingVisitor))),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.JoinCondition((ExpressionSyntax)node.LeftExpression.Accept(TriviaConvertingVisitor), (ExpressionSyntax)node.RightExpression.Accept(TriviaConvertingVisitor)))
                    );
                }
            }

            public override VisualBasicSyntaxNode VisitOrderByClause(CSS.OrderByClauseSyntax node)
            {
                return SyntaxFactory.OrderByClause(
                    SyntaxFactory.SeparatedList(node.Orderings.Select(o => (OrderingSyntax)o.Accept(TriviaConvertingVisitor)))
                );
            }

            public override VisualBasicSyntaxNode VisitOrdering(CSS.OrderingSyntax node)
            {
                if (node.IsKind(CS.SyntaxKind.DescendingOrdering)) {
                    return SyntaxFactory.Ordering(SyntaxKind.DescendingOrdering, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
                } else {
                    return SyntaxFactory.Ordering(SyntaxKind.AscendingOrdering, (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor));
                }
            }

            public override VisualBasicSyntaxNode VisitArgumentList(CSS.ArgumentListSyntax node)
            {
                return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitBracketedArgumentList(CSS.BracketedArgumentListSyntax node)
            {
                return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (ArgumentSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitArgument(CSS.ArgumentSyntax node)
            {
                NameColonEqualsSyntax name = null;
                if (node.NameColon != null) {
                    name = SyntaxFactory.NameColonEquals((IdentifierNameSyntax)node.NameColon.Name.Accept(TriviaConvertingVisitor));
                }
                var value = (ExpressionSyntax)node.Expression.Accept(TriviaConvertingVisitor);
                return SyntaxFactory.SimpleArgument(name, value);
            }

            public override VisualBasicSyntaxNode VisitThrowExpression(CSS.ThrowExpressionSyntax node)
            {
                var convertedExceptionExpression = (ExpressionSyntax) node.Expression.Accept(TriviaConvertingVisitor);
                if (IsReturnValueDiscarded(node)) return SyntaxFactory.ThrowStatement(convertedExceptionExpression);

                _cSharpHelperMethodDefinition.AddThrowMethod = true;
                var typeName = SyntaxFactory.ParseTypeName(_semanticModel.GetTypeInfo(node.Parent).ConvertedType?.GetFullMetadataName() ?? "Object");
                var throwEx = SyntaxFactory.GenericName(CSharpHelperMethodDefinition.QualifiedThrowMethodName, SyntaxFactory.TypeArgumentList(typeName));
                var argList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.SimpleArgument(convertedExceptionExpression)));
                return SyntaxFactory.InvocationExpression(throwEx, argList);
            }

            #endregion

            #region Types / Modifiers

            public override VisualBasicSyntaxNode VisitArrayType(CSS.ArrayTypeSyntax node)
            {
                return SyntaxFactory.ArrayType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor),
                    SyntaxFactory.List(node.RankSpecifiers.Select(rs => (ArrayRankSpecifierSyntax)rs.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitArrayRankSpecifier(CSS.ArrayRankSpecifierSyntax node)
            {
                return SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    SyntaxFactory.TokenList(Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), node.Rank - 1)),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken));
            }

            public override VisualBasicSyntaxNode VisitTypeParameterList(CSS.TypeParameterListSyntax node)
            {
                return SyntaxFactory.TypeParameterList(node.Parameters.Select(p => (TypeParameterSyntax)p.Accept(TriviaConvertingVisitor)).ToArray());
            }

            public override VisualBasicSyntaxNode VisitTypeParameter(CSS.TypeParameterSyntax node)
            {
                SyntaxToken variance = default(SyntaxToken);
                if (!SyntaxTokenExtensions.IsKind(node.VarianceKeyword, CS.SyntaxKind.None)) {
                    variance = SyntaxFactory.Token(SyntaxTokenExtensions.IsKind(node.VarianceKeyword, CS.SyntaxKind.InKeyword) ? SyntaxKind.InKeyword : SyntaxKind.OutKeyword);
                }
                // copy generic constraints
                var clause = FindClauseForParameter(node);
                return SyntaxFactory.TypeParameter(variance, CSharpConverter.ConvertIdentifier(node.Identifier), (TypeParameterConstraintClauseSyntax)clause?.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitTypeParameterConstraintClause(CSS.TypeParameterConstraintClauseSyntax node)
            {
                if (node.Constraints.Count == 1)
                    return SyntaxFactory.TypeParameterSingleConstraintClause((ConstraintSyntax)node.Constraints[0].Accept(TriviaConvertingVisitor));
                return SyntaxFactory.TypeParameterMultipleConstraintClause(SyntaxFactory.SeparatedList(node.Constraints.Select(c => (ConstraintSyntax)c.Accept(TriviaConvertingVisitor))));
            }

            public override VisualBasicSyntaxNode VisitClassOrStructConstraint(CSS.ClassOrStructConstraintSyntax node)
            {
                if (node.IsKind(CS.SyntaxKind.ClassConstraint))
                    return SyntaxFactory.ClassConstraint(SyntaxFactory.Token(SyntaxKind.ClassKeyword));
                if (node.IsKind(CS.SyntaxKind.StructConstraint))
                    return SyntaxFactory.StructureConstraint(SyntaxFactory.Token(SyntaxKind.StructureKeyword));
                throw new NotSupportedException();
            }

            public override VisualBasicSyntaxNode VisitTypeConstraint(CSS.TypeConstraintSyntax node)
            {
                return SyntaxFactory.TypeConstraint((TypeSyntax)node.Type.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitConstructorConstraint(CSS.ConstructorConstraintSyntax node)
            {
                return SyntaxFactory.NewConstraint(SyntaxFactory.Token(SyntaxKind.NewKeyword));
            }

            private CSS.TypeParameterConstraintClauseSyntax FindClauseForParameter(CSS.TypeParameterSyntax node)
            {
                SyntaxList<CSS.TypeParameterConstraintClauseSyntax> clauses;
                var parentBlock = node.Parent.Parent;
                clauses = parentBlock.TypeSwitch(
                    (CSS.MethodDeclarationSyntax m) => m.ConstraintClauses,
                    (CSS.ClassDeclarationSyntax c) => c.ConstraintClauses,
                    (CSS.DelegateDeclarationSyntax d) => d.ConstraintClauses,
                    _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
                );
                return clauses.FirstOrDefault(c => c.Name.ToString() == node.ToString());
            }

            private void ConvertBaseList(CSS.BaseTypeDeclarationSyntax type, List<InheritsStatementSyntax> inherits, List<ImplementsStatementSyntax> implements)
            {
                TypeSyntax[] arr;
                switch (type.Kind()) {
                    case CS.SyntaxKind.ClassDeclaration:
                        var classOrInterface = type.BaseList?.Types.FirstOrDefault()?.Type;
                        if (classOrInterface == null) return;
                        var classOrInterfaceSymbol = ModelExtensions.GetSymbolInfo(_semanticModel, classOrInterface).Symbol;
                        if (classOrInterfaceSymbol?.IsInterfaceType() == true) {
                            arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(TriviaConvertingVisitor)).ToArray();
                            if (arr.Length > 0)
                                implements.Add(SyntaxFactory.ImplementsStatement(arr));
                        } else {
                            inherits.Add(SyntaxFactory.InheritsStatement((TypeSyntax)classOrInterface.Accept(TriviaConvertingVisitor)));
                            arr = type.BaseList?.Types.Skip(1).Select(t => (TypeSyntax)t.Type.Accept(TriviaConvertingVisitor)).ToArray();
                            if (arr.Length > 0)
                                implements.Add(SyntaxFactory.ImplementsStatement(arr));
                        }
                        break;
                    case CS.SyntaxKind.StructDeclaration:
                        arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(TriviaConvertingVisitor)).ToArray();
                        if (arr?.Length > 0)
                            implements.Add(SyntaxFactory.ImplementsStatement(arr));
                        break;
                    case CS.SyntaxKind.InterfaceDeclaration:
                        arr = type.BaseList?.Types.Select(t => (TypeSyntax)t.Type.Accept(TriviaConvertingVisitor)).ToArray();
                        if (arr?.Length > 0)
                            inherits.Add(SyntaxFactory.InheritsStatement(arr));
                        break;
                }
            }

            public override VisualBasicSyntaxNode VisitPredefinedType(CSS.PredefinedTypeSyntax node)
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(CS.CSharpExtensions.Kind(node.Keyword).ConvertToken()));
            }

            public override VisualBasicSyntaxNode VisitNullableType(CSS.NullableTypeSyntax node)
            {
                return SyntaxFactory.NullableType((TypeSyntax)node.ElementType.Accept(TriviaConvertingVisitor));
            }

            public override VisualBasicSyntaxNode VisitOmittedTypeArgument(CSS.OmittedTypeArgumentSyntax node)
            {
                return SyntaxFactory.ParseTypeName("");
            }

            #endregion

            #region NameSyntax

            public override VisualBasicSyntaxNode VisitIdentifierName(CSS.IdentifierNameSyntax node)
            {
                return WrapTypedNameIfNecessary(SyntaxFactory.IdentifierName(CSharpConverter.ConvertIdentifier(node.Identifier)), node);
            }

            public override VisualBasicSyntaxNode VisitGenericName(CSS.GenericNameSyntax node)
            {
                return WrapTypedNameIfNecessary(SyntaxFactory.GenericName(CSharpConverter.ConvertIdentifier(node.Identifier), (TypeArgumentListSyntax)node.TypeArgumentList.Accept(TriviaConvertingVisitor)), node);
            }

            public override VisualBasicSyntaxNode VisitQualifiedName(CSS.QualifiedNameSyntax node)
            {
                return WrapTypedNameIfNecessary(SyntaxFactory.QualifiedName((NameSyntax)node.Left.Accept(TriviaConvertingVisitor), (SimpleNameSyntax)node.Right.Accept(TriviaConvertingVisitor)), node);
            }

            public override VisualBasicSyntaxNode VisitAliasQualifiedName(CSS.AliasQualifiedNameSyntax node)
            {
                return WrapTypedNameIfNecessary(SyntaxFactory.QualifiedName((NameSyntax)node.Alias.Accept(TriviaConvertingVisitor), (SimpleNameSyntax)node.Name.Accept(TriviaConvertingVisitor)), node);
            }

            public override VisualBasicSyntaxNode VisitTypeArgumentList(CSS.TypeArgumentListSyntax node)
            {
                return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(node.Arguments.Select(a => (TypeSyntax)a.Accept(TriviaConvertingVisitor))));
            }

            VisualBasicSyntaxNode WrapTypedNameIfNecessary(ExpressionSyntax name, CSS.ExpressionSyntax originalName)
            {
                if (originalName.Parent is CSS.NameSyntax
                    || originalName.Parent is CSS.AttributeSyntax
                    || originalName.Parent is CSS.MemberAccessExpressionSyntax
                    || originalName.Parent is CSS.MemberBindingExpressionSyntax
                    || originalName.Parent is CSS.InvocationExpressionSyntax
                    || _semanticModel.SyntaxTree != originalName.SyntaxTree)
                    return name;

                var symbolInfo = _semanticModel.GetSymbolInfo(originalName);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                if (symbol.IsKind(SymbolKind.Method)) {
                    var addressOf = SyntaxFactory.AddressOfExpression(name);

                    if (originalName.Parent is CSS.ArgumentSyntax nameArgument &&
                        nameArgument.Parent?.Parent is CSS.InvocationExpressionSyntax ies) {
                        var argIndex = ies.ArgumentList.Arguments.IndexOf(nameArgument);
                        //TODO: Deal with named parameters
                        var destinationType = _semanticModel.GetSymbolInfo(ies.Expression).CandidateSymbols
                            .Select(m => m.GetParameters()).Where(p => p.Length > argIndex).Select(p => p[argIndex].Type).FirstOrDefault();
                        if (destinationType != null) {
                            var toCreate = (TypeSyntax)
                                CS.SyntaxFactory
                                    .ParseTypeName(destinationType.ToMinimalDisplayString(_semanticModel,
                                        originalName.SpanStart))
                                    .Accept(TriviaConvertingVisitor);
                            return SyntaxFactory.ObjectCreationExpression(toCreate).WithArgumentList(ExpressionSyntaxExtensions.CreateArgList(addressOf));
                        }
                    }

                    return addressOf;
                }

                return name;
            }

            #endregion
        }
    }
}
