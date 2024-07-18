
Namespace CSharpRefReturn
    Public Class RefReturnList(Of T)
        Private dummy As T
                ''' Cannot convert IndexerDeclarationSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.<>c__DisplayClass53_0.<ConvertPropertyBlock>b__1()
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.ConvertPropertyBlock(BasePropertyDeclarationSyntax node, SyntaxToken id, SyntaxTokenList modifiers, ParameterListSyntax parameterListSyntax, ArrowExpressionClauseSyntax arrowExpressionClauseSyntax, EqualsValueSyntax initializerOrNull)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitIndexerDeclaration(IndexerDeclarationSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
'''         public ref T this[int i] {
'''             get {
'''                 return ref this.dummy;
'''             }
'''         }
''' 
''' 
    End Class
End Namespace
