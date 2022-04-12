using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

public class PropertyDeclarationParameters : DeclarationParameters
{
    public AccessorListSyntax Accessors { get; }

    public PropertyDeclarationParameters(SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers,
        TypeSyntax returnType, AccessorListSyntax accessors) : base(attributes, modifiers, returnType)
    {
        Accessors = accessors;
    }
}