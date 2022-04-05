using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

public class MethodDeclarationParameters : DeclarationParameters
{
    public TypeParameterListSyntax TypeParameters { get; }
    public ParameterListSyntax ParameterList { get; }
    public SyntaxList<TypeParameterConstraintClauseSyntax> Constraints { get; }
    public ArrowExpressionClauseSyntax ArrowClause { get; }

    public MethodDeclarationParameters(SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers,
        TypeSyntax returnType, TypeParameterListSyntax typeParameters, ParameterListSyntax parameterList,
        SyntaxList<TypeParameterConstraintClauseSyntax> constraints,
        ArrowExpressionClauseSyntax arrowClause, SyntaxToken identifier)
        : base(attributes, modifiers, returnType, identifier)
    {
        TypeParameters = typeParameters;
        ParameterList = parameterList;
        Constraints = constraints;
        ArrowClause = arrowClause;
    }

    public MethodDeclarationParameters(SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers,
        TypeSyntax returnType, TypeParameterListSyntax typeParameters, ParameterListSyntax parameterList,
        SyntaxList<TypeParameterConstraintClauseSyntax> constraints,
        ArrowExpressionClauseSyntax arrowClause) : base(attributes, modifiers, returnType)
    {
        TypeParameters = typeParameters;
        ParameterList = parameterList;
        Constraints = constraints;
        ArrowClause = arrowClause;
    }
}