﻿
Namespace CSharpRefReturn
    Public Class RefReturnList(Of T)
        Private dummy As T
                ''' Cannot convert IndexerDeclarationSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.<>c__DisplayClass53_0.<ConvertPropertyBlock>b__1() in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 543
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.ConvertPropertyBlock(BasePropertyDeclarationSyntax node, SyntaxToken id, SyntaxTokenList modifiers, ParameterListSyntax parameterListSyntax, ArrowExpressionClauseSyntax arrowExpressionClauseSyntax, EqualsValueSyntax initializerOrNull) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 576
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitIndexerDeclaration(IndexerDeclarationSyntax node) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 526
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
'''         public ref T this[int i] {
'''             get {
'''                 return ref this.dummy;
'''             }
'''         }
''' 
''' 
                ''' Cannot convert PropertyDeclarationSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.<>c__DisplayClass53_0.<ConvertPropertyBlock>b__1() in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 543
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.ConvertPropertyBlock(BasePropertyDeclarationSyntax node, SyntaxToken id, SyntaxTokenList modifiers, ParameterListSyntax parameterListSyntax, ArrowExpressionClauseSyntax arrowExpressionClauseSyntax, EqualsValueSyntax initializerOrNull) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 576
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitPropertyDeclaration(PropertyDeclarationSyntax node) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\NodesVisitor.cs:line 514
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in C:\_Work\thirdPartyTools\CodeConverter\CodeConverter\VB\CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
'''         public ref T RefProperty
'''         {
'''             get
'''             {
'''                 return ref this.dummy;
'''             }
'''         }
''' 
''' 
    End Class
End Namespace
