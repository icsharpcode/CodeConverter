using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

public class DeclarationParameters
{
    public SyntaxList<AttributeListSyntax> Attributes { get; }
    public SyntaxTokenList Modifiers { get; }
    public TypeSyntax ReturnType { get; }
    public SyntaxToken Identifier { get; }

    public DeclarationParameters(SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers,
        TypeSyntax returnType, SyntaxToken identifier) : this(attributes, modifiers, returnType)
    {
        Identifier = identifier;
    }

    public DeclarationParameters(SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers,
        TypeSyntax returnType)
    {
        Attributes = attributes;
        Modifiers = modifiers;
        ReturnType = returnType;
    }
}